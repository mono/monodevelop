
using System;
using Gtk;
using Gdk;
using System.Text;

namespace Stetic.Editor
{
	public class TimeSpanEditorCell: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			return ((TimeSpan)Value).ToString ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new TimeSpanEditor ();
		}
	}
	
	public class TimeSpanEditor: Gtk.HBox, IPropertyEditor
	{
		Gtk.Entry entry;
		TimeSpan time;
		
		public TimeSpanEditor()
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
				time = (TimeSpan) value;
				entry.Changed -= OnChanged;
				entry.Text = time.ToString ();
				entry.Changed += OnChanged;
			}
		}
		
		void OnChanged (object o, EventArgs a)
		{
			string s = entry.Text;
			
			try {
				time = TimeSpan.Parse (s);
				if (ValueChanged != null)
					ValueChanged (this, a);
			} catch {
			}
		}
		
		public event EventHandler ValueChanged;
	}
}
