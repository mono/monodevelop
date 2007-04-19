
using System;

namespace MonoDevelop.Deployment
{
	public class DeployPlatformInfo
	{
		string id;
		string description;
		
		public DeployPlatformInfo (string id, string description)
		{
			this.id = id;
			this.description = description;
		}
		
		public string Id {
			get { return id; }
		}
		
		public string Description {
			get { return description; }
		}
		
		public override bool Equals (object o)
		{
			DeployPlatformInfo other = o as DeployPlatformInfo;
			return other != null && other.id == id;
		}
		
		public override int GetHashCode ()
		{
			return id.GetHashCode ();
		}
	}
}
