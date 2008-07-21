
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.VersionControl
{
	class VersionControlConfiguration
	{
		[ItemProperty ("Repositories")]
		List<Repository> repositories = new List<Repository> ();
		
		public List<Repository> Repositories {
			get { return repositories; }
		}
	}
}
