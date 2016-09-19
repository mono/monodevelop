using System;
using System.Collections.Immutable;
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

		string solutionPadDisplayName;
		Status status = (Status)(-1);

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.ConnectedService"/> class.
		/// </summary>
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
		/// Gets the display name of the service to show to the user in the solution pad
		/// </summary>
		public string SolutionPadDisplayName {
			get {
				if (this.solutionPadDisplayName != null)
					return solutionPadDisplayName;

				return this.DisplayName;
			}

			set {
				solutionPadDisplayName = value;
			}
		}

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
		/// Gets the current status of the service.
		/// </summary>
		public Status Status {
			get {
				if ((int)status == -1)
					status = this.GetIsAddedToProject() ? Status.Added : Status.NotAdded;
				return status;
			}
		}

		/// <summary>
		/// Gets the dependencies that will be added to the project
		/// </summary>
		public ImmutableArray<IConnectedServiceDependency> Dependencies { get; protected set; }

		/// <summary>
		/// Gets the dependencies section to be displayed before the configuration section
		/// </summary>
		public IConfigurationSection DependenciesSection { get; protected set; }

		/// <summary>
		/// Gets a value indicating whether ALL the depenencies are installed.
		/// </summary>
		public bool AreDependenciesInstalled {
			get {
				return this.Dependencies.All (x => x.Status == Status.Added);
			}
		}

		/// <summary>
		/// Gets the array of sections to be displayed to the user after the dependencies section.
		/// </summary>
		public ImmutableArray<IConfigurationSection> Sections { get; protected set; }

		/// <summary>
		/// Occurs when the status of the service has changed.
		/// </summary>
		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		/// <summary>
		/// Adds the service to the project
		/// </summary>
		public async Task<bool> AddToProject ()
		{
			if (this.GetIsAddedToProject()) {
				LoggingService.LogWarning ("Skipping adding of the service, it has already been added");
				return true;
			}

			this.ChangeStatus (Status.Adding);

			try {
				await this.AddDependencies (CancellationToken.None).ConfigureAwait (false);
				await this.OnAddToProject ().ConfigureAwait (false);
				await this.StoreAddedState ().ConfigureAwait (false);

				this.ChangeStatus (this.GetIsAddedToProject() ? Status.Added : Status.NotAdded);
				return true;
			} catch (Exception ex) {
				LoggingService.LogError ("An error occurred while adding the service to the project", ex);
				this.ChangeStatus (Status.NotAdded, ex);
				return false;
			}
		}

		/// <summary>
		/// Removes the service from the project
		/// </summary>
		public async Task<bool> RemoveFromProject () 
		{
			if (!this.GetIsAddedToProject()) {
				LoggingService.LogWarning ("Skipping removing of the service, it is not added to the project");
				return true;
			}

			this.ChangeStatus (Status.Removing);

			// try to remove the dependencies first, these may sometimes fail if one of the packages is or has dependencies
			// that other packages are dependent upon. A common one might be Json.Net for instance
			// we need to allow for this and continue on afterwards.
			var dependenciesFailed = false;
			try {
				await this.RemoveDependencies (CancellationToken.None).ConfigureAwait (false);
			} catch (Exception) {
				LoggingService.LogError ("An error occurred while removing the service dependencies from the project");
				dependenciesFailed = true;
			}

			// right, now remove the service
			try {
				await this.OnRemoveFromProject ().ConfigureAwait (false);
				await this.RemoveAddedState ().ConfigureAwait (false);
				this.ChangeStatus (Status.NotAdded);

				if (dependenciesFailed) {
					// TODO: notify the user about the dependencies that failed, we might already have this from the package console and that might be enough for now
				}

				return true;
			} catch (Exception ex) {
				LoggingService.LogError ("An error occurred while removing the service from the project", ex);
				this.ChangeStatus (Status.Added, ex);
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
		/// Changes the status of the service and notifies subscribers
		/// </summary>
		void ChangeStatus(Status newStatus, Exception error = null)
		{
			var oldStatus = this.Status;
			this.status = newStatus;
			this.NotifyStatusChanged (newStatus, oldStatus, error);
		}

		/// <summary>
		/// Notifies subscribers that the service status has changed
		/// </summary>
		void NotifyStatusChanged(Status newStatus, Status oldStatus, Exception error = null)
		{
			var handler = this.StatusChanged;
			if (handler != null) {
				handler (this, new StatusChangedEventArgs (newStatus, oldStatus, error));
			}
		}
	}
}