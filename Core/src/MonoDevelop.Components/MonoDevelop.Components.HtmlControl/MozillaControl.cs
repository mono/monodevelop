//
// MozillaControl - An Html widget that uses Gecko#
//
// Author: John Luke  <jluke@cfl.rr.com>
//
// Copyright 2003 John Luke
//

using System;
using Gecko;

namespace MonoDevelop.Components.HtmlControl
{
	public class MozillaControl : WebControl, IWebBrowser
	{
		internal static GLib.GType gtype;
		private string html;
		private string css;
		private bool reShown = false;
		
		public MozillaControl ()
		{
//			WebControl.SetProfilePath ("/tmp", "MonoDevelop");
			
			//FIXME: suppress a strange bug with control not getting drawn first time it's shown, or when docking changed.
			//For some reason this event only fires when control 'appears' or is re-docked, which corresponds 1:1 to the bug.
			//FIXME: OnExposeEvent doesn't fire, but ExposeEvent does
/*			this.ExposeEvent += delegate {
				if (!reShown) {
					Console.WriteLine ("P1");
					Hide ();
					Show ();
					
					//Normally we would expect this event to fire with every redraw event, so put in a limiter in case this is fixed in future.
					reShown = true;
					GLib.Timeout.Add (1000, delegate { reShown = false; return false; } );
				}
			};
			
			this.Realized += delegate {
				Console.WriteLine ("REALIZED");
			};
			
			this.Unrealized += delegate {
				Console.WriteLine ("UNREALIZED");
			};
			
			this.ParentSet += delegate {
				Console.WriteLine ("PARENTSET " + Parent);
			};
*/		}
		
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
			Console.WriteLine ("InitializeWithBase " + html.Length);
			
			//Runtime.LoggingService.Info (base_uri);
			if (html.Length > 0)
			{
/*    			OpenStream (base_uri, "text/html");
    			int chunk = 100;
    			for (int n=0; n<html.Length; n+=chunk)
    			{
    				int max = chunk;
    				if (n+max > html.Length) max = html.Length-n;
    				AppendData (html.Substring (n, max));
    			}
    			
    			CloseStream ();
*/			
			
    			this.RenderData ("hi", base_uri, "text/html");
			}
		}
		
		public void DelayedInitialize ()
		{
			Console.WriteLine ("DelayedInitialize");
			InitializeWithBase ("file://");
		}
	}
}
