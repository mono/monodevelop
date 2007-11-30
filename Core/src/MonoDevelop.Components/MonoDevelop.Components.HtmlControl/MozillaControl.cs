//
// MozillaControl - An Html widget that uses Gecko#
//
// Author: John Luke  <jluke@cfl.rr.com>
//
// Copyright 2003 John Luke
//
/*
using System;
using Gecko;
using GLib;

namespace MonoDevelop.Components.HtmlControl
{
	public class MozillaControl : WebControl, IWebBrowser
	{
		internal static GLib.GType gtype;
		private string html;
		private string css;
		private bool reShown = false;
		string delayedBaseUri;
		
		static MozillaControl ()
		{
			
		}
		
		public MozillaControl ()
		{
			//FIXME: suppress a strange bug with control not getting drawn first time it's shown, or when docking changed.
			//For some reason this event only fires when control 'appears' or is re-docked, which corresponds 1:1 to the bug.
			//FIXME: OnExposeEvent doesn't fire, but ExposeEvent does
			this.ExposeEvent += delegate {
				if (!reShown) {
					Hide ();
					Show ();
					
					//Normally we would expect this event to fire with every redraw event, so put in a limiter in case this is fixed in future.
					reShown = true;
					GLib.Timeout.Add (1000, delegate { reShown = false; return false; } );
				}
			};
			this.Realized += delegate {
				if (delayedBaseUri != null) {
					InitializeWithBase (delayedBaseUri);
					delayedBaseUri = null;
				}
			};
		}
		
		public void GoHome ()
		{
			LoadUrl ("about:blank");
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
		
		public void Stop ()
		{
			this.StopLoad ();
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
			if ((this.WidgetFlags & Gtk.WidgetFlags.Realized) == 0) {
				delayedBaseUri = base_uri;
				return;
			}
			
			if (html.Length > 0)
			{
				GLib.Idle.Add (delegate {
					this.RenderData (html, base_uri, "text/html");
					return false;
				});
			}
		}
		
		public void DelayedInitialize ()
		{
			InitializeWithBase ("file://");
		}
	}
}
*/