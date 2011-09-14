using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;
using EventStore.Dispatcher;
using EventStore.Persistence;
using EventStore.Serialization;
using Magnum.Reflection;
using MassTransit;

namespace Logibit.Rebuild
{
	internal class Program
	{
		private IServiceBus _bus;

		private static void Main(string[] args)
		{
			var p = new Program();

			if (args.Length < 2) {
				Help();
				return;
			}

			try
			{
				Console.WriteLine("{0} events published. Quitting.", p.Run(args));
			}
			catch (StorageUnavailableException e)
			{
				Console.WriteLine("Could not connect to storage: {0}", e);
			}
			catch (StorageException e)
			{
				Console.WriteLine(e);
			}

			p.Stop();
		}

		private int Run(IList<string> args)
		{
			var endpointUri = "rabbitmq://localhost/" + args[0];
			var connectionName = args[1];
			var aggregate = args[2];

			IEndpoint endpoint;
			if ((endpoint = BuildBus(endpointUri)) == null)
			{
				Console.WriteLine("Could not find endpoint named {0}", endpointUri);
				return 0;
			}

			var events = Wireup.Init()
				.UsingRavenPersistence(connectionName, new DocumentObjectSerializer())
				.InitializeStorageEngine()
				.UsingSynchronousDispatcher(new NullDispatcher())
				.Build()
				.GetFrom(DateTime.MinValue)
				.Where(c => c.Headers["AggregateType"].Equals(aggregate))
				.SelectMany(commit => commit.Events)
				.ToList();
			
			events.ForEach(e => this.FastInvoke("Send", endpoint, e.Body));

			return events.Count;
		}

		private void Send<T>(IEndpoint endpoint, T message)
			where T : class
		{
			endpoint.Send(message);
		}

		private IEndpoint BuildBus(string endpointUri)
		{
			_bus = ServiceBusFactory.New(c =>
				{
					c.ReceiveFrom("rabbitmq://localhost/es-rebuild");
					c.UseRabbitMqRouting();
				});

			return _bus.GetEndpoint(new Uri(endpointUri));
		}

		private void Stop()
		{
			if (_bus != null) 
				_bus.Dispose();
		}

		private static void Help()
		{
			Console.WriteLine(
				@"
es-rebuild <target-exchange> <connection-string> <ar-type> [ar-id]

Examples:
	es-rebuild Company.Listener raven       Company.Domain.Division
			   ^ Exchange       ^ Source    ^ AggregateType
");
		}
	}
}