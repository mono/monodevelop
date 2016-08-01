using System;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// A specific instance of a connected service for a given project
	/// </summary>
	public interface IConnectedService
	{
		/// <summary>
		/// Gets the Id of the service
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets the display name of the service to show to the user in the solution pad
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the description of the service to display to the user in the services gallery.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets the project that this service instance is attached to
		/// </summary>
		DotNetProject Project { get; }

		/// <summary>
		/// Gets the icon to display in the services gallery.
		/// </summary>
		Xwt.Drawing.Image GalleryIcon { get; }

		// TODO: the following methods are a guide only at this point

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedService"/> is added to Project or not.
		/// This is independent of whether or not the dependencies are installed or the service has been configured or not. It does imply that 
		/// any code scaffolding that can be done has been done.
		/// </summary>
		bool IsAdded { get; }

		/// <summary>
		/// Gets the dependencies that will be added to the project
		/// </summary>
		IConnectedServiceDependency [] Dependencies { get; }

		/// <summary>
		/// Gets a value indicating whether ALL the depenencies are installed.
		/// </summary>
		bool AreDependenciesInstalled { get; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedService"/> is configured or not.
		/// </summary>
		bool IsConfigured { get; }

		/// <summary>
		/// Creates and returns the widget to display in the Configuration section of the service details view.
		/// </summary>
		object GetConfigurationWidget ();

		/// <summary>
		/// Creates and returns the widget to display in the Getting Started section of the service details view.
		/// </summary>
		object GetGettingStartedWidget ();
	}

	// TODO: WIP
	public interface IConnectedServiceDependency
	{
		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the nuget package id of the dependency that is added to the project
		/// </summary>
		string PackageId { get; }

		// TODO: Package Version ??

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		bool IsAdded { get; }
	}

	// TODO: WIP
	public abstract class ConnectedServiceDependency : IConnectedServiceDependency
	{
		public static IConnectedServiceDependency [] Empty = new IConnectedServiceDependency [0];

		protected ConnectedServiceDependency (string id, string displayName)
		{
			this.PackageId = id;
			this.DisplayName = displayName;
		}

		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets the nuget package id of the dependency that is added to the project
		/// </summary>
		public string PackageId { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		public virtual bool IsAdded {
			get {
				// TODO: we can check the project and determine if the project has the package installed
				// this may mean we want to grab a reference to the project here
				return false;
			}
		}
	}
}