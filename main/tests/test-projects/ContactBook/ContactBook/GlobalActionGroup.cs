
using System;

namespace ContactBook
{
	
	
	public partial class GlobalActionGroup : Gtk.ActionGroup
	{
		
		public GlobalActionGroup() : 
				base("ContactBook.GlobalActionGroup")
		{
			Build();
		}

		protected virtual void OnRunActivated(object sender, System.EventArgs e)
		{
			Console.WriteLine ("RUN!");
		}
	}
}
