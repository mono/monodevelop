//
// MozillaControl - An Html widget that uses Gecko#
//
// Author: John Luke  <jluke@cfl.rr.com>
//
// Copyright 2003 John Luke
//

using System;
using Gecko;

namespace MonoDevelop.Gui.HtmlControl
{
	public class MozillaControl : WebControl, IWebBrowser
	{
		internal static GLib.GType gtype;
		private string html;
		private string css;
		
		public MozillaControl ()
		{
			WebControl.SetProfilePath ("/tmp", "MonoDevelop");
		}
		
		public void GoHome ()
		{
			LoadUrl ("about:blank");
		}
		
		public void GoSearch ()
		{
		}
		
		public void Navigate (string Url, ref object Flags, ref object targetFrame, ref object postData, ref object headers)
		{
			// TODO: what is all that other crap for
			LoadUrl (Url);
		}
		
		public void Refresh ()
		{
			this.Reload ((int) ReloadFlags.Reloadnormal);
		}
		
		public void Refresh2 ()
		{
			this.Reload ((int) ReloadFlags.Reloadnormal);
		}
		
		public void Stop ()
		{
			this.StopLoad ();
		}
		
		public void GetApplication ()
		{
		}
		
		public void GetParent ()
		{
		}
		
		public void GetContainer ()
		{
		}
		
		public IHTMLDocument2 GetDocument ()
		{
			return null;
		}

		public string Html
		{
			get { return html; }
			set { html = value; }
		}
		
		public string Css
		{
			get { return css; }
			set { css = value; }
		}

		public void InitializeWithBase (string base_uri)
		{
			//Runtime.LoggingService.Info (base_uri);
			if (html.Length > 0)
			{
				this.RenderData (html, base_uri, "text/html");
			}
		}
		
		public void DelayedInitialize ()
		{
			InitializeWithBase ("file://");
		}
	}
}
