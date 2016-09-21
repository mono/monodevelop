using System;
using System.Threading.Tasks;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Threading;
using System.Linq;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Builtin section object that displays the dependencies for the service
	/// </summary>
	sealed class DependenciesSection: IConfigurationSection
	{
		bool isAdded;
		bool initialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.DependenciesSection"/> class.
		/// </summary>
		public DependenciesSection (IConnectedService service)
		{
			this.Service = service;
			this.DisplayName = GettextCatalog.GetString ("Dependencies");
			// dependencies are added when the service is added, therefore it's not really optional
			this.CanBeAdded = false;

			this.isAdded = this.Service.AreDependenciesInstalled;

			Service.StatusChanged += HandleServiceStatusChanged;
		}

		/// <summary>
		/// Gets the service for this section
		/// </summary>
		public IConnectedService Service { get; private set; }

		/// <summary>
		/// Gets the name of the section to display to the user.
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets the description of the section to display to the user.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Gets a value indidating if this section represents something that can be added to the project.
		/// </summary>
		public bool CanBeAdded { get; private set; }

		/// <summary>
		/// Gets a value indicating that whatever changes to the project that can be added by this section have been added.
		/// </summary>
		public bool IsAdded {
			get {
				if (!initialized) {
					isAdded = this.Service.AreDependenciesInstalled;
					initialized = true;
				}
				return isAdded;
			}
			private set {
				if (isAdded != value) {
					isAdded = value;
					if (isAdded)
						NotifyAddedToProject ();
					else
						NotifyRemovedFromProject ();
				}
			}
		}

		/// <summary>
		/// Occurs when the status of the section changes
		/// </summary>
		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		/// <summary>
		/// Gets the widget to display to the user
		/// </summary>
		public Control GetSectionWidget ()
		{
			return new DependenciesSectionWidget (this);
		}

		/// <summary>
		/// Adds the service dependencies to the project
		/// </summary>
		public async Task<bool> AddToProject (CancellationToken token)
		{
			this.NotifyAddingToProject ();

			// ask all the dependencies to add themselves to the project
			// we'll do them one at a time in case there are interdependencies between them

			// we are going to short circuit package dependencies though and install them in one go
			var packages = this.Service.Dependencies.OfType<PackageDependency> ().Cast<IPackageDependency>().ToList();
			await this.Service.Project.AddPackageDependencies (packages).ConfigureAwait (false);

			try {
				foreach (var dependency in Service.Dependencies) {
					if (packages.Contains (dependency)) {
						continue;
					}

					await dependency.AddToProject (token).ConfigureAwait (false);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not add dependency", ex);
				NotifyAddingToProjectFailed ();
				return IsAdded = false;
			}
			return IsAdded = true;
		}

		void HandleServiceStatusChanged (object sender, StatusChangedEventArgs e)
		{
			// update the status when the service is removed
			if (e.WasRemoved) {
				IsAdded = this.Service.AreDependenciesInstalled;
			}
		}

		/// <summary>
		/// Invokes the Adding event on the main thread
		/// </summary>
		void NotifyAddingToProject ()
		{
			this.NotifyStatusChange (Status.Adding, Status.NotAdded);
		}

		/// <summary>
		/// Notifies subscribers that adding the dependencies to the project has failed
		/// </summary>
		void NotifyAddingToProjectFailed ()
		{
			this.NotifyStatusChange (Status.NotAdded, Status.Adding);
		}

		/// <summary>
		/// Notifies subscribers that all dependencies have been added to the project
		/// </summary>
		void NotifyAddedToProject ()
		{
			this.NotifyStatusChange (Status.Added, Status.Adding);
		}

		/// <summary>
		/// Notifies subscribers that a dependency has been removed from the project
		/// </summary>
		void NotifyRemovedFromProject ()
		{
			this.NotifyStatusChange (Status.NotAdded, Status.Removing);
		}

		void NotifyStatusChange(Status newStatus, Status oldStatus)
		{
			var handler = this.StatusChanged;
			if (handler != null) {
				// make sure this gets called on the main thread in case we have async calls for adding a nuget
				Xwt.Application.Invoke (() => {
					handler (this, new StatusChangedEventArgs (newStatus, oldStatus, null));
				});
			}
		}

		internal void HandleDependenciesChanged ()
		{
			IsAdded = this.Service.AreDependenciesInstalled;
		}
	}
}