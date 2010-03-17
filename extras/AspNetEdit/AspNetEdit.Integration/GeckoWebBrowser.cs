// 
// GeckoWebBrowser.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using Gecko;

using MonoDevelop.Ide.WebBrowser;

namespace AspNetEdit.Integration
{
	
	public class GeckoWebBrowser : WebControl, IWebBrowser
	{
		string delayedUrl;
		string oldTempFile;
		bool reShown = false;
		
		bool suppressLinkClickedBecauseCausedByLoadUrlCall = false;
		
		public GeckoWebBrowser ()
		{
			WebControl.SetProfilePath ("/tmp", "MonoDevelop");
			
			//FIXME: On{Event} doesn't fire
			this.ExposeEvent += exposeHandler;
			this.OpenUri += delegate (object o, OpenUriArgs args) {
				args.RetVal = OnLocationChanging (args.AURI);
			};
			this.LocChange += delegate (object sender, EventArgs e) {
				OnLocationChanged ();
			};
			this.Progress += delegate (object sender, ProgressArgs e) {
				OnLoadingProgressChanged (e.Curprogress / e.Maxprogress);
			};
			this.ECMAStatus += delegate {
				OnJSStatusChanged ();
			};
			this.LinkStatusChanged += delegate {
				OnLinkStatusChanged ();
			};
			this.TitleChange += delegate {
				OnTitleChanged ();
			};
		}
		
		//FIXME: OnExposeEvent doesn't fire, but ExposeEvent does
		void exposeHandler (object sender, Gtk.ExposeEventArgs e)
		{
			if (delayedUrl != null) {
				realLoadUrl (delayedUrl);
				delayedUrl = null;
			}
			
			//FIXME: suppress a strange bug with control not getting drawn first time it's shown, or when docking changed.
			//For some reason this event only fires when control 'appears' or is re-docked, which corresponds 1:1 to the bug.
			if (!reShown) {
				Hide ();
				Show ();					
				//Normally we would expect this event to fire with every redraw event, so put in a limiter 
				//in case this is fixed in future.
				reShown = true;
				GLib.Timeout.Add (1000, delegate { reShown = false; return false; } );
			}
		}
		
		string IWebBrowser.Title {
			get { return base.Title; }
		}
		
		string IWebBrowser.JSStatus {
			get { return base.JsStatus; }
		}
		
		string IWebBrowser.LinkStatus {
			get { return base.LinkMessage; }
		}
		
		string IWebBrowser.Location {
			get { return base.Location; }
		}

		bool IWebBrowser.CanGoBack {
			get { return base.CanGoBack (); }
		}
		
		bool IWebBrowser.CanGoForward {
			get { return base.CanGoForward (); }
		}
		
		void IWebBrowser.GoForward ()
		{
			base.GoForward ();
		}
		
		void IWebBrowser.GoBack ()
		{
			base.GoBack ();
		}
		
		void IWebBrowser.LoadUrl (string url)
		{
			realLoadUrl (url);
		}
		
		void IWebBrowser.LoadHtml (string html)
		{
			string tempFile = System.IO.Path.GetTempFileName ();
			
			StreamWriter writer = null;
			try {
				writer = File.CreateText (tempFile);
				writer.Write (html);
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not write temporary HTML file '{0}' for Gecko web control\n{1}", tempFile, ex.ToString ());
			} finally {
				if (writer != null)
					writer.Close ();
			}
			
			realLoadUrl ("tempfile://" + tempFile);
		}
		
		void realLoadUrl (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");
			
			if (!this.IsRealized) {
				delayedUrl = url;
				return;
			}
			
			if (url == delayedUrl) {
				delayedUrl = null;
			}
			
			if (oldTempFile != null) {
				try {
					File.Delete (oldTempFile);
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Could not delete temp file '{0}'\n{1]", oldTempFile, ex.ToString ());
				}
				oldTempFile = null;
			}
			
			suppressLinkClickedBecauseCausedByLoadUrlCall = true;
			
			if (url.StartsWith ("tempfile://")) {
				oldTempFile = url.Substring (11);
				base.LoadUrl (oldTempFile);
			} else {
				base.LoadUrl (url);
			}
		}
		
		void IWebBrowser.Reload ()
		{
			base.Reload ((int) ReloadFlags.Reloadnormal);
		}
		
		void IWebBrowser.StopLoad ()
		{
			base.StopLoad ();
		}
		
		public event PageLoadedHandler PageLoaded;
		public event LocationChangingHandler LocationChanging;
		public event LocationChangingHandler LinkClicked;
		public event LocationChangedHandler LocationChanged;
		public event TitleChangedHandler TitleChanged;
		public event StatusMessageChangedHandler JSStatusChanged;
		public event StatusMessageChangedHandler LinkStatusChanged;
		public event LoadingProgressChangedHandler LoadingProgressChanged;
		
		protected bool OnLocationChanging (string aURI)
		{
			LocationChangingEventArgs args = new LocationChangingEventArgs (aURI, false);
			args.SuppressChange = false;
			if (LocationChanging != null)
				LocationChanging (this, args);
			OnLinkClicked (args);
			return args.SuppressChange;
		}
		
		protected virtual void OnLinkClicked (LocationChangingEventArgs args)
		{
			if (suppressLinkClickedBecauseCausedByLoadUrlCall) {
				suppressLinkClickedBecauseCausedByLoadUrlCall = false;
				return;
			}
			if (LinkClicked != null)
				LinkClicked (this, args);
		}
		
		protected virtual void OnLocationChanged ()
		{
			if (LocationChanged != null)
				LocationChanged (this, new LocationChangedEventArgs (null));
		}
		
		protected virtual void OnLoadingProgressChanged (float progress)
		{
			if (LoadingProgressChanged != null)
				LoadingProgressChanged (this, new LoadingProgressChangedEventArgs (progress));
		}
		
		protected virtual void OnJSStatusChanged ()
		{
			if (JSStatusChanged != null)
				JSStatusChanged (this, new StatusMessageChangedEventArgs (base.JsStatus));
		}
		
		protected virtual void OnLinkStatusChanged ()
		{
			if (LinkStatusChanged != null)
				LinkStatusChanged (this, new StatusMessageChangedEventArgs (base.LinkMessage));
		}
		
		protected virtual void OnTitleChanged ()
		{
			if (TitleChanged != null)
				TitleChanged (this, new TitleChangedEventArgs (base.Title));
		}
	}
}
