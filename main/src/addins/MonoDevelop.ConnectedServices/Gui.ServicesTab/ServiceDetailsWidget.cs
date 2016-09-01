using System;
using System.Linq;
using MonoDevelop.Ide.Gui;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Widget that displays the service details
	/// </summary>
	class ServiceDetailsWidget : Widget
	{
		ServiceWidget details;
		VBox sections;

		/// <summary>
		/// A service that can be added to the project
		/// </summary>
		IConnectedService service;

		public ServiceDetailsWidget ()
		{
			Margin = 30;

			var container = new VBox ();

			details = new ServiceWidget (true);
			details.BorderWidth = 0;
			sections = new VBox ();

			container.Spacing = sections.Spacing = 0;
			container.PackStart (details);
			container.PackStart (sections);

			var frame = new FrameBox (container);
			frame.BackgroundColor = Styles.BaseBackgroundColor;
			frame.BorderColor = Styles.ThinSplitterColor;
			frame.BorderWidth = 1;

			// pack the frame into an additional VBox, to prevent it
			// from filling the parent widget.
			var contentBox = new VBox ();
			contentBox.PackStart (frame);
			Content = contentBox;
		}

		/// <summary>
		/// Loads the service details for the given service
		/// </summary>
		public void LoadService (IConnectedService service)
		{
			if (service != null) {
				service.Added -= HandleServiceAdded;
				foreach (var child in sections.Children.ToArray ()) {
					sections.Remove (child);
					child.Dispose ();
				}
			}
			
			this.service = details.Service = service;

			if (service.DependenciesSection != null) {
				var dependencies = new ConfigurationSectionWidget (service.DependenciesSection);
				dependencies.ExpandedChanged += HandleSectionExpandedChanged;
				sections.PackStart (dependencies);
				dependencies.Expanded = true;
			}

			foreach (var section in service.Sections) {
				var w = new ConfigurationSectionWidget (section);
				if (sections.Children.Count () == 0)
					w.Expanded = true;
				w.ExpandedChanged += HandleSectionExpandedChanged;
				sections.PackStart (w);
			}

			service.Added += HandleServiceAdded;

			// expand the first section if the service is already added to the project
			if (service.IsAdded) {
				var section = service.Sections.FirstOrDefault ();
				if (section != null) {
					foreach (ConfigurationSectionWidget child in this.sections.Children.Where (c => c is ConfigurationSectionWidget)) {
						if (child.Section == section) {
							child.Expanded = true;
							break;
						}
					}
				}
			}
		}

		void HandleServiceAdded (object sender, EventArgs e)
		{
			if (service.AreDependenciesInstalled) {
				Core.Runtime.RunInMainThread (delegate {
					var configuration = sections.Children.FirstOrDefault (s => (s as ConfigurationSectionWidget)?.Section != service.DependenciesSection) as ConfigurationSectionWidget;
					if (configuration != null)
						configuration.Expanded = true;
				});
			}
		}

		void HandleSectionExpandedChanged (object sender, EventArgs e)
		{
			var section = sender as ConfigurationSectionWidget;
			if (section?.Expanded == true) {
				foreach (ConfigurationSectionWidget child in sections.Children.Where (c => c is ConfigurationSectionWidget)) {
					if (child != sender)
						child.Expanded = false;
				}
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (service != null) {
				service.Added -= HandleServiceAdded;
				service = null;
			}
			base.Dispose (disposing);
		}
	}
}
