using System;
using System.Collections.Generic;

namespace Pook.Net.ServiceDiscovery
{
	public interface IServiceMonitor
	{
		bool Running { get; }
		string MachineName { get; }
		string LocalZone { get; }
		string LocalZoneFallback { get; }

		IServiceMonitor Start();
		void Stop();

		/// <summary>
		/// Triggers when a Beacon is recieved
		/// </summary>
		event Action<ServiceBeacon> ServiceHeartbeat;
		/// <summary>
		/// Triggers when a new <see cref="ServiceBeacon.ServiceKey"/> is seen
		/// </summary>
		event Action<ServiceBeacon> ServiceUp;
		/// <summary>
		/// Triggers when <see cref="ServiceBeacon.ServiceUri"/> or <see cref="ServiceBeacon.Version"/> changes
		/// </summary>
		event Action<ServiceBeacon> ServiceChange;
		/// <summary>
		/// Triggers when a Beacon times out
		/// </summary>
		event Action<ServiceBeacon> ServiceDown;
		/// <summary>
		/// Triggers when the <see cref="ServiceBeacon.ServiceData"/> of a Beacon changes.
		/// <para>
		/// The string param is the value of the _previous_ version
		/// </para>
		/// </summary>
		event Action<ServiceBeacon, string> ServiceDataChange;


		IEnumerable<ServiceBeacon> LocalServices { get; }
		IEnumerable<ServiceBeacon> Services { get; }

		/// <summary>
		/// Create a new beacon
		/// </summary>
		/// <param name="serviceID"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceUri"></param>
		/// <param name="serviceData"></param>
		/// <returns></returns>
		ServiceBeacon CreateBeacon(string serviceID, string serviceName, string serviceUri = null, string serviceData = null);

		/// <summary>
		/// Add an extension that provider its own set of beacons to include
		/// </summary>
		/// <param name="provider"></param>
		void AddBeaconProvider(IServiceBeaconProvider provider);
		void ClearBeaconProviders();

		/// <summary>
		/// Send a simple message to announce presence
		/// <para>Each recipient will respond with the list of their local services</para>
		/// </summary>
		void Hello();

		/// <summary>
		/// Send a single heartbeat
		/// <para>
		/// Typically you will use the (tenantID, serviceName, serviceUri) extension override 
		/// which fills in zone, version etc.
		/// </para>
		/// </summary>
		/// <param name="service"></param>
		void Ping(ServiceBeacon service);

		/// <summary>
		/// Register a sevice
		/// </summary>
		/// <param name="beacon"></param>
		/// <returns></returns>
		IServiceMonitor RegisterService(ServiceBeacon beacon);

		/// <summary>
		/// De-register a service.
		/// <para>Remove a local service from the registry</para>
		/// </summary>
		/// <param name="serviceID"></param>
		/// <param name="serviceName"></param>
		/// <returns></returns>
		IServiceMonitor DeregisterService(string serviceID, string serviceName);

		/// <summary>
		/// Tests if a specific service is registered
		/// </summary>
		/// <param name="serviceID"></param>
		/// <returns></returns>
		ServiceBeacon Get(string serviceID);

		/// <summary>
		/// Returns a list of discovered services that match the criteria
		/// </summary>
		/// <param name="serviceName">The name of the service to find</param>
		/// <param name="id">Only return services registered by this tenant</param>
		/// <param name="zone">The zone the service is registered in. Normally this should not be specified so only services in the local zone are returned</param>
		/// <param name="host">The host/machine the service is registered on</param>
		/// <returns>The list of services that match the crieria</returns>
		IEnumerable<ServiceBeacon> Find(string serviceName = null, string id = null, string host = null);
	}
}