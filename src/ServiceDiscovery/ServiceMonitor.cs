using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Pook.Net.Serialization;

namespace Pook.Net.ServiceDiscovery
{
	public class ServiceMonitor : IServiceMonitor
	{
		public static IServiceMonitor StartNew()
		{
			var monitor = new ServiceMonitor();
			monitor.Start();
			return monitor;
		}

		public const string DefaultAddress = "239.192.1.1";
		public const int DefaultPort = 41400;
		public const byte DefaultVersion = 1;
		public const string GlobalTenantID = "**";
		public const string Wildcard = "*";
		public static class Messages
		{
			public const string ServiceBeacon = "Beacon";
			public const string DeregisterService = "DeregisterService";
			public const string Hello = "hello";
		}

		public ServiceMonitor(string listenAddress = DefaultAddress, int port = DefaultPort, int timeToLive = 5, int heartbeatPeriod = 2, bool? localMachineOnly = null)
		{
			this.listenAddress = listenAddress;
			this.port = port;
			this.timeToLive = timeToLive;
			this.heartbeatPeriod = heartbeatPeriod;
			MachineName = Environment.MachineName;
			if (localMachineOnly.HasValue)
				this.localMachineOnly = localMachineOnly.Value;
			sender = new MulticastUdpSender(listenAddress, port, this.localMachineOnly);
		}

		private readonly string listenAddress;
		private readonly int port;
		private readonly int timeToLive;
		private readonly int heartbeatPeriod;
		private readonly bool localMachineOnly;
		private readonly BlockingCollection<ServiceBeacon> beaconQueue = new BlockingCollection<ServiceBeacon>();
		private readonly BlockingCollection<Action> eventQueue = new BlockingCollection<Action>();
		private readonly INetSender sender;
		private readonly ConcurrentDictionary<string, ServiceBeacon> localServices = new ConcurrentDictionary<string, ServiceBeacon>();
		private ConcurrentBag<IServiceBeaconProvider> beaconProviders = new ConcurrentBag<IServiceBeaconProvider>();
		private Timer heartbeatTimer;
		private MulticastUdpListener listener;
		private readonly ConcurrentDictionary<string, Heartbeat> services = new ConcurrentDictionary<string, Heartbeat>();
		private Timer expiryTimer;

		public bool Running => listener != null;

		public string MachineName { get; }
		public string LocalZone { get; }
		public string LocalZoneFallback { get; }

		public event Action<ServiceBeacon> ServiceHeartbeat;
		public event Action<ServiceBeacon> ServiceUp;
		public event Action<ServiceBeacon> ServiceChange;
		public event Action<ServiceBeacon> ServiceDown;
		public event Action<ServiceBeacon, string> ServiceDataChange;
		private void OnServiceHeartbeat(ServiceBeacon beacon)
		{
			var handler = ServiceHeartbeat;
			if (handler != null)
				EnqueueEvent(() => handler(beacon));
		}

		private void OnServiceUp(ServiceBeacon beacon)
		{
			var handler = ServiceUp;
			if (handler != null)
				EnqueueEvent(() => handler(beacon));
		}

		private void OnServiceChange(ServiceBeacon beacon)
		{
			var handler = ServiceChange;
			if (handler != null)
				EnqueueEvent(() => handler(beacon));
		}

		private void OnServiceDataChange(ServiceBeacon beacon, string oldData)
		{
			var handler = ServiceDataChange;
			if (handler != null)
				EnqueueEvent(() => handler(beacon, oldData));
		}

		private void OnServiceDown(ServiceBeacon beacon)
		{
			var handler = ServiceDown;
			if (handler != null)
				EnqueueEvent(() => handler(beacon));
		}

		private bool eventQueueWarning;
		private void EnqueueEvent(Action action)
		{
			eventQueue.Add(action);
			if (eventQueue.Count >= 100 && eventQueue.Count % 100 == 0)
			{
				eventQueueWarning = true;
				Trace.TraceWarning("*** eventQueue: length=" + eventQueue.Count);
			}
			else if (eventQueueWarning && eventQueue.Count < 100)
			{
				eventQueueWarning = false;
				Trace.TraceInformation("*** eventQueue: returning to normal");
			}
		}

		public IEnumerable<ServiceBeacon> Services => services.Values.Select(s => s.Beacon);

		public IEnumerable<ServiceBeacon> LocalServices => localServices.Values;

		public void AddBeaconProvider(IServiceBeaconProvider provider)
		{
			beaconProviders.Add(provider);
		}

		public void ClearBeaconProviders()
		{
			foreach (var beaconProvider in beaconProviders)
				beaconProvider.Dispose();
			beaconProviders = new ConcurrentBag<IServiceBeaconProvider>();
		}

		public ServiceBeacon Get(string serviceID)
		{
			Heartbeat hb;
			if (services.TryGetValue(serviceID, out hb))
				return hb.Beacon;
			return null;
		}

		public IEnumerable<ServiceBeacon> Find(string serviceName = null, string id = null, string host = null)
		{
			var matches = FindInternal(serviceName, id, host);
			return matches;
		}

		private IEnumerable<ServiceBeacon> FindInternal(string serviceName, string id, string host)
		{
			if (string.IsNullOrEmpty(host))
				host = Wildcard;
			if (string.IsNullOrEmpty(serviceName))
				serviceName = Wildcard;
			if (string.IsNullOrEmpty(id))
				id = Wildcard;
			var matches = Services.Where(s => (host == Wildcard || string.Equals(s.MachineName, host, StringComparison.OrdinalIgnoreCase)) && (id == Wildcard || string.Equals(s.ID, id, StringComparison.OrdinalIgnoreCase)) && (serviceName == Wildcard || string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))).ToList();
			return matches;
		}

		public IServiceMonitor Start()
		{
			StartBeaconProcessing();
			StartEventProcessing();
			StartListener();
			StartHeartbeat();
			return this;
		}

		private void StartBeaconProcessing()
		{
			Task.Factory.StartNew(ProcessBeacons, TaskCreationOptions.LongRunning).ContinueWith(t => Trace.TraceError(t.Exception.Message), TaskContinuationOptions.OnlyOnFaulted);
		}

		private void StartEventProcessing()
		{
			Task.Factory.StartNew(s => ProcessEvents(), TaskCreationOptions.LongRunning).ContinueWith(t => Trace.TraceError(t.Exception.Message), TaskContinuationOptions.OnlyOnFaulted);
		}

		private void ProcessEvents()
		{
			var partitioner = Partitioner.Create(eventQueue.GetConsumingEnumerable(), EnumerablePartitionerOptions.None);
			Parallel.ForEach(partitioner, new ParallelOptions { MaxDegreeOfParallelism = 4 }, evt =>
				 {
					 try
					 {
						 evt();
					 }
					 catch (Exception ex)
					 {
						 Trace.TraceError("ProcessEvent: " + ex.Message);
					 }
				 }
			);
		}

		private void StartListener()
		{
			if (listener != null)
				return;
			listener = new MulticastUdpListener(listenAddress, port, localMachineOnly);
			listener.OnMessage = (ep, data) =>
			{
				try
				{
					if (data.Length == 0)
						return;
					ReadMessage(data);
				}
				catch
				{
					// silently swallow "bad" messages
				}
			};

			listener.Start();
			expiryTimer = new Timer(_ => CheckExpiry(), null, TimeSpan.FromSeconds(timeToLive), TimeSpan.FromSeconds(timeToLive));
		}

		private void ReadMessage(byte[] data)
		{
			byte version = data[0];
			if (version != 1)
				return;
			var reader = new BufferDataReader(data);
			// read "version" byte
			reader.ReadByte();
			string msg = reader.ReadShortText();
			// invalid/unknown messages are silently swallowed
			switch (msg)
			{
				case Messages.ServiceBeacon:
					var b = reader.ReadServiceBeacon();
					EnqueueBeacon(b);
					break;
				case Messages.DeregisterService:
					string serviceID = reader.ReadShortText();
					RemoveService(serviceID);
					break;
				case Messages.Hello:
					SendBeacons();
					break;
			}
		}

		private void StartHeartbeat()
		{
			if (heartbeatTimer != null)
				return;
			heartbeatTimer = new Timer(_ => SendBeacons(), null, TimeSpan.Zero, TimeSpan.FromSeconds(heartbeatPeriod));
		}

		private void SendBeacons()
		{
			foreach (var beacon in localServices.Values)
				sender.Send(beacon);
			if (beaconProviders.Count == 0)
				return;
			foreach (var provider in beaconProviders)
			{
				foreach (var beacon in provider.GetBeacons())
					sender.Send(beacon);
			}
		}

		public void Stop()
		{
			if (expiryTimer != null)
				expiryTimer.Dispose();
			beaconQueue.CompleteAdding();
			eventQueue.CompleteAdding();
			StopListener();
			StopHeartbeat();
			foreach (var beaconProvider in beaconProviders)
				beaconProvider.Dispose();
			DeregisterAll();
			var disposable = sender as IDisposable;
			if (disposable != null)
				disposable.Dispose();
		}

		private void StopListener()
		{
			if (listener == null)
				return;
			listener.Stop();
			listener = null;
		}

		private void StopHeartbeat()
		{
			if (heartbeatTimer == null)
				return;
			heartbeatTimer.Dispose();
			heartbeatTimer = null;
		}

		public void Hello()
		{
			sender.SendHello();
		}

		public void Ping(ServiceBeacon beacon)
		{
			sender.Send(beacon);
		}

		public IServiceMonitor RegisterService(ServiceBeacon beacon)
		{
			localServices[beacon.ServiceKey] = beacon;
			return this;
		}

		public IServiceMonitor DeregisterService(string ID, string serviceName)
		{
			foreach (var service in localServices.Values)
			{
				if (string.Equals(service.ID, ID, StringComparison.InvariantCultureIgnoreCase) && string.Equals(service.ServiceName, serviceName, StringComparison.InvariantCultureIgnoreCase))
				{
					ServiceBeacon info;
					localServices.TryRemove(service.ServiceKey, out info);
					RemoveService(service.ServiceKey);
				}
			}

			return this;
		}

		private void DeregisterAll()
		{
			foreach (var service in LocalServices)
				sender.SendDeregisterService(service.ServiceKey);
			localServices.Clear();
		}

		private void RemoveService(string serviceID)
		{
			Heartbeat hb;
			if (services.TryRemove(serviceID, out hb))
			{
				sender.SendDeregisterService(serviceID);
				OnServiceDown(hb.Beacon);
			}
		}

		public ServiceBeacon CreateBeacon(string serviceID, string serviceName, string serviceUri = null, string serviceData = null)
		{
			if (serviceUri != null && !serviceUri.StartsWith("http://"))
			{
				if (!serviceUri.StartsWith("/"))
					serviceUri = "/" + serviceUri;
				serviceUri = "http://" + Environment.MachineName + serviceUri;
			}

			return new ServiceBeacon { MachineName = MachineName, ID = serviceID, ServiceName = serviceName, Version = SemVer.Current, ServiceUri = serviceUri ?? string.Empty, ServiceData = serviceData };
		}

		private bool beaconQueueWarning;
		private void EnqueueBeacon(ServiceBeacon beacon)
		{
			if (beacon == null)
				return;
			if (beaconQueue.IsAddingCompleted)
				return;
			beaconQueue.Add(beacon);
			if (beaconQueue.Count >= 100 && beaconQueue.Count % 100 == 0)
			{
				beaconQueueWarning = true;
				Trace.TraceWarning("*** beaconQueue: length=" + beaconQueue.Count);
			}
			else if (beaconQueueWarning && beaconQueue.Count < 100)
			{
				beaconQueueWarning = false;
				Trace.TraceWarning("*** beaconQueue: returning to normal");
			}
		}

		private void ProcessBeacons()
		{
			foreach (var beacon in beaconQueue.GetConsumingEnumerable())
				try
				{
					ProcessBeacon(beacon);
				}
				catch (Exception ex)
				{
					Trace.TraceError("ProcessBeacon: " + ex.MessageAggregator());
				}
		}

		private void ProcessBeacon(ServiceBeacon beacon)
		{
			OnServiceHeartbeat(beacon);
			services.AddOrUpdate(
				beacon.ServiceKey,
				id =>
				{
					OnServiceUp(beacon);
					return new Heartbeat(beacon);
				},
				(s, heartbeat) =>
				{
					heartbeat.Pulse();
					bool change = false;
					if ((!string.IsNullOrEmpty(beacon.Version) && heartbeat.Beacon.Version != beacon.Version))
					{
						heartbeat.Beacon.Version = beacon.Version;
						change = true;
					}

					if (!string.IsNullOrEmpty(beacon.ServiceUri) && heartbeat.Beacon.ServiceUri != beacon.ServiceUri)
					{
						heartbeat.Beacon.ServiceUri = beacon.ServiceUri;
						change = true;
					}

					if (change)
						OnServiceChange(heartbeat.Beacon);
					if (heartbeat.Beacon.ServiceData != beacon.ServiceData)
					{
						var oldData = heartbeat.Beacon.ServiceData;
						heartbeat.Beacon.ServiceData = beacon.ServiceData;
						OnServiceDataChange(heartbeat.Beacon, oldData);
					}

					return heartbeat;
				}
			);
		}

		private void CheckExpiry()
		{
			var oldestValidPulse = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeToLive));
			var timedoutHeartbeats = services.Values.Where(hb => hb.LastPulse < oldestValidPulse).ToList();
			foreach (var heartbeat in timedoutHeartbeats)
			{
				Heartbeat hb;
				if (services.TryRemove(heartbeat.Beacon.ServiceKey, out hb))
					OnServiceDown(heartbeat.Beacon);
			}
		}

		private class Heartbeat
		{
			public Heartbeat(ServiceBeacon beacon)
			{
				Beacon = beacon;
				LastPulse = DateTime.UtcNow;
			}

			public DateTime LastPulse { get; private set; }

			public ServiceBeacon Beacon { get; set; }

			public void Pulse()
			{
				LastPulse = DateTime.UtcNow;
			}

			public override int GetHashCode()
			{
				return Beacon.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as Heartbeat;
				if (other == null)
					return false;
				return Beacon.ServiceKey == other.Beacon.ServiceKey;
			}
		}
	}
}