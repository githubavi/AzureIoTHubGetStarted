using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CreateDeviceIdentity
{
    class Program
    {
        static RegistryManager registryManager;
        static string connectionString = "HostName=iothubavi.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=leQmYr0PLYzJOyS6WQTlhshSCVK1qH99DAXMsClQvBY=";
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;
        static string deviceKey;
        static DeviceClient deviceClient;
        static string iotHubUri = "iothubavi.azure-devices.net";

        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddDeviceAsync().Wait();
            Console.ReadLine();

            Console.WriteLine("Receive messages. Ctrl-C to exit.\n");

            //connectionString = "HostName=iothubavi.azure-devices.net;DeviceId=deviceavi;SharedAccessKey=GrDhkkcm8ZE51l1m0V53NYrHEFquL4Uuf+6TwAU+f6o=";
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
                System.Threading.Thread.Sleep(3000);
                return;
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromHubAsync(partition, cts.Token));
            }

            Console.WriteLine("Simulated device\n");
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            SendDeviceToCloudMessagesAsync(cts.Token);

            Task.WaitAll(tasks.ToArray());

            Console.ReadLine();

        }
        static string deviceId = "deviceavi";
        private static async Task AddDeviceAsync()
        {
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            deviceKey = device.Authentication.SymmetricKey.PrimaryKey;
            Console.WriteLine("Generated device key: {0}", deviceKey);
        }

        private static async Task ReceiveMessagesFromHubAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);
            }
        }

        private static async void SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            double avgWindSpeed = 10; // m/s
            Random rand = new Random();
            Random rand2 = new Random();

            while (true)
            {
                if (ct.IsCancellationRequested) break;

                double currentWindSpeed = avgWindSpeed + rand.NextDouble() * 4 - 2;

                var telemetryDataPoint = new
                {
                    deviceId = deviceId,
                    windSpeed = currentWindSpeed,
                    partition = rand2.Next(0, 5),
                    runid = Guid.NewGuid()
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(messageString));

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
            }
        }
    }
}
