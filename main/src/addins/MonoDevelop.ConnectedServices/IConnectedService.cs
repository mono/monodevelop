using System;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.ConnectedServices.Gui.ServicesTab;
using MonoDevelop.Core;
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
		/// Gets the array of sections to be displayed to the user after the dependencies section.
		/// </summary>
		IConfigurationSection [] Sections { get; }

		/// <summary>
		/// Adds the service to the project
		/// </summary>
		void AddToProject ();
	}

	/// <summary>
	/// Represents a section to be displayed to the user in the service details page.
	/// Each section has the concept of being able to be "added" to the project, that is,
	/// to perform some action on the project when 'AddToProject' is invoked. It is the responsibility of
	/// the section to store state appropriately and to return `IsAdded` as required. The Added event should
	/// be trigered once the task has been completed.
	/// 
	/// Sections that do not perform an action on the project can return false from CanBeAdded.
	/// </summary>
	public interface IConfigurationSection 
	{
		/// <summary>
		/// Gets the service for this section
		/// </summary>
		IConnectedService Service { get; }

		/// <summary>
		/// Gets the name of the section to display to the user.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the description of the section to display to the user.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets a value indidating if this section represents something that can be added to the project.
		/// </summary>
		bool CanBeAdded { get; }

		/// <summary>
		/// Gets a value indicating that whatever changes to the project that can be added by this section have been added.
		/// </summary>
		bool IsAdded { get; }

		/// <summary>
		/// Gets the widget to display to the user
		/// </summary>
		Widget GetSectionWidget ();

		/// <summary>
		/// Performs the tasks necessary to add the components that this section represents to the project
		/// </summary>
		Task AddToProject ();

		/// <summary>
		/// Occurs when the section is added to the project
		/// </summary>
		event EventHandler<EventArgs> Added;
	}

	/// <summary>
	/// Builtin section object that displays the dependencies for the service
	/// </summary>
	sealed class DependenciesSection: IConfigurationSection
	{
		public DependenciesSection (IConnectedService service)
		{
			this.Service = service;
			this.DisplayName = GettextCatalog.GetString ("Dependencies");
			this.CanBeAdded = service.Dependencies.Length > 0;
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
		/// Performs the tasks necessary to add the components that this section represents to the project
		/// </summary>
		public Task AddToProject ()
		{
			// TODO: foreach dependency that's not added, add the nuget package to the project
			foreach (var dep in this.Service.Dependencies) {
			}

			this.NotifyAddedToProject ();

			return Task.FromResult (true);
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

	/// <summary>
	/// Abstract implementation of IConfigurationSection.
	/// </summary>
	public abstract class ConfigurationSection : IConfigurationSection
	{
		public static readonly IConfigurationSection [] Empty = new IConfigurationSection [0];

		protected ConfigurationSection (IConnectedService service, string displayName, bool canBeAdded = true)
		{
			this.Service = service;
			this.DisplayName = displayName;
			this.CanBeAdded = canBeAdded;
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
		public abstract Widget GetSectionWidget ();

		/// <summary>
		/// Performs the tasks necessary to add the components that this section represents to the project
		/// </summary>
		public async Task AddToProject ()
		{
			await this.OnAddToProject ();
			this.NotifyAddedToProject ();
		}

		protected abstract Task OnAddToProject ();

		/// <summary>
		/// Invokes the Added event on the main thread
		/// </summary>
		protected void NotifyAddedToProject ()
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

	/// <summary>
	/// Basic implementation of a ConfigurationSection that represents a getting started section that display code snippets to the user
	/// </summary>
	public abstract class GettingStartedConfigurationSection : ConfigurationSection
	{
		protected GettingStartedConfigurationSection (IConnectedService service, int snippetCount) : base (service, GettextCatalog.GetString (ConnectedServices.GettingStartedSectionDisplayName), false)
		{
			this.SnippetCount = snippetCount;
		}

		public int SnippetCount { get; private set; }

		public override Widget GetSectionWidget ()
		{
			// TODO: create a wiget that contains the sections of code snippets that have been set in tabs
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Gets the code snippet title to show for the given snippet index
		/// </summary>
		protected virtual string GetSnippetTitle (int snippet)
		{
			return GettextCatalog.GetString ("Snippet {0}", snippet);
		}

		/// <summary>
		/// Gets the code snippet to show for the given snippet index
		/// </summary>
		protected virtual string GetSnippet(int snippet)
		{
			return string.Empty;
		}
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