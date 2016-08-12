using System;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
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
			this.PackStart (label);

			// TODO: expander

			if (this.Section.CanBeAdded) {
				var addBtn = new Button () { Label = "add '" + this.Section.DisplayName + "' to the project" };
				this.PackStart (addBtn);
				addBtn.Clicked += this.AddBtnClicked;

				this.Section.Added += (sender, e) => {
					addBtn.Sensitive = this.Section.IsAdded;
				};
			}

			this.PackStart (this.GetSectionWidget());
		}
	}
}
