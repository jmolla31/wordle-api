using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Wapi
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((duh, sp)=>
                {
                    var config = duh.Configuration;
                    sp.AddSingleton(new TableServiceClient(config["TableStorageConnection"]));
                })
                .Build();

            host.Run();
        }
    }
}