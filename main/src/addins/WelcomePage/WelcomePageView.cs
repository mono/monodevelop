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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide;

namespace MonoDevelop.WelcomePage
{	
	class WelcomePageView : AbstractViewContent
	{
		bool loadingProject;
		EventHandler recentChangesHandler;
		
		WelcomePageWidget widget;
		ScrolledWindow scroller;
		
		EventHandler newsUpdatedHandler;
		
		// netNewsXml is where online the news.xml file can be found
		static string netNewsXml {
			get {
				return "http://www.monodevelop.com/files/news/news.xml";
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
		
		public override Widget Control {
			get { return scroller;  }
		}
		
		public WelcomePageView () : base ()
		{
			this.ContentName = GettextCatalog.GetString ("Welcome");
			this.IsViewOnly = true;
			
			recentChangesHandler = DispatchService.GuiDispatch (new EventHandler (RecentChangesHandler));
			DesktopService.RecentFiles.Changed += recentChangesHandler;
			newsUpdatedHandler = (EventHandler) DispatchService.GuiDispatch (new EventHandler (HandleNewsUpdate));
			NewsUpdated += newsUpdatedHandler;
			
			UpdateNews ();
			
			Build ();
		}
		
		void Build ()
		{
			scroller = new ScrolledWindow ();
			widget = new WelcomePageWidget (this);
			scroller.AddWithViewport (widget);
			scroller.ShadowType = ShadowType.None;
			scroller.FocusChain = new Widget[] { widget };
			scroller.Show ();
		}
		
		static string NewsFile {
			get {
				return UserProfile.Current.CacheDir.Combine ("WelcomePageNews.xml");
			}
		}
		
		static XmlReaderSettings GetSafeReaderSettings ()
		{
			//allow DTD but not try to resolve it from web
			return new XmlReaderSettings () {
				CloseInput = true,
				ProhibitDtd = false,
				XmlResolver = null,
			};
		}
		
		public XmlDocument GetUpdatedXmlDocument ()
		{
			string localCachedNewsFile = NewsFile;
			var settings = GetSafeReaderSettings ();
			
			var stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("WelcomePageContent.xml");
			var contentDoc = new XmlDocument ();
			using (var reader = XmlReader.Create (stream, settings))
				contentDoc.Load (reader);
			
			try {
				if (File.Exists (localCachedNewsFile)) {
					var updateDoc = new XmlDocument ();

					using (var reader = XmlReader.Create (localCachedNewsFile, settings))
						updateDoc.Load (reader);
					
					XmlNode oNodeWhereInsert =
						contentDoc.SelectSingleNode ("/WelcomePage/Links[@_title=\"News Links\"]"); 
					foreach (XmlNode link in updateDoc.SelectSingleNode ("/links")) 
						oNodeWhereInsert.AppendChild (contentDoc.ImportNode (link,true)); 
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading Welcome Page content file.", ex);
				if (File.Exists (localCachedNewsFile))
					File.Delete (localCachedNewsFile);
			}
			
			return contentDoc;
		}
		
		static void UpdateNewsXmlAsync ()
		{
			//check to see if the online news file has been modified since it was last downloaded
			string netNewsXml = WelcomePageView.netNewsXml;
			LoggingService.LogInfo ("Updating Welcome Page from '{0}'.", netNewsXml);
			
			var request = (HttpWebRequest) WebRequest.Create (netNewsXml);
			string localCachedNewsFile = NewsFile;
			var localNewsXml = new FileInfo (localCachedNewsFile);
			if (localNewsXml.Exists)
				request.IfModifiedSince = localNewsXml.LastWriteTime;
			
			try {
				request.BeginGetResponse (delegate (IAsyncResult ar) {
					try {
						var tempFile = localCachedNewsFile + ".temp";
						//FIXME: limit this size in case open wifi hotspots provide bad data
						var response = (HttpWebResponse) request.EndGetResponse (ar);
						if (response.StatusCode == HttpStatusCode.OK) {
							using (var fs = File.Create (tempFile))
								response.GetResponseStream ().CopyTo (fs, 2048);
						}
						
						//check the document is valid, might get bad ones from wifi hotspots etc
						try {
							var updateDoc = new XmlDocument ();
							using (var reader = XmlReader.Create (tempFile, GetSafeReaderSettings ()))
								updateDoc.Load (reader);
							updateDoc.SelectSingleNode ("/links");
						} catch (Exception ex) {
							LoggingService.LogWarning ("Welcome Page news file is bad, keeping old version.", ex);
							File.Delete (tempFile);
							return;
						}
						
						if (File.Exists (localCachedNewsFile))
							File.Delete (localCachedNewsFile);
						File.Move (tempFile, localCachedNewsFile);
						
						NewsUpdated (null, EventArgs.Empty);
					} catch (System.Net.WebException wex) {
						var httpResp = wex.Response as HttpWebResponse;
						if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotModified) {
							LoggingService.LogInfo ("Welcome Page already up-to-date.");
						} else if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotFound) {
							LoggingService.LogInfo ("Welcome Page update file not found.", netNewsXml);
						} else {
							LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", wex);
						}
					} catch (Exception ex) {
						LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
					} finally {
						lock (updateLock)
							isUpdating = false;
					}
				}, null);
			} catch (Exception ex) {
				LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
				lock (updateLock)
					isUpdating = false;
			}
		}
		
		public static void UpdateNews ()
		{
			if (!WelcomePageOptions.UpdateFromInternet)
				return;
			
			lock (updateLock) {
				if (isUpdating)
					return;
				else
					isUpdating = true;
			}
			
			UpdateNewsXmlAsync ();
		}
		
		static object updateLock = new object ();
		static bool isUpdating;
		static event EventHandler NewsUpdated;
		
		void HandleNewsUpdate (object sender, EventArgs args)
		{
			widget.Rebuild ();
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
					DesktopService.ShowUrl (uri);
				} catch (Exception ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("Could not open the url '{0}'", uri));
				}
			}
		}
		
		StatusBarContext statusBar;
		
		public void SetLinkStatus (string link)
		{
			if (link == null) {
				if (statusBar != null) {
					statusBar.Dispose ();
					statusBar = null;
				}
				return;
			}
			if (link.IndexOf ("monodevelop://") != -1)
				return;
				
			if (statusBar == null)
				statusBar = IdeApp.Workbench.StatusBar.CreateContext ();
			
			if (link.IndexOf ("project://") != -1) {
				string message = link;
				message = message.Substring (10);
				string msg = GettextCatalog.GetString ("Open solution {0}", message);
				if (IdeApp.Workspace.IsOpen)
					msg += " - " + GettextCatalog.GetString ("Hold Control key to open in current workspace.");
				statusBar.ShowMessage (msg);
			} else {
				string msg = GettextCatalog.GetString ("Open {0}", link);
				statusBar.ShowMessage (msg);
			}
		}

		void RecentChangesHandler (object sender, EventArgs e)
		{
			widget.LoadRecent ();
		}

		public override void Dispose ()
		{
			NewsUpdated -= newsUpdatedHandler;
			if (recentChangesHandler != null) {
				DesktopService.RecentFiles.Changed -= recentChangesHandler;
				recentChangesHandler = null;
			}
			base.Dispose ();
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
		
		public IList<RecentFile> GetRecentProjects ()
		{
			return DesktopService.RecentFiles.GetProjects ();
		}
	}
}
