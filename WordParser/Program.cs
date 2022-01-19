// See https://aka.ms/new-console-template for more information

using Azure.Data.Tables;
using System.Text.Json;

namespace ConsoleApp1
{
    internal class Program
    {
        private static string TableConnection = "";

        static async Task Main(string[] args)
        {
            var wordsFile = await File.ReadAllTextAsync("words.json");
            var parsedWords = JsonSerializer.Deserialize<List<string>>(wordsFile);
            var grouped = parsedWords.GroupBy(x => x.Length).Where(x => x.Key >= 5 && x.Key <= 7).ToList();

            var tableClient = new TableServiceClient(TableConnection).GetTableClient("DailyEN");

            await tableClient.CreateIfNotExistsAsync();


            var random = new Random();


            foreach (var group in grouped)
            {
                var date = DateTime.Today;

                var total = group.Count();
                var counter = 1;

                var randomizedGroup = group.OrderBy(x => random.NextDouble()).ToList();

                foreach (var items in randomizedGroup.Chunk(100))
                {
                    Console.WriteLine($"Saving words group [{counter} of {total}]");

                    List<Word> batchWords = new List<Word>(100);

                    foreach (var word in items)
                    {
                        batchWords.Add(new Word
                        {
                            PartitionKey = group.Key.ToString(),
                            RowKey = date.ToString("yyyyMMdd"),
                            Text = word.ToString()
                        });

                        date = date.AddDays(1);
                    }

                    var batch = batchWords.Select(x => new TableTransactionAction(TableTransactionActionType.UpsertReplace, x));
                    await tableClient.SubmitTransactionAsync(batch);

                    counter += items.Count();
                }
            }
        }
    }
}
