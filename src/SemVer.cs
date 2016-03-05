using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace Pook.Net
{
	/// <summary>
	/// Parses a full product version string into component parts
	/// </summary>
	public sealed class SemVer : IComparable<SemVer>
	{
		public static readonly SemVer NullVersion = new SemVer((string)null);

		public static SemVer GetFromPath(string path)
		{
			if (!File.Exists(path))
				return NullVersion;
			var version = FileVersionInfo.GetVersionInfo(path);
			return new SemVer(version.ProductVersion);
		}

		private static SemVer current;
		public static SemVer Current { get { return current ?? (current = new SemVer()); } }

		/// <summary>
		/// Version associated with the main assembly
		/// </summary>
		public SemVer() : this(Assembly.GetEntryAssembly()) { }

		/// <summary>
		/// Version associated with the specified type
		/// </summary>
		/// <param name="type"></param>
		public SemVer(Type type) : this(type.Assembly) { }

		/// <summary>
		/// Version associated with the specified assembly
		/// </summary>
		/// <param name="assy"></param>
		public SemVer(Assembly assy)
		{
			string fullVersion = null;
			if (assy != null)
			{
				AssyName = assy.GetName().Name;
				// try get InformationalVersion as this supports full SemVer
				object[] infoVerAttributes = assy.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (infoVerAttributes.Length > 0)
					fullVersion = ((AssemblyInformationalVersionAttribute)infoVerAttributes[0]).InformationalVersion;
				else
				{
					// fall back to FileVersion
					object[] fileVerAttributes = assy.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
					if (fileVerAttributes.Length > 0)
						fullVersion = ((AssemblyFileVersionAttribute)fileVerAttributes[0]).Version;
				}
			}
			ParseVersion(fullVersion);
		}
		/// <summary>
		/// Version associated with the specified Info Version Attribute
		/// </summary>
		/// <param name="versionAttr"></param>
		public SemVer(AssemblyInformationalVersionAttribute versionAttr) : this(versionAttr.InformationalVersion) { }

		/// <summary>
		/// Version decoded from the specified version string
		/// </summary>
		/// <param name="fullVersion"></param>
		public SemVer(string fullVersion)
		{
			AssyName = string.Empty;
			ParseVersion(fullVersion);
		}

		public string AssyName { get; private set; }

		private void InitNull()
		{
			FullVersion = "0.0.0";
			Version = "0.0";
			ShortVersion = "0.0";
			Major = 0;
			Minor = 0;
			Patch = 0;
			PreRelease = string.Empty;
			RevisionString = string.Empty;
			Build = string.Empty;
			BuildDate = DateTime.MinValue;
			Revision = 0;
		}

		private void ParseVersion(string fullVersion)
		{
			if (string.IsNullOrEmpty(fullVersion))
			{
				InitNull();
				return;
			}

			FullVersion = fullVersion;

			int hyphenPos = fullVersion.IndexOf('-');
			int plusPos = fullVersion.IndexOf('+');
			if (plusPos > 0 && hyphenPos > plusPos)
				hyphenPos = -1;
			else
				plusPos = fullVersion.IndexOf('+', hyphenPos > 0 ? hyphenPos : 0);

			int endOfVersion;
			if (hyphenPos > 0)
				endOfVersion = hyphenPos;
			else if (plusPos > 0)
				endOfVersion = plusPos;
			else
				endOfVersion = fullVersion.Length;

			var versionParts = fullVersion.Substring(0, endOfVersion).Split('.');
			if (versionParts.Length > 0)
				Major = Convert.ToInt32(versionParts[0]);
			else
				Major = 0;

			if (versionParts.Length > 1)
				Minor = Convert.ToInt32(versionParts[1]);
			else
				Minor = 0;

			if (versionParts.Length > 2)
				Patch = Convert.ToInt32(versionParts[2]);
			else
				Patch = 0;

			ShortVersion = string.Format("{0}.{1}", Major, Minor);

			if (Patch == 0)
				Version = string.Format("{0}.{1}", Major, Minor);
			else
				Version = string.Format("{0}.{1}.{2}", Major, Minor, Patch);

			if (versionParts.Length < 3)
				FullVersion = string.Format("{0}.{1}.{2}", Major, Minor, Patch);

			if (versionParts.Length > 3)
			{
				// old version format
				if (!string.IsNullOrEmpty(versionParts[3]))
				{
					DateTime buildDate;
					if (DateTime.TryParseExact(versionParts[3], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out buildDate))
						BuildDate = buildDate;
				}
				if (hyphenPos > 0)
				{
					RevisionString = fullVersion.Substring(hyphenPos + 1, fullVersion.Length - hyphenPos - 1);
					int rev;
					int.TryParse(RevisionString, out rev);
					Revision = rev;
				}
				PreRelease = string.Empty;
				Build = string.Empty;
			}
			else
			{
				//semver format
				if (hyphenPos > 0)
				{
					if (plusPos < 0)
						PreRelease = fullVersion.Substring(hyphenPos + 1, fullVersion.Length - hyphenPos - 1);
					else
						PreRelease = fullVersion.Substring(hyphenPos + 1, plusPos - hyphenPos - 1);
				}
				else
					PreRelease = string.Empty;

				if (plusPos > 0)
					Build = fullVersion.Substring(plusPos + 1);

				if (!string.IsNullOrEmpty(Build))
				{
					var buildParts = Build.Split('.');
					if (!string.IsNullOrEmpty(buildParts[0]))
					{
						DateTime buildDate;
						if (DateTime.TryParseExact(buildParts[0], "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out buildDate))
							BuildDate = buildDate;
					}
					if (buildParts.Length > 1 && buildParts[1][0] == 'r')
					{
						RevisionString = buildParts[1].Substring(1);
						int rev;
						int.TryParse(RevisionString, out rev);
						Revision = rev;
					}
				}
				else
					Build = string.Empty;
			}
		}

		/// <summary>
		/// Full version string as found in an assembly
		/// </summary>
		public string FullVersion { get; private set; }
		/// <summary>
		/// Short version number just containing major, minor and release if it is not 0
		/// </summary>
		public string Version { get; private set; }
		/// <summary>
		/// Short version number just containing major, minor
		/// </summary>
		public string ShortVersion { get; private set; }
		/// <summary>
		/// Major version number
		/// </summary>
		public int Major { get; private set; }
		/// <summary>
		/// Minor version number
		/// </summary>
		public int Minor { get; private set; }
		/// <summary>
		/// Patch number
		/// </summary>
		public int Patch { get; private set; }
		/// <summary>
		/// Signifies a pre-release version (optional)
		/// </summary>
		public string PreRelease { get; private set; }
		/// <summary>
		/// Build metadata (optional)
		/// </summary>
		public string Build { get; private set; }
		/// <summary>
		/// Date code was built (only date part valid)
		/// </summary>
		public DateTime BuildDate { get; private set; }
		/// <summary>
		/// Subversion revision number (as a string)
		/// </summary>
		public string RevisionString { get; private set; }
		/// <summary>
		/// Subversion revision number
		/// </summary>
		public int Revision { get; private set; }

		/// <summary>
		/// The full version string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Version;
		}

		/// <summary>
		/// implicitly converts a <see cref="SemVer"/> to a string
		/// </summary>
		/// <param name="productVersion"></param>
		/// <returns></returns>
		public static implicit operator string(SemVer productVersion)
		{
			return productVersion.FullVersion;
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			var other = obj as SemVer;
			if (other == null)
				throw new ArgumentException("A CodeVersion object is required for comparison.", "obj");

			return CompareTo(other);
		}

		/// <summary>
		/// Just compares major and minor
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareShortVersionTo(SemVer other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			if (FullVersion == other.FullVersion)
				return 0;

			if (Major > other.Major)
				return 1;
			if (Major < other.Major)
				return -1;

			// Major parts are equal

			if (Minor > other.Minor)
				return 1;
			if (Minor < other.Minor)
				return -1;

			// Minor parts are equal
			return 0;
		}

		/// <summary>
		/// Just compares major, minor and patch
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareVersionTo(SemVer other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			if (FullVersion == other.FullVersion)
				return 0;

			if (Major > other.Major)
				return 1;
			if (Major < other.Major)
				return -1;

			// Major parts are equal

			if (Minor > other.Minor)
				return 1;
			if (Minor < other.Minor)
				return -1;

			// Minor parts are equal

			if (Patch > other.Patch)
				return 1;
			if (Patch < other.Patch)
				return -1;

			// patch parts are equal
			return 0;
		}

		/// <summary>
		/// Compare FullVersion accroding to semver 2.0
		/// <para>http://semver.org/</para>
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(SemVer other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			if (FullVersion == other.FullVersion)
				return 0;

			if (Major > other.Major)
				return 1;
			if (Major < other.Major)
				return -1;

			// Major parts are equal

			if (Minor > other.Minor)
				return 1;
			if (Minor < other.Minor)
				return -1;

			// Minor parts are equal

			if (Patch > other.Patch)
				return 1;
			if (Patch < other.Patch)
				return -1;

			// Patch parts are equal

			// Precedence refers to how versions are compared to each other when ordered. Precedence MUST be
			// calculated by separating the version into major, minor, patch and pre-release identifiers in that order 
			// (Build metadata does not figure into precedence). Precedence is determined by the first difference when 
			// comparing each of these identifiers from left to right as follows: Major, minor, and patch versions are always
			// compared numerically. Example: 1.0.0 < 2.0.0 < 2.1.0 < 2.1.1. When major, minor, and patch are equal, a 
			// pre-release version has lower precedence than a normal version. Example: 1.0.0-alpha < 1.0.0. Precedence
			// for two pre-release versions with the same major, minor, and patch version MUST be determined by 
			// comparing each dot separated identifier from left to right until a difference is found as follows: identifiers 
			// consisting of only digits are compared numerically and identifiers with letters or hyphens are compared 
			// lexically in ASCII sort order. Numeric identifiers always have lower precedence than non-numeric identifiers. 
			// A larger set of pre-release fields has a higher precedence than a smaller set, if all of the preceding identifiers
			// are equal. Example: 1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-alpha.beta < 1.0.0-beta < 1.0.0-beta.2 < 1.0.0-beta.11 < 1.0.0-rc.1 < 1.0.0.
			// Source: http://semver.org/
			if (PreRelease == other.PreRelease)
				return 0;
			if (!string.IsNullOrWhiteSpace(PreRelease) && string.IsNullOrWhiteSpace(other.PreRelease))
				return -1;
			if (string.IsNullOrWhiteSpace(PreRelease) && !string.IsNullOrWhiteSpace(other.PreRelease))
				return 1;

			var parts = PreRelease.Split('.');
			var otherParts = other.PreRelease.Split('.');

			int commonLength = Math.Min(parts.Length, otherParts.Length);

			for (int i = 0; i < commonLength; i++)
			{
				int part;
				int otherPart;
				bool partIsInt = int.TryParse(parts[i], out part);
				bool otherPartIsInt = int.TryParse(otherParts[i], out otherPart);

				// Numeric identifiers always have lower precedence than non-numeric identifiers
				if (!partIsInt && otherPartIsInt)
					return 1;
				if (partIsInt && !otherPartIsInt)
					return -1;

				// they are both the same type
				if (partIsInt)
				{
					// numeric

					if (part > otherPart)
						return 1;
					if (part < otherPart)
						return -1;
				}
				else
				{
					// non-numeric
					int stringCompare = string.Compare(parts[i], otherParts[i], StringComparison.Ordinal);
					if (stringCompare != 0)
						return stringCompare;
				}
			}

			// common parts are equal

			if (parts.Length > otherParts.Length)
				return 1;
			if (parts.Length < otherParts.Length)
				return -1;

			return 0;
		}

		/// <summary>
		/// Just compares major, minor and patch with null checks
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static int CompareVersion(SemVer left, SemVer right)
		{
			if (ReferenceEquals(left, right))
				return 0;
			if (ReferenceEquals(left, null))
				return -1;
			return left.CompareVersionTo(right);
		}

		/// <summary>
		/// Compare FullVersion accroding to semver 2.0 with null checks
		/// <para>http://semver.org/</para>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static int Compare(SemVer left, SemVer right)
		{
			if (ReferenceEquals(left, right))
				return 0;
			if (ReferenceEquals(left, null))
				return -1;
			return left.CompareTo(right);
		}

		public override bool Equals(object obj)
		{
			var other = obj as SemVer;
			if (ReferenceEquals(other, null))
				return false;

			return CompareTo(other) == 0;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = FullVersion != null ? FullVersion.GetHashCode() : 0;
				return hashCode;
			}
		}

		public static bool operator ==(SemVer left, SemVer right)
		{
			if (ReferenceEquals(left, null))
				return ReferenceEquals(right, null);

			return left.Equals(right);
		}

		public static bool operator !=(SemVer left, SemVer right)
		{
			return !(left == right);
		}

		public static bool operator <(SemVer left, SemVer right)
		{
			return Compare(left, right) < 0;
		}

		public static bool operator >(SemVer left, SemVer right)
		{
			return Compare(left, right) > 0;
		}

		public static bool operator <=(SemVer left, SemVer right)
		{
			return Compare(left, right) <= 0;
		}

		public static bool operator >=(SemVer left, SemVer right)
		{
			return Compare(left, right) >= 0;
		}
	}
}