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
				service.StatusChanged -= HandleServiceStatusChanged;
				foreach (var child in sections.Children.ToArray ()) {
					sections.Remove (child);
					child.Dispose ();
				}
			}
			
			this.service = details.Service = service;

			if (service.DependenciesSection != null) {
				var dependencies = new ConfigurationSectionWidget (service.DependenciesSection);
				sections.PackStart (dependencies);
			}

			foreach (var section in service.Sections) {
				var w = new ConfigurationSectionWidget (section);
				sections.PackStart (w);
			}

			service.StatusChanged += HandleServiceStatusChanged;

			// expand the first unconfigured section if the service is already added to the project
			if (service.Status == Status.Added) {
				ExpandFirstOrUnconfiguredSection ();
			}
		}

		void HandleServiceStatusChanged (object sender, StatusChangedEventArgs e)
		{
			// handle when the service has finished being added
			if (e.WasAdded) {
				ExpandFirstOrUnconfiguredSection ();
			}
		}

		void ExpandFirstOrUnconfiguredSection ()
		{
			var _sections = service.Sections.AsEnumerable ();
			if (service.DependenciesSection != null) {
				_sections = new IConfigurationSection [] { service.DependenciesSection }.Concat (_sections).AsEnumerable ();
			}

			var section = _sections.FirstOrDefault (s => !s.IsAdded) ?? _sections.FirstOrDefault ();
			if (section != null) {
				foreach (ConfigurationSectionWidget child in this.sections.Children.Where (c => c is ConfigurationSectionWidget)) {
					if (child.Section == section) {
						child.Expanded = true;
						break;
					}
				}
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (service != null) {
				service.StatusChanged -= HandleServiceStatusChanged;
				service = null;
			}
			base.Dispose (disposing);
		}
	}
}
