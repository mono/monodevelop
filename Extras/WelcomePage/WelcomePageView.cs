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

using System.Xml;
using System.Xml.Xsl;
using System.IO;

namespace MonoDevelop.Core
{
	public class XslGettextCatalog
	{
		public XslGettextCatalog() {}
		
		public static string GetString (string str)
		{
			return GettextCatalog.GetString(str);
		}
	}
}

namespace MonoDevelop.WelcomePage
{	
	public class WelcomePageView : AbstractViewContent
	{
		static IWebBrowser browser;
		
		bool loadingProject;
		
		string datadir;
		
		public override Gtk.Widget Control {
			get {
				return (Widget) browser;
			}
		}
		
		public override string StockIconId {
			get {
				return Gtk.Stock.Home;
			}
		}
		
		public override void Load (string fileName) 
		{
		}

		public WelcomePageView () : base ()
		{
			this.ContentName = GettextCatalog.GetString ("Welcome");
			this.IsViewOnly = true;

			if (browser == null) {
				browser = WebBrowserService.GetWebBrowser ();
				Control.Show ();
				Control.Show ();
				browser.LocationChanging += CatchUri;
				browser.LinkStatusChanged += LinkMessage;
			}
			
			datadir = "file://" + Path.GetDirectoryName (typeof(ShowWelcomePageHandler).Assembly.Location) + "/";

			if (PlatformID.Unix != Environment.OSVersion.Platform)
				datadir = datadir.Replace("\\","/");

			LoadContent ();

			IdeApp.Workbench.RecentOpen.RecentProjectChanged += RecentChangesHandler;
		}
		
		void LoadContent ()
		{
			// Get the Xml
			XmlDocument inxml = BuildXmlDocument ();
			
			XsltArgumentList arg = new XsltArgumentList ();
			arg.AddExtensionObject ("urn:MonoDevelop.Core.XslGettextCatalog", new MonoDevelop.Core.XslGettextCatalog ());
			
			XslTransform xslt = new XslTransform();
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream ("WelcomePage.xsl")) {
				xslt.Load (new XmlTextReader (stream));
			}		
			StringWriter fs = new StringWriter ();
			xslt.Transform (inxml, arg, fs, null);
			
			browser.LoadHtml (fs.ToString ());
		}

		void RecentChangesHandler (object sender, EventArgs e)
		{
			LoadContent ();
			Initialize ();
		}
		
		void LinkMessage (object sender, StatusMessageChangedEventArgs e)
		{
			if (String.IsNullOrEmpty (e.Message) || e.Message.IndexOf ("monodevelop://") != -1) {
				IdeApp.Workbench.StatusBar.SetMessage (null);
			} else {
				string message = e.Message;
				if (message.IndexOf ("project://") != -1) 
					message = message.Substring (10);
				IdeApp.Workbench.StatusBar.SetMessage (message);
			}
		}
		
		void CatchUri (object sender, LocationChangingEventArgs e)
		{
			e.SuppressChange = true;
	
			string URI = e.NextLocation;

			// HACK: Necessary for win32; I have no idea why
			if (PlatformID.Unix != Environment.OSVersion.Platform)
				Console.WriteLine ("WelcomePage: Handling URI: " + URI);

			if (URI.StartsWith ("project://")) {
				if (loadingProject)
					return;
					
				string projectUri = URI.Substring (10);			
				Uri fileuri = new Uri (projectUri);
				try {
					loadingProject = true;
					IAsyncOperation oper = IdeApp.ProjectOperations.OpenCombine (fileuri.LocalPath);
					oper.WaitForCompleted ();
				} finally {
					loadingProject = false;
				}
			} else if (URI.StartsWith ("monodevelop://")) {
				// Launch MonoDevelop Gui Commands
				switch (URI.Substring (14))
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
					Gnome.Url.Show (URI);
				} catch (Exception) {
					string msg = String.Format (GettextCatalog.GetString ("Could not open the url {0}"), URI);
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
		
		private XmlDocument BuildXmlDocument()
		{
			XmlDocument xml = new XmlDocument();
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream ("WelcomePageContent.xml")) {
				xml.Load (new XmlTextReader (stream));
			}
			
			// Get the Parent node
			XmlNode parent = xml.SelectSingleNode ("/WelcomePage");
			
			// Resource Path
			XmlElement element = xml.CreateElement ("ResourcePath");
			element.InnerText = datadir;
			parent.AppendChild(element);
			
			RecentOpen recentOpen = IdeApp.Workbench.RecentOpen;
			if (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0)
			{
				XmlElement projectList  = xml.CreateElement ("RecentProjects");
				parent.AppendChild(projectList);
				foreach (RecentItem ri in recentOpen.RecentProject)
				{
					XmlElement project = xml.CreateElement ("Project");
					projectList.AppendChild(project);
					// Uri
					element = xml.CreateElement ("Uri");
					element.InnerText = ri.LocalPath;
					project.AppendChild (element);
					// Name
					element = xml.CreateElement ("Name");
					element.InnerText = (ri.Private != null && ri.Private.Length > 0) ? ri.Private : Path.GetFileNameWithoutExtension(ri.LocalPath);
					project.AppendChild (element);
					// Date Modified
					element = xml.CreateElement ("DateModified");
					element.InnerText = TimeSinceEdited (ri.Timestamp);
					project.AppendChild (element);
				}
			} 
			return xml;
		}

		public void Initialize()
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
	}
}
