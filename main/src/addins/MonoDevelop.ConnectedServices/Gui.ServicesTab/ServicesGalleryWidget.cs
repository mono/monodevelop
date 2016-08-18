using System;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Widget that displays the services gallery
	/// </summary>
	class ServicesGalleryWidget : Widget
	{
		VBox enabledList, availableList;
		Label enabledLabel, availableLabel;

		IConnectedService [] services;
		
		public ServicesGalleryWidget ()
		{
			var container = new VBox ();
			
			enabledList = new VBox ();
			availableList = new VBox ();

			enabledLabel = new Label (GettextCatalog.GetString ("Enabled")) { Font = Font.WithSize (14) };
			availableLabel = new Label (GettextCatalog.GetString ("Available")) { Font = Font.WithSize (14) };

			enabledLabel.TextColor = availableLabel.TextColor = Styles.SecondaryTextColor;
			enabledLabel.MarginLeft = availableLabel.MarginLeft = 15;
			enabledLabel.MarginTop = availableLabel.MarginTop = 5;
			enabledLabel.MarginBottom = availableLabel.MarginBottom = 5;

			container.PackStart (enabledLabel);
			container.PackStart (enabledList);
			container.PackStart (availableLabel);
			container.PackStart (availableList);
			container.Margin = 30;
			container.Spacing = 0;

			Content = container;
		}

		/// <summary>
		/// Loads the given services into the gallery
		/// </summary>
		public void LoadServices(IConnectedService [] services)
		{
			this.services = services;

			ClearServices ();

			//TODO: sort the lists
			foreach (var service in services) {
				var serviceWidget = new ServiceWidget (service);
				serviceWidget.MarginTop = 5;
				serviceWidget.ButtonReleased += HandleServiceWidgetButtonReleased;

				if (service.IsAdded) {
					enabledList.PackStart (serviceWidget);
					enabledLabel.Visible = true;
				} else {
					availableList.PackStart (serviceWidget);
					availableLabel.Visible = true;
				}
				service.Added += HandleServiceAddedRemoved;
				service.Removed += HandleServiceAddedRemoved;
			}
		}

		void ClearServices ()
		{
			foreach (var widget in availableList.Children.Where ((c) => c is ServiceWidget).Cast<ServiceWidget> ()) {
				widget.Service.Added -= HandleServiceAddedRemoved;
				widget.Service.Removed -= HandleServiceAddedRemoved;
				widget.ButtonReleased -= HandleServiceWidgetButtonReleased;
			}
			availableList.Clear ();
			foreach (var widget in enabledList.Children.Where ((c) => c is ServiceWidget).Cast<ServiceWidget> ()) {
				widget.Service.Added -= HandleServiceAddedRemoved;
				widget.Service.Removed -= HandleServiceAddedRemoved;
				widget.ButtonReleased -= HandleServiceWidgetButtonReleased;
			}
			enabledList.Clear ();
			enabledLabel.Visible = false;
			availableLabel.Visible = false;
		}

		void HandleServiceAddedRemoved (object sender, EventArgs e)
		{
			var service = (IConnectedService)sender;
			//TODO: sort the lists
			Application.Invoke (delegate {
				if (service.IsAdded) {
					foreach (var widget in availableList.Children.Where ((c) => c is ServiceWidget).Cast<ServiceWidget> ()) {
						if (widget.Service == service) {
							availableList.Remove (widget);
							enabledList.PackStart (widget);
							break;
						}
					}
				} else {
					foreach (var widget in enabledList.Children.Where ((c) => c is ServiceWidget).Cast<ServiceWidget> ()) {
						if (widget.Service == service) {
							enabledList.Remove (widget);
							availableList.PackStart (widget);
							break;
						}
					}
				}

				enabledLabel.Visible = services.Any (s => s.IsAdded);
				availableLabel.Visible = services.Any (s => !s.IsAdded);
			});
		}

		void HandleServiceWidgetButtonReleased (object sender, ButtonEventArgs e)
		{
			if (e.Button == PointerButton.Left) {
				ServiceSelected?.Invoke (this, new ServiceEventArgs ((sender as ServiceWidget)?.Service));
			}
		}

		public event EventHandler<ServiceEventArgs> ServiceSelected;

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				ClearServices ();
			base.Dispose (disposing);
		}
	}

	class ServiceWidget : FrameBox
	{
		HBox addedWidget;
		ImageView image;
		Label title, description, platforms;
		Button addButton;
		bool showDetails = false;

		IConnectedService service;

		public IConnectedService Service {
			get {
				return service;
			}
			set {
				if (service == value)
					return;
				if (value == null)
					throw new InvalidOperationException ("Service can not be null");
				if (service != null) {
					service.Added -= HandleServiceAddedRemoved;
					service.Removed -= HandleServiceAddedRemoved;
				}
				
				service = value;
				image.Image = (service.GalleryIcon ?? ImageService.GetIcon ("md-project")).WithSize (IconSize.Medium);
				title.Markup = "<b>" + service.DisplayName + "</b>";
				description.Text = service.Description;

				platforms.Text = service.SupportedPlatforms;

				addedWidget.Visible = service.IsAdded && !showDetails;

				addButton.Visible = showDetails;
				addButton.Sensitive = !service.IsAdded;
				addButton.Image = service.IsAdded ? ImageService.GetIcon ("md-prefs-task-list").WithSize (IconSize.Small).WithAlpha (0.4) : null;
				addButton.Label = service.IsAdded ? GettextCatalog.GetString ("Enabled") : GettextCatalog.GetString ("Enable");

				service.Added += HandleServiceAddedRemoved;
				service.Removed += HandleServiceAddedRemoved;
			}
		}

		public bool ShowDetails {
			get {
				return showDetails;
			}
			set {
				showDetails = value;
				platforms.Visible = showDetails && !string.IsNullOrEmpty (service?.SupportedPlatforms);
				addButton.Visible = showDetails;
				addedWidget.Visible = service?.IsAdded == true && !showDetails;
			}
		}

		public ServiceWidget (IConnectedService service, bool showDetails = false) : this (showDetails)
		{
			if (service == null)
				throw new ArgumentNullException (nameof (service));
			Service = service;
		}

		public ServiceWidget (bool showDetails = false)
		{
			BackgroundColor = Styles.BaseBackgroundColor;
			BorderColor = Styles.ThinSplitterColor;
			BorderWidth = 1;

			image = new ImageView ();
			title = new Label ();
			title.Font = title.Font.WithSize (16);

			addedWidget = new HBox ();
			addedWidget.Spacing = 3;
			addedWidget.PackStart (new ImageView (ImageService.GetIcon ("md-prefs-task-list").WithSize (IconSize.Small)));
			addedWidget.PackStart (new Label (GettextCatalog.GetString ("Enabled")) {
				Font = Font.WithSize (12),
				TextColor = Styles.SecondaryTextColor,
			});
			addedWidget.Visible = false;

			addButton = new Button (GettextCatalog.GetString ("Enable"));
			addButton.Visible = false;
			addButton.Clicked += HandleAddButtonClicked;

			var header = new HBox ();
			header.Spacing = 10;
			header.PackStart (image);
			header.PackStart (title);
			header.PackStart (addedWidget);

			var vbox = new VBox ();
			vbox.Spacing = 10;
			vbox.PackStart (header);

			description = new Label ();
			description.TextColor = Styles.SecondaryTextColor;
			description.Wrap = WrapMode.Word;
			description.Ellipsize = EllipsizeMode.None;

			platforms = new Label ();
			platforms.TextColor = Styles.SecondaryTextColor;

			vbox.PackStart (description);
			vbox.PackStart (platforms);

			var container = new HBox { Spacing = 0 };
			container.Margin = 30;
			container.PackStart (vbox, true);
			container.PackEnd (addButton, vpos: WidgetPlacement.Start);

			Content = container;
			ShowDetails = showDetails;
		}

		void HandleAddButtonClicked (object sender, EventArgs e)
		{
			if (!service.IsAdded) {
				addButton.Label = GettextCatalog.GetString ("Enabling \u2026");
				addButton.Sensitive = false;
				service.AddToProject ();
			}
		}

		void HandleServiceAddedRemoved (object sender, EventArgs e)
		{
			Application.Invoke (delegate {
				var node = service.Project.GetConnectedServicesBinding ().ServicesNode;
				node?.NotifyServicesChanged ();
				addedWidget.Visible = Service.IsAdded && !showDetails;
				addButton.Image = service.IsAdded ? ImageService.GetIcon ("md-prefs-task-list").WithSize (IconSize.Small).WithAlpha (0.4) : null;
				addButton.Label = service.IsAdded ? GettextCatalog.GetString ("Enabled") : GettextCatalog.GetString ("Enable");
				addButton.Sensitive = !Service.IsAdded;
			});
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && Service != null)
				Service.Added -= HandleServiceAddedRemoved;
			base.Dispose (disposing);
		}
	}
}
