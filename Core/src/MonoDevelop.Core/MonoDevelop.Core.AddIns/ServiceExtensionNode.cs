
using System;
using Mono.Addins;

namespace MonoDevelop.Core.Addins
{
	public class ServiceExtensionNode: TypeExtensionNode
	{
		protected override void OnAddinLoaded ()
		{
			ServiceManager.AddService ((IService) base.CreateInstance ());
		}
	}
}
