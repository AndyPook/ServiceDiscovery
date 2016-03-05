using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Pook.Net
{
	public class MulticastUdpListener : IDisposable
	{
		public MulticastUdpListener(int channel, int port, bool localOnly = false) : this("239.192.1." + channel, port, localOnly) { }
		public MulticastUdpListener(string address, int port, bool localOnly = false)
		{
			this.MulticastGroup = IPAddress.Parse(address);
			this.MulticastEP = new IPEndPoint(MulticastGroup, port);
			this.localOnly = localOnly;
		}

		public IPAddress MulticastGroup { get; private set; }
		public IPEndPoint MulticastEP { get; private set; }

		private Action<IPEndPoint, byte[]> onMessage = delegate { };
		public Action<IPEndPoint, byte[]> OnMessage
		{
			get { return onMessage; }
			set { onMessage = value ?? delegate { }; }
		}

		private bool running;
		private UdpClient client;
		private readonly bool localOnly;

		public void Start()
		{
			Listen().ConfigureAwait(false);
		}
		public void Stop()
		{
			running = false;
		}

		private async Task Listen()
		{
			client = new UdpClient();
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			if (localOnly)
				client.Client.Bind(new IPEndPoint(IPAddress.Loopback, MulticastEP.Port));
			else
				client.Client.Bind(new IPEndPoint(IPAddress.Any, MulticastEP.Port));

			foreach (var localAddr in Network.GetLocalAddresses(includeLoopback: true))
				client.JoinMulticastGroup(MulticastGroup, localAddr);
			client.MulticastLoopback = true;
			client.Client.ReceiveTimeout = 200;

			running = true;
			while (running)
			{
				try
				{
					var result = await client.ReceiveAsync();
					if (result.Buffer.Length > 0)
						OnMessage(result.RemoteEndPoint, result.Buffer);
				}
				catch (ObjectDisposedException)
				{
					running = false;
				}
				catch (SocketException socketex)
				{
					// see http://msdn.microsoft.com/en-us/library/windows/desktop/ms740668(v=vs.85).aspx for error codes
					Trace.WriteLine("" + socketex.ErrorCode);
				}
			}

			client.Close();
			Trace.WriteLine("listener stopped");
		}

		public void Dispose()
		{
			Stop();
		}
	}
}