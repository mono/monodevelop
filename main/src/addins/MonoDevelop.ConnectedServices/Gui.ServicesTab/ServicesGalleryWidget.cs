using System;
using System.Linq;
using MonoDevelop.Core;
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

			enabledLabel = new Label (GettextCatalog.GetString ("Added")) { Font = Font.WithSize (14) };
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

				if (service.Status == Status.Added) {
					enabledList.PackStart (serviceWidget);
					enabledLabel.Visible = true;
				} else {
					availableList.PackStart (serviceWidget);
					availableLabel.Visible = true;
				}
				serviceWidget.ButtonReleased += HandleServiceWidgetButtonReleased;
				service.StatusChanged += HandleServiceStatusChanged;
				serviceWidget.Cursor = CursorType.Hand;
			}
		}

		void ClearServices ()
		{
			foreach (var widget in availableList.Children.Where ((c) => c is ServiceWidget).Cast<ServiceWidget> ()) {
				widget.Service.StatusChanged -= HandleServiceStatusChanged;
				widget.ButtonReleased -= HandleServiceWidgetButtonReleased;
			}
			availableList.Clear ();
			foreach (var widget in enabledList.Children.Where ((c) => c is ServiceWidget).Cast<ServiceWidget> ()) {
				widget.Service.StatusChanged -= HandleServiceStatusChanged;
				widget.ButtonReleased -= HandleServiceWidgetButtonReleased;
			}
			enabledList.Clear ();
			enabledLabel.Visible = false;
			availableLabel.Visible = false;
		}

		void HandleServiceStatusChanged (object sender, StatusChangedEventArgs e)
		{
			switch (e.NewStatus) {
			case Status.Added:
			case Status.NotAdded:
				this.HandleServiceAddedRemoved ((IConnectedService)sender);

				break;
			}
		}

		void HandleServiceAddedRemoved (IConnectedService service)
		{
			//TODO: sort the lists
			Runtime.RunInMainThread (delegate {
				if (service.Status == Status.Added) {
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

				enabledLabel.Visible = services.Any (s => s.Status == Status.Added);
				availableLabel.Visible = services.Any (s => s.Status == Status.NotAdded);
			});
		}

		void HandleServiceWidgetButtonReleased (object sender, ButtonEventArgs e)
		{
			if (e.Button == PointerButton.Left) {
				ServiceSelected?.Invoke (this, new ServiceEventArgs ((sender as ServiceWidget)?.Service));
			}
		}

		public event EventHandler<ServiceEventArgs> ServiceSelected;
	}
}
