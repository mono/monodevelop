
using System;

namespace VersionControl.Service.Cvs
{
	public class CvsSystem: VersionControlSystem
	{
		public override string Name {
			get { return "CVS"; }
		}
		
		protected override Repository OnCreateRepositoryInstance ()
		{
			return new CvsRepository ();
		}
		
		public override Gtk.Widget CreateRepositoryEditor (Repository repo)
		{
			return new UrlBasedRepositoryEditor ((CvsRepository) repo);
		}
	}
}
