using System;
using AddInManager;

namespace MonoDevelop.Components.Commands
{
	public class ShowAddInManager : AbstractMenuCommand
	{
		public override void Run ()
		{
			using (AddInManagerDialog d = new AddInManagerDialog ()) {
				d.Run ();
				d.Hide ();
			}
		}
	}
}

