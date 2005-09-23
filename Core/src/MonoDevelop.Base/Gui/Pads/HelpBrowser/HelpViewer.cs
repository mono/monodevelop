using System;
using System.IO;

using Gtk;
using Monodoc;

using MonoDevelop.Gui;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Gui
{

	public class HelpViewer : AbstractViewContent
	{
		HTML html_viewer = new HTML ();

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

		public HelpViewer ()
		{
			html_viewer.LinkClicked += new LinkClickedHandler (LinkClicked);
			html_viewer.UrlRequested += new UrlRequestedHandler (UrlRequested);
			html_viewer.OnUrl += new OnUrlHandler (OnUrl);
			scroller.Add (html_viewer);
			Control.ShowAll ();
		}

		void OnUrl (object sender, OnUrlArgs args)
		{
			if (args.Url == null)
				Runtime.Gui.StatusBar.SetMessage ("");
			else
				Runtime.Gui.StatusBar.SetMessage (args.Url);
		}

		void UrlRequested (object sender, UrlRequestedArgs args)
		{
			Runtime.LoggingService.Info ("Image requested: " + args.Url);
			Stream s = Runtime.Documentation.HelpTree.GetImage (args.Url);
			
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
			if (url.StartsWith("#"))
			{
				html_viewer.JumpToAnchor(url.Substring(1));
				return;
			}
			
			Node node;
			
			string res = Runtime.Documentation.HelpTree.RenderUrl (url, out node);
			if (res != null) {
				Render (res, node, url);
			}
		}
		
		public void Render (string text, Node matched_node, string url)
		{
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
