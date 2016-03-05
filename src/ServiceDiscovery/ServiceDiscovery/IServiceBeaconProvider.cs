using System;
using System.Collections.Generic;

namespace Pook.Net.ServiceDiscovery
{
	public interface IServiceBeaconProvider : IDisposable
	{
		IEnumerable<ServiceBeacon> GetBeacons();
	}
}