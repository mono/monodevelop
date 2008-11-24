
using System;

namespace Stetic.Editor
{
	public class TextEditorDialog: IDisposable
	{
		[Glade.Widget] Gtk.TextView textview;
		[Glade.Widget] Gtk.CheckButton checkTranslatable;
		[Glade.Widget] Gtk.Entry entryContext;
		[Glade.Widget] Gtk.Entry entryComment;
		[Glade.Widget] Gtk.Table translationTable;
		[Glade.Widget ("TextEditorDialog")] Gtk.Dialog dialog;
		
		public TextEditorDialog ()
		{
			Glade.XML xml = new Glade.XML (null, "stetic.glade", "TextEditorDialog", null);
			xml.Autoconnect (this);
			entryContext.Sensitive = entryComment.Sensitive = false;
		}
		
		public string Text {
			get { return textview.Buffer.Text; }
			set { textview.Buffer.Text = value; }
		}
		
		public string ContextHint {
			get { return entryContext.Text; }
			set { entryContext.Text = value != null ? value : ""; }
		}
		
		public string Comment {
			get { return entryComment.Text; }
			set { entryComment.Text = value != null ? value : ""; }
		}
		
		public bool Translated {
			get { return checkTranslatable.Active; }
			set { checkTranslatable.Active = value; }
		}
		
		public void SetTranslatable (bool translatable)
		{
			if (!translatable) {
				translationTable.Visible = false;
				checkTranslatable.Visible = false;
			}
		}
		
		protected void OnTranslatableToggled (object s, EventArgs a)
		{
			entryContext.Sensitive = checkTranslatable.Active;
			entryComment.Sensitive = checkTranslatable.Active;
		}
		
		public int Run ()
		{
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
	}
}
