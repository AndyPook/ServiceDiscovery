using Pook.Net.Serialization;
using Pook.Net.ServiceDiscovery;

namespace Pook.Net
{
	public static class SerializationExtensions
	{
		public static void WriteServiceBeacon(this IDataWriter writer, ServiceBeacon info)
		{
			writer.WriteShortText(info.MachineName);
			writer.WriteShortText(info.ID);
			writer.WriteShortText(info.ServiceName);
			writer.WriteShortText(info.Version);
			writer.WriteShortText(info.ServiceUri);
			writer.WriteShortText(info.ServiceData);
		}

		public static ServiceBeacon ReadServiceBeacon(this IDataReader reader)
		{
			var beacon = new ServiceBeacon { MachineName = reader.ReadShortText(), ID = reader.ReadShortText(), ServiceName = reader.ReadShortText(), Version = reader.ReadShortText(), ServiceUri = reader.ReadShortText(), ServiceData = reader.ReadShortText() };
			return beacon;
		}

		public static void SendHello(this INetSender sender)
		{
			var writer = new BufferDataWriter();
			writer.WriteVersion(1);
			writer.WriteShortText(ServiceMonitor.Messages.Hello);
			sender.Send(writer.GetBytes());
		}

		public static void SendDeregisterService(this INetSender sender, string serviceID)
		{
			var writer = new BufferDataWriter();
			writer.WriteVersion(1);
			writer.WriteShortText(ServiceMonitor.Messages.DeregisterService, serviceID);
			sender.Send(writer.GetBytes());
		}

		public static void Send(this INetSender sender, ServiceBeacon beacon)
		{
			var writer = new BufferDataWriter();
			writer.WriteVersion(1);
			writer.WriteShortText(ServiceMonitor.Messages.ServiceBeacon);
			writer.WriteServiceBeacon(beacon);
			sender.Send(writer.GetBytes());
		}
	}
}