// 
// MacUpdater.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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

using System;
using System.Linq;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using System.Net;
using MonoDevelop.Core.Gui;
using System.Collections.Generic;

namespace MonoDevelop.Platform
{

	public static class MacUpdater
	{
		const int formatVersion = 1;
		
		public static string[] DefaultUpdateInfoPaths {
			get {
				return new string[] {
					"/Developer/MonoTouch/updateinfo",
					"/Library/Frameworks/Mono.framework/Versions/Current/updateinfo",
					Path.GetDirectoryName (typeof (MacPlatform).Assembly.Location) + "../../updateinfo"
				};
			}
		}
		
		public static void RunCheck (bool automatic)
		{
			RunCheck (DefaultUpdateInfoPaths, automatic);
		}
		
		public static void RunCheck (string[] updateInfos, bool automatic)
		{
			var query = new StringBuilder ("http://go-mono.com/macupdate/update?v=");
			query.Append (formatVersion);
			
			bool foundInfo = false;
			
			foreach (var name in updateInfos.Where (x => File.Exists (x))) {
				try {
					using (var f = File.OpenText (name)) {
						var s = f.ReadLine ();
						var parts = s.Split (' ');
						Guid guid = new Guid (parts[0]);
						long version = long.Parse (parts[1]);
						query.AppendFormat ("&{0}={1}", guid, version);
						foundInfo = true;
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Error reading update info file '" + name + "'", ex);
				}
			}
			
			if (!foundInfo)
				return;
			
			if (!string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MONODEVELOP_UPDATER_TEST")))
				query.Append ("&test=1");
			
			var request = (HttpWebRequest) WebRequest.Create (query.ToString ());
			
			//FIXME: use IfModifiedSince
			//request.IfModifiedSince = somevalue;
			
			request.BeginGetResponse (delegate (IAsyncResult ar) {
				ReceivedResponse (request, ar, automatic);
			}, null);
		}
		
		static void ReceivedResponse (HttpWebRequest request, IAsyncResult ar, bool automatic)
		{
			List<Update> updates = null;
			try {
				using (var response = (HttpWebResponse) request.EndGetResponse (ar)) {
					var encoding = Encoding.GetEncoding (response.CharacterSet);
					using (var reader = new StreamReader (response.GetResponseStream(), encoding)) {
						var doc = System.Xml.Linq.XDocument.Load (reader);
						updates = (from x in doc.Root.Elements ("Application")
							let first = x.Elements ("Update").First ()
							select new Update () {
								Name = x.Attribute ("name").Value,
								Url = first.Attribute ("url").Value,
								Version = first.Attribute ("version").Value,
								Releases = x.Elements ("Update").Select (y => new Release () {
									Version = y.Attribute ("version").Value,
									Date = DateTime.Parse (y.Attribute ("date").Value),
									Notes = y.Value
								}).ToList ()
							}).ToList ();
					}
				}
			} catch (WebException ex) {
				LoggingService.LogError ("Error retrieving update information", ex);
				if (!automatic)
					MessageService.ShowException (ex, GettextCatalog.GetString ("Error retrieving update information"));
			} catch (Exception ex) {
				LoggingService.LogError ("Error retrieving update information", ex);
				if (!automatic)
					MessageService.ShowException (ex, GettextCatalog.GetString ("Error retrieving update information"));
			}
			
			if (updates == null || updates.Count <= 0)
				return;
			
			
		}
		
		class Update
		{
			public string Name;
			public string Url;
			public string Version;
			public List<Release> Releases;
		}
		
		class Release
		{
			public string Version;
			public DateTime Date;
			public string Notes;
		}
	}
}
