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

			Accessible.Role = Xwt.Accessibility.Role.Group;
			Accessible.IsAccessible = true;

			details = new ServiceWidget (true);
			details.BorderWidth = 1;
			details.CornerRadius = new Components.RoundedFrameBox.BorderCornerRadius (6, 6, 0, 0);
			sections = new VBox ();

			container.Spacing = sections.Spacing = 0;
			container.PackStart (details);
			container.PackStart (sections);

			Content = container;
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
			ConfigurationSectionWidget lastSection = null;

			if (service.DependenciesSection != null) {
				var dependencies = lastSection = new ConfigurationSectionWidget (service.DependenciesSection);
				dependencies.BorderLeft = dependencies.BorderRight = dependencies.BorderBottom = true;
				dependencies.BorderTop = false;
				dependencies.BorderWidth = 1;
				sections.PackStart (dependencies);
				if (service.Status != Status.Added)
					dependencies.Expanded = true;
			}

			foreach (var section in service.Sections) {
				var w = lastSection = new ConfigurationSectionWidget (section);
				w.BorderLeft = w.BorderRight = w.BorderBottom = true;
				w.BorderTop = false;
				w.BorderWidth = 1;
				sections.PackStart (w);
			}

			if (lastSection != null)
				lastSection.CornerRadius = new Components.RoundedFrameBox.BorderCornerRadius (0, 0, 6, 6);


			service.StatusChanged += HandleServiceStatusChanged;

			// expand the first unconfigured section if the service is already added to the project
			if (service.Status == Status.Added) {
				ExpandFirstOrUnconfiguredSection ();
			}

			Accessible.Label = service.DisplayName;
		}

		void HandleServiceStatusChanged (object sender, StatusChangedEventArgs e)
		{
			// handle when the service has finished being added
			if (e.WasAdded) {
				InvokeAsync (ExpandFirstOrUnconfiguredSection);
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
					if (child.Section == section)
						child.Expanded = true;
					else
						child.Expanded = false;
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
