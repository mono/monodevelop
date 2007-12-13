/*
Copyright (c) 2005 Scott Ellington

Permission is hereby granted, free of charge, to any person 
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without 
restriction, including without limitation the rights to use, 
copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following 
conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
OTHER DEALINGS IN THE SOFTWARE. 
*/
using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.WebBrowser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

using Freedesktop.RecentFiles;

using System.IO;

namespace MonoDevelop.WelcomePage
{	
	public abstract class WelcomePageView : AbstractViewContent
	{
		string datadir;
		bool loadingProject;
		
		public override string StockIconId {
			get { return Gtk.Stock.Home; }
		}
		
		public override void Load (string fileName) 
		{
		}

		public WelcomePageView () : base ()
		{
			this.ContentName = GettextCatalog.GetString ("Welcome");
			this.IsViewOnly = true;
			
			datadir = Path.GetDirectoryName (typeof(ShowWelcomePageHandler).Assembly.Location) + "/";

			if (PlatformID.Unix != Environment.OSVersion.Platform)
				datadir = datadir.Replace("\\","/");

			IdeApp.Workbench.RecentOpen.RecentProjectChanged += RecentChangesHandler;
		}
		
		public void HandleLinkAction (string uri)
		{
			// HACK: Necessary for win32; I have no idea why
			if (PlatformID.Unix != Environment.OSVersion.Platform)
				Console.WriteLine ("WelcomePage: Handling URI: " + uri);

			if (uri.StartsWith ("project://")) {
				if (loadingProject)
					return;
					
				string projectUri = uri.Substring (10);			
				Uri fileuri = new Uri (projectUri);
				try {
					loadingProject = true;
					IAsyncOperation oper = IdeApp.ProjectOperations.OpenCombine (fileuri.LocalPath);
					oper.WaitForCompleted ();
				} finally {
					loadingProject = false;
				}
			} else if (uri.StartsWith ("monodevelop://")) {
				// Launch MonoDevelop Gui Commands
				switch (uri.Substring (14))
				{
					case "NewProject":
						IdeApp.CommandService.DispatchCommand (FileCommands.NewProject);
						break;
					case "OpenFile":
						IdeApp.CommandService.DispatchCommand (FileCommands.OpenFile);
						break;
				}
			}
			else
			{
				//Launch the Uri externally
				try {
					IdeApp.Services.PlatformService.ShowUrl (uri);
				} catch (Exception) {
					string msg = String.Format (GettextCatalog.GetString ("Could not open the url {0}"), uri);
					Gtk.MessageDialog md = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, msg);
					try {
						md.Run ();
						md.Hide ();
					} finally {
						md.Destroy ();
					}
				}
			}
		}
		
		public void SetLinkStatus (string link)
		{
			if (String.IsNullOrEmpty (link) || link.IndexOf ("monodevelop://") != -1) {
				IdeApp.Workbench.StatusBar.SetMessage (null);
			} else {
				string message = link;
				if (link.IndexOf ("project://") != -1) 
					message = message.Substring (10);
				IdeApp.Workbench.StatusBar.SetMessage (message);
			}
		}
		
		public static WelcomePageView GetWelcomePage ()
		{
			//FIXME: make the HTML version worth using
			//if (WebBrowserService.CanGetWebBrowser)
			//	return new WelcomePageBrowser ();
			//else
				return new WelcomePageFallbackView ();
		}

		protected virtual void RecentChangesHandler (object sender, EventArgs e)
		{
		}

		public override void Dispose ()
		{
			IdeApp.Workbench.RecentOpen.RecentProjectChanged -= RecentChangesHandler;
		}
		
		public static string TimeSinceEdited (DateTime prjtime)
		{
			TimeSpan sincelast = DateTime.UtcNow - prjtime;

			if (sincelast.Days >= 1)
				return GettextCatalog.GetPluralString ("{0} day", "{0} days", sincelast.Days, sincelast.Days);
			if (sincelast.Hours >= 1)
				return GettextCatalog.GetPluralString ("{0} hour", "{0} hours", sincelast.Hours, sincelast.Hours);
			if (sincelast.Minutes > 0)
				return GettextCatalog.GetPluralString ("{0} minute", "{0} minutes", sincelast.Minutes, sincelast.Minutes);
			
			return GettextCatalog.GetString ("Less than a minute");
		}
		
		public virtual string DataDirectory {
			get { return datadir; }
		}
		
		public RecentItem[] RecentProjects {
			get {
				return IdeApp.Workbench.RecentOpen.RecentProject;
			}
		}
	}
}
