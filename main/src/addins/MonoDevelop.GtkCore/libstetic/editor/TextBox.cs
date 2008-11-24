using System;

namespace Stetic.Editor {

	public class TextBox : Gtk.ScrolledWindow {

		Gtk.TextView textview;

		public TextBox (int nlines)
		{
			ShadowType = Gtk.ShadowType.In;
			SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Automatic);

			textview = new Gtk.TextView ();
			textview.WrapMode = Gtk.WrapMode.Word;
			textview.Show ();
			Add (textview);

			Pango.Context ctx = textview.PangoContext;
			Pango.FontMetrics metrics = ctx.GetMetrics (textview.Style.FontDescription,
								    ctx.Language);
			int lineHeight = (metrics.Ascent + metrics.Descent) / (int)Pango.Scale.PangoScale;
			SetSizeRequest (-1, lineHeight * nlines);

			textview.Buffer.Changed += Buffer_Changed;
		}

		public Gtk.TextView TextView {
			get {
				return textview;
			}
		}

		public string Text {
			get {
				return textview.Buffer.Text;
			}
			set {
				textview.Buffer.Text = value;
			}
		}

		void Buffer_Changed (object obj, EventArgs args)
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}

		public event EventHandler Changed;
	}
}
