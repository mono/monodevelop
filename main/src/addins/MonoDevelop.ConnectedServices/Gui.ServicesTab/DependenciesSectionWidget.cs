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
			dependency.Added += HandleDependencyAdded;
			dependency.Adding += HandleDependencyAdding;
			dependency.AddingFailed += HandleDependencyAddingFailed;
			dependency.Removed += HandleDependencyRemoved;

			iconView = new ImageView (dependency.Icon);
			nameLabel = new Label (dependency.DisplayName);

			statusIconView = new ImageView ();
			statusLabel = new Label ();
			statusLabel.LinkClicked += (sender, e) => {
				if (!dependency.IsAdded)
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
		}

		void SetStatusIcon (IconId stockId)
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
				statusIconView.Image = animatedStatusIcon.FirstFrame;
				statusIconAnimation = animatedStatusIcon.StartAnimation (p => {
					statusIconView.Image = p;
				});
			} else
				statusIconView.Image = ImageService.GetIcon (stockId).WithSize (Xwt.IconSize.Small);
			statusIconView.Visible = true;
		}

		void Update ()
		{
			if (Service.IsAdded) {
				if (Dependency.IsAdded) {
					nameLabel.TextColor = Styles.BaseForegroundColor;
					iconView.Image = Dependency.Icon;
					SetStatusIcon (IconId.Null);
					statusLabel.Visible = false;
				} else {
					nameLabel.TextColor = Styles.DimTextColor;
					iconView.Image = Dependency.Icon.WithAlpha (0.4);
					SetStatusIcon ("md-warning");
					statusLabel.Markup = "<a href=''>" + GettextCatalog.GetString ("Add Dependency") + "</a>";
					statusLabel.Visible = true;
				}
			} else {
				nameLabel.TextColor = Styles.BaseForegroundColor;
				iconView.Image = Dependency.Icon;
				if (Dependency.IsAdded) {
					SetStatusIcon ("md-done");
					statusLabel.Visible = false;
				}
			}
			return;
			
			if (Dependency.IsAdded) {
				nameLabel.TextColor = Styles.BaseForegroundColor;
				iconView.Image = Dependency.Icon;
				SetStatusIcon ("md-done");
				statusLabel.Visible = false;
			} else {
				if (!Service.IsAdded) {
					nameLabel.TextColor = Styles.BaseForegroundColor;
					iconView.Image = Dependency.Icon;
					statusIconView.Visible = false;
					statusLabel.Visible = false;
				} else {
					nameLabel.TextColor = Styles.DimTextColor;
					iconView.Image = Dependency.Icon.WithAlpha (0.4);
					SetStatusIcon ("md-warning");
					statusLabel.Markup = "<a href=''>" + GettextCatalog.GetString ("Add Dependency") + "</a>";
					statusLabel.Visible = true;
				}
			}
		}

		void HandleDependencyAdding (object sender, EventArgs e)
		{
			nameLabel.TextColor = Styles.DimTextColor;
			iconView.Image = Dependency.Icon.WithAlpha (0.4);

			SetStatusIcon ("md-spinner-16");
			statusLabel.Markup = GettextCatalog.GetString ("Adding \u2026");
			statusLabel.Visible = true;
		}

		void HandleDependencyAdded (object sender, EventArgs e)
		{
			Update ();
		}

		void HandleDependencyRemoved (object sender, EventArgs e)
		{
			Update ();
		}

		void HandleDependencyAddingFailed (object sender, EventArgs e)
		{
			nameLabel.TextColor = Styles.DimTextColor;
			iconView.Image = Dependency.Icon.WithAlpha (0.4);
			SetStatusIcon ("md-error");
			statusLabel.Markup = GettextCatalog.GetString ("Adding failed") + ", <a href=''>" + GettextCatalog.GetString ("Retry") + "</a>";
			statusLabel.Visible = true;
		}

		protected override void Dispose (bool disposing)
		{
			if (Dependency != null) {
				Dependency.Added -= HandleDependencyAdded;
				Dependency.Removed -= HandleDependencyRemoved;
				Dependency = null;
			}
			if (disposing && statusIconAnimation != null) {
				statusIconAnimation.Dispose ();
				statusIconAnimation = null;
			}
			base.Dispose (disposing);
		}
	}
}
