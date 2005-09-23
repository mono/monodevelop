// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Text;
using System.Reflection;

using Gtk;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Dialogs
{
	internal class AboutMonoDevelopTabPage : VBox
	{
		public AboutMonoDevelopTabPage ()
		{
			Label versionLabel = new Label ();
			Label licenseLabel = new Label ();
			Label copyrightLabel = new Label ();

			Version v = Assembly.GetEntryAssembly ().GetName ().Version;
			versionLabel.Markup = String.Format ("<b>{0}</b>\n    {1}", GettextCatalog.GetString ("Version"), v.Major + "." + v.Minor);
			HBox hboxVersion = new HBox ();
			hboxVersion.PackStart (versionLabel, false, false, 5);
			
			HBox hboxLicense = new HBox ();
			licenseLabel.Markup = String.Format ("<b>License</b>\n    {0}", GettextCatalog.GetString ("Released under the GNU General Public license."));
			hboxLicense.PackStart (licenseLabel, false, false, 5);

			HBox hboxCopyright = new HBox ();
			copyrightLabel.Markup = "<b>Copyright</b>\n    (c) 2000-2003 by icsharpcode.net\n    (c) 2004-2005 by MonoDevelop contributors";
			hboxCopyright.PackStart (copyrightLabel, false, false, 5);

			this.PackStart (hboxVersion, false, true, 0);
			this.PackStart (hboxLicense, false, true, 5);
			this.PackStart (hboxCopyright, false, true, 5);
			this.ShowAll ();
		}
	}
	
	internal class VersionInformationTabPage : VBox
	{
		public VersionInformationTabPage ()
		{
			TreeView listView = new TreeView ();
			listView.RulesHint = true;
			listView.AppendColumn (GettextCatalog.GetString ("Name"), new CellRendererText (), "text", 0);
			listView.AppendColumn (GettextCatalog.GetString ("Version"), new CellRendererText (), "text", 1);
			listView.AppendColumn (GettextCatalog.GetString ("Path"), new CellRendererText (), "text", 2);
			
			listView.Model = FillListView ();
			ScrolledWindow sw = new ScrolledWindow ();
			sw.Add (listView);
			this.PackStart (sw, true, true, 0);
		}
		
		ListStore FillListView()
		{
			ListStore store = new ListStore (typeof (string), typeof (string), typeof (string));
			
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				AssemblyName name = asm.GetName ();
				
				string loc;
				
				try {
					loc = System.IO.Path.GetFullPath (asm.Location);
				} catch {
					loc = GettextCatalog.GetString ("dynamic");
				}
				
				store.AppendValues (name.Name, name.Version.ToString (), loc);
			}
			
			store.SetSortColumnId (0, SortType.Ascending);
			return store;
		}
	}
}

