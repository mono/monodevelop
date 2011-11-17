// 
// WelcomePageNewsFeed.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Gtk;
using System;
using MonoDevelop.Core;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Net;

namespace MonoDevelop.Ide.WelcomePage
{
	class WelcomePageNewsFeed : VBox
	{
		string newsUrl;
		string id;
		XElement defaultContent;
		bool destroyed;
		
		public WelcomePageNewsFeed (XElement el)
		{
			newsUrl = (string) el.Attribute ("src");
			if (string.IsNullOrEmpty (newsUrl))
				throw new Exception ("News feed is missing src attribute");
			id = (string) el.Attribute ("id");
			if (string.IsNullOrEmpty (id))
				throw new Exception ("News feed is missing id attribute");
			
			defaultContent = el;
			UpdateNews ();
			LoadNews ();
		}
		
		protected override void OnDestroyed ()
		{
			destroyed = true;
			base.OnDestroyed ();
		}
		
		void LoadNews ()
		{
			//can get called from async handler
			if (destroyed)
				return;
			
			foreach (var c in Children) {
				this.Remove (c);
				c.Destroy ();
			}
			
			try {
				var news = GetNewsXml ();
				if (news.FirstNode == null) {
					var label = new Label (GettextCatalog.GetString ("No news found.")) { Xalign = 0, Xpad = 6 };
					this.PackStart (label, true, false, 0);
				} else {
					foreach (var child in news.Elements ()) {
						if (child.Name != "link" && child.Name != "Link")
							throw new Exception ("Unexpected child '" + child.Name + "'");
						var button = new WelcomePageLinkButton (child);
						this.PackStart (button, true, false, 0);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogWarning ("Error loading welcome page news.", ex);
			}
			this.ShowAll ();
		}
		
		void UpdateNews ()
		{
			if (!WelcomePageOptions.UpdateFromInternet)
				return;
			
			lock (updateLock) {
				if (isUpdating)
					return;
				else
					isUpdating = true;
			}
			
			UpdateNewsXmlAsync ();
		}
		
		object updateLock = new object ();
		bool isUpdating;
		
		void UpdateNewsXmlAsync ()
		{
			if (string.IsNullOrEmpty (newsUrl)) {
				LoggingService.LogWarning ("Welcome Page net news url invalid.");
				return;
			}
			LoggingService.LogInfo ("Updating Welcome Page from '{0}'.", newsUrl);
			
			//HACK: .NET blocks on DNS in BeginGetResponse, so use a threadpool thread
			// see http://stackoverflow.com/questions/1232139#1232930
			System.Threading.ThreadPool.QueueUserWorkItem ((state) => {
				var request = (HttpWebRequest)WebRequest.Create (newsUrl);
				
				try {
					//check to see if the online news file has been modified since it was last downloaded
					string localCachedNewsFile = CacheFile;
					var localNewsXml = new FileInfo (localCachedNewsFile);
					if (localNewsXml.Exists)
						request.IfModifiedSince = localNewsXml.LastWriteTime;
					
					request.BeginGetResponse (HandleResponse, request);
				} catch (Exception ex) {
					LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
					lock (updateLock)
						isUpdating = false;
				}
			});
		}
		
		void HandleResponse (IAsyncResult ar)
		{
			string localCachedNewsFile = CacheFile;
			try {
				var request = (HttpWebRequest) ar.AsyncState;
				var tempFile = localCachedNewsFile + ".temp";
				//FIXME: limit this size in case open wifi hotspots provide bad data
				var response = (HttpWebResponse) request.EndGetResponse (ar);
				if (response.StatusCode == HttpStatusCode.OK) {
					using (var fs = File.Create (tempFile))
						response.GetResponseStream ().CopyTo (fs, 2048);
				}
				
				//check the document is valid, might get bad ones from wifi hotspots etc
				try {
					var updateDoc = new XmlDocument ();
					using (var reader = XmlReader.Create (tempFile, GetSafeReaderSettings ()))
						updateDoc.Load (reader);
					updateDoc.SelectSingleNode ("/links");
				} catch (Exception ex) {
					LoggingService.LogWarning ("Welcome Page news file is bad, keeping old version.", ex);
					File.Delete (tempFile);
					return;
				}
				
				if (File.Exists (localCachedNewsFile))
					File.Delete (localCachedNewsFile);
				File.Move (tempFile, localCachedNewsFile);
				
				Gtk.Application.Invoke (delegate {
					LoadNews ();
				});
			} catch (System.Net.WebException wex) {
				var httpResp = wex.Response as HttpWebResponse;
				if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotModified) {
					LoggingService.LogInfo ("Welcome Page already up-to-date.");
				} else if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotFound) {
					LoggingService.LogInfo ("Welcome Page update file not found.", newsUrl);
				} else {
					LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", wex);
				}
			} catch (Exception ex) {
				LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
			} finally {
				lock (updateLock)
					isUpdating = false;
			}
		}
		
		static XmlReaderSettings GetSafeReaderSettings ()
		{
			//allow DTD but not try to resolve it from web
			return new XmlReaderSettings () {
				CloseInput = true,
				ProhibitDtd = false,
				XmlResolver = null,
			};
		}
		
		string CacheFile {
			get {
				return UserProfile.Current.CacheDir.Combine ("WelcomePage-" + id + ".xml");
			}
		}
		
		XElement GetNewsXml ()
		{
			string localCachedNewsFile = CacheFile;
			try {
				var settings = GetSafeReaderSettings ();
				if (File.Exists (localCachedNewsFile)) {
					using (var reader = XmlReader.Create (localCachedNewsFile, settings))
						return XDocument.Load (reader).Root;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading Welcome Page content file.", ex);
				if (File.Exists (localCachedNewsFile)) {
					try {
						File.Delete (localCachedNewsFile);
					} catch {}
				}
			}
			
			return defaultContent;
		}
	}
}