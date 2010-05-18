
using System;
using System.Collections;

namespace CustomWidgets
{
	
	
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TextSelector : Gtk.Bin
	{
		string[] values;
		
		public TextSelector()
		{
			Build ();
		}

		protected virtual void OnEntryChanged(object sender, System.EventArgs e)
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}

		protected virtual void OnComboboxChanged(object sender, System.EventArgs e)
		{
			entry.Text = values [combobox.Active];
		}

		protected virtual void OnUnaMsActivated(object sender, System.EventArgs e)
		{
			Console.WriteLine ("OPCIONAT!!!");
		}
		
		public string[] Values {
			get { return values; }
			set {
				values = value;
				foreach (string s in values) {
					combobox.AppendText (s);
				}
			}
		}
		
		public EventHandler Changed;
	}
}
