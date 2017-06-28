using System;
using Microsoft.Azure.Devices.Gateway;
using OSIsoft.Data.Edge.Storage;
using OSIsoft.Data.Edge.Server.Helpers;
using OSIsoft.Data.Edge.Server.Contracts.DataServer;
using OSIsoft.Data.Edge.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Net.Http;
using OSIsoft.Data.Reflection;
using Newtonsoft.Json;

namespace QiEdgeModule
{
    public class DotNetQiEdgeModule : IGatewayModule, IGatewayModuleStart
    {
        private string configuration;
        private DataServerBootstrapper dataServerBootstrapper;
        public void Create(Broker broker, byte[] configuration)
        {
            this.configuration = System.Text.Encoding.UTF8.GetString(configuration);
            Console.WriteLine("I have been created from QiEdge!");


            var defaultlocalStorageLocation = Environment.OSVersion.Platform == PlatformID.Unix
                ? EdgeConstants.LinuxRootInstallFolder
                : EdgeConstants.WindowsRootInstallFolder;

            ////if (args.Length > 0)
            ////{
            ////    if (args[0] == $"--{Constants.HelpFlag}")
            ////    {
            ////        Console.WriteLine();
            ////        Console.WriteLine("Optional commandline arguments shown below with their default values.");
            ////        Console.WriteLine();
            ////        Console.WriteLine("#####################");
            ////        Console.WriteLine();
            ////        Console.WriteLine(
            ////            $"--{Constants.DataServerPortKey} {Constants.DataServerDefaultPort} --{Constants.DataServerLocalStorageLocationKey} {defaultlocalStorageLocation}");
            ////        Console.WriteLine();
            ////        Console.WriteLine("#####################");
            ////        return;
            ////    }
            ////}

            var dataServerStartupArgs = new StartupArguments();
            var dataServerStoreConfig = new LocalStoreConfiguration
            {
                StreamStorageLimitMb = -1,
            };

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                ////.AddCommandLine(args)
                .AddJsonFile("hosting.json", optional: true)
                .Build();

            ////if (config.GetValue<bool>(Constants.HelpFlag))
            ////{
            ////    Console.WriteLine("Optional commandline arguments shown below with their default values.");
            ////    Console.WriteLine($"--{Constants.DataServerPortKey} {Constants.DataServerDefaultPort} --{Constants.DataServerLocalStorageLocationKey} {defaultlocalStorageLocation}");
            ////    return;
            ////}

            ////var dataServerPort = config.GetValue<int?>(Constants.DataServerPortKey) ?? Convert.ToInt32(Constants.DataServerDefaultPort);
            var dataServerPort = Convert.ToInt32(Constants.DataServerDefaultPort);
            ////var localStorageLocation = config.GetValue<string>(Constants.DataServerLocalStorageLocationKey) ?? defaultlocalStorageLocation;
            var localStorageLocation = defaultlocalStorageLocation;
            dataServerStartupArgs.ListenerPort = dataServerPort.ToString();
            dataServerStartupArgs.LocalStorageLocation = localStorageLocation;
            dataServerBootstrapper = new DataServerBootstrapper(dataServerStartupArgs, dataServerStoreConfig);

            var administrationHost = new WebHostBuilder()
                .CaptureStartupErrors(true)
                .UseKestrel()
                .ConfigureServices(
                    services => services.AddSingleton(dataServerBootstrapper))
                .UseConfiguration(config)
                .UseIISIntegration()
                .UseKestrel()
                .UseStartup<StartupAdminServer>()
                .Build();

            Task.Run(() => administrationHost.Run());

            Task.Delay(6000).Wait();

            dataServerBootstrapper.InternalAdminService.GetOrCreateTenantAsync(new OSIsoft.Data.QiTenant("msftiot")).Wait();
            dataServerBootstrapper.GetQiAdministrationService("msftiot").GetOrCreateNamespaceAsync(new OSIsoft.Data.QiNamespace("firstns")).Wait();
            var iotType = dataServerBootstrapper.GetQiMetadataService("msftiot", "firstns").GetOrCreateTypeAsync(QiTypeBuilder.CreateQiType<MyDataType>()).Result;
            dataServerBootstrapper.GetQiMetadataService("msftiot", "firstns").GetOrCreateStreamAsync(new OSIsoft.Data.QiStream()
            {
                Id = "somestream",
                TypeId = iotType.Id,
            });
        }

        public void Start()
        {
            Console.WriteLine("We are saying HELLO WORLD from QiEdge!!!!!");
        }

        public void Destroy()
        {
            Console.WriteLine("I have been destroyed from QiEdge!");
        }

        public void Receive(Message received_message)
        {
            if (received_message != null)
            {
                string messageData = System.Text.Encoding.UTF8.GetString(received_message.Content, 0, received_message.Content.Length);
                Console.WriteLine("{0}> QiEdge module received message: {1}", DateTime.Now.ToLocalTime(), messageData);
 
                int propCount = 0;
                foreach (var prop in received_message.Properties)
                {
                    Console.WriteLine("\tProperty[{0}]> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                    var item = JsonConvert.DeserializeObject(prop.Value);
                    dataServerBootstrapper.GetQiDataService("msftiot", "firstns").UpdateValueAsync("somestream", item);
                }

                var start = DateTime.Today.AddDays(-2).ToString("o");
                var end = DateTime.Today.AddDays(2).ToString("o");
                var events = dataServerBootstrapper.GetQiDataService("msftiot", "firstns").GetWindowValuesAsync<MyDataType>("somestream", start, end).Result;
                var total = 0;
                foreach (var entry in events)
                {
                    total++;
                }

                Console.WriteLine($"Total events written so far: {total}");
            }
        }
    }
}
