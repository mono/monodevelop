using System;
using Xwt;

namespace MonoDevelop.ConnectedServices.Gui.ServicesTab
{
	/// <summary>
	/// Widget that displays the services gallery
	/// </summary>
	class ServicesGalleryWidget : VBox
	{
		public ServicesGalleryWidget ()
		{
			this.Build ();
		}

		/// <summary>
		/// Loads the given services into the gallery
		/// </summary>
		public void LoadServices(IConnectedService[] services)
		{
			
			// test code
			foreach (var service in services) {
				var label = new Label ("service: " + service.DisplayName);
				this.PackStart (label);

				var svc = service;
				var btn = new Button () { Label = "Add " + service.DisplayName };
				btn.Clicked += (sender, e) => {
					if (!svc.IsAdded) {
						svc.AddToProject ();
						var node = svc.Project.GetConnectedServicesBinding ().ServicesNode;
						node?.X ();
					}
				};

				this.PackStart (btn);
			}
		}

		/// <summary>
		/// Builds the widget
		/// </summary>
		void Build ()
		{
			var label = new Label ("gallery");
			this.PackStart (label);
		}
	}
}
