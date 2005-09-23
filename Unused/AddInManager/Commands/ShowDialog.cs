using System;
using MonoDevelop.Core.AddIns.Codons;
using AddInManager;

namespace MonoDevelop.Commands
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

