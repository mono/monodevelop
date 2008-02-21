
using System;
using System.Text;
using Gtk;
using Gdk;

namespace Stetic.Editor
{
	public class Identifier: Gtk.Entry, IPropertyEditor
	{
		string id;
		int min = -1;
		int max = -1;
		
		public Identifier()
		{
			ShowAll ();
			HasFrame = false;
		}
		
		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(string))
				throw new InvalidOperationException ("TextEditor only can edit string properties");
				
			try {
				if (descriptor.Minimum != null)
					min = Convert.ToInt32 (descriptor.Minimum);
			} catch {}
				
			try {
				if (descriptor.Maximum != null)
					max = Convert.ToInt32 (descriptor.Maximum);
			} catch {}
		}
		
		public void AttachObject (object obj)
		{
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus e)
		{
			DoChanged ();
			return base.OnFocusOutEvent (e);
		}
		
		void DoChanged ()
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in Text) {
				if (char.IsLetterOrDigit (c) || c == '_')
					sb.Append (c);
			}
			
			string s = sb.ToString ();
			if (min != -1 && s.Length < min)
				return;
			if (max != -1 && s.Length > max)
				return;
			
			if (s == Text) {
				id = Text;
				if (ValueChanged != null)
					ValueChanged (this, EventArgs.Empty);
			} else {
				Text = s;
			}
		}
		
		public object Value {
			get { return id; }
			set { id = Text = (value != null ? (string) value : ""); }
		}

		// To be fired when the edited value changes.
		public event EventHandler ValueChanged;	
	}
}
