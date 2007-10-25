using System;
using System.IO;

using Gtk;
using Monodoc;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{

	public class HelpViewer : AbstractViewContent
	{
		HTML html_viewer;

		ScrolledWindow scroller = new ScrolledWindow ();

		public override bool IsViewOnly {
			get { return true; }
		}

		public override Gtk.Widget Control {
			get { return scroller; }
		}

		public override string ContentName {
			get { return GettextCatalog.GetString ("Documentation"); }
		}
		
		public override void Dispose ()
		{
			scroller.Destroy ();
			base.Dispose ();
		}


		public HelpViewer ()
		{
			try {
				html_viewer = new HTML ();
				html_viewer.LinkClicked += new LinkClickedHandler (LinkClicked);
				html_viewer.UrlRequested += new UrlRequestedHandler (UrlRequested);
				html_viewer.OnUrl += new OnUrlHandler (OnUrl);
				scroller.Add (html_viewer);
			} catch (Exception ex) {
				Label lab = new Label (GettextCatalog.GetString ("The help viewer could not be loaded.") + "\n\n" + ex.GetType() + ": " + ex.Message);
				scroller.Add (lab);
			}
			Control.ShowAll ();
		}

		void OnUrl (object sender, OnUrlArgs args)
		{
			if (args.Url == null)
				IdeApp.Workbench.StatusBar.SetMessage ("");
			else
				IdeApp.Workbench.StatusBar.SetMessage (args.Url);
		}

		void UrlRequested (object sender, UrlRequestedArgs args)
		{
			Runtime.LoggingService.DebugFormat ("Image requested: {0}", args.Url);
			Stream s = Services.DocumentationService.HelpTree.GetImage (args.Url);
			
			if (s != null) {
				byte [] buffer = new byte [8192];
				int n;

				while ((n = s.Read (buffer, 0, 8192)) != 0)
					args.Handle.Write (buffer, (ulong)n);
			}
			args.Handle.Close (HTMLStreamStatus.Ok);
		}

		void LinkClicked (object o, LinkClickedArgs args)
		{
			LoadUrl (args.Url);
		}
 
		public void LoadUrl (string url)
		{
			if (html_viewer == null)
				return;
			
			if (url.StartsWith("#"))
			{
				html_viewer.JumpToAnchor(url.Substring(1));
				return;
			}
			
			Node node;
			
			string res = Services.DocumentationService.HelpTree.RenderUrl (url, out node);
			if (res != null) {
				Render (res, node, url);
			}
		}
		
		public void Render (string text, Node matched_node, string url)
		{
			if (html_viewer == null)
				return;
			
			Gtk.HTMLStream stream = html_viewer.Begin ("text/html");
			
			stream.Write ("<html><body>");
			stream.Write (text);
			stream.Write ("</body></html>");
			html_viewer.End (stream, HTMLStreamStatus.Ok);
		}

		public override void Load (string s)
		{
		}

	}

}
