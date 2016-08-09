using System;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.Core;

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
			this.CanBeAdded = service.Dependencies.Length > 0 && !service.AreDependenciesInstalled;
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
		public bool IsAdded { get; }

		/// <summary>
		/// Occurs when the section is added to the project
		/// </summary>
		public event EventHandler<EventArgs> Added;

		/// <summary>
		/// Gets the widget to display to the user
		/// </summary>
		public Widget GetSectionWidget ()
		{
			return new DependenciesSectionWidget (this);
		}

		/// <summary>
		/// Adds the service dependencies to the project
		/// </summary>
		public async Task AddToProject ()
		{
			await this.Service.Project.AddDependencies (this.Service.Dependencies);
			this.NotifyAddedToProject ();
		}

		/// <summary>
		/// Invokes the Added event on the main thread
		/// </summary>
		void NotifyAddedToProject ()
		{
			var handler = this.Added;
			if (handler != null) {
				// make sure this gets called on the main thread in case we have async calls for adding a nuget
				Xwt.Application.Invoke (() => {
					handler (this, new EventArgs ());
				});
			}
		}
	}
}