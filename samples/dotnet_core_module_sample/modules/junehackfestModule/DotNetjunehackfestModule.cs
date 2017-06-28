using System;
using Microsoft.Azure.Devices.Gateway;

namespace junehackfestModule
{
    public class DotNetjunehackfestModule : IGatewayModule, IGatewayModuleStart
    {
        private string configuration;
        public void Create(Broker broker, byte[] configuration)
        {
            this.configuration = System.Text.Encoding.UTF8.GetString(configuration);
            Console.WriteLine("I have been created!");
        }

        public void Start()
        {
            Console.WriteLine("We are saying HELLO WORLD from JuneHackfest!!!!!");
        }

        public void Destroy()
        {
            Console.WriteLine("I have been destroyed!");
        }

        public void Receive(Message received_message)
        {
            if (received_message != null)
            {
                string messageData = System.Text.Encoding.UTF8.GetString(received_message.Content, 0, received_message.Content.Length);
                Console.WriteLine("{0}> JuneHackfest module received message: {1}", DateTime.Now.ToLocalTime(), messageData);
 
                int propCount = 0;
                foreach (var prop in received_message.Properties)
                {
                    Console.WriteLine("\tProperty[{0}]> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                }
            }
        }
    }
}
