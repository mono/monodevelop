
using System;
using Gtk;
using Gdk;
using System.Text;

namespace Stetic.Editor
{
	public class DateTimeEditorCell: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			return ((DateTime)Value).ToLongDateString ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new DateTimeEditor ();
		}
	}
	
	public class DateTimeEditor: Gtk.HBox, IPropertyEditor
	{
		Gtk.Entry entry;
		DateTime time;
		
		public DateTimeEditor()
		{
			entry = new Gtk.Entry ();
			entry.Changed += OnChanged;
			PackStart (entry, true, true, 0);
			ShowAll ();
		}
		
		public void Initialize (PropertyDescriptor descriptor)
		{
		}
		
		public void AttachObject (object ob)
		{
		}
		
		public object Value {
			get { return time; }
			set {
				time = (DateTime) value;
				entry.Changed -= OnChanged;
				entry.Text = time.ToString ("G");
				entry.Changed += OnChanged;
			}
		}
		
		void OnChanged (object o, EventArgs a)
		{
			string s = entry.Text;
			
			foreach (string form in formats) {
				try {
					time = DateTime.ParseExact (s, form, null);
					if (ValueChanged != null)
						ValueChanged (this, a);
					break;
				} catch {
				}
			}
		}
		
		public event EventHandler ValueChanged;
		
		static string[] formats = {"u", "G", "g", "d", "T", "t"};
	}
}
