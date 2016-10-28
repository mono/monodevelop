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
		bool initialized;

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
				if (!initialized) {
					// HACK: ProjectManagementService may be not initialized during the extension point initialization
					//       therefore we subscribe to its events when the services are requested for the first time.
					PackageManagementServices.ProjectOperations.PackageReferenceAdded += HandlePackageReferenceAddedRemoved;
					PackageManagementServices.ProjectOperations.PackageReferenceRemoved += HandlePackageReferenceAddedRemoved;
					initialized = true;
				}
				return this.services;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServicesProject"/> has any services that have been added.
		/// </summary>
		public bool HasAddedServices { 
			get {
				return this.services.Any(x => x.Status == Status.Added);
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
					service.StatusChanged -= HandleServiceStatusChanged;
				}
			}
			services = ConnectedServices.GetServices (Project);
			foreach (var service in services) {
				service.StatusChanged += HandleServiceStatusChanged;
			}
		}

		void HandleServiceStatusChanged (object sender, StatusChangedEventArgs e)
		{
			if ((e.WasAdded || e.WasRemoved) && ServicesNode != null) {
				Core.Runtime.RunInMainThread (() => ServicesNode.NotifyServicesChanged ());
			}
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
			if (initialized) {
				PackageManagementServices.ProjectOperations.PackageReferenceAdded -= HandlePackageReferenceAddedRemoved;
				PackageManagementServices.ProjectOperations.PackageReferenceRemoved -= HandlePackageReferenceAddedRemoved;
			}
			if (services != null) {
				foreach (var service in services) {
					service.StatusChanged -= HandleServiceStatusChanged;
				}
			}
			base.Dispose ();
		}
	}
}