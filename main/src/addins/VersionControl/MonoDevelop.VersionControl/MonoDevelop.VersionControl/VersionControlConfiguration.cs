
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.VersionControl
{
	class VersionControlConfiguration
	{
		[ItemProperty ("Repositories")]
		readonly List<Repository> repositories = new List<Repository> ();
		
		public List<Repository> Repositories {
			get { return repositories; }
		}

		[ItemProperty]
		public bool Disabled;
	}
}
