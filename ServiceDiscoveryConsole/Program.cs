using System;

using Pook.Net.ServiceDiscovery;

namespace ServiceDiscoveryConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			var monitor = new ServiceMonitor();
			monitor.ServiceUp += b => Console.WriteLine($"ServiceUP:   {b.ID} {b.MachineName} {b.Version}\n             {b.ServiceUri}");
			monitor.ServiceDown += b => Console.WriteLine($"ServiceDOWN: {b.ID} {b.MachineName} {b.Version}\n             {b.ServiceUri}");
			monitor.Start();

			string id = Guid.NewGuid().ToString("N").Substring(0, 4);
			monitor.RegisterService(id, "MyService", $"http://{Environment.MachineName}/{id}/Myservice");

			Console.WriteLine("this ID=" + id);
			Console.WriteLine("Press enter to exit:");
			Console.WriteLine();
			Console.ReadLine();
		}
	}
}
