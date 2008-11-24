using System;

namespace Stetic {

	public class MessageDialog : Gtk.Dialog {

		public MessageDialog ()
		{
			Resizable = false;
			HasSeparator = false;
			BorderWidth = 12;

			label = new Gtk.Label ();
			label.LineWrap = true;
			label.Selectable = true;
			label.UseMarkup = true;
			label.SetAlignment (0.0f, 0.0f);

			secondaryLabel = new Gtk.Label ();
			secondaryLabel.LineWrap = true;
			secondaryLabel.Selectable = true;
			secondaryLabel.UseMarkup = true;
			secondaryLabel.SetAlignment (0.0f, 0.0f);

			icon = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Dialog);
			icon.SetAlignment (0.5f, 0.0f);

			Gtk.StockItem item = Gtk.Stock.Lookup (icon.Stock);
			Title = item.Label;

			Gtk.HBox hbox = new Gtk.HBox (false, 12);
			Gtk.VBox vbox = new Gtk.VBox (false, 12);

			vbox.PackStart (label, false, false, 0);
			vbox.PackStart (secondaryLabel, true, true, 0);

			hbox.PackStart (icon, false, false, 0);
			hbox.PackStart (vbox, true, true, 0);

			VBox.PackStart (hbox, false, false, 0);
			hbox.ShowAll ();

			Buttons = Gtk.ButtonsType.OkCancel;
		}

		Gtk.Label label, secondaryLabel;
		Gtk.Image icon;

		public Gtk.MessageType MessageType {
			get {
				if (icon.Stock == Gtk.Stock.DialogInfo)
					return Gtk.MessageType.Info;
				else if (icon.Stock == Gtk.Stock.DialogQuestion)
					return Gtk.MessageType.Question;
				else if (icon.Stock == Gtk.Stock.DialogWarning)
					return Gtk.MessageType.Warning;
				else
					return Gtk.MessageType.Error;
			}
			set {
				Gtk.StockItem item = Gtk.Stock.Lookup (icon.Stock);
				bool setTitle = (Title == "") || (Title == item.Label);

				if (value == Gtk.MessageType.Info)
					icon.Stock = Gtk.Stock.DialogInfo;
				else if (value == Gtk.MessageType.Question)
					icon.Stock = Gtk.Stock.DialogQuestion;
				else if (value == Gtk.MessageType.Warning)
					icon.Stock = Gtk.Stock.DialogWarning;
				else
					icon.Stock = Gtk.Stock.DialogError;

				if (setTitle) {
					item = Gtk.Stock.Lookup (icon.Stock);
					Title = item.Label;
				}
			}
		}

		public string primaryText;
		public string PrimaryText {
			get {
				return primaryText;
			}
			set {
				primaryText = value;
				label.Markup = "<b>" + value + "</b>";
			}
		}

		public string SecondaryText {
			get {
				return secondaryLabel.Text;
			}
			set {
				secondaryLabel.Markup = value;
			}
		}

		Gtk.ButtonsType buttons;
		public new Gtk.ButtonsType Buttons {
			get {
				return buttons;
			}
			set {
				Gtk.Widget[] oldButtons = ActionArea.Children;
				foreach (Gtk.Widget w in oldButtons)
					ActionArea.Remove (w);

				buttons = value;
				switch (buttons) {
				case Gtk.ButtonsType.None:
					// nothing
					break;

				case Gtk.ButtonsType.Ok:
					AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);
					break;

				case Gtk.ButtonsType.Close:
					AddButton (Gtk.Stock.Close, Gtk.ResponseType.Close);
					break;

				case Gtk.ButtonsType.Cancel:
					AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
					break;

				case Gtk.ButtonsType.YesNo:
					AddButton (Gtk.Stock.No, Gtk.ResponseType.No);
					AddButton (Gtk.Stock.Yes, Gtk.ResponseType.Yes);
					break;

				case Gtk.ButtonsType.OkCancel:
					AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
					AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);
					break;
				}
			}
		}
	}
}
