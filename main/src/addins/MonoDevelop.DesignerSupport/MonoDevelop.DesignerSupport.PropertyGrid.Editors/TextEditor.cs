//
// TextEditor.cs
//
// Author:
//   Lluis Sanchez Gual
//   Michael Hutchinson
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.ComponentModel;

using Gtk;
using Gdk;

namespace MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (string))]
	public class TextEditor: Gtk.HBox, IPropertyEditor
	{
		EditSession session;
		bool disposed;
		string initialText;
		Entry entry;
		
		public TextEditor()
		{
		}
		
		public void Initialize (EditSession session)
		{
			this.session = session;
			
			//if standard values are supported by the converter, then 
			//we list them in a combo
			if (session.Property.Converter.GetStandardValuesSupported (session))
			{
				ComboBoxEntry combo = ComboBoxEntry.NewText ();
				PackStart (combo, true, true, 0);
				combo.Changed += TextChanged;
				entry = combo.Entry;
				entry.HeightRequest = combo.SizeRequest ().Height;
				
				//but if the converter doesn't allow nonstandard values, 
				// then we make the entry uneditable
				if (session.Property.Converter.GetStandardValuesExclusive (session)) {
					entry.IsEditable = false;
					entry.CanFocus = false;
				}
				
				//fill the list
				foreach (object stdValue in session.Property.Converter.GetStandardValues (session)) {
					combo.AppendText (session.Property.Converter.ConvertToString (session, stdValue));
				}
			}
			// no standard values, so just use an entry
			else {
				entry = new Entry ();
				PackStart (entry, true, true, 0);
			}
			
			//either way we have an entry to play with
			entry.HasFrame = false;
			entry.Activated += TextChanged;
			
			if (ShouldShowDialogButton ()) {
				Button button = new Button ("...");
				PackStart (button, false, false, 0);
				button.Clicked += ButtonClicked;
			}
			
			Spacing = 3;
			ShowAll ();
		}
		
		protected virtual bool ShouldShowDialogButton ()
		{
			//if the object's Localizable, show a dialog, since the text's likely to be more substantial
			LocalizableAttribute at = (LocalizableAttribute) session.Property.Attributes [typeof(LocalizableAttribute)];
			return (at != null && at.IsLocalizable);
		}
		
		void ButtonClicked (object s, EventArgs a)
		{
			using (TextEditorDialog dlg = new TextEditorDialog ()) {
				dlg.Text = entry.Text;
				if (dlg.Run () == (int) ResponseType.Ok) {
					entry.Text = dlg.Text;
					TextChanged (null, null);
				}
			}
		}
		
		void TextChanged (object s, EventArgs a)
		{
			if (initialText == entry.Text)
				return;
			
			bool valid = false;
			if (session.Property.Converter.IsValid (session, entry.Text)) {
				try {
					session.Property.Converter.ConvertFromString (session, entry.Text);
					initialText = entry.Text;
					if (ValueChanged != null)
						ValueChanged (this, a);
					valid = true;
				} catch {
					// Invalid format
				}
			}
			
			if (valid)
				entry.ModifyFg (Gtk.StateType.Normal);
			else
				entry.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (255, 0, 0));
		}
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		public object Value {
			get {
				return session.Property.Converter.ConvertFromString (session, entry.Text);
			}
			set {
				string val = session.Property.Converter.ConvertToString (session, value);
				initialText = entry.Text;
				entry.Text = val ?? string.Empty;
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			((IDisposable)this).Dispose ();
		}

		void IDisposable.Dispose ()
		{
			if (!disposed && initialText != entry.Text) {
				TextChanged (null, null);
			}
			disposed = true;
		}

		// To be fired when the edited value changes.
		public event EventHandler ValueChanged;	
	}
}
