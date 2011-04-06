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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Setup;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Globalization;
using Mono.Addins;
using Mono.Addins.Setup;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Updater
{
	static class UpdateService
	{
		const int formatVersion = 1;
		const string updateAutoPropertyKey = "AppUpdater.CheckAutomatically";
		const string updateLevelKey = "AppUpdate.UpdateLevel";
		
		static UpdateInfo[] updateInfos;
		
		static UpdateInfo[] LoadUpdateInfos ()
		{
			if (string.IsNullOrEmpty (DesktopService.GetUpdaterUrl ()))
				return new UpdateInfo[0];
			
			var list = new List<UpdateInfo> ();
			foreach (var node in Mono.Addins.AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/Updater")) {
				var n = node as UpdateInfoExtensionNode;
				if (n == null)
					continue;
				string file = n.File;
				if (!File.Exists (file))
					continue;
				
				try {
					list.Add (UpdateInfo.FromFile (file));
				} catch (Exception ex) {
					LoggingService.LogError ("Error reading update info file '" + file + "'", ex);
				}
			}
			return list.ToArray ();
		}
		
		static UpdateService ()
		{
			updateInfos = LoadUpdateInfos ();
		}
		
		public static bool CanUpdate {
			get { return true; }
		}
		
		public static UpdateInfo[] DefaultUpdateInfos {
			get { return updateInfos; }
		}
		
		public static bool CheckAutomatically {
			get {
				return PropertyService.Get<bool> (updateAutoPropertyKey, true);
			}
			set {
				PropertyService.Set (updateAutoPropertyKey, value);
			}
		}
		
		public static UpdateLevel UpdateLevel {
			get {
				return PropertyService.Get<UpdateLevel> (updateLevelKey, UpdateLevel.Stable);
			}
			set {
				PropertyService.Set (updateLevelKey, value);
			}
		}
		
		public static void RunCheckDialog (bool automatic)
		{
			RunCheckDialog (DefaultUpdateInfos, automatic);
		}
		
		public static void RunCheckDialog (UpdateInfo[] updateInfos, bool automatic)
		{
			if (!CanUpdate || (automatic && !CheckAutomatically))
				return;
			
			if (!automatic) {
				ShowUpdateDialog ();
				QueryUpdateServer (updateInfos, UpdateLevel, delegate (UpdateResult result) {
					ShowUpdateResult (result);
				});
			} else {
				QueryUpdateServer (updateInfos, UpdateLevel, delegate (UpdateResult result) {
					if (result.HasError || !result.HasUpdates)
						return;
					ShowUpdateDialog ();
					ShowUpdateResult (result);
				});
			}
		}
		
		#region Singleton dialog management. Methods are threadsafe, field is not
		
		static UpdateDialog visibleDialog;
		
		static void ShowUpdateDialog ()
		{
			Gtk.Application.Invoke (delegate {
				if (visibleDialog == null) {
					visibleDialog = new UpdateDialog ();
					MessageService.ShowCustomDialog (visibleDialog);
					visibleDialog = null;
				} else {
					visibleDialog.GdkWindow.Focus (0);
				}
			});
		}
		
		static void ShowUpdateResult (UpdateResult result)
		{
			Gtk.Application.Invoke (delegate {
				if (visibleDialog != null)
					visibleDialog.LoadResult (result);
			});
		}
			
		#endregion
		
		public static void QueryUpdateServer (UpdateInfo[] updateInfos, UpdateLevel level, Action<UpdateResult> callback)
		{
			if (!string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MONODEVELOP_UPDATER_TEST")))
				level = UpdateLevel.Test;
			
			if (updateInfos == null || updateInfos.Length == 0) {
				QueryAddinUpdates (level, callback);
				return;
			}
			
			var query = new StringBuilder (DesktopService.GetUpdaterUrl ());
			query.Append ("?v=");
			query.Append (formatVersion);
			
			foreach (var info in updateInfos)
				query.AppendFormat ("&{0}={1}", info.AppId, info.VersionId);
			
			if (level != UpdateLevel.Stable) {
				query.Append ("&level=");
				query.Append (level.ToString ().ToLower ());
			}
			
			bool hasEnv = false;
			foreach (string flag in DesktopService.GetUpdaterEnvironmentFlags ()) {
				if (!hasEnv) {
					hasEnv = true;
					query.Append ("&env=");
					query.Append (flag);
				} else {
					query.Append (",");
					query.Append (flag);
				}
			}
			
			var requestUrl = query.ToString ();
			var request = (HttpWebRequest) WebRequest.Create (requestUrl);
			
			LoggingService.LogDebug ("Checking for updates: {0}", requestUrl);
			
			//FIXME: use IfModifiedSince, with a cached value
			//request.IfModifiedSince = somevalue;
			
			request.BeginGetResponse (delegate (IAsyncResult ar) {
				ReceivedResponse (request, ar, level, callback);
			}, null);
		}
		
		static void ReceivedResponse (HttpWebRequest request, IAsyncResult ar, UpdateLevel level, Action<UpdateResult> callback)
		{
			List<Update> updates = null;
			string error = null;
			Exception errorDetail = null;
			
			try {
				using (var response = (HttpWebResponse) request.EndGetResponse (ar)) {
					var encoding = !string.IsNullOrEmpty (response.CharacterSet) ? Encoding.GetEncoding (response.CharacterSet) : Encoding.UTF8;
					using (var reader = new StreamReader (response.GetResponseStream(), encoding)) {
						var doc = System.Xml.Linq.XDocument.Load (reader);
						updates = (from x in doc.Root.Elements ("Application")
							let first = x.Elements ("Update").First ()
							select new Update () {
								Name = x.Attribute ("name").Value,
								Url = first.Attribute ("url").Value,
								Version = first.Attribute ("version").Value,
								Level = first.Attribute ("level") != null
									? (UpdateLevel)Enum.Parse (typeof(UpdateLevel), (string)first.Attribute ("level"))
									: UpdateLevel.Stable,
								Date = DateTime.Parse (first.Attribute ("date").Value),
								Releases = x.Elements ("Update").Select (y => new Release () {
									Version = y.Attribute ("version").Value,
									Date = DateTime.Parse (y.Attribute ("date").Value),
									Notes = y.Value
								}).ToList ()
							}).ToList ();
					}
				}
				
				CheckAddinUpdates (updates, level);
				
			} catch (Exception ex) {
				LoggingService.LogError ("Could not retrieve update information", ex);
				error = GettextCatalog.GetString ("Error retrieving update information");
				errorDetail = ex;
			}
			
			callback (new UpdateResult (updates, level, error, errorDetail));
		}
		
		public static void QueryAddinUpdates (UpdateLevel level, Action<UpdateResult> callback)
		{
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				List<Update> updates = new List<Update> ();
				string error = null;
				Exception errorDetail = null;
				
				try {
					CheckAddinUpdates (updates, level);
					
				} catch (Exception ex) {
					LoggingService.LogError ("Could not retrieve update information", ex);
					error = GettextCatalog.GetString ("Error retrieving update information");
					errorDetail = ex;
				}
				
				callback (new UpdateResult (updates, level, error, errorDetail));
			});
		}
		
		static void CheckAddinUpdates (List<Update> updates, UpdateLevel level)
		{
			for (UpdateLevel n=UpdateLevel.Stable; n<=level; n++)
				Runtime.AddinSetupService.RegisterMainRepository ((UpdateLevel)n, true);
			
			AddinUpdateHandler.QueryAddinUpdates ();
			
			updates.AddRange (GetAddinUpdates (UpdateLevel.Stable));
			if (level >= UpdateLevel.Beta)
				updates.AddRange (GetAddinUpdates (UpdateLevel.Beta));
			if (level >= UpdateLevel.Alpha)
				updates.AddRange (GetAddinUpdates (UpdateLevel.Alpha));
			if (level == UpdateLevel.Test)
				updates.AddRange (GetAddinUpdates (UpdateLevel.Test));
		}
		
		static IEnumerable<Update> GetAddinUpdates (UpdateLevel level)
		{
			List<Update> res = new List<Update> ();
			string url = Runtime.AddinSetupService.GetMainRepositoryUrl (level);
			List<AddinRepositoryEntry> list = new List<AddinRepositoryEntry> ();
			list.AddRange (Runtime.AddinSetupService.Repositories.GetAvailableUpdates (url));
			FilterOldVersions (list);
			foreach (var ventry in list) {
				var entry = ventry;
				string notify = entry.Addin.Properties.GetPropertyValue ("NotifyUpdate").ToLower ();
				if (notify != "yes" && notify != "true")
					continue;
				string sdate = entry.Addin.Properties.GetPropertyValue ("ReleaseDate");
				DateTime date;
				if (!string.IsNullOrEmpty (sdate))
					date = DateTime.Parse (sdate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
				else
					date = DateTime.Now;
				res.Add (new Update () {
					Name = entry.Addin.Name,
					InstallAction = (m) => InstallAddin (m, entry),
					Version = entry.Addin.Version,
					Level = level,
					Date = date,
					Releases = ParseReleases (entry.Addin.Id, entry).ToList ()
				});
			}
			return res;
		}
		
		static void FilterOldVersions (List<AddinRepositoryEntry> addins)
		{
			Dictionary<string,string> versions = new Dictionary<string, string> ();
			foreach (AddinRepositoryEntry a in addins) {
				string last;
				string id, version;
				Addin.GetIdParts (a.Addin.Id, out id, out version);
				if (!versions.TryGetValue (id, out last) || Addin.CompareVersions (last, version) > 0)
					versions [id] = version;
			}
			for (int n=0; n<addins.Count; n++) {
				AddinRepositoryEntry a = addins [n];
				string id, version;
				Addin.GetIdParts (a.Addin.Id, out id, out version);
				if (versions [id] != version)
					addins.RemoveAt (n--);
			}
		}
		
		static IEnumerable<Release> ParseReleases (string addinId, AddinRepositoryEntry entry)
		{
			// Format of release notes is:
			// {{version1,date1}} release note text {{version2,date2}} release note text ...
			// Releases msyu
			// for example:
			// {{1.1,2011-01-10}} Release notes for 1.1 {{1.2,2011-03-22}} Release notes for 2.3
			
			string releaseNotes = entry.Addin.Properties.GetPropertyValue ("ReleaseNotes");
			if (releaseNotes.Length == 0) {
				string file = entry.Addin.Properties.GetPropertyValue ("ReleaseNotesFile");
				if (file.Length > 0) {
					IAsyncResult res = entry.BeginDownloadSupportFile (file, null, null);
					res.AsyncWaitHandle.WaitOne ();
					try {
						using (Stream s = entry.EndDownloadSupportFile (res)) {
							StreamReader sr = new StreamReader (s);
							releaseNotes = sr.ReadToEnd ();
						}
					} catch (Exception ex) {
						LoggingService.LogError ("Could not download release notes", ex);
					}
				}
			}
			
			if (releaseNotes.Length == 0)
				yield break;
			
			var addin = AddinManager.Registry.GetAddin (Addin.GetIdName (addinId));
			string currentVersion = addin != null ? addin.Version : null;
			
			int i = releaseNotes.IndexOf ("{{");
			while (i != -1) {
				int j = releaseNotes.IndexOf ("}}", i + 2);
				if (j == -1)
					break;
				string[] h = releaseNotes.Substring (i + 2, j - i - 2).Trim ().Split (',');
				if (h.Length != 2)
					break;
				DateTime date;
				if (!DateTime.TryParse (h[1].Trim (), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
					break;
				int k = releaseNotes.IndexOf ("{{", j + 2);
				string version = h[0].Trim ();
				if (currentVersion == null || Addin.CompareVersions (currentVersion, version) > 0) {
					string txt = k != -1 ? releaseNotes.Substring (j + 2, k - j - 2) : releaseNotes.Substring (j + 2);
					yield return new Release () {
						Version = version,
						Date = date,
						Notes = txt.Trim (' ','\t','\r','\n')
					};
				}
				i = k;
			}
		}
		
		static IAsyncOperation InstallAddin (IProgressMonitor monitor, AddinRepositoryEntry addin)
		{
			// Add-in engine changes must be done in the gui thread since mono.addins is not thread safe
			DispatchService.GuiDispatch (delegate {
				using (monitor) {
					Runtime.AddinSetupService.Install (new ProgressStatusMonitor (monitor), addin);
				}
			});
			return monitor.AsyncOperation;
		}
	}
}
