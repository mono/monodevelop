// 
// IWebBrowser.cs
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

namespace MonoDevelop.Ide.WebBrowser
{
	
	public interface IWebBrowser
	{
		void GoForward ();
		void GoBack ();
		void LoadUrl (string url);
		void LoadHtml (string html);
		
		string Title { get; }
		string JSStatus { get; } //status bar messages shown from javascript
		string Location { get; }
		string LinkStatus { get; } //staus bar messages shown when hovering over links
		
		bool CanGoBack { get; }
		bool CanGoForward { get; }
		
		void Reload ();
		void StopLoad ();
		
		event PageLoadedHandler PageLoaded;
		
		//the differnce between these two is that LocationChanging will be fired by all nav changes including calls to 
		//LoadUrl or LoadHtml, whereas LocationChanging is only fired by changes initiated from within the browser
		event LocationChangingHandler LinkClicked;
		event LocationChangingHandler LocationChanging;
		
		event LocationChangedHandler LocationChanged;
		event TitleChangedHandler TitleChanged;
		event StatusMessageChangedHandler JSStatusChanged;
		event StatusMessageChangedHandler LinkStatusChanged;
		event LoadingProgressChangedHandler LoadingProgressChanged;
		event EventHandler NetStart;
		event EventHandler NetStop;
	}
	
	public delegate void PageLoadedHandler (object sender, EventArgs args);
	public delegate void LocationChangedHandler (object sender, LocationChangedEventArgs args);
	public delegate void LocationChangingHandler (object sender, LocationChangingEventArgs args);
	public delegate void TitleChangedHandler (object sender, TitleChangedEventArgs args);	
	public delegate void StatusMessageChangedHandler (object sender, StatusMessageChangedEventArgs args);
	public delegate void LoadingProgressChangedHandler (object sender, LoadingProgressChangedEventArgs args);
}
