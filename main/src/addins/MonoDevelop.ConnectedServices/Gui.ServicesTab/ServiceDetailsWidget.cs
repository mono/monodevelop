using System;
using Gtk;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Widget that displays the service details
	/// </summary>
	class ServiceDetailsWidget : VBox
	{
		/// <summary>
		/// A service that can be added to the project
		/// </summary>
		IConnectedService service;

		public ServiceDetailsWidget ()
		{
		}

		/// <summary>
		/// Loads the service details for the given service
		/// </summary>
		public void LoadService (IConnectedService service)
		{
			this.service = service;

			// test code
			var label = new Label ("service: " + service.DisplayName);
			this.PackStart (label, false, false, 0);

			this.Build ();

			this.ShowAll ();
		}

		/// <summary>
		/// Builds widget for the default configuration widget for the dependencies of a service
		/// </summary>
		static Widget BuildDependencySection(IConnectedService service)
		{
			return new ConfigurationSectionWidget (new DependenciesSection (service));
		}

		/// <summary>
		/// Builds widget for the sections for the service and insert the dependencies widget at the start
		/// </summary>
		static Widget BuildSections(IConnectedService service)
		{
			// TODO: make this pretty
			var vbox = new VBox ();

			// always add the dependencies
			vbox.PackStart (BuildDependencySection (service), false, false, 0);

			// now add the services sections
			foreach (var section in service.Sections) {
				vbox.PackStart (new ConfigurationSectionWidget(section));
			}

			return vbox;
		}

		/// <summary>
		/// Builds the widget
		/// </summary>
		void Build()
		{
			if (this.service == null) {
				return;
			}

			// TODO: make this pretty
			var label = new Label ("details");
			this.PackStart (label, false, false, 0);

			var btn = new Button () { Label = "Add" };
			btn.Clicked += (sender, e) => {
				if (this.service != null && !this.service.IsAdded) {
					this.service.AddToProject ();
				}
			};

			this.PackStart (btn, false, false, 10);

			this.PackStart (BuildSections (this.service), false, false, 0);
		}
	}

	/// <summary>
	/// Default widget that displays a ConfigurationSection
	/// </summary>
	public class ConfigurationSectionWidget : VBox
	{
		public ConfigurationSectionWidget (IConfigurationSection section)
		{
			this.Section = section;
			this.Build ();
		}

		public IConfigurationSection Section { get; private set; }

		/// <summary>
		/// Adds the section to the project
		/// </summary>
		protected virtual void OnAddSectionToProject()
		{
			this.Section.AddToProject ();
		}

		/// <summary>
		/// Gets the widget to display the content of the section
		/// </summary>
		protected virtual Widget GetSectionWidget()
		{
			return this.Section.GetSectionWidget ();
		}

		/// <summary>
		/// Handles the addBtn clicked event
		/// </summary>
		void AddBtnClicked (object sender, EventArgs e)
		{
			this.OnAddSectionToProject ();
		}

		/// <summary>
		/// Builds the widget
		/// </summary>
		void Build()
		{
			var label = new Label { Text = this.Section.DisplayName };
			this.PackStart (label, false, false, 0);

			// TODO: expander

			if (this.Section.CanBeAdded) {
				var addBtn = new Button () { Label = "add '" + this.Section.DisplayName + "' to the project" };
				this.PackStart (addBtn, false, false, 0);
				addBtn.Clicked += this.AddBtnClicked;

				this.Section.Added += (sender, e) => {
					addBtn.Sensitive = this.Section.IsAdded;
				};
			}

			this.PackStart (this.GetSectionWidget());
		}
	}

	/// <summary>
	/// Default widget that displays the dependencies for a service
	/// </summary>
	class DependenciesSectionWidget : VBox
	{
		readonly IConfigurationSection section;

		public DependenciesSectionWidget (IConfigurationSection section) 
		{
			this.section = section;
			this.Build ();
		}

		void Build()
		{
			// list the dependecies in a tree

			if (this.section.Service.Dependencies.Length == 0) {
				this.PackStart (new Label { Text = "None" }, false, false, 0);
				return;
			}

			foreach (var dependency in this.section.Service.Dependencies) {
				this.PackStart (new Label { Text = dependency.DisplayName }, false, false, 0);
				this.PackStart (new Label { Text = dependency.DisplayName + " is " + (dependency.IsAdded ? "Added" : " Not added") }, false, false, 0);
			}
		}
	}
}
