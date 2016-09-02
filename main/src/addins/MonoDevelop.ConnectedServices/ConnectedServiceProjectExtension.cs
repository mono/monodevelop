using System;
using System.Linq;
using System.Xml;
using MonoDevelop.ConnectedServices.Gui.SolutionPad;
using MonoDevelop.PackageManagement;
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
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServicesProject"/> has any services that support the project.
		/// </summary>
		public bool HasSupportedServices {
			get {
				return this.services != null && this.services.Length > 0;
			}
		}

		/// <summary>
		/// Gets the services that support the project
		/// </summary>
		/// <value>The services.</value>
		public IConnectedService [] SupportedServices { 
			get {
				return this.services;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServicesProject"/> has any services that have been added.
		/// </summary>
		public bool HasAddedServices { 
			get {
				return this.services.Any(x => x.IsAdded);
			}
		}

		/// <summary>
		/// Gets or sets the services node that is shown in the solution pad.
		/// </summary>
		/// <remarks>
		/// Only the node builder should set this.
		/// </remarks>
		public ConnectedServiceFolderNode ServicesNode { get; set; }

		/// <summary>
		/// Handles when the project is initialised from a template and gathers the list of services that support this project
		/// </summary>
		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, template);
			UpdateServices ();
		}

		/// <summary>
		/// Handles the project being loaded and gathers the list of services that support this project
		/// </summary>
		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			UpdateServices ();
		}

		void UpdateServices ()
		{
			if (services != null) { // update might be called several times
				foreach (var service in services) {
					service.Added -= HandleServiceAddedRemoved;
					service.Removed -= HandleServiceAddedRemoved;
				}
			} else {
				PackageManagementServices.ProjectOperations.PackageReferenceAdded += HandlePackageReferenceAddedRemoved;
				PackageManagementServices.ProjectOperations.PackageReferenceRemoved += HandlePackageReferenceAddedRemoved;
			}
			services = ConnectedServices.GetServices (Project);
			foreach (var service in services) {
				service.Added += HandleServiceAddedRemoved;
				service.Removed += HandleServiceAddedRemoved;
			}
		}

		void HandleServiceAddedRemoved (object sender, EventArgs e)
		{
			if (ServicesNode != null)
				Core.Runtime.RunInMainThread (() => ServicesNode.NotifyServicesChanged ());
		}

		void HandlePackageReferenceAddedRemoved (object sender, PackageManagementPackageReferenceEventArgs e)
		{
			if (services == null || e.Project != Project)
				return;

			foreach (var service in services) {
				foreach (var dep in service.Dependencies.Where (d => d is PackageDependency).Cast<PackageDependency> ()) {
					if (dep.PackageId == e.PackageReference.Id)
						dep.HandlePackageStatusChanged ();
				}
			}
		}

		public override void Dispose ()
		{
			if (services != null)
				PackageManagementServices.ProjectOperations.PackageReferenceAdded -= HandlePackageReferenceAddedRemoved;
				PackageManagementServices.ProjectOperations.PackageReferenceRemoved -= HandlePackageReferenceAddedRemoved;
				foreach (var service in services) {
					service.Added -= HandleServiceAddedRemoved;
					service.Removed -= HandleServiceAddedRemoved;
				}
			base.Dispose ();
		}
	}
}