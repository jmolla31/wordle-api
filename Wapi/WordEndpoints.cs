using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Wapi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Wapi
{
    public class WordEndpoints
    {
        /// <summary>
        ///     Max number of words each size list contains. 
        ///     (this is ugly AF, I know, but I have to work around the limitations of table storage, $$$)
        ///     If you roll out your own version, these values have to be updated to match the data source.
        /// </summary>
        private static readonly Dictionary<int, int> _maxWords = new()
        {
            { 5, 8885 },
            { 6, 15720 },
            { 7, 23950 },
        };

        private static readonly Random _rnd = new();

        private readonly ILogger _logger;

        //Source for the "random" endpoint.
        private readonly TableClient _wordsClient;
        
        //Source for the "check" endpoint.
        private readonly TableClient _lookupClient;
        
        //Source for the "daily" endpoint.
        private readonly TableClient _dailyClient;

        public WordEndpoints(ILoggerFactory loggerFactory, TableServiceClient tableServiceClient)
        {
            _logger = loggerFactory.CreateLogger<WordEndpoints>();
            _wordsClient = tableServiceClient.GetTableClient("RandomEN");
            _lookupClient = tableServiceClient.GetTableClient("LookupEN");
            _dailyClient = tableServiceClient.GetTableClient("DailyEN");
        }

        /// <summary>
        ///     Get a random word of the desired lenght.
        /// </summary>
        /// <param name="req">HttpRequest</param>
        /// <param name="size">Desired word size, supported values are 5, 6 or 7. Defaults to 5 if not specified</param>
        /// <returns>Word string</returns>
        [Function("random")]
        public async Task<HttpResponseData> Random([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, string size)
        {
            var (success, errorCode) = CheckAndNormalizeRequestedSize(size, out var normalizedSize);
            if (!success)
            {
                return await CreateStringResponse(req, HttpStatusCode.BadRequest, errorCode);
            }

            var numberKey = GetRandomInt(normalizedSize);
            var word = await GetWord(normalizedSize, numberKey);

            return await CreateStringResponse(req, HttpStatusCode.OK, word);
        }

        /// <summary>
        ///     Get today's word for the desired lenght. 
        ///     The same word will be returned during a given day no matter how many times the endpoint is called.
        /// </summary>
        /// <param name="req">HttpRequest</param>
        /// <param name="size">Desired word size, supported values are 5, 6 or 7. Defaults to 5 if not specified</param>
        /// <returns>Word string</returns>
        [Function("daily")]
        public async Task<HttpResponseData> Daily([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, string size)
        {
            var (success, errorCode) = CheckAndNormalizeRequestedSize(size, out var normalizedSize);
            if (!success)
            {
                return await CreateStringResponse(req, HttpStatusCode.BadRequest, errorCode);
            }

            var dayKey = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
            var word = await GetDailyWord(normalizedSize, dayKey);

            return await CreateStringResponse(req, HttpStatusCode.OK, word);
        }

        /// <summary>
        ///     Check if a word is valid (exist in the source dictionary).
        /// </summary>
        /// <param name="req">HttpRequest</param>
        /// <param name="input">Input word</param>
        /// <returns>'OK' or 'not_found'</returns>
        [Function("check")]
        public async Task<HttpResponseData> Check([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return await CreateStringResponse(req, HttpStatusCode.BadRequest, "word_input_null");
            }

            var word = input.Trim().ToLowerInvariant();
            var wordSize = word.Length;

            if (wordSize < 5 || wordSize > 7)
            {
                return await CreateStringResponse(req, HttpStatusCode.BadRequest, "word_input_invalid_size");
            }

            return await CheckWord(wordSize, word)
                ? await CreateStringResponse(req, HttpStatusCode.OK, "OK")
                : await CreateStringResponse(req, HttpStatusCode.OK, "not_found");
        }

        #region private helpers

        private static (bool success, string errorCode) CheckAndNormalizeRequestedSize(string input, out int normalizedValue)
        {
            normalizedValue = 5;
            if (string.IsNullOrEmpty(input)) return (true, null);

            if (int.TryParse(input, out normalizedValue))
            {
                return normalizedValue < 5 || normalizedValue > 7
                    ? (false, "size_parameter_out_of_range")
                    : (true, null);
            }
            else
            {
                return (false, "size_parameter_invalid_value");
            }
        }

        private async Task<bool> CheckWord(int size, string word)
        {
            try
            {
                var exists = await _lookupClient.GetEntityAsync<Word>(size.ToString(), word);
                return exists?.Value != null;
            }
            catch (Azure.RequestFailedException)
            {
                //Table storage throws an exception for not found entities :(
                return false;
            }
        }

        private async Task<string> GetWord(int size, int numberKey)
        {
            var word = await _wordsClient.GetEntityAsync<Word>(size.ToString(), numberKey.ToString());
            return word?.Value?.Text;
        }

        private async Task<string> GetDailyWord(int size, string dayKey)
        {
            var word = await _dailyClient.GetEntityAsync<Word>(size.ToString(), dayKey);
            return word?.Value?.Text;
        }

        private static int GetRandomInt(int size)
            => _rnd.Next(1, _maxWords[size]);

        private static async Task<HttpResponseData> CreateStringResponse(HttpRequestData req, HttpStatusCode statusCode, string content)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteStringAsync(content, System.Text.Encoding.UTF8);
            return response;
        }

        #endregion
    }
}
