
using System;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	
	public interface IToolboxLoader
	{
		//whether this loader should be remoted in another AppDomain
		bool ShouldIsolate {
			get;
		}
		
		//comma-separated extensions
		string [] FileTypes {
			get;
		}
		
		System.Collections.Generic.IList<ItemToolboxNode> Load (string filename);
	}
}
