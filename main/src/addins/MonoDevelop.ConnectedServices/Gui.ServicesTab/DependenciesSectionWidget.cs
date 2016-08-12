using System;
using Xwt;

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
				this.PackStart (new Label { Text = "None" });
				return;
			}

			foreach (var dependency in this.section.Service.Dependencies) {
				this.PackStart (new Label { Text = dependency.DisplayName });
				this.PackStart (new Label { Text = dependency.DisplayName + " is " + (dependency.IsAdded ? "Added" : " Not added") });
			}
		}
	}
}
