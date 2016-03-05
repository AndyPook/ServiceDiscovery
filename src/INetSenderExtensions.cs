using System;

using Pook.Net.Serialization;

namespace Pook.Net
{
	public static class INetSenderExtensions
	{
		public static void SendShortText(this INetSender sender, string msg)
		{
			var writer = new BufferDataWriter();
			writer.WriteShortText(msg);
			sender.Send(writer.GetBytes());
		}
		public static void SendShortText(this INetSender sender, params string[] msg)
		{
			var writer = new BufferDataWriter();
			writer.WriteShortText(msg);
			sender.Send(writer.GetBytes());
		}

		public static void Send(this INetSender sender, Action<IDataWriter> writerActions)
		{
			var writer = new BufferDataWriter();
			writerActions(writer);
			sender.Send(writer.GetBytes());
		}

		//public static void Send<T>(this INetSender sender, T data) where T : class
		//{
		//	var writer = new BufferDataWriter();
		//	writer.Write(data);
		//	sender.Send(writer.GetBytes());
		//}
	}
}