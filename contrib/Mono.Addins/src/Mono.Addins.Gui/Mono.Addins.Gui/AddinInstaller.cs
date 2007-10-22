

using System;
using Mono.Addins.Setup;
using Mono.Unix;

namespace Mono.Addins.Gui
{
	public class AddinInstaller: IAddinInstaller
	{
		public void InstallAddins (AddinRegistry reg, string message, string[] addinIds)
		{
			AddinInstallerDialog dlg = new AddinInstallerDialog (reg, message, addinIds);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Cancel)
					throw new InstallException (Catalog.GetString ("Installation cancelled"));
				else if (dlg.ErrMessage != null)
					throw new InstallException (dlg.ErrMessage);
			}
			finally {
				dlg.Destroy ();
			}
		}
	}
}
