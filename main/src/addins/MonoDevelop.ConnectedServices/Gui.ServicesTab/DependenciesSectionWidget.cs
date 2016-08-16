using System;
using MonoDevelop.Components;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Default widget that displays the dependencies for a service
	/// </summary>
	class DependenciesSectionWidget : AbstractXwtControl
	{
		readonly IConfigurationSection section;
		readonly VBox widget;

		public override Widget Widget {
			get {
				return widget;
			}
		}

		public DependenciesSectionWidget (IConfigurationSection section) 
		{
			this.section = section;

			widget = new VBox ();

			this.Build ();
		}

		void Build()
		{
			// list the dependecies in a tree

			if (this.section.Service.Dependencies.Length == 0) {
				widget.PackStart (new Label { Text = "None" });
				return;
			}

			foreach (var dependency in this.section.Service.Dependencies) {
				widget.PackStart (new Label { Text = dependency.DisplayName });
				widget.PackStart (new Label { Text = dependency.DisplayName + " is " + (dependency.IsAdded ? "Added" : " Not added") });
			}
		}
	}
}
