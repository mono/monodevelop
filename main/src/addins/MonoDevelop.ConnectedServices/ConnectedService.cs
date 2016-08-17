using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Newtonsoft.Json;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Base class for implementing IConnectedService
	/// </summary>
	public abstract class ConnectedService : IConnectedService
	{
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
				return HasConnectedServiceJsonFile (this.Project, this.Id);
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
		/// Occurs when service is added to the project;
		/// </summary>
		public event EventHandler<EventArgs> Added;

		/// <summary>
		/// Adds the service to the project
		/// </summary>
		public async Task AddToProject ()
		{
			try {
				if (HasConnectedServiceJsonFile (this.Project, this.Id)) {
					LoggingService.LogWarning ("Skipping adding of the service, it has already been added");
					return;
				}
				this.NotifyServiceAdding ();
				// TODO: add ProgressMonitor support and cancellation

				await this.AddDependencies (CancellationToken.None).ConfigureAwait (false);
				await this.OnAddToProject ().ConfigureAwait (false);
				this.StoreAddedState ();
				this.NotifyServiceAdded ();

				// TODO: not here, but somewhere, we need to refresh the sln pad.

			} catch (Exception ex) {
				LoggingService.LogError ("An error occurred while adding the service to the project", ex);
			}
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
		/// Creates a new object for storing state about the service in
		/// </summary>
		protected virtual ConnectedServiceState CreateStateObject()
		{
			return new ConnectedServiceState ();
		}

		/// <summary>
		/// Stores some state that the service has been added to the project.
		/// </summary>
		protected void StoreAddedState()
		{
			var state = this.CreateStateObject();
			this.OnStoreAddedState (state);
			WriteConnectedServiceJsonFile (this.Project, this.Id, state);
		}

		/// <summary>
		/// Modifies the state object to set any service specific values.
		/// </summary>
		/// <remarks>
		/// The api for this might change, we're introducing a "ProvideId" that appears to have little to do with the Id of 
		/// the service as well as a Version and GettingStartedUrl that may or may not be relevant at this point. 
		/// Override this method to store what you need into the state object.
		/// </remarks>
		protected virtual void OnStoreAddedState(ConnectedServiceState state)
		{
			state.ProviderId = this.Id;
		}

		/// <summary>
		/// Writes a ConnectedServices.json file in the connected services folder of the project for the given service, overwriting the file if it exists already
		/// </summary>
		internal static void WriteConnectedServiceJsonFile(DotNetProject project, string id, ConnectedServiceState state)
		{
			var jsonFilePath = GetConnectedServiceJsonFilePath (project, id, true);
			var json = JsonConvert.SerializeObject (state, Formatting.Indented);

			if (File.Exists (jsonFilePath)) {
				File.Delete (jsonFilePath);
			}

			File.WriteAllText (jsonFilePath, json);
		}

		/// <summary>
		/// Gets whether or not a ConnectedService.json file exists for the given projec and service id.
		/// </summary>
		internal static bool HasConnectedServiceJsonFile (DotNetProject project, string id)
		{
			var jsonFilePath = GetConnectedServiceJsonFilePath (project, id, false);
			return File.Exists (jsonFilePath);
		}

		/// <summary>
		/// Gets the file path for the ConnectedService.json file for the given project and optionally ensures that the folder
		/// that the file should reside in exists.
		/// </summary>
		static string GetConnectedServiceJsonFilePath(DotNetProject project, string id, bool ensureFolderExists)
		{
			var dir = project.BaseDirectory.Combine (ConnectedServices.ProjectStateFolderName).Combine (id);
			if (ensureFolderExists) {
				Directory.CreateDirectory (dir);
			}

			return dir.Combine (ConnectedServices.ConnectedServicesJsonFileName);
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
		/// Notifies subscribers that the service has been added to the project
		/// </summary>
		void NotifyServiceAdded()
		{
			var handler = this.Added;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}
	}
}