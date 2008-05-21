// 
// WelcomePageBrowser.cs
// 
// Author:
//   Scott Ellington
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2005 Scott Ellington
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using System.Xml;
using System.Xml.Xsl;
using System.IO;
using System.Text;
using System.Reflection;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.WebBrowser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Projects;

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
	
	public class WelcomePageBrowser : WelcomePageView
	{
		static IWebBrowser browser;
		
		public WelcomePageBrowser () : base ()
		{
			if (browser == null) {
				browser = WebBrowserService.GetWebBrowser ();
				Control.Show ();
				Control.Show ();
				browser.LinkClicked += CatchUri;
				browser.LinkStatusChanged += LinkMessage;
			}
			
			LoadContent ();
		}
		
		public override Widget Control {
			get { return (Widget) browser; }
		}
		
		protected override void HandleNewsUpdate (object sender, EventArgs args)
		{
			LoadContent ();
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
		
		void CatchUri (object sender, LocationChangingEventArgs e)
		{
			e.SuppressChange = true;
			base.HandleLinkAction (e.NextLocation);
		}
		private XmlDocument BuildXmlDocument()
		{
			XmlDocument xml = GetUpdatedXmlDocument ();

			// Get the Parent node
			XmlNode parent = xml.SelectSingleNode ("/WelcomePage");
			
			// Resource Path
			XmlElement element = xml.CreateElement ("ResourcePath");
			element.InnerText = DataDirectory;
			parent.AppendChild(element);
			
			RecentItem[] items = RecentProjects;
			if (items != null && items.Length > 0)
			{
				XmlElement projectList  = xml.CreateElement ("RecentProjects");
				parent.AppendChild(projectList);
				foreach (RecentItem ri in items)
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
		
		void LinkMessage (object sender, StatusMessageChangedEventArgs e)
		{
			base.SetLinkStatus (e.Message);
		}
		
		public override string DataDirectory {
			get { return "file://" + base.DataDirectory; }
		}
		
		protected override void RecentChangesHandler (object sender, EventArgs e)
		{
			LoadContent ();
		}
	}
}
