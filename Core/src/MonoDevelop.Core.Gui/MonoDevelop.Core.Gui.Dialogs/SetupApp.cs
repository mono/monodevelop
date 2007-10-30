
using System;
using Mono.Addins;
using Mono.Addins.Setup;
using Mono.Addins.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui.Dialogs
{
	class SetupApp: IApplication
	{
		public int Run (string[] arguments)
		{
			Gtk.Application.Init ();
			AddinManagerWindow.Run (null);
			return 0;
		}
	}
}
