
using System;

namespace MonoDevelop.Deployment
{
	[System.ComponentModel.Editor (typeof(DeployDirectoryInfoEditor), typeof(MonoDevelop.Components.PropertyGrid.PropertyEditorCell))]
	public class DeployDirectoryInfo
	{
		string id;
		string description;
		
		public DeployDirectoryInfo (string id, string description)
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
			DeployDirectoryInfo other = o as DeployDirectoryInfo;
			return other != null && other.id == id;
		}
		
		public override int GetHashCode ()
		{
			return id.GetHashCode ();
		}
	}
}
