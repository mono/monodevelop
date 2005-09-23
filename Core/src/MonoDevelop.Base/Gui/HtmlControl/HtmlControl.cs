using System;
using Gtk;
using GtkSharp;

namespace MonoDevelop.Gui.HtmlControl
{
	public class HtmlControl : Frame, IWebBrowserEvents
	{
		private static GLib.GType type;
				
		IWebBrowser control = null;	
		string url           = "";
		string html          = "";
		string cssStyleSheet = "";
		bool initialized     = false;
		ControlType control_type;
		
		static HtmlControl ()
		{
			type = RegisterGType (typeof (HtmlControl));
		}
		
		public HtmlControl () : base (type)
		{
			if ((int) Environment.OSVersion.Platform != 128)
			{
				control_type = ControlType.IE;
			}
			else
			{
				control_type = ControlType.GtkMozilla;
			}
			
			AttachInterfaces ();
		}
		
		public virtual void RaiseNavigateComplete(string url)
		{
			BrowserNavigateEventArgs e = new BrowserNavigateEventArgs(url, false);
			if (NavigateComplete != null) {
				NavigateComplete(this, e);
			}
		}
		
		public virtual void RaiseBeforeNavigate(string url, int flags, string targetFrameName, ref object postData, string headers, ref bool cancel)
		{
			if (initialized) {
				BrowserNavigateEventArgs e = new BrowserNavigateEventArgs(url, false);
				if (BeforeNavigate != null) {
					BeforeNavigate(this, e);
				}
				cancel = e.Cancel;
			}
		}
		
		public string CascadingStyleSheet {
			get {
				return cssStyleSheet;
			}
			set {
				cssStyleSheet = value;
				ApplyCascadingStyleSheet();
			}
		}
		
		public string Url {
			get {
				return this.url;
			}
			set {
				this.url = value;
				if (control_type == ControlType.GtkMozilla)
				{
					((MozillaControl) control).LoadUrl (value);
				}
				else
					Runtime.LoggingService.Info ("unable to load url");
			}
		}
		
		public string Html {
			get {
				return this.html;
			}
			set {
				this.html = value;
				//ApplyBody(html);
			}
		}
		
		protected void DetachSink()
		{
			try {
			//	this.cookie.Disconnect();
			} catch {
			}
		}
		
		protected void CreateSink()
		{
			try {
			//	this.cookie = new ConnectionPointCookie(this.GetOcx(), this, typeof(IWebBrowserEvents));
			} catch {
			}
		}
		
		protected void AttachInterfaces()
		{
			switch (control_type) {
				case ControlType.IE:
					this.control = (IWebBrowser)this.GetOcx();
					//this.Add ((IEControl) this.control);
					break;
				case ControlType.GtkMozilla:
					this.control = new MozillaControl ();
					((MozillaControl) this.control).Show ();
					this.Add ((MozillaControl) this.control);
					Runtime.LoggingService.Info ("added MozillaControl to HtmlControl");
					break;
				default:
					throw new NotImplementedException (control_type.ToString ());
			}
		}
		
		protected IHTMLDocument2 GetOcx ()
		{
			return null;
			//if (control_type == ControlType.IE)
				//GetOcx ();
		}
		
		protected void OnHandleCreated(EventArgs e)
		{
			//base.OnHandleCreated(e);
			
			NavigateComplete += new BrowserNavigateEventHandler(DelayedInitializeCaller);
			
			object flags       = 0;
			object targetFrame = String.Empty;
			object postData    = String.Empty;
			object headers     = String.Empty;
			this.control.Navigate("about:blank", ref flags, ref targetFrame, ref postData, ref headers);
		}
		
		void DelayedInitializeCaller(object sender, BrowserNavigateEventArgs e)
		{
			//MethodInvoker mi = new MethodInvoker(this.DelayedInitialize);
			//this.BeginInvoke(mi);
			NavigateComplete -= new BrowserNavigateEventHandler(DelayedInitializeCaller);
		}
		
		public void DelayedInitialize()
		{
			initialized = true;
			if (html.Length > 0) {
				ApplyBody(html);
			} else {
				Runtime.LoggingService.Info ("no html to apply");
			}
			UIActivate();
			ApplyCascadingStyleSheet();
		}
		
		void UIActivate()
		{
			//this.DoVerb(OLEIVERB_UIACTIVATE);
		}
		
		void ApplyBody(string val)
		{
		    try {
				if (control != null) {
					if (control_type == ControlType.GtkMozilla)
					{
						Runtime.LoggingService.Info ("rendering");
						((MozillaControl) control).Show ();
						((MozillaControl) control).RenderData (val, "file://", "text/html");
						Runtime.LoggingService.Info ("rendered");
						return;
					}
					else
					{
						Runtime.LoggingService.Info ("not rendering with mozilla");
					}
					
					IHTMLElement el    = null;
					IHTMLDocument2 doc = this.control.GetDocument();
					
					if (doc != null) {
						el = doc.GetBody();
					}
					
					if (el != null) {
						UIActivate();
						el.SetInnerHTML(val);
						return;
					}
					else
					{
						Runtime.LoggingService.Info ("IHTMLElement is null");
					}
				}
				else
				{
					Runtime.LoggingService.Info ("control is null");
				}
			}
			catch (Exception e)
			{
				Runtime.LoggingService.Info (e.ToString ());
			} 
		}
		
		void ApplyCascadingStyleSheet()
		{
			if (control != null) {
				IHTMLDocument2 htmlDoc = control.GetDocument();
				if (htmlDoc != null) {
					htmlDoc.CreateStyleSheet(cssStyleSheet, 0);
				}
			}
		}

		public void GoBack ()
		{
			control.GoBack ();
		}

		public void GoForward ()
		{
			control.GoForward ();
		}

		public void Stop ()
		{
			control.Stop ();
		}

		public void Refresh ()
		{
			control.Refresh ();
		}
		
		public Gtk.Widget Control
		{
			get
			{
				switch (control_type) {
				case ControlType.IE:
					// return (IEControl) control;
					return null; //FIXME
				case ControlType.GtkMozilla:
					return (MozillaControl) control;
				default:
					throw new NotImplementedException (control_type.ToString ());
				}
			}
		}
		
		public ControlType HtmlType
		{
			set { control_type = value; }
		}
		
		public event BrowserNavigateEventHandler BeforeNavigate;
		public event BrowserNavigateEventHandler NavigateComplete;
	}
}
