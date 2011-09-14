using System;
using System.IO;
using MassTransit;
using MyCompany.Domain.Events;
using Newtonsoft.Json;
using log4net.Config;

namespace EventStore.ExampleRead
{
	internal class Program : Consumes<ThingCreated>.All
	{
		private IServiceBus _bus;

		private static object _serializer = new object();
		private string _fileName = Path.Combine("tmp", "log.txt");

		private static void Main(string[] args)
		{
			BasicConfigurator.Configure();
			var p = new Program();
			p.Run();
		}

		private void Run()
		{
			if (!Directory.Exists("tmp"))
				Directory.CreateDirectory("tmp");

			var lines = new string[0];
			
			if (File.Exists(_fileName))
				lines = File.ReadAllLines(_fileName);

			Console.WriteLine("I got this:");
			
			foreach (var line in lines)
				Console.WriteLine(line);
			
			Console.WriteLine("That's all. Starting listener!");

			WaitForMessages();
			Console.WriteLine("Listener started! Press a key to exit.");
			Console.ReadKey(true);
			_bus.Dispose();
		}

		private void WaitForMessages()
		{
			_bus = ServiceBusFactory.New(conf =>
				{
					conf.ReceiveFrom("rabbitmq://localhost/Logibit.Example");
					conf.UseRabbitMqRouting();
					conf.Subscribe(x => x.Consumer(() => new Program()));
				});
		}

		public void Consume(ThingCreated message)
		{
			lock (_serializer)
			{
				Console.WriteLine("Consuming {0}", message);

				using (var a = File.AppendText(_fileName))
				{
					a.WriteLine(JsonConvert.SerializeObject(message));
					a.Flush();
					a.Close();
				}
			}
		}
	}
}