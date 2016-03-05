using System;

namespace Pook.Net.ServiceDiscovery
{
	public class AlertMessage
	{
		public string Zone { get; set; }
		public string TenantID { get; set; }
		public string MachineName { get; set; }
		public string ServiceName { get; set; }
		public string Version { get; set; }
		public string Message { get; set; }

		public override string ToString()
		{
			return string.Format(
				"{0} {1} - {2} {3} {4} [{5}]",
				Zone, MachineName, TenantID, ServiceName, Version, Message
			);
		}
	}
}