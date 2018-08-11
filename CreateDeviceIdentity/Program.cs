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
using Microsoft.Azure.Devices.Client.Samples;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace CreateDeviceIdentity
{
    class Program
    {
        static RegistryManager registryManager;
        static string deviceConnectionString = "HostName=iottestavi.azure-devices.net;SharedAccessKeyName=device;SharedAccessKey=MN+B2PfQmDezOl+9W+yCPB6P6J1t2gseXHzs6L4JcZ4=";
        
        static  string registryConnectionString = "HostName=iottestavi.azure-devices.net;SharedAccessKeyName=registryReadWrite;SharedAccessKey=havzBkGDPzhByLvpeEn3bP+/ATqCaTwK0X2JcbuKV6Y=";
        static string iotHubD2cEndpoint = "messages/events";
        static string deviceKey;
        static DeviceClient deviceClient;
        static string iotHubUri = "iottestavi.azure-devices.net";

        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(registryConnectionString);
            AddDeviceAsync().GetAwaiter().GetResult();
            Console.ReadLine();

            Console.WriteLine("Receive messages. Ctrl-C to exit.\n");

            //connectionString = "HostName=iothubavi.azure-devices.net;DeviceId=deviceavi;SharedAccessKey=GrDhkkcm8ZE51l1m0V53NYrHEFquL4Uuf+6TwAU+f6o=";
            deviceClient = DeviceClient.CreateFromConnectionString(
                //"HostName=iottestavi.azure-devices.net;DeviceId=deviceavi;SharedAccessKey=Ywt7XBLXN/NRbJc6nKWb65o6mHSWbr7Df+tH3HfTIgs=");
                $"HostName=iottestavi.azure-devices.net;DeviceId=deviceavi;SharedAccessKey={deviceKey}");

            if (deviceClient == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
                Console.ReadLine();
                return;
            }

            var sample = new MessageSample(deviceClient);
            sample.RunSampleAsync().GetAwaiter().GetResult();

            Console.WriteLine("Done.\n");
           
            Console.ReadLine();
        }
        static string deviceId = "deviceavi";
        private static async Task<string> AddDeviceAsync()
        {
            Device device;
            try
            {
                device = await registryManager.GetDeviceAsync(deviceId);
                if(device == null)
                    device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            deviceKey = device.Authentication.SymmetricKey.SecondaryKey;
            Console.WriteLine("Generated device key: {0}", deviceKey);
            return deviceKey;
        }

        //private static async Task ReceiveMessagesFromHubAsync(string partition, CancellationToken ct)
        //{
        //    var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            
        //    while (true)
        //    {
        //        if (ct.IsCancellationRequested) break;
        //        EventData eventData = await eventHubReceiver.ReceiveAsync();
        //        if (eventData == null) continue;

        //        string data = Encoding.UTF8.GetString(eventData.GetBytes());
        //        Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);
        //    }
        //}

        //private static async void SendDeviceToCloudMessagesAsync(CancellationToken ct)
        //{
        //    double avgWindSpeed = 10; // m/s
        //    Random rand = new Random();
        //    Random rand2 = new Random();

        //    while (true)
        //    {
        //        if (ct.IsCancellationRequested) break;

        //        double currentWindSpeed = avgWindSpeed + rand.NextDouble() * 4 - 2;

        //        var telemetryDataPoint = new
        //        {
        //            deviceId = deviceId,
        //            windSpeed = currentWindSpeed,
        //            partition = rand2.Next(0, 5),
        //            runid = Guid.NewGuid()
        //        };
        //        var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
        //        var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(messageString));

        //        await deviceClient.SendEventAsync(message);
        //        Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

        //        await Task.Delay(1000);
        //    }
        //}
    }
}
