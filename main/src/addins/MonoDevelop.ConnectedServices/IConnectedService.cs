using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
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
		/// Gets the display name of the service to show to the user
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the display name of the service to show to the user in the solution pad
		/// </summary>
		string SolutionPadDisplayName { get; }

		/// <summary>
		/// Gets the description of the service to display to the user in the services gallery.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets the description of the service to display to the user in the service details tab.
		/// </summary>
		string DetailsDescription { get; }

		/// <summary>
		/// Gets a description of the supported platforms. This is largely just informational as the service provider decides
		/// whether a project is supported or not.
		/// </summary>
		string SupportedPlatforms { get; }

		/// <summary>
		/// Gets the project that this service instance is attached to
		/// </summary>
		DotNetProject Project { get; }

		/// <summary>
		/// Gets the icon to display in the services gallery.
		/// </summary>
		Xwt.Drawing.Image GalleryIcon { get; }

		/// <summary>
		/// Gets the dependencies that will be added to the project
		/// </summary>
		ImmutableArray<IConnectedServiceDependency> Dependencies { get; }

		/// <summary>
		/// Gets the dependencies section to be displayed before the configuration section
		/// </summary>
		IConfigurationSection DependenciesSection { get; }

		/// <summary>
		/// Gets a value indicating whether ALL the depenencies are installed.
		/// </summary>
		bool AreDependenciesInstalled { get; }

		/// <summary>
		/// Gets the array of sections to be displayed to the user after the dependencies section.
		/// </summary>
		ImmutableArray<IConfigurationSection> Sections { get; }

		/// <summary>
		/// Gets the current status of the service.
		/// This is independent of whether or not the dependencies are installed or the service has been configured or not. 
		/// If the status is Added, it does imply that any code scaffolding that can be done has been done.
		/// </summary>
		Status Status { get; }

		/// <summary>
		/// Occurs when the status of the service has changed.
		/// </summary>
		event EventHandler<StatusChangedEventArgs> StatusChanged;

		/// <summary>
		/// Adds the service to the project
		/// </summary>
		/// <returns> <c>true</c> if the service has been added successfully; otherwise <c>false</c> </returns>
		Task<bool> AddToProject ();

		/// <summary>
		/// Removes the service from the project
		/// </summary>
		/// <returns> <c>true</c> if the service has been removed successfully; otherwise <c>false</c> </returns>
		Task<bool> RemoveFromProject ();
	}
}