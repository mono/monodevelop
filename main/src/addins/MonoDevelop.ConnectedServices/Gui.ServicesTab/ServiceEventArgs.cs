using System;
namespace MonoDevelop.ConnectedServices.Gui
{
	public class ServiceEventArgs : EventArgs
	{
		public IConnectedService Service { get; private set; }
		
		public ServiceEventArgs (IConnectedService service)
		{
			Service = service;
		}
	}
}

