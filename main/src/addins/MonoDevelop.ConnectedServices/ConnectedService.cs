using System;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Base class for implementing IConnectedService
	/// </summary>
	public abstract class ConnectedService : IConnectedService
	{
		protected ConnectedService (DotNetProject project)
		{
			this.Project = project;
			this.Dependencies = ConnectedServiceDependency.Empty;
		}

		/// <summary>
		/// Gets the Id of the service
		/// </summary>
		public string Id { get; protected set; }

		/// <summary>
		/// Gets the display name of the service to show to the user in the solution pad
		/// </summary>
		public string DisplayName { get; protected set; }

		/// <summary>
		/// Gets the description of the service to display to the user in the services gallery.
		/// </summary>
		public string Description { get; protected set; }

		/// <summary>
		/// Gets the project that this service instance is attached to
		/// </summary>
		public DotNetProject Project { get; private set; }

		/// <summary>
		/// Gets the icon to display in the services gallery.
		/// </summary>
		public Xwt.Drawing.Image GalleryIcon { get; protected set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedService"/> is added to Project or not.
		/// This is independent of whether or not the dependencies are installed or the service has been configured or not. It does imply that 
		/// any code scaffolding that can be done has been done.
		/// </summary>
		public abstract bool IsAdded { get; }

		/// <summary>
		/// Gets the dependencies that will be added to the project
		/// </summary>
		public IConnectedServiceDependency [] Dependencies { get; protected set; }

		/// <summary>
		/// Gets a value indicating whether ALL the depenencies are installed.
		/// </summary>
		public bool AreDependenciesInstalled {
			get {
				return this.Dependencies.All (x => x.IsAdded);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedService"/> is configured or not.
		/// </summary>
		public abstract bool IsConfigured { get; }

		/// <summary>
		/// Creates and returns the widget to display in the Configuration section of the service details view.
		/// </summary>
		public abstract object GetConfigurationWidget ();

		/// <summary>
		/// Creates and returns the widget to display in the Getting Started section of the service details view.
		/// </summary>
		public abstract object GetGettingStartedWidget ();
	}
}