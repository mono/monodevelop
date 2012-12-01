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
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Net;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageNewsFeed : WelcomePageSection
	{
		string newsUrl;
		string id;
		int limit;
		bool destroyed;
		Gtk.VBox box;
		
		public WelcomePageNewsFeed (string title, string newsUrl, string id, int limit = 5): base (title)
		{
			box = new VBox (false, Styles.WelcomeScreen.Pad.News.Item.MarginBottom);
			if (string.IsNullOrEmpty (newsUrl))
				throw new Exception ("News feed is missing src attribute");
			if (string.IsNullOrEmpty (id))
				throw new Exception ("News feed is missing id attribute");

			this.newsUrl = newsUrl;
			this.id = id;
			this.limit = limit;

			UpdateNews ();
			LoadNews ();
			SetContent (box);
			ContentAlignment.TopPadding += 10;
			WidthRequest = Styles.WelcomeScreen.Pad.News.Width;
		}
		
		protected override void OnDestroyed ()
		{
			destroyed = true;
			base.OnDestroyed ();
		}

		protected virtual IEnumerable<Gtk.Widget> OnLoadNews (XElement news)
		{
			foreach (var child in news.Elements ()) {
				if (child.Name != "link" && child.Name != "Link")
					throw new Exception ("Unexpected child '" + child.Name + "'");
				yield return new WelcomePageFeedItem (child);
			}
		}
		
		void LoadNews ()
		{
			//can get called from async handler
			if (destroyed)
				return;

			foreach (var c in box.Children) {
				box.Remove (c);
				c.Destroy ();
			}
			
			try {
				var news = GetNewsXml ();
				if (news.FirstNode == null) {
					var label = new Label (GettextCatalog.GetString ("No news found.")) { Xalign = 0, Xpad = 6 };
					box.PackStart (label, true, false, 0);
				} else {
					foreach (var child in OnLoadNews (news).Take (limit))
						box.PackStart (child, true, false, 0);
				}
			} catch (Exception ex) {
				LoggingService.LogWarning ("Error loading news feed.", ex);
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

			FileService.UpdateDownloadedCacheFile (newsUrl, CacheFile, s => {
				using (var reader = XmlReader.Create (s, GetSafeReaderSettings ())) {
					return reader.ReadToDescendant ("links");
				}
			}).ContinueWith (t => {
				try {
					if (t.IsCanceled)
						return;

					if (!t.Result) {
						LoggingService.LogInfo ("Welcome Page already up-to-date.");
						return;
					}

					LoggingService.LogInfo ("Welcome Page updated.");
					Gtk.Application.Invoke (delegate { LoadNews (); });

				} catch (Exception ex) {
					LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
				} finally {
					lock (updateLock)
						isUpdating = false;
				}
			});
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
			
			return new XElement ("News");
		}
	}
}