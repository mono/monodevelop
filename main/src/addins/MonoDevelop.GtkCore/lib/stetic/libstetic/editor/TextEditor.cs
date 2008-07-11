
using System;
using Gtk;
using Gdk;

namespace Stetic.Editor
{
	public class TextEditor: Gtk.HBox, IPropertyEditor
	{
		protected Gtk.Entry entry;
		protected Gtk.Button button;
		PropertyDescriptor prop;
		object obj;
		
		public TextEditor()
		{
			Spacing = 3;
			entry = new Entry ();
			entry.HasFrame = false;
			PackStart (entry, true, true, 0);
			button = new Button ("...");
			button.Relief = ReliefStyle.Half;
			PackStart (button, false, false, 0);
			button.Clicked += ButtonClicked;
			entry.Activated += TextChanged;
			ShowAll ();
		}
		
		void ButtonClicked (object s, EventArgs a)
		{
			using (TextEditorDialog dlg = new TextEditorDialog ()) {
				dlg.Text = entry.Text;
				dlg.SetTranslatable (prop.Translatable);
				if (prop.Translatable) {
					dlg.Translated = prop.IsTranslated (obj);
					dlg.ContextHint = prop.TranslationContext (obj);
					dlg.Comment = prop.TranslationComment (obj);
				}
				if (dlg.Run () == (int) ResponseType.Ok) {
					if (prop.Translatable) {
						prop.SetTranslated (obj, dlg.Translated);
						if (dlg.Translated) {
							prop.SetTranslationComment (obj, dlg.Comment);
							prop.SetTranslationContext (obj, dlg.ContextHint);
						}
					}
					entry.Text = dlg.Text;
					TextChanged (null, null);
				}
			}
		}
		
		void TextChanged (object s, EventArgs a)
		{
			if (ValueChanged != null)
				ValueChanged (this, a);
		}
		
		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(string))
				throw new InvalidOperationException ("TextEditor only can edit string properties");
			prop = descriptor;
		}
		
		public void AttachObject (object obj)
		{
			this.obj = obj;
		}
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		public object Value {
			get { return entry.Text; }
			set { entry.Text = value != null ? (string) value : ""; }
		}

		// To be fired when the edited value changes.
		public event EventHandler ValueChanged;	
	}
}
