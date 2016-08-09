using System;
using Gtk;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
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
