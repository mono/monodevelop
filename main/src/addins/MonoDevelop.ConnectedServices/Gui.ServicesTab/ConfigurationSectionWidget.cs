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
	public class ConfigurationSectionWidget : Components.RoundedFrameBox
	{
		static readonly Image arrowRight = ImageService.GetIcon ("md-expander-arrow-closed").WithSize (8, 8);
		static readonly Image arrowDown = ImageService.GetIcon ("md-expander-arrow-expanded").WithSize (8, 8);

		Label titleLabel, statusLabel;
		HBox statusBox;
		ImageView expanderImage, statusImage;
		Button addBtn;
		HBox header;
		Widget sectionWidget;
		bool expanded;

		/// <summary>
		/// Gets the sectino that this widget represents
		/// </summary>
		public IConfigurationSection Section { get; private set; }

		public IConnectedService Service { get; private set; }

		public bool Expanded {
			get {
				return expanded;
			}
			set {
				if (expanded != value) {
					if (value) {
						expanderImage.Image = arrowDown;
						InnerBackgroundColor = Styles.BaseBackgroundColor;
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
			if (section == null)
				throw new ArgumentNullException (nameof (section));
			if (section.Service == null)
				throw new InvalidOperationException ("The service of a section is null");
			Section = section;
			Service = section.Service;

			BackgroundColor = Styles.BackgroundColor;
			InnerBackgroundColor = Styles.BaseBackgroundColor;
			BorderWidth = 0;
			Padding = new WidgetSpacing (15, 4, 15, 5);

			header = new HBox ();

			header.Spacing = 7;
			header.MarginLeft = 0;
			header.MarginRight = 15;
			header.MinHeight = 36;

			expanderImage = new ImageView (arrowRight);

			titleLabel = new Label { Text = this.Section.DisplayName };

			statusLabel = new Label (GettextCatalog.GetString ("Enabled"));
			statusLabel.TextColor = Styles.SecondaryTextColor;

			statusImage = new ImageView (ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small));
			statusImage.Accessible.LabelWidget = statusLabel;

			statusBox = new HBox ();
			statusBox.MarginLeft = 10;
			statusBox.Spacing = 3;
			statusBox.PackStart (statusImage);
			statusBox.PackStart (statusLabel);

			var headerTitle = new HBox ();
			headerTitle.Spacing = 7;
			headerTitle.PackStart (expanderImage);
			headerTitle.PackStart (titleLabel);
			headerTitle.PackStart (statusBox);

			headerTitle.Accessible.Role = Xwt.Accessibility.Role.Disclosure;
			headerTitle.Accessible.IsAccessible = true;
			headerTitle.Accessible.LabelWidget = titleLabel;

			header.PackStart (headerTitle);

			addBtn = new Button (GettextCatalog.GetString ("Add to the project"));
			addBtn.MinWidth = 128;
			addBtn.MinHeight = 34;
			header.PackEnd (addBtn);
			addBtn.VerticalPlacement = WidgetPlacement.Center;
			addBtn.Clicked += this.AddBtnClicked;

			var container = new VBox ();
			sectionWidget = GetSectionWidget ();
			sectionWidget.MarginLeft = sectionWidget.MarginRight = 15;
			sectionWidget.MarginBottom = 20;
			sectionWidget.Visible = false;

			container.PackStart (header);
			container.PackStart (sectionWidget);

			Content = container;

			UpdateStatus ();
			Section.StatusChanged += HandleSectionStatusChanged;
			Service.StatusChanged += HandleServiceStatusChanged;
		}

		void UpdateStatus ()
		{
			if (disposed)
				return;

			addBtn.Sensitive = Service.Status == Status.Added;

			if (Section.IsAdded && (Section.CanBeAdded || Section == Section.Service.DependenciesSection)) {
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
			if (Section != Section.Service.DependenciesSection)
				Expanded = Sensitive = Service.Status == Status.Added;
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

		void HandleSectionStatusChanged (object sender, StatusChangedEventArgs e)
		{
			if (e.IsAdding) {
				HandleSectionAdding ();
			} else {
				Runtime.RunInMainThread (() => UpdateStatus ());
			}
		}

		void HandleSectionAdding ()
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

		void HandleServiceStatusChanged (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => UpdateStatus ());
		}

		protected override void OnButtonReleased (ButtonEventArgs args)
		{
			base.OnButtonReleased (args);
			if (args.Button == PointerButton.Left) {
				if (Expanded) {
					var headerRect = new Rectangle (0, 0, Size.Width, header.Size.Height + header.MarginTop + header.MarginBottom);
					if (!headerRect.Contains (args.Position))
						return;
				}
				Expanded = !Expanded;
			}
		}

		protected override void OnMouseEntered (EventArgs args)
		{
			base.OnMouseEntered (args);
			if (!Expanded) {
				InnerBackgroundColor = Styles.BackgroundColor;
			}
		}

		protected override void OnMouseExited (EventArgs args)
		{
			base.OnMouseExited (args);
			InnerBackgroundColor = Styles.BaseBackgroundColor;
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

		bool disposed;

		protected override void Dispose (bool disposing)
		{
			disposed = true;
			if (Section != null) {
				Section.StatusChanged -= HandleSectionStatusChanged;
				Section = null;
			}
			if (Service != null) {
				Service.StatusChanged -= HandleServiceStatusChanged;
				Service = null;
			}
			base.Dispose (disposing);
		}
	}
}
