using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Default widget that displays a ConfigurationSection
	/// </summary>
	public class ConfigurationSectionWidget : FrameBox
	{
		static readonly Image arrowRight = ImageService.GetIcon ("arrow-right").WithSize (IconSize.Small);
		static readonly Image arrowDown = ImageService.GetIcon ("arrow-down").WithSize (IconSize.Small);
		
		Label status, title;
		ImageView expanderImage;
		Widget sectionWidget;
		bool expanded;
		
		public IConfigurationSection Section { get; private set; }

		public bool Expanded {
			get {
				return expanded;
			}
			set {
				if (expanded != value) {
					if (value) {
						expanderImage.Image = arrowDown;
					} else {
						expanderImage.Image = arrowRight;
					}
					expanded = sectionWidget.Visible = value;

					ExpandedChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler ExpandedChanged;

		public ConfigurationSectionWidget (IConfigurationSection section)
		{
			Section = section;

			BackgroundColor = Styles.BaseBackgroundColor;
			BorderColor = Styles.ThinSplitterColor;
			BorderWidthTop = 1;

			var header = new HBox ();
			header.Spacing = 7;
			header.MarginLeft = 7;
			header.MarginTop = MarginBottom = 10;
			header.MarginRight = 30;

			expanderImage = new ImageView (ImageService.GetIcon ("arrow-right").WithSize (IconSize.Small));


			title = new Label { Markup = this.Section.DisplayName };
			status = new Label ();

			header.PackStart (expanderImage);
			header.PackStart (title);
			header.PackStart (status);

			if (this.Section.CanBeAdded) {
				var addBtn = new Button () { Label = GettextCatalog.GetString ("Add to the project") };
				header.PackEnd (addBtn);
				addBtn.Clicked += this.AddBtnClicked;

				this.Section.Added += (sender, e) => {
					addBtn.Sensitive = this.Section.IsAdded;
				};
			}

			var container = new VBox ();
			sectionWidget = GetSectionWidget ();
			sectionWidget.Margin = 30;
			sectionWidget.MarginTop = 10;
			sectionWidget.MarginBottom = 10;
			sectionWidget.Visible = false;

			container.PackStart (header);
			container.PackStart (sectionWidget);

			Content = container;
		}

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

		protected override void OnButtonReleased (ButtonEventArgs args)
		{
			base.OnButtonReleased (args);
			if (args.Button == PointerButton.Left && !Expanded)
				Expanded = true;
		}

		protected override void OnMouseEntered (EventArgs args)
		{
			base.OnMouseEntered (args);
			if (!Expanded) {
				// FIXME: Background bounds calculation is broken in Xwt.FrameBox
				//        temporaly: using bold text for highlighting
				//BackgroundColor = Styles.BackgroundColor;
				title.Markup = "<b>" + Section.DisplayName + "</b>";
			}
		}

		protected override void OnMouseExited (EventArgs args)
		{
			base.OnMouseExited (args);
			BackgroundColor = Styles.BaseBackgroundColor;
			title.Markup = Section.DisplayName;
		}

		/// <summary>
		/// Handles the addBtn clicked event
		/// </summary>
		void AddBtnClicked (object sender, EventArgs e)
		{
			this.OnAddSectionToProject ();
		}
	}
}
