using System;
using System.Linq;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Default widget that displays the dependencies for a service
	/// </summary>
	class DependenciesSectionWidget : Components.AbstractXwtControl
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

			if (this.section.Service.Dependencies.Length == 0) {
				widget.PackStart (new Label { Text = GettextCatalog.GetString ("This service has no dependencies") });
				return;
			}

			bool firstCategory = true;

			foreach (var category in this.section.Service.Dependencies.Select (d => d.Category).Distinct ()) {
				var categoryIcon = new ImageView (category.Icon.WithSize (IconSize.Small));
				var categoryLabel = new Label (category.Name);
				var categoryBox = new HBox ();

				if (!firstCategory)
					categoryBox.MarginTop += 5;

				categoryBox.PackStart (categoryIcon);
				categoryBox.PackStart (categoryLabel);
				widget.PackStart (categoryBox);

				foreach (var dependency in this.section.Service.Dependencies.Where (d => d.Category == category)) {
					widget.PackStart (new DependencyWidget (section.Service, dependency) {
						MarginLeft = category.Icon.Size.Width / 2
					});
				}

				if (firstCategory)
					firstCategory = false;
			}
		}
	}

	class DependencyWidget : Widget
	{
		HBox container;
		ImageView iconView, statusIconView;
		Label nameLabel, statusLabel;
		AnimatedIcon animatedStatusIcon;
		IDisposable statusIconAnimation;

		public IConnectedServiceDependency Dependency { get; private set; }
		public IConnectedService Service { get; private set; }
		
		public DependencyWidget (IConnectedService service, IConnectedServiceDependency dependency)
		{
			Dependency = dependency;
			Service = service;

			iconView = new ImageView (dependency.Icon.WithSize (Xwt.IconSize.Small));
			nameLabel = new Label (dependency.DisplayName);

			statusIconView = new ImageView ();
			statusLabel = new Label ();
			statusLabel.LinkClicked += (sender, e) => {
				if (dependency.Status == Status.NotAdded)
					dependency.AddToProject (CancellationToken.None);
				e.SetHandled ();
			};

			container = new HBox ();
			container.PackStart (iconView);
			container.PackStart (nameLabel);
			container.PackStart (statusIconView);
			container.PackStart (statusLabel);

			Content = container;
			Update ();

			dependency.StatusChanged += HandleDependencyStatusChange;
			service.StatusChanged += HandleServiceStatusChanged;
		}

		void SetStatusIcon (IconId stockId, double alpha = 1.0)
		{
			animatedStatusIcon = null;
			if (statusIconAnimation != null) {
				statusIconAnimation.Dispose ();
				statusIconAnimation = null;
			}
			if (stockId.IsNull) {
				statusIconView.Visible = false;
				return;
			}
			if (ImageService.IsAnimation (stockId, Gtk.IconSize.Menu)) {
				animatedStatusIcon = ImageService.GetAnimatedIcon (stockId, Gtk.IconSize.Menu);
				statusIconView.Image = animatedStatusIcon.FirstFrame.WithAlpha (alpha);
				statusIconAnimation = animatedStatusIcon.StartAnimation (p => {
					statusIconView.Image = p.WithAlpha (alpha);
				});
			} else
				statusIconView.Image = ImageService.GetIcon (stockId).WithSize (Xwt.IconSize.Small).WithAlpha (alpha);
			statusIconView.Visible = true;
		}

		void Update ()
		{
			if (Service.Status == Status.Added) {
				if (Dependency.Status == Status.Added) {
					nameLabel.TextColor = Styles.BaseForegroundColor;
					iconView.Image = Dependency.Icon.WithSize (Xwt.IconSize.Small);
					SetStatusIcon (IconId.Null);
					statusLabel.Visible = false;
				} else if (Dependency.Status == Status.NotAdded) {
					nameLabel.TextColor = Styles.SecondaryTextColor;
					iconView.Image = Dependency.Icon.WithSize (Xwt.IconSize.Small).WithAlpha (0.4);
					SetStatusIcon ("md-warning");
					statusLabel.Markup = "<a href=''>" + GettextCatalog.GetString ("Add Dependency") + "</a>";
					statusLabel.Visible = true;
				}
			} else {
				nameLabel.TextColor = Styles.BaseForegroundColor;
				iconView.Image = Dependency.Icon.WithSize (Xwt.IconSize.Small);
				statusLabel.Visible = false;
				if (Dependency.Status == Status.Added)
					SetStatusIcon ("md-done");
				else if (Dependency.Status == Status.Adding)
					SetStatusIcon ("md-spinner-16");
				else
					SetStatusIcon (IconId.Null);
			}
		}

		void HandleDependencyStatusChange (object sender, StatusChangedEventArgs e)
		{
			if (e.IsAdding) {
				this.HandleDependencyAdding ();
			} else if (e.WasAdded || (e.NewStatus == Status.Added && e.OldStatus == Status.Added)) {
				// we check for WasAdded or a double up of Added states, the double up comes from when the user uses the 
				// link to restore the package - we get 2 events, the first from the AddToProject and the second from the
				// event raised from the packagemanagement system
				this.HandleDependencyAdded ();
			} else if (e.WasRemoved) {
				this.HandleDependencyRemoved ();
			} else if (e.DidAddingFail) {
				this.HandleDependencyAddingFailed ();
			}
		}

		void HandleDependencyAdding ()
		{
			Runtime.RunInMainThread (delegate {
				nameLabel.TextColor = Styles.SecondaryTextColor;
				iconView.Image = Dependency.Icon.WithSize (Xwt.IconSize.Small).WithAlpha (0.4);

				SetStatusIcon ("md-spinner-16");
				statusLabel.Markup = GettextCatalog.GetString ("Adding\u2026");
				statusLabel.Visible = true;
			});
		}

		void HandleDependencyAdded ()
		{
			Runtime.RunInMainThread (() => Update ());
		}

		void HandleDependencyRemoved ()
		{
			Runtime.RunInMainThread (() => Update ());
		}

		void HandleDependencyAddingFailed ()
		{
			Runtime.RunInMainThread (delegate {
				nameLabel.TextColor = Styles.SecondaryTextColor;
				iconView.Image = Dependency.Icon.WithSize (Xwt.IconSize.Small).WithAlpha (0.4);
				SetStatusIcon ("md-error");
				statusLabel.Markup = GettextCatalog.GetString ("Adding failed") + " (<a>" + GettextCatalog.GetString ("Retry") + "</a>)";
				statusLabel.Visible = true;
			});
		}

		void HandleServiceStatusChanged (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => Update ());
		}

		protected override void Dispose (bool disposing)
		{
			if (Dependency != null) {
				Dependency.StatusChanged -= HandleDependencyStatusChange;
				Dependency = null;
			}
			if (Service != null) {
				Service.StatusChanged -= HandleServiceStatusChanged;
				Service = null;
			}
			if (disposing && statusIconAnimation != null) {
				statusIconAnimation.Dispose ();
				statusIconAnimation = null;
			}
			base.Dispose (disposing);
		}
	}
}
