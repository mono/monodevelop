// 
// WebKitWebBrowser.cs
// 
// Author:
//   Eric Butler <eric@extremeboredom.net>
// 
// Copyright (C) 2008 Eric Butler <eric@extremeboredom.net>
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
using Gtk;
using MonoDevelop.Core.Gui.WebBrowser;

namespace MonoDevelop.WebBrowsers
{
	public class WebKitWebBrowser : ScrolledWindow, IWebBrowser
	{
		#region Private Class Variables
		WebKit.WebView webView;
		string jsStatus = null;
		string linkStatus = null;
		#endregion

		#region Constructor
		public WebKitWebBrowser ()
		{
			webView = new WebKit.WebView();
			Add(webView);
			webView.Show();

			#region WebView Events
			webView.StatusBarTextChanged += delegate (object o, WebKit.StatusBarTextChangedArgs args) {
				jsStatus = args.Value;
				if (JSStatusChanged != null)
					JSStatusChanged (this, new StatusMessageChangedEventArgs (jsStatus));
			};

			webView.HoveringOverLink += delegate (object o, WebKit.HoveringOverLinkArgs args) {
				linkStatus = args.Link;
				if (LinkStatusChanged != null)
					LinkStatusChanged (this, new StatusMessageChangedEventArgs (linkStatus));
			};

			webView.TitleChanged += delegate (object o, WebKit.TitleChangedArgs args) {
				if (TitleChanged != null)
					TitleChanged (this, new TitleChangedEventArgs(args.Title));
			};

			webView.NavigationRequested += delegate (object o, WebKit.NavigationRequestedArgs args) {
				// FIXME: There's currently no way to tell the difference 
				// between a link being clicked and another navigation event.
				// This is a temporary workaround.
				if (args.Request.Uri == linkStatus) {
					if (LinkClicked != null)
						LinkClicked (this, new LocationChangingEventArgs(args.Request.Uri, false));
				} else {
					if (LocationChanged != null)
						LocationChanged (this, new LocationChangedEventArgs (args.Request.Uri));
				}
				args.RetVal = WebKit.NavigationResponse.Accept;
			};

			webView.LoadStarted += delegate {
				if (NetStart != null)
					NetStart (this, EventArgs.Empty);
			};

			webView.LoadFinished += delegate {
				if (NetStop != null)
					NetStop (this, EventArgs.Empty);
			};

			webView.LoadProgressChanged += delegate (object o, WebKit.LoadProgressChangedArgs args) {
				if (LoadingProgressChanged != null)
					LoadingProgressChanged(this, new LoadingProgressChangedEventArgs(args.Progress));
			};
			
			#endregion
		}
		#endregion

		#region IWebBrowser Interface Implementation
		string IWebBrowser.Title {
			get { return webView.MainFrame.Title; }
		}
		
		string IWebBrowser.JSStatus {
			get { return jsStatus; }
		}
		
		string IWebBrowser.LinkStatus {
			get { return linkStatus; }
		}
		
		string IWebBrowser.Location {
			get { return webView.MainFrame.Uri; }
		}

		bool IWebBrowser.CanGoBack {
			get { return webView.CanGoBack (); }
		}
		
		bool IWebBrowser.CanGoForward {
			get { return webView.CanGoForward (); }
		}
		
		void IWebBrowser.GoForward ()
		{
			webView.GoForward ();
		}
		
		void IWebBrowser.GoBack ()
		{
			webView.GoBack ();
		}
		
		void IWebBrowser.LoadUrl (string url)
		{
			webView.Open (url);
		}
		
		void IWebBrowser.LoadHtml (string html)
		{
			webView.LoadHtmlString (html, "tempfile://");
		}
		
		void IWebBrowser.Reload ()
		{
			webView.Reload ();
		}
		
		void IWebBrowser.StopLoad ()
		{
			webView.StopLoading ();
		}
		#endregion

		#region Public Events
		public event PageLoadedHandler PageLoaded; // FIXME: Not implemented
		public event LocationChangingHandler LocationChanging;  // FIXME: Not implemented
		public event LocationChangingHandler LinkClicked;
		public event LocationChangedHandler LocationChanged;
		public event TitleChangedHandler TitleChanged;
		public event StatusMessageChangedHandler JSStatusChanged;
		public event StatusMessageChangedHandler LinkStatusChanged;
		public event LoadingProgressChangedHandler LoadingProgressChanged;
		public event EventHandler NetStart;
		public event EventHandler NetStop;
		#endregion
	}
}
