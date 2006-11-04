
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects.Serialization;

namespace VersionControl.Service
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
