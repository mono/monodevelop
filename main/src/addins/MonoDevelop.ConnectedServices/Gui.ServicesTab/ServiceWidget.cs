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
	class ServiceWidget : FrameBox
	{
		HBox statusWidget;
		ImageView statusIcon;
		Label statusText;
		ImageView image;
		Label title, description, platforms;
		HBox platformWidget;
		Button addButton;
		AnimatedIcon animatedButtonIcon, animatedStatusIcon;
		IDisposable buttonIconAnimation, statusIconAnimation;
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
					service.Adding -= HandleServiceAdding;
					service.AddingFailed -= HandleServiceAddingFailed;
					service.Removed -= HandleServiceAddedRemoved;
					service.Removing -= HandleServiceRemoving;
				}
				
				service = value;
				image.Image = (service.GalleryIcon ?? ImageService.GetIcon ("md-service")).WithSize (IconSize.Medium);
				title.Markup = "<b>" + service.DisplayName + "</b>";
				description.Text = service.Description;

				platforms.Markup = string.Format ("<span color='{1}'><b>{0}</b></span>", service.SupportedPlatforms, Styles.SecondaryTextColor.ToHexString ());

				statusWidget.Visible = service.IsAdded && !showDetails;

				addButton.Visible = showDetails;
				addButton.Sensitive = !service.IsAdded;
				addButton.Image = service.IsAdded ? ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small).WithAlpha (0.4) : null;
				addButton.Label = service.IsAdded ? GettextCatalog.GetString ("Added") : GettextCatalog.GetString ("Add");

				service.Added += HandleServiceAddedRemoved;
				service.Adding += HandleServiceAdding;
				service.AddingFailed += HandleServiceAddingFailed;
				service.Removed += HandleServiceAddedRemoved;
				service.Removing += HandleServiceRemoving;

				this.ShowDetails = this.showDetails;
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
				statusWidget.Visible = service?.IsAdded == true && !showDetails;
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

			statusWidget = new HBox ();
			statusWidget.Spacing = 3;
			statusIcon = new ImageView (ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small));
			statusText = new Label (GettextCatalog.GetString ("Added")) {
				Font = Font.WithSize (12),
				TextColor = Styles.SecondaryTextColor,
			};
			statusWidget.PackStart (statusIcon);
			statusWidget.PackStart (statusText);
			statusWidget.Visible = false;

			addButton = new Button (GettextCatalog.GetString ("Add"));
			addButton.Visible = false;
			addButton.Clicked += HandleAddButtonClicked;

			if (ImageService.IsAnimation ("md-spinner-16", Gtk.IconSize.Menu)) {
				animatedButtonIcon = ImageService.GetAnimatedIcon ("md-spinner-16", Gtk.IconSize.Menu);
				animatedStatusIcon = ImageService.GetAnimatedIcon ("md-spinner-16", Gtk.IconSize.Menu);
			}

			var header = new HBox ();
			header.Spacing = 10;
			header.PackStart (image);
			header.PackStart (title);
			header.PackStart (statusWidget);

			var vbox = new VBox ();
			vbox.Spacing = 10;
			vbox.PackStart (header);

			description = new Label ();
			description.TextColor = Styles.SecondaryTextColor;
			description.Wrap = WrapMode.Word;
			description.Ellipsize = EllipsizeMode.None;

			platforms = new Label ();
			platforms.TextColor = Styles.SecondaryTextColor;

			platformWidget = new HBox ();
			platformWidget.PackStart (new Label { Text = GettextCatalog.GetString ("Platforms:"), TextColor = Styles.SecondaryTextColor }, false, (WidgetPlacement)4, (WidgetPlacement)4, -1, -1, 20, -1, -1);
			platformWidget.PackStart (platforms);

			vbox.PackStart (description);
			vbox.PackStart (platformWidget, false, (WidgetPlacement)4, (WidgetPlacement)4, -1, 10, -1, -1, -1);

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
				var addProjects = new Dictionary<string, DotNetProject> ();

				foreach (DotNetProject project in service.Project.ParentSolution.GetAllProjects ().Where (p => p is DotNetProject && p != service.Project)) {
					var svc = project.GetConnectedServicesBinding ()?.SupportedServices.FirstOrDefault (s => s.Id == service.Id);
					if (svc != null && !svc.IsAdded)
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
						}
					});
				}

				AddSelectedServices (service, servicesToAdd);
			}
		}

		async void AddSelectedServices(IConnectedService service, List<IConnectedService> others)
		{
			await service.AddToProject (false);
			foreach (var svc in others) {
				await svc.AddToProject (true);
			}
		}

		void HandleServiceAdding (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (delegate {
				if (!showDetails) {
					statusText.Text = GettextCatalog.GetString ("Adding\u2026");
					if (statusIconAnimation == null) {
						if (animatedStatusIcon != null) {
							statusIcon.Image = animatedStatusIcon.FirstFrame;
							buttonIconAnimation = animatedStatusIcon.StartAnimation (p => {
								statusIcon.Image = p;
							});
						} else
							statusIcon.Image = ImageService.GetIcon ("md-spinner-16").WithSize (Xwt.IconSize.Small);
					}
					statusWidget.Visible = true;
				} else {
					addButton.Label = GettextCatalog.GetString ("Adding\u2026");
					if (buttonIconAnimation == null) {
						if (animatedButtonIcon != null) {
							addButton.Image = animatedButtonIcon.FirstFrame;
							buttonIconAnimation = animatedButtonIcon.StartAnimation (p => {
								addButton.Image = p;
							});
						} else
							addButton.Image = ImageService.GetIcon ("md-spinner-16").WithSize (Xwt.IconSize.Small);
					}
					addButton.Sensitive = false;
				}
			});
		}

		void StopIconAnimations ()
		{
			if (buttonIconAnimation != null) {
				buttonIconAnimation.Dispose ();
				buttonIconAnimation = null;
			}
			if (statusIconAnimation != null) {
				statusIconAnimation.Dispose ();
				statusIconAnimation = null;
			}
		}

		void HandleServiceAddingFailed (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (delegate {
				statusWidget.Visible = false;
				statusIcon.Image = ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small);
				StopIconAnimations ();
				addButton.Image = ImageService.GetIcon ("md-error").WithSize (IconSize.Small);
				addButton.Label = GettextCatalog.GetString ("Retry");
				addButton.Sensitive = true;
			});
		}

		void HandleServiceAddedRemoved (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (delegate {
				statusIcon.Image = ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small);
				statusText.Text = GettextCatalog.GetString ("Added");
				statusWidget.Visible = Service.IsAdded && !showDetails;
				StopIconAnimations ();
				addButton.Image = service.IsAdded ? ImageService.GetIcon ("md-checkmark").WithSize (IconSize.Small).WithAlpha (0.4) : null;
				addButton.Label = service.IsAdded ? GettextCatalog.GetString ("Added") : GettextCatalog.GetString ("Add");
				addButton.Sensitive = !Service.IsAdded;

				// if the service has just been added then the document view won't know this and does not have the correct DocumentObject
				// tell it to update
				var servicesView = ConnectedServices.LocateServiceView (this.Service.Project);
				servicesView?.UpdateCurrentNode ();
			});
		}

		void HandleServiceRemoving (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (delegate {
				if (!showDetails) {
					statusText.Text = GettextCatalog.GetString ("Removing\u2026");
					if (statusIconAnimation == null) {
						if (animatedStatusIcon != null) {
							statusIcon.Image = animatedStatusIcon.FirstFrame;
							buttonIconAnimation = animatedStatusIcon.StartAnimation (p => {
								statusIcon.Image = p;
							});
						} else
							statusIcon.Image = ImageService.GetIcon ("md-spinner-16").WithSize (Xwt.IconSize.Small);
					}
					statusWidget.Visible = true;
				}
			});
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				StopIconAnimations ();

			if (service != null) {
				service.Added -= HandleServiceAddedRemoved;
				service.Adding -= HandleServiceAdding;
				service.AddingFailed -= HandleServiceAddingFailed;
				service.Removed -= HandleServiceAddedRemoved;
				service.Removing -= HandleServiceRemoving;
				service = null;
			}
			base.Dispose (disposing);
		}
	}
}
