using System;
using MonoDevelop.ConnectedServices.Gui.SolutionPad;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Provides a default implementation of IConnectedServicesBinding for when there is no project (shared projects)
	/// </summary>
	sealed class NullConnectedServicesBinding : IConnectedServicesBinding
	{
		public static readonly IConnectedServicesBinding Null = new NullConnectedServicesBinding ();

		private NullConnectedServicesBinding ()
		{
		}

		public bool HasAddedServices {
			get { return false; }
		}

		public bool HasSupportedServices {
			get { return false; }
		}

		public DotNetProject Project { 
			get { return null; }
		}

		public IConnectedService [] SupportedServices {
			get { return ConnectedService.Empty; }
		}

		public ConnectedServiceFolderNode ServicesNode { get; set; }
	}
}