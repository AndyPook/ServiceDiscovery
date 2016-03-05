using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;

namespace Pook.Net
{
	public static class Network
	{
		private static string localHostName = null;
		public static string LocalHostName
		{
			get
			{
				if (localHostName == null)
					localHostName = Dns.GetHostName();
				return localHostName;
			}
		}

		private static IPAddress localHostAddr = null;
		public static IPAddress LocalHostAddr
		{
			get
			{
				if (localHostAddr == null)
				{
					IPHostEntry hostEntry = Dns.GetHostEntry(LocalHostName);
					if (hostEntry.AddressList.Length > 0)
						localHostAddr = hostEntry.AddressList[0];
					else
						localHostAddr = IPAddress.Parse("127.0.0.1");
				}

				return localHostAddr;
			}
		}

		public static string GetLocalIP()
		{
			return GetLocalIPs().FirstOrDefault();
		}

		public static IEnumerable<string> GetLocalIPs(bool includeLoopback = false)
		{
			return GetLocalAddresses(includeLoopback).Select(a => a.ToString());
		}

		public static IEnumerable<IPAddress> GetLocalAddresses(bool includeLoopback = false)
		{
			var suitableNics = NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up && (includeLoopback || nic.NetworkInterfaceType != NetworkInterfaceType.Loopback) && nic.SupportsMulticast && nic.Supports(NetworkInterfaceComponent.IPv4));
			foreach (var nic in suitableNics)
			{
				var properties = nic.GetIPProperties();
				foreach (var addr in properties.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
					yield return addr.Address;
			}
		}

		/// <summary>
		/// Find a free TCP port in the specified range
		///     0 - 1023  (     0 -  0x3FF): "Well-known" ports
		///  1024 - 49151 ( 0x400 - 0xBFFF): "Allocated" ports
		/// 49152 - 65535 (0xC000 - 0xFFFF): "Dynamic" ports
		/// </summary>
		/// <param name = "fromPort">Start of range</param>
		/// <param name = "toPort">End of range</param>
		/// <returns></returns>
		public static int FindFreePort(int fromPort, int toPort)
		{
			var usedPorts = GetUsedPorts(fromPort, toPort);
			for (int i = fromPort; i <= toPort; i++)
			{
				if (!usedPorts.Contains(i))
					return i;
			}

			return -1;
		}

		public static int FindFreePort()
		{
			return FindFreePort(0xC000, 0xCFFF);
		}

		public static void LogDynamicPorts(int fromPort = 49152, int toPort = 65535)
		{
			var usedPorts = GetUsedPorts(fromPort, toPort);
			Trace.TraceInformation($"Dynamic ports in use: {usedPorts.Count()}");
		}

		public static string GetFullyQualifiedDomainName()
		{
			return Dns.GetHostEntry(LocalHostName).HostName;
		}

		private static List<int> GetUsedPorts(int fromPort, int toPort)
		{
			var ipGlobal = IPGlobalProperties.GetIPGlobalProperties();
			var tcpInfoList = ipGlobal.GetActiveTcpConnections();
			var tcpListenerInfoList = ipGlobal.GetActiveTcpListeners();
			var usedPorts = (
					from ep in tcpInfoList
					where ep.LocalEndPoint.Port >= fromPort && ep.LocalEndPoint.Port <= toPort
					select ep.LocalEndPoint.Port).Union(
					from ep in tcpListenerInfoList
					where ep.Port >= fromPort && ep.Port <= toPort
					select ep.Port).ToList();
			return usedPorts;
		}
	}
}