using System;
using MonoDevelop.Services;

class T
{
	static void Main ()
	{
		MonoDevelop.Gui.Utils.Vfs.Init ();
		FdoRecentFiles frf = new FdoRecentFiles ();	
		frf.AddFile ("test.cs");
		frf.AddProject ("test.cmbx");

		Console.WriteLine ("Recent Files:");
		foreach (RecentItem ri in frf.RecentFiles)
			Console.WriteLine ("{0} {1} {2}", ri.Uri, ri.Group, ri.Timestamp);

		Console.WriteLine ("Recent Projects:");
		foreach (RecentItem ri in frf.RecentProjects)
			Console.WriteLine ("{0} {1} {2}", ri.Uri, ri.Group, ri.Timestamp);
	}
}
