namespace Pook.Net.ServiceDiscovery
{
	public class ServiceBeacon
	{
		public string ServiceKey => MachineName + ":" + ID + ":" + ServiceName;

		public string MachineName { get; set; }
		public string ID { get; set; }
		public string ServiceName { get; set; }

		public string Version { get; set; }
		public string ServiceUri { get; set; }
		public string ServiceData { get; set; }

		public override int GetHashCode()
		{
			return ServiceKey.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			var other = obj as ServiceBeacon;
			if (other == null)
				return false;

			return ServiceKey == other.ServiceKey;
		}

		public override string ToString()
		{
			return $"{MachineName} - {ID} {ServiceName} {Version} [{ServiceUri}] {ServiceData}";
		}
	}
}