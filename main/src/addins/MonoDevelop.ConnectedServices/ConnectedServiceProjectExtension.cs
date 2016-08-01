using System;
using System.Xml;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Project extension that attaches to all projects and maintains state for the list of connected services that are applicable
	/// to the project.
	/// </summary>
	sealed class ConnectedServiceProjectExtension : DotNetProjectExtension, IConnectedServicesBinding
	{
		IConnectedService[] services;

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServicesProject"/> has services.
		/// </summary>
		public bool HasServices {
			get {
				return this.services != null && this.services.Length > 0;
			}
		}

		/// <summary>
		/// Gets the services that support the project
		/// </summary>
		/// <value>The services.</value>
		public IConnectedService [] Services { 
			get {
				return this.services;
			}
		}

		/// <summary>
		/// Handles when the project is initialised from a template and gathers the list of services that support this project
		/// </summary>
		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, template);
			this.services = ConnectedServices.GetServices (this.Project);
		}

		/// <summary>
		/// Handles the project being loaded and gathers the list of services that support this project
		/// </summary>
		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			this.services = ConnectedServices.GetServices (this.Project);
		}
	}
}