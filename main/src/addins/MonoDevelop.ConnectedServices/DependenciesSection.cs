using System;
using System.Threading.Tasks;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Threading;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Builtin section object that displays the dependencies for the service
	/// </summary>
	sealed class DependenciesSection: IConfigurationSection
	{
		public DependenciesSection (IConnectedService service)
		{
			this.Service = service;
			this.DisplayName = GettextCatalog.GetString ("Dependencies");
			// dependencies are added when the service is added, therefore it's not really optional
			this.CanBeAdded = false;

			Service.Removed += HandleServiceRemoved;
		}

		void HandleServiceRemoved (object sender, EventArgs e)
		{
			if (!IsAdded)
				NotifyRemovedFromProject ();
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
		public bool IsAdded { get { return this.Service.AreDependenciesInstalled; } }

		/// <summary>
		/// Occurs when the section is added to the project
		/// </summary>
		public event EventHandler<EventArgs> Added;

		/// <summary>
		/// Occurs when adding dependencies to the project has failed
		/// </summary>
		public event EventHandler<EventArgs> AddingFailed;

		/// <summary>
		/// Occurs before the section is added to the project
		/// </summary>
		public event EventHandler<EventArgs> Adding;

		/// <summary>
		/// Occurs when the section has been removed from the project
		/// </summary>
		public event EventHandler<EventArgs> Removed;

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
		public async Task<bool> AddToProject (bool licensesAccepted, CancellationToken token)
		{
			this.NotifyAddingToProject ();

			// ask all the dependencies to add themselves to the project
			// we'll do them one at a time in case there are interdependencies between them
			foreach (var dependency in Service.Dependencies) {
				try {
					await dependency.AddToProject (licensesAccepted, token).ConfigureAwait (false);
				} catch (Exception ex) {
					LoggingService.LogError ("Could not add dependency", ex);
					NotifyAddingToProjectFailed ();
					return false;
				}
			}
			NotifyAddedToProject ();
			return true;
		}

		/// <summary>
		/// Invokes the Adding event on the main thread
		/// </summary>
		void NotifyAddingToProject ()
		{
			var handler = this.Adding;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that adding the dependencies to the project has failed
		/// </summary>
		void NotifyAddingToProjectFailed ()
		{
			var handler = this.AddingFailed;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that all dependencies have been added to the project
		/// </summary>
		void NotifyAddedToProject ()
		{
			var handler = this.Added;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Notifies subscribers that a dependency has been removed from the project
		/// </summary>
		void NotifyRemovedFromProject ()
		{
			var handler = this.Removed;
			if (handler != null) {
				handler (this, new EventArgs ());
			}
		}
	}
}