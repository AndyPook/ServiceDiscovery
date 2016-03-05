using System;
using System.Collections.Generic;
using System.Linq;

namespace Pook.Net.ServiceDiscovery
{
	public static class IServiceMonitorExtensions
	{
		public static bool Exists(this IServiceMonitor monitor, ServiceBeacon beacon)
		{
			return monitor.Get(beacon.ServiceKey) != null;
		}
		public static ServiceBeacon Get(this IServiceMonitor monitor, ServiceBeacon beacon)
		{
			return monitor.Get(beacon.ServiceKey);
		}

		/// <summary>
		/// Send a single heartbeat
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceID"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceUri"></param>
		public static void Ping(this IServiceMonitor monitor, string serviceID, string serviceName, string serviceUri)
		{
			var service = new ServiceBeacon
			{
				MachineName = monitor.MachineName,
				ID = serviceID,
				ServiceName = serviceName,
				Version = SemVer.Current,
				ServiceUri = serviceUri ?? string.Empty
			};

			monitor.Ping(service);
		}

		/// <summary>
		/// Register a service
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceID"></param>
		/// <param name="serviceUri"></param>
		/// <param name="serviceData">Place for service to add some extra info</param>
		/// <returns></returns>
		public static IServiceMonitor RegisterService(this IServiceMonitor monitor, string serviceID, string serviceName, string serviceUri = null, string serviceData = null)
		{
			var beacon = monitor.CreateBeacon(serviceID, serviceName, serviceUri, serviceData);
			monitor.RegisterService(beacon);
			return monitor;
		}

		/// <summary>
		/// Create a beacon for a Global service
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceUri"></param>
		/// <param name="serviceData"></param>
		/// <returns></returns>
		public static ServiceBeacon CreateGlobalBeacon(this IServiceMonitor monitor, string serviceName, string serviceUri = null, string serviceData = null)
		{
			return monitor.CreateBeacon(ServiceMonitor.GlobalTenantID, serviceName, serviceUri, serviceData);
		}

		/// <summary>
		/// Register a global service
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceUri"></param>
		/// <param name="serviceData">Place for service to add some extra info</param>
		public static IServiceMonitor RegisterGlobalService(this IServiceMonitor monitor, string serviceName, string serviceUri = null, string serviceData = null)
		{
			monitor.RegisterService(ServiceMonitor.GlobalTenantID, serviceName, serviceUri, serviceData);
			return monitor;
		}

		/// <summary>
		/// Register a global service
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceUri"></param>
		/// <param name="serviceData">Place for service to add some extra info</param>
		public static IServiceMonitor RegisterGlobalService(this IServiceMonitor monitor, string serviceName, Uri serviceUri, string serviceData = null)
		{
			monitor.RegisterService(ServiceMonitor.GlobalTenantID, serviceName, serviceUri.ToString(), serviceData);
			return monitor;
		}

		/// <summary>
		/// Register a service
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceID"></param>
		/// <param name="serviceName"></param>
		/// <param name="serviceUri"></param>
		/// <param name="serviceData">Place for service to add some extra info</param>
		public static IServiceMonitor RegisterService(this IServiceMonitor monitor, string serviceID, string serviceName, Uri serviceUri, string serviceData = null)
		{
			monitor.RegisterService(serviceID, serviceName, serviceUri.ToString(), serviceData);
			return monitor;
		}

		public static string FindGlobalServiceUri(this IServiceMonitor monitor, string serviceName, string zone = null)
		{
			var service = monitor.Find(serviceName, ServiceMonitor.GlobalTenantID, zone).FirstOrDefault();
			if (service == null)
				return string.Empty;

			return service.ServiceUri;
		}

		public static string FindServiceUri(this IServiceMonitor monitor, string serviceName, string tenantID = null, string zone = null)
		{
			var service = monitor.Find(serviceName, tenantID, zone).FirstOrDefault();
			if (service == null)
				return string.Empty;

			return service.ServiceUri;
		}

		public static IEnumerable<ServiceBeacon> Find(this IServiceMonitor monitor, params string[] args)
		{
			string zone = null;
			string tenantID = ServiceMonitor.Wildcard;
			string serviceName = ServiceMonitor.Wildcard;

			switch (args.Length)
			{
				case 0:
					break;
				case 1:
					serviceName = args[0];
					break;
				case 2:
					serviceName = args[0];
					tenantID = args[1];
					break;
				default:
					serviceName = args[0];
					tenantID = args[1];
					zone = args[2];
					break;
			}

			return monitor.Find(serviceName, tenantID, zone);
		}

		/// <summary>
		/// Get a list of machines (that have services issuing beacons
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="zone">Filter list by zone. Ommit zone to get for all zones</param>
		/// <returns>List of machine names</returns>
		public static IEnumerable<string> GetMachines(this IServiceMonitor monitor)
		{
			return new HashSet<string>(monitor.Services.Select(x => x.MachineName));
		}

		/// <summary>
		/// Reports whether a tenant service is available
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="serviceID">The id of the Tenant</param>
		/// <param name="serviceName">The required service name</param>
		/// <param name="zone">The zone. Uses the local zone if left null</param>
		/// <returns></returns>
		public static bool IsAvailable(this IServiceMonitor monitor, string serviceID, string serviceName, string zone = null)
		{
			var service = monitor.Find(serviceName, serviceID, zone).FirstOrDefault();
			return service != null;
		}

		/// <summary>
		/// Reports whether a Global service is available
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="globalServiceName">The required service name</param>
		/// <param name="zone">The zone. Uses the local zone if left null</param>
		/// <returns></returns>
		public static bool IsAvailable(this IServiceMonitor monitor, string globalServiceName, string zone = null)
		{
			var service = monitor.Find(globalServiceName, ServiceMonitor.GlobalTenantID, zone).FirstOrDefault();
			return service != null;
		}
	}
}