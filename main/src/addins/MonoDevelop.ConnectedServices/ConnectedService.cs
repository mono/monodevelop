using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Base class for implementing IConnectedService. It stores state in a .json file that is created in a sub-folder of the project
	/// </summary>
	public abstract class ConnectedService : IConnectedService
	{
		/// <summary>
		/// Empty array of IConnectedService
		/// </summary>
		public static readonly IConnectedService[] Empty = new IConnectedService[0];

		protected ConnectedService (DotNetProject project)
		{
			this.Project = project;
			this.Dependencies = ConnectedServiceDependency.Empty;
			this.Sections = ConfigurationSection.Empty;
			this.DependenciesSection = new DependenciesSection (this);
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
		/// Gets a description of the supported platforms. This is largely just informational as the service provider decides
		/// whether a project is supported or not.
		/// </summary>
		public string SupportedPlatforms { get; protected set; }

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
		public bool IsAdded {
			get {
				return this.GetIsAddedToProject ();
			}
		}

		/// <summary>
		/// Gets the dependencies that will be added to the project
		/// </summary>
		public IConnectedServiceDependency [] Dependencies { get; protected set; }

		/// <summary>
		/// Gets the dependencies section to be displayed before the configuration section
		/// </summary>
		public IConfigurationSection DependenciesSection { get; protected set; }

		/// <summary>
		/// Gets a value indicating whether ALL the depenencies are installed.
		/// </summary>
		public bool AreDependenciesInstalled {
			get {
				return this.Dependencies.All (x => x.IsAdded);
			}
		}

		/// <summary>
		/// Gets the array of sections to be displayed to the user after the dependencies section.
		/// </summary>
		public IConfigurationSection [] Sections { get; protected set; }

		/// <summary>
		/// Occurs before the service is added to the project;
		/// </summary>
		public event EventHandler<EventArgs> Adding;

		/// <summary>
		/// Occurs when adding the service to the project has failed;
		/// </summary>
		public event EventHandler<EventArgs> AddingFailed;

		/// <summary>
		/// Occurs when service is added to the project;
		/// </summary>
		public event EventHandler<EventArgs> Added;

		/// <summary>
		/// Occurs before the service is removed from the project;
		/// </summary>
		public event EventHandler<EventArgs> Removing;

		/// <summary>
		/// Occurs when removing the service from the project has failed;
		/// </summary>
		public event EventHandler<EventArgs> RemovingFailed;

		/// <summary>
		/// Occurs when service has been removed from the project;
		/// </summary>
		public event EventHandler<EventArgs> Removed;

		/// <summary>
		/// Adds the service to the project
		/// </summary>
		public async Task<bool> AddToProject ()
		{
			if (this.IsAdded) {
				LoggingService.LogWarning ("Skipping adding of the service, it has already been added");
				return true;
			}

			// TODO: add ProgressMonitor support ? 
			this.NotifyServiceAdding ();

			try {
				await this.AddDependencies (CancellationToken.None).ConfigureAwait (false);
				await this.OnAddToProject ().ConfigureAwait (false);
				await this.StoreAddedState ().ConfigureAwait (false);

				this.NotifyServiceAdded ();
				return true;
			} catch (Exception ex) {
				LoggingService.LogError ("An error occurred while adding the service to the project", ex);
				NotifyServiceAddingFailed ();
				return false;
			}
		}

		/// <summary>
		/// Removes the service from the project
		/// </summary>
		public async Task<bool> RemoveFromProject () 
		{
			if (!this.IsAdded) {
				LoggingService.LogWarning ("Skipping removing of the service, it is not added to the project");
				return true;
			}

			// TODO: add ProgressMonitor support ? 

			this.NotifyServiceRemoving ();

			// try to remove the dependencies first, these may sometimes fail if one of the packages is or has dependencies
			// that other packages are dependent upon. A common one might be Json.Net for instance
			// we need to allow for this and continue on afterwards.
			var dependenciesFailed = false;
			try {
				await this.RemoveDependencies (CancellationToken.None).ConfigureAwait (false);
			} catch (Exception ex) {
				LoggingService.LogError ("An error occurred while removing the service dependencies from the project", ex);
				dependenciesFailed = true;
			}

			// right, now remove the service
			try {
				await this.OnRemoveFromProject ().ConfigureAwait (false);
				await this.RemoveAddedState ().ConfigureAwait (false);
				this.NotifyServiceRemoved ();

				if (dependenciesFailed) {
					// TODO: notify the user about the dependencies that failed, we might already have this from the package console and that might be enough for now
				}

				return true;
			} catch (Exception ex) {
				LoggingService.LogError ("An error occurred while removing the service from the project", ex);
				NotifyServiceRemovingFailed ();
				return false;
			}
		}

		/// <summary>
		/// Determines if the service has been added to the project.
		/// </summary>
		protected virtual bool GetIsAddedToProject()
		{
			return false;
		}

		/// <summary>
		/// Performs the logic of removing the service from the project. This is called after the dependencies have been removed.
		/// </summary>
		protected virtual Task OnRemoveFromProject()
		{
			return Task.FromResult (true);
		}

		/// <summary>
		/// Performs the logic of adding the service to the project. This is called after the dependencies have been added.
		/// </summary>
		protected virtual Task OnAddToProject ()
		{
			return Task.FromResult (true);
		}

		/// <summary>
		/// Adds the dependencies to the project
		/// </summary>
		protected virtual async Task<bool> AddDependencies(CancellationToken token)
		{
			return await DependenciesSection.AddToProject (token);
		}

		/// <summary>
		/// Removes the dependencies from the project
		/// </summary>
		protected virtual async Task RemoveDependencies (CancellationToken token)
		{
			// ask all the dependencies to add themselves to the project
			// we'll do them one at a time in case there are interdependencies between them
			foreach (var dependency in this.Dependencies.Reverse ()) {
				try {
					await dependency.RemoveFromProject (token).ConfigureAwait (false);
				} catch (Exception ex) {
					LoggingService.LogError ("Could not remove dependency", ex);
					throw;
				}
			}
		}

		/// <summary>
		/// Stores some state that the service has been added to the project.
		/// </summary>
		protected virtual Task StoreAddedState()
		{
			return IdeApp.ProjectOperations.SaveAsync (this.Project);
		}

		/// <summary>
		/// Stores some state that the service has been added to the project.
		/// </summary>
		protected virtual Task RemoveAddedState ()
		{
			return IdeApp.ProjectOperations.SaveAsync (this.Project);
		}

		/// <summary>
		/// Notifies subscribers that the service will be added to the project
		/// </summary>
		void NotifyServiceAdding ()
		{
			var handler = this.Adding;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that adding the service to the project has failed
		/// </summary>
		void NotifyServiceAddingFailed ()
		{
			var handler = this.AddingFailed;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that the service has been added to the project
		/// </summary>
		void NotifyServiceAdded()
		{
			var handler = this.Added;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that the service will be removed from the project
		/// </summary>
		void NotifyServiceRemoving ()
		{
			var handler = this.Removing;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that removing the service from the project has failed
		/// </summary>
		void NotifyServiceRemovingFailed ()
		{
			var handler = this.RemovingFailed;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that the service has been removed from the project
		/// </summary>
		void NotifyServiceRemoved ()
		{
			var handler = this.Removed;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}
	}
}