using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	class ServiceWidget : Components.RoundedFrameBox
	{
		HBox statusWidget;
		ImageView statusIcon;
		Label statusText;
		ImageView image;
		Label title, platforms;
		MarkupView description;
		HBox platformWidget;
		Button addButton;
		AnimatedIcon animatedStatusIcon;
		IDisposable statusIconAnimation;
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
					service.StatusChanged -= HandleServiceStatusChanged;
				}
				
				service = value;
				image.Image = (service.GalleryIcon ?? ImageService.GetIcon ("md-service")).WithSize (IconSize.Medium);
				title.Markup = "<b>" + service.DisplayName + "</b>";
				UpdateDescription ();

				platforms.Markup = string.Format ("<span color='{1}'><b>{0}</b></span>", service.SupportedPlatforms, Styles.SecondaryTextColor.ToHexString ());
				platformWidget.Visible = showDetails && !string.IsNullOrEmpty (service?.SupportedPlatforms);

				service.StatusChanged += HandleServiceStatusChanged;

				UpdateServiceStatus ();
				UpdateAccessibility ();
			}
		}

		public bool ShowDetails {
			get {
				return showDetails;
			}
			set {
				showDetails = value;
				platformWidget.Visible = showDetails && !string.IsNullOrEmpty (service?.SupportedPlatforms);
				addButton.Visible = showDetails;
				statusWidget.Visible = service?.Status == Status.Added && !showDetails;
				description.HorizontalPlacement = value ? WidgetPlacement.Start : WidgetPlacement.Fill;
				UpdateDescription ();
			}
		}

		public new CursorType Cursor {
			get { return base.Cursor; }
			set {
				base.Cursor = value;
				description.Cursor = value;
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
			InnerBackgroundColor = Styles.BaseBackgroundColor;
			BorderWidth = 1;
			CornerRadius = new BorderCornerRadius (6, 6, 6, 6);
			Padding = 30;

			image = new ImageView ();
			image.MarginBottom = 2;
			title = new Label ();
			title.Font = Xwt.Drawing.Font.SystemFont.WithSize (16);

			statusWidget = new HBox ();
			statusWidget.Spacing = 3;
			statusIcon = new ImageView ();
			statusText = new Label () {
				Font = Font.WithSize (12),
				TextColor = Styles.SecondaryTextColor,
			};
			statusWidget.PackStart (statusIcon);
			statusWidget.PackStart (statusText);
			statusWidget.Visible = false;
			statusWidget.MarginLeft = 5;

			addButton = new Button ();
			addButton.BackgroundColor = Styles.BaseSelectionBackgroundColor;
			addButton.LabelColor = Styles.BaseSelectionTextColor;
			addButton.MinWidth = 128;
			addButton.MinHeight = 34;
			addButton.Visible = false;
			addButton.Clicked += HandleAddButtonClicked;

			if (ImageService.IsAnimation ("md-spinner-16", Gtk.IconSize.Menu)) {
				animatedStatusIcon = ImageService.GetAnimatedIcon ("md-spinner-16", Gtk.IconSize.Menu);
			}

			var header = new HBox ();
			header.Spacing = 5;
			header.PackStart (image);
			header.PackStart (title);
			header.PackStart (statusWidget);

			var vbox = new VBox ();
			vbox.Spacing = 6;
			vbox.PackStart (header);

			description = new MarkupView {
				Selectable = false,
				LineSpacing = 6,
				MinWidth = 640,
				BackgroundColor = Styles.BaseBackgroundColor,
			};

			platforms = new Label ();
			platforms.TextColor = Styles.SecondaryTextColor;

			platformWidget = new HBox ();
			platformWidget.PackStart (new Label { Text = GettextCatalog.GetString ("Platforms:"), TextColor = Styles.SecondaryTextColor }, marginRight: 20);
			platformWidget.PackStart (platforms);

			vbox.PackStart (description, false, hpos: WidgetPlacement.Start);
			vbox.PackStart (platformWidget, false);

			var container = new HBox { Spacing = 0 };
			container.PackStart (vbox, true);
			container.PackEnd (addButton, vpos: WidgetPlacement.Start);

			Content = container;
			ShowDetails = showDetails;

			UpdateAccessibility ();
		}

		void UpdateAccessibility ()
		{
			Accessible.IsAccessible = true;
			Accessible.Role = ShowDetails ? Xwt.Accessibility.Role.Group : Xwt.Accessibility.Role.Button;
			Accessible.LabelWidget = title;

			addButton.Accessible.LabelWidget = title;

			image.Accessible.Label = GettextCatalog.GetString ("Service Icon");
		}

		void HandleAddButtonClicked (object sender, EventArgs e)
		{
			if (service.Status == Status.NotAdded) {
				var addProjects = new Dictionary<string, DotNetProject> ();

				foreach (DotNetProject project in service.Project.ParentSolution.GetAllProjects ().Where (p => p is DotNetProject && p != service.Project)) {
					var svc = project.GetConnectedServicesBinding ()?.SupportedServices.FirstOrDefault (s => s.Id == service.Id);
					if (svc != null && svc.Status == Status.NotAdded)
						addProjects [project.ItemId] = project;
				}

				var servicesToAdd = new List<IConnectedService> ();

				if (addProjects.Count > 0) {
					var question = new Xwt.QuestionMessage (GettextCatalog.GetString ("Add {0} to {1}", this.Service.DisplayName, this.Service.Project.Name));
					question.SecondaryText = GettextCatalog.GetString ("Also add '{0}' to other projects in the solution?", this.Service.DisplayName);

					foreach (var project in addProjects)
						question.AddOption (project.Key, project.Value.Name, true);

					var cmdContinue = new Command (GettextCatalog.GetString ("Continue"));
					var cmdProjectOnly = new Command (GettextCatalog.GetString ("Skip"));
					var cmdCancel = new Command (GettextCatalog.GetString ("Cancel"));
					question.Buttons.Add (cmdContinue);
					question.Buttons.Add (cmdProjectOnly);
					question.Buttons.Add (cmdCancel);
					question.DefaultButton = 0;

					Xwt.Toolkit.NativeEngine.Invoke (delegate {
						var result = MessageDialog.AskQuestion (question);
						if (result != cmdCancel) {
							if (result == cmdContinue) {
								foreach (var project in addProjects) {
									if (question.GetOptionValue (project.Key)) {
										servicesToAdd.Add (project.Value.GetConnectedServicesBinding ()?.SupportedServices.FirstOrDefault (s => s.Id == service.Id));
									}
								}
							}

							AddSelectedServices (service, servicesToAdd);
							IdeApp.CommandService.DispatchCommand (MonoDevelop.ConnectedServices.Commands.AddServiceTelemetry, service.Id);
						}
					});
				} else {
					AddSelectedServices (service, servicesToAdd);
					IdeApp.CommandService.DispatchCommand (MonoDevelop.ConnectedServices.Commands.AddServiceTelemetry, service.Id);
				}
			}
		}

		async void AddSelectedServices(IConnectedService service, List<IConnectedService> others)
		{
			await service.AddToProject ();
			foreach (var svc in others) {
				await svc.AddToProject ();
			}
		}

		void UpdateServiceStatus ()
		{
			if (Service == null)
				return;
			Runtime.RunInMainThread (delegate {

				StopIconAnimations ();
				statusWidget.Visible = !showDetails && Service.Status != Status.NotAdded;
				addButton.Sensitive = Service.Status == Status.NotAdded;

				switch (Service.Status) {
				case Status.NotAdded:
					addButton.Label = GettextCatalog.GetString ("Add");
					addButton.Image = statusIcon.Image = null;

					// if the service has just been added/removed then the document view won't know this and does not have the correct DocumentObject
					// tell it to update
					ConnectedServices.LocateServiceView (this.Service.Project)?.UpdateCurrentNode ();
					break;
				case Status.Added:
					addButton.Label = statusText.Text = GettextCatalog.GetString ("Added");
					addButton.Image = ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small).WithAlpha (0.4);
					statusIcon.Image = ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small);

					// if the service has just been added/removed then the document view won't know this and does not have the correct DocumentObject
					// tell it to update
					ConnectedServices.LocateServiceView (this.Service.Project)?.UpdateCurrentNode ();
					break;
				case Status.Adding:
				case Status.Removing:
					addButton.Label = statusText.Text =
						Service.Status == Status.Adding
						? GettextCatalog.GetString ("Adding\u2026")
						: GettextCatalog.GetString ("Removing\u2026");
					if (statusIconAnimation == null) {
						if (animatedStatusIcon != null) {
							statusIcon.Image = animatedStatusIcon.FirstFrame;
							addButton.Image = animatedStatusIcon.FirstFrame.WithAlpha (0.4);
							statusIconAnimation = animatedStatusIcon.StartAnimation (p => {
								statusIcon.Image = p;
								addButton.Image = p.WithAlpha (0.4);
							});
						} else {
							statusIcon.Image = ImageService.GetIcon ("md-spinner-16").WithSize (Xwt.IconSize.Small);
							addButton.Image = ImageService.GetIcon ("md-spinner-16").WithSize (Xwt.IconSize.Small).WithAlpha (0.4);
						}
					}
					break;
				}
			});
		}

		void UpdateDescription()
		{
			if (this.service == null) {
				return;
			}

			if (showDetails) {
				description.Markup = "<span foreground='" + Styles.SecondaryTextColor.ToHexString (false) + "'>" + (service.DetailsDescription ?? service.Description) + "</span>";
			} else {
				description.Markup = "<span foreground='" + Styles.SecondaryTextColor.ToHexString (false) + "'>" + service.Description + "</span>";
			}
		}

		void StopIconAnimations ()
		{
			if (statusIconAnimation != null) {
				statusIconAnimation.Dispose ();
				statusIconAnimation = null;
			}
		}

		void HandleServiceStatusChanged (object sender, StatusChangedEventArgs e)
		{
			UpdateServiceStatus ();
			if (e.DidAddingFail) {
				Runtime.RunInMainThread (delegate {
					addButton.Image = ImageService.GetIcon ("md-error").WithSize (IconSize.Small);
					addButton.Label = GettextCatalog.GetString ("Retry");
				});
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				StopIconAnimations ();

			if (service != null) {
				service.StatusChanged -= HandleServiceStatusChanged;
				service = null;
			}
			base.Dispose (disposing);
		}
	}
}
