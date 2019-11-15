using System;

namespace Microsoft.WebTools.Scaffolding.Core
{
	/// <summary>
	/// Represents whether the project's dependencies fall under Long Term Support (LTS) 1.0,
	/// FTS 1.1, NetStandard2.0, etc.
	/// 
	/// LTS10 => The package versions should be 1.0.x
	/// FTS11 => The package versions should be 1.1.x
	/// NetStandard20 => The package versions should be 2.0.x
	/// NetStandard21 => The package versions should be 2.1.x
	/// Net22 => The package versions should be 2.2.x
	/// </summary>
	class SupportPolicyVersion : IComparable, IComparable<SupportPolicyVersion>
	{
		public static readonly SupportPolicyVersion LTS10 = new SupportPolicyVersion (new Version ("1.0.0"));
		public static readonly SupportPolicyVersion FTS11 = new SupportPolicyVersion (new Version ("1.1.0"));
		public static readonly SupportPolicyVersion NetStandard20 = new SupportPolicyVersion (new Version ("2.0.0"));
		public static readonly SupportPolicyVersion NetStandard21 = new SupportPolicyVersion (new Version ("2.1.0"));
		public static readonly SupportPolicyVersion Net220 = new SupportPolicyVersion (new Version ("2.2.0"));
		public static readonly SupportPolicyVersion UnSupported = new SupportPolicyVersion (null);

		public Version Version {
			get;
		}

		public static bool TryCreateFromVersionString (string versionString, out SupportPolicyVersion supportPolicyVersion)
		{
			if (Version.TryParse (versionString, out Version version)) {
				// always coerce into Major.Minor.Build, to match the dynamic policy identifiers in the config.
				int major = version.Major != -1 ? version.Major : 0;
				int minor = version.Minor != -1 ? version.Minor : 0;
				int build = version.Build != -1 ? version.Build : 0;

				Version normalizedVersion = new Version (major, minor, build);
				supportPolicyVersion = new SupportPolicyVersion (normalizedVersion);

				return true;
			}

			supportPolicyVersion = null;
			return false;
		}

		private SupportPolicyVersion (Version version)
		{
			Version = version;
		}

		public bool IsNewerOrSame (SupportPolicyVersion supportPolicyVersion)
		{
			if (supportPolicyVersion == null) {
				return true;
			}

			if (this == UnSupported) {
				return false;
			}

			return Version.CompareTo (supportPolicyVersion.Version) >= 0;
		}

		public int CompareTo (object obj)
		{
			if (obj == null) {
				return 1;
			}

			if (obj is SupportPolicyVersion otherVersion) {
				return Version.CompareTo (otherVersion.Version);
			} else {
				throw new ArgumentException ("Input object is not a SupportPolicyVersion");
			}
		}

		public bool Equals (SupportPolicyVersion otherPolicy)
		{
			return Version.Equals (otherPolicy.Version);
		}

		public override bool Equals (object otherPolicy)
		{
			return Equals (otherPolicy as SupportPolicyVersion);
		}

		public override int GetHashCode ()
		{
			return Version.GetHashCode ();
		}

		public int CompareTo (SupportPolicyVersion other)
		{
			return other.Version.CompareTo (Version);
		}

		public static bool operator == (SupportPolicyVersion s1, SupportPolicyVersion s2)
		{
			if (ReferenceEquals (s1, s2)) {
				return true;
			}

			if (ReferenceEquals (s1, null) || ReferenceEquals (s2, null)) {
				return false;
			}

			return s1.Equals (s2);
		}

		public static bool operator != (SupportPolicyVersion s1, SupportPolicyVersion s2)
		{
			return !(s1 == s2);
		}

		public static bool operator < (SupportPolicyVersion s1, SupportPolicyVersion s2)
		{
			if (s1 == null) {
				if (s2 == null) {
					return false;
				}

				return true;
			}

			return s1.CompareTo (s2) < 0;
		}

		public static bool operator > (SupportPolicyVersion s1, SupportPolicyVersion s2)
		{
			if (s1 == null) {
				return false;
			}

			return s1.CompareTo (s2) > 0;
		}

		public static bool operator <= (SupportPolicyVersion s1, SupportPolicyVersion s2)
		{
			return (s1 < s2) || (s1 == s2);
		}

		public static bool operator >= (SupportPolicyVersion s1, SupportPolicyVersion s2)
		{
			return (s1 > s2) || (s1 == s2);
		}
	}
}
