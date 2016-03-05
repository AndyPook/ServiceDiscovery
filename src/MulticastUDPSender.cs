using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Pook.Net
{
	public class MulticastUdpSender : INetSender, IDisposable
	{
		public MulticastUdpSender(int channel, int port, bool localOnly = false) : this("239.192.1." + channel, port, localOnly) { }
		public MulticastUdpSender(string address, int port, bool localOnly = false)
		{
			var multicastGroup = IPAddress.Parse(address);
			multicastEndPoint = new IPEndPoint(multicastGroup, port);

			if (localOnly)
				AddClient(multicastGroup, IPAddress.Loopback);
			else
				foreach (var localAddr in Network.GetLocalAddresses())
					AddClient(multicastGroup, localAddr);
		}

		private void AddClient(IPAddress multicastGroup, IPAddress localAddr)
		{
			var client = new UdpClient();
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			client.Client.Bind(new IPEndPoint(localAddr, multicastEndPoint.Port));
			client.JoinMulticastGroup(multicastGroup, localAddr);
			client.MulticastLoopback = true;
			clients.Add(client);
		}

		private readonly IPEndPoint multicastEndPoint;
		private readonly List<UdpClient> clients = new List<UdpClient>();

		public void Send(byte[] data)
		{
			foreach (var client in clients)
			{
				try
				{
					client.Send(data, data.Length, multicastEndPoint);
				}
				catch (SocketException ex)
				{
					Trace.TraceError("SocketErrorCode : {0}, ErrorCode : {1}, Message : {2}", ex.SocketErrorCode.ToString(), ex.ErrorCode, ex.Message);
				}
				catch (Exception ex)
				{
					Trace.TraceError(ex.Message);
				}
			}
		}

		public void Dispose()
		{
			foreach (var client in clients)
				client.Close();
		}
	}
}