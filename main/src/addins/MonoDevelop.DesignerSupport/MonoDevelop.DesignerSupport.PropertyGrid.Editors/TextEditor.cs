//
// TextEditor.cs
//
// Author:
//   Lluis Sanchez Gual
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
		protected Gtk.Entry entry;
		protected Gtk.Button button;
		PropertyDescriptor property;
		bool disposed;
		string initialText;
		
		public TextEditor()
		{
			Spacing = 3;
			entry = new Entry ();
			entry.HasFrame = false;
			PackStart (entry, true, true, 0);
			button = new Button ("...");
			PackStart (button, false, false, 0);
			button.Clicked += ButtonClicked;
			entry.Activated += TextChanged;
			ShowAll ();
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
			
			try {
				property.Converter.ConvertFromString (entry.Text);
				entry.ModifyFg (Gtk.StateType.Normal);
				initialText = entry.Text;
				if (ValueChanged != null)
					ValueChanged (this, a);
			} catch {
				// Invalid format
				entry.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (255, 0, 0));
			}
		}
		
		public void Initialize (EditSession session)
		{
			property = session.Property;
			LocalizableAttribute at = (LocalizableAttribute) property.Attributes [typeof(LocalizableAttribute)];
			button.Visible = (at != null && at.IsLocalizable);
		}
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		public object Value {
			get {
				return property.Converter.ConvertFromString (entry.Text);
			}
			set {
				string val = property.Converter.ConvertToString (value);
				entry.Text = val != null ? val : "";
				initialText = entry.Text;
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
