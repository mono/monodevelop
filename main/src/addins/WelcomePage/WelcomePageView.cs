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

using System.Net;
using System.Xml;
using System.IO;

namespace MonoDevelop.WelcomePage
{	
	public abstract class WelcomePageView : AbstractViewContent
	{
		string datadir;
		bool loadingProject;
		EventHandler recentChangesHandler;
		
		// netNewsXml is where online the news.xml file can be found
		static string netNewsXml {
			get {
				return "http://mono.ximian.com/monodevelop-news/"
					+ Mono.Addins.AddinManager.CurrentAddin.Version 
					+ "/news.xml";
			}
		}
		
		public override string StockIconId {
			get { return Gtk.Stock.Home; }
		}
		
		public override void Load (string fileName) 
		{
		}

		public override bool IsFile
		{
			get { return false; }
		}
		
		public WelcomePageView () : base ()
		{
			this.ContentName = GettextCatalog.GetString ("Welcome");
			this.IsViewOnly = true;
			
			datadir = Path.GetDirectoryName (typeof(ShowWelcomePageHandler).Assembly.Location) + "/";

			if (PlatformID.Unix != Environment.OSVersion.Platform)
				datadir = datadir.Replace("\\","/");

			recentChangesHandler = (EventHandler) DispatchService.GuiDispatch (new EventHandler (RecentChangesHandler));
			IdeApp.Workbench.RecentOpen.RecentProjectChanged += recentChangesHandler;
			NewsUpdated += (EventHandler) DispatchService.GuiDispatch (new EventHandler (HandleNewsUpdate));
			
			UpdateNews ();
		}
		
		public XmlDocument GetUpdatedXmlDocument ()
		{
			string localCachedNewsFile = System.IO.Path.Combine (PropertyService.ConfigPath, "news.xml");
			
			Stream stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("WelcomePageContent.xml");
			XmlDocument contentDoc = new XmlDocument ();
			using (XmlTextReader reader = new XmlTextReader (stream))
				contentDoc.Load (reader);
			
			try {
				if (File.Exists (localCachedNewsFile)) {
					XmlDocument updateDoc = new XmlDocument (); 
					using (XmlTextReader reader = new XmlTextReader (localCachedNewsFile))
						updateDoc.Load (reader);
					
					XmlNode oNodeWhereInsert =
						contentDoc.SelectSingleNode ("/WelcomePage/Links[@_title=\"News Links\"]"); 
					foreach (XmlNode link in updateDoc.SelectSingleNode ("/links")) 
						oNodeWhereInsert.AppendChild (contentDoc.ImportNode (link,true)); 
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error updating Welcome Page content.", ex);
			}
			
			return contentDoc;
		}
		
		static void updateNewsXmlThread ()
		{
			//check to see if the online news file has been modified since it was last downloaded
			string netNewsXml = WelcomePageView.netNewsXml;
			LoggingService.LogInfo ("Updating Welcome Page from '{0}'.", netNewsXml);
			
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (netNewsXml);
			string localCachedNewsFile = System.IO.Path.Combine (PropertyService.ConfigPath, "news.xml");
			FileInfo localNewsXml = new FileInfo (localCachedNewsFile);
			if (localNewsXml.Exists)
				request.IfModifiedSince = localNewsXml.LastWriteTime;
			
			try {
				HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
				if (response.StatusCode ==  HttpStatusCode.OK) {
					Stream responseStream = response.GetResponseStream ();
					using (FileStream fs = File.Create (localCachedNewsFile)) {
						long avail = response.ContentLength;
						int position = 0;
						int readBytes = -1;
						byte[] buffer = new byte[2048];
						while (readBytes != 0) {							
							readBytes = responseStream.Read
								(buffer, position, (int) (avail > 2048 ? 2048 : avail));
							position += readBytes;
							avail -= readBytes;
							fs.Write (buffer, 0, readBytes);
						}
					}
				}
				NewsUpdated (null, EventArgs.Empty);
			} catch (System.Net.WebException wex) {
				HttpWebResponse httpResp = wex.Response as HttpWebResponse;
				if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotModified) {
					LoggingService.LogInfo ("Welcome Page already up-to-date.");
				} else if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotFound) {
					LoggingService.LogInfo ("Welcome Page update file not found.", netNewsXml);
				} else {
					LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", wex);
				}
			} catch (Exception ex) {
				LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
			}
			lock (updateLock)
				isUpdating = false;
		}
		
		public static void UpdateNews ()
		{
			if (!PropertyService.Get<bool>("WelcomePage.UpdateFromInternet", true))
				return;
			
			lock (updateLock) {
				if (isUpdating)
					return;
				else
					isUpdating = true;
			}
			System.Threading.ThreadStart ts = new System.Threading.ThreadStart (updateNewsXmlThread);
			System.Threading.Thread updateThread = new System.Threading.Thread (ts);
			updateThread.Start ();
		}
		
		static object updateLock = new object ();
		static bool isUpdating;
		static event EventHandler NewsUpdated;
		protected abstract void HandleNewsUpdate (object sender, EventArgs args);
		
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
					Gdk.ModifierType mtype;
					bool inWorkspace = Gtk.Global.GetCurrentEventState (out mtype) 
						&& (mtype & Gdk.ModifierType.ControlMask) != 0;
					IAsyncOperation oper = IdeApp.Workspace.OpenWorkspaceItem (fileuri.LocalPath, !inWorkspace);
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
					Gtk.MessageDialog md = new Gtk.MessageDialog
						(null, Gtk.DialogFlags.Modal| Gtk.DialogFlags.DestroyWithParent,
						 Gtk.MessageType.Error, Gtk.ButtonsType.Ok, msg);
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
				IdeApp.Workbench.StatusBar.ShowMessage (null);
			} else if (link.IndexOf ("project://") != -1) {
				string message = link;
				message = message.Substring (10);
				string msg = GettextCatalog.GetString ("Open solution {0}", message);
				if (IdeApp.Workspace.IsOpen)
					msg += " - " + GettextCatalog.GetString ("Hold Control key to open in current workspace.");
				IdeApp.Workbench.StatusBar.ShowMessage (msg);
			} else {
				string msg = GettextCatalog.GetString ("Open {0}", link);
				IdeApp.Workbench.StatusBar.ShowMessage (msg);
			}
		}

		public static WelcomePageView GetWelcomePage ()
		{
			return new WelcomePageFallbackView ();
		}

		protected virtual void RecentChangesHandler (object sender, EventArgs e)
		{
		}

		public override void Dispose ()
		{
			IdeApp.Workbench.RecentOpen.RecentProjectChanged -= recentChangesHandler;
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
