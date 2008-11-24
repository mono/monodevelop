using System;
using System.Xml;
using System.Collections;

namespace Stetic.Wrapper {

	public class Dialog : Window {

		Stetic.Wrapper.ButtonBox ActionArea;

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);

			ActionArea = (ButtonBox)Container.Lookup (dialog.ActionArea);
			ActionArea.SetActionDialog (this);

			if (!initialized) {
				dialog.HasSeparator = false;
				if (!initialized && dialog.VBox.Children.Length == 1) {
					Container vbox = Container.Lookup (dialog.VBox);
					Placeholder ph = vbox.AddPlaceholder ();
					ph.SetSizeRequest (200, 200);
					Buttons = 1;
				}
			} else
				ButtonsChanged (ActionArea);

			ActionArea.ContentsChanged += ButtonsChanged;
		}
		
		public override void Dispose ()
		{
			ActionArea.ContentsChanged -= ButtonsChanged;
			ActionArea.SetActionDialog (null);
			base.Dispose ();
		}
		
		protected override void ReadChildren (ObjectReader reader, XmlElement elem)
		{
			// Ignore changes in the buttons while loading
			ActionArea.ContentsChanged -= ButtonsChanged;
			base.ReadChildren (reader, elem);
			ActionArea.ContentsChanged += ButtonsChanged;
			ActionArea.SetActionDialog (this);
		}

		Gtk.Dialog dialog {
			get {
				return (Gtk.Dialog)Wrapped;
			}
		}

		public int Buttons {
			get {
				return ActionArea.Size - ExtraButtons;
			}
			set {
				ActionArea.Size = value + ExtraButtons;
				EmitNotify ("Buttons");
			}
		}

		int ExtraButtons {
			get {
				return helpButton == null ? 0 : 1;
			}
		}

		Gtk.Button helpButton;

		public bool HelpButton {
			get {
				return helpButton != null;
			}
			set {
				if (HelpButton == value)
					return;

				if (value) {
					helpButton = AddButton (Gtk.Stock.Help, Gtk.ResponseType.Help, false);
					// Make it the first child, so that decreasing
					// Buttons won't delete it
					dialog.ActionArea.ReorderChild (helpButton, 0);
				} else {
					helpButton.Destroy ();
					helpButton = null;
				}

				EmitNotify ("HelpButton");
			}
		}

		// Check that a button is the Help button
		bool ButtonIsHelp (Gtk.Button button)
		{
			return (button.UseStock &&
				button.Label == Gtk.Stock.Help &&
				dialog.ActionArea.GetChildSecondary (button));
		}

		Gtk.Button AddButton (string stockId, Gtk.ResponseType response, bool hasDefault)
		{
			Stetic.Wrapper.Button wrapper;
			Gtk.Button button;

			button = (Gtk.Button)Registry.NewInstance ("Gtk.Button", proj);
			wrapper = (Stetic.Wrapper.Button) ObjectWrapper.Lookup (button);
			if (stockId != null) {
				wrapper.Type = Button.ButtonType.StockItem;
				wrapper.StockId = stockId;
			} else {
				wrapper.Type = Button.ButtonType.TextOnly;
				wrapper.Label = button.Name;
			}
			wrapper.ResponseId = (int)response;

			Stetic.Wrapper.Container actionArea = Stetic.Wrapper.Container.Lookup (dialog.ActionArea);
			actionArea.Add (button);

			button.CanDefault = true;
			wrapper.HasDefault = hasDefault;

			if (stockId == Gtk.Stock.Help)
				((Gtk.ButtonBox)actionArea.Wrapped).SetChildSecondary (button, true);

			return button;
		}

		void ButtonsChanged (Container container)
		{
			Gtk.Widget[] children = dialog.ActionArea.Children;

			// If the user manually removes (or breaks) the Help button,
			// uncheck the corresponding property
			if (helpButton != null) {
				if (Array.IndexOf (children, helpButton) == -1 ||
				    !ButtonIsHelp (helpButton)) {
					helpButton = null;
					EmitNotify ("HelpButton");
				}
			}

			// If the user manually creates a Help button, set the property
			if (helpButton == null) {
				foreach (Gtk.Widget w in children) {
					Gtk.Button button = w as Gtk.Button;
					if (button != null && ButtonIsHelp (button)) {
						helpButton = button;
						dialog.ActionArea.ReorderChild (helpButton, 0);
						EmitNotify ("HelpButton");
						break;
					}
				}
			}

			// If the user removed all (non-Secondary) buttons, add back a
			// single custom button
			if (Buttons == 0)
				Buttons = 1;
		}
	}
}
