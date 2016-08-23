using System;
using System.Threading;
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
		static readonly Image arrowRight = ImageService.GetIcon ("md-expander-arrow-closed").WithSize (8, 8);
		static readonly Image arrowDown = ImageService.GetIcon ("md-expander-arrow-expanded").WithSize (8, 8);

		Label titleLabel, statusLabel;
		HBox statusBox;
		ImageView expanderImage, statusImage;
		Button addBtn;
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

			expanderImage = new ImageView (ImageService.GetIcon ("md-expander-arrow-closed").WithSize (8, 8));

			titleLabel = new Label { Markup = this.Section.DisplayName };

			statusLabel = new Label (GettextCatalog.GetString ("Enabled"));
			statusLabel.Font = Font.WithSize (12);
			statusLabel.TextColor = Styles.SecondaryTextColor;

			statusImage = new ImageView (ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small));

			statusBox = new HBox ();
			statusBox.MarginLeft = 10;
			statusBox.Spacing = 3;
			statusBox.PackStart (statusImage);
			statusBox.PackStart (statusLabel);

			header.PackStart (expanderImage);
			header.PackStart (titleLabel);
			header.PackStart (statusBox);

			addBtn = new Button (GettextCatalog.GetString ("Add to the project"));

			header.PackEnd (addBtn);
			addBtn.Clicked += this.AddBtnClicked;

			var container = new VBox ();
			sectionWidget = GetSectionWidget ();
			sectionWidget.Margin = 30;
			sectionWidget.MarginTop = 10;
			sectionWidget.MarginBottom = 10;
			sectionWidget.Visible = false;

			container.PackStart (header);
			container.PackStart (sectionWidget);

			Content = container;

			UpdateStatus ();
			Section.Adding += HandleSectionAdding;
			Section.AddingFailed += HandleSectionAddingFailed;
			Section.Added += HandleSectionAdded;
			Section.Removed += HandleSectionRemoved;
		}

		void UpdateStatus ()
		{
			if (Section.IsAdded) {
				if (Section == Section.Service.DependenciesSection)
					statusLabel.Text = GettextCatalog.GetString ("Installed");
				else
					statusLabel.Text = GettextCatalog.GetString ("Configured");
				statusImage.Visible = true;
				statusBox.Visible = true;
				addBtn.Visible = false;
			} else {
				statusBox.Visible = false;
				addBtn.Visible = Section.CanBeAdded;
			}
		}

		/// <summary>
		/// Adds the section to the project
		/// </summary>
		protected virtual void OnAddSectionToProject()
		{
			this.Section.AddToProject (CancellationToken.None);
		}

		/// <summary>
		/// Gets the widget to display the content of the section
		/// </summary>
		protected virtual Widget GetSectionWidget()
		{
			return this.Section.GetSectionWidget ();
		}

		void HandleSectionAdding (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (delegate {
				if (Section is DependenciesSection)
					statusLabel.Text = GettextCatalog.GetString ("Installing\u2026");
				else
					statusLabel.Text = GettextCatalog.GetString ("Adding\u2026");
				statusImage.Visible = false;
				statusBox.Visible = true;
				addBtn.Visible = false;
			});
		}

		void HandleSectionAddingFailed (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => UpdateStatus ());
		}

		void HandleSectionAdded (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => UpdateStatus ());
		}

		void HandleSectionRemoved (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => UpdateStatus ());
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
				titleLabel.Markup = "<b>" + Section.DisplayName + "</b>";
			}
		}

		protected override void OnMouseExited (EventArgs args)
		{
			base.OnMouseExited (args);
			BackgroundColor = Styles.BaseBackgroundColor;
			titleLabel.Markup = Section.DisplayName;
		}

		/// <summary>
		/// Handles the addBtn clicked event
		/// </summary>
		void AddBtnClicked (object sender, EventArgs e)
		{
			if (this.Section.CanBeAdded) {
				this.OnAddSectionToProject ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (Section != null) {
				Section.Adding -= HandleSectionAdding;
				Section.Added -= HandleSectionAdded;
				Section = null;
			}
			base.Dispose (disposing);
		}
	}
}
