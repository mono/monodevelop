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
}
