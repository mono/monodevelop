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
			Content = frame;
		}

		/// <summary>
		/// Loads the service details for the given service
		/// </summary>
		public void LoadService (IConnectedService service)
		{
			this.service = details.Service = service;
			sections.Clear ();

			var dependencies = new ConfigurationSectionWidget (service.DependenciesSection);
			dependencies.ExpandedChanged += HandleSectionExpandedChanged;
			sections.PackStart (dependencies);

			foreach (var section in service.Sections) {
				var w = new ConfigurationSectionWidget (section);
				w.ExpandedChanged += HandleSectionExpandedChanged;
				sections.PackStart (w);
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
	}
}
