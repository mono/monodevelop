// 
// DownloadService.cs
//  
// Author:
//       lluis <${AuthorEmail}>
// 
// Copyright (c) 2011 lluis
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
using MonoDevelop.Core;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MonoDevelop.Ide.Updater
{
	static class DownloadService
	{
		static DownloaderStatus status = DownloaderStatus.Stopped;
		static string statusMessage;
		static object serviceLock = new object ();
		static bool pauseRequested;
		static Stream globalLock;
		
		static Gdk.Pixbuf runningIcon;
		static Gdk.Pixbuf pausedIcon;
		static StatusBarIcon statusBarIcon;
		
		public static event EventHandler StatusChanged;
		
		public static event EventHandler DownloadFinished;
		
		public static bool Enabled {
			get; private set;
		}
		
		public static FilePath DownloadDir {
			get { return PropertyService.Locations.Cache.Combine ("TempDownload"); }
		}
		
		public static FilePath DownloadIndex {
			get { return DownloadDir.Combine ("index.xml"); }
		}
		
		public static DownloaderStatus Status {
			get { return status; }
			set {
				status = value;
				if (value == DownloaderStatus.Paused)
					statusMessage = GettextCatalog.GetString ("Download paused");
				else
					statusMessage = string.Empty;
				OnStatusChanged ();
				Monitor.Pulse (serviceLock);
			}
		}
		
		public static string StatusMessage {
			get { return statusMessage; }
			private set {
				if (value != statusMessage) {
					statusMessage = value;
					Gtk.Application.Invoke (delegate {
						if (statusBarIcon != null)
							statusBarIcon.ToolTip = statusMessage;
					});
				}
			}
		}
		
		internal static void Initialize ()
		{
			runningIcon = Gdk.Pixbuf.LoadFromResource ("downloader-running.png");
			pausedIcon = Gdk.Pixbuf.LoadFromResource ("downloader-paused.png");
			
			if (!Directory.Exists (DownloadDir))
				Directory.CreateDirectory (DownloadDir);
			try {
				string lockFile = DownloadDir.Combine ("downloader-lock");
				globalLock = new FileStream (lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			} catch (IOException) {
				// File is locked
				Enabled = false;
			}
			IdeApp.Exiting += HandleIdeAppExiting;
		}

		static void HandleIdeAppExiting (object sender, ExitEventArgs args)
		{
			lock (serviceLock) {
				// Stop the downloader
				ForcePause (4);
			}
		}
		
		static bool ForcePause (int timeoutSeconds)
		{
			PauseDownloader ();
			DateTime t = DateTime.Now + TimeSpan.FromSeconds (timeoutSeconds);
			while (Status == DownloaderStatus.Running) {
				DateTime now = DateTime.Now;
				if (now >= t)
					return false;
				Monitor.Wait (serviceLock, t - now);
			}
			return true;
		}
		
		static void Shutdown ()
		{
			if (globalLock != null) {
				globalLock.Close ();
				globalLock = null;
			}
		}
		
		public static void SetUpdates (IEnumerable<Update> targets)
		{
			lock (serviceLock) {
				// Stop the downloader
				if (!ForcePause (10))
					return;
				
				var currentDownloads = ReadDownloads ();
				
				// Remove downloads which don't exist in the new set
				foreach (var d in currentDownloads.ToArray ().Where (d => !targets.Any (t => t.Id == d.Id && t.VersionId == d.VersionId))) {
					currentDownloads.Remove (d);
					DeleteDownloadedFile (d);
				}
				
				// Add new downloads
				foreach (var d in targets.Where (t => !currentDownloads.Any (c => c.Id == t.Id && c.VersionId == t.VersionId))) {
					currentDownloads.Add (d);
					d.Status = UpdateStatus.PendingDownload;
				}
				
				// Restart failed downloads and reset pending install flag
				foreach (var d in currentDownloads) {
					if (d.Status == UpdateStatus.Failed) {
						d.Status = UpdateStatus.PendingDownload;
						DeleteDownloadedFile (d);
					}
					else if (d.Status == UpdateStatus.PendingInstall)
						d.Status = UpdateStatus.Downloaded;
				}
				
				SaveDownloads (currentDownloads);
				StartDownloader ();
			}
		}

		public static void Remove (Update dt)
		{
			lock (serviceLock) {
				var currentDownloads = ReadDownloads ();
				currentDownloads.Remove (currentDownloads.First (u => u.Id == dt.Id && u.VersionId == dt.VersionId));
				DeleteDownloadedFile (dt);
				SaveDownloads (currentDownloads);
			}
		}
		
		public static void MarkForInstallation (IEnumerable<Update> updates)
		{
			lock (serviceLock) {
				var currentDownloads = ReadDownloads ();
				foreach (Update u in currentDownloads.Where (dt => updates.Any (u => u.Id == dt.Id && u.VersionId == dt.VersionId)))
					u.Status = UpdateStatus.PendingInstall;
				SaveDownloads (currentDownloads);
			}
		}
		
		static void DeleteDownloadedFile (Update t)
		{
			try {
				if (!string.IsNullOrEmpty (t.File) && File.Exists (t.File))
					File.Delete (t.File);
			} catch (Exception ex) {
				LoggingService.LogError ("Downloaded file could not be deleted: " + t.File, ex);
			}
		}
		
		public static bool AllDownloaded {
			get {
				var currentDownloads = ReadDownloads ();
				return currentDownloads.All (d => d.Status == UpdateStatus.Downloaded);
			}
		}
		
		public static IEnumerable<Update> GetAllDownloads ()
		{
			return ReadDownloads ();
		}
		
		public static void PauseDownloader ()
		{
			lock (serviceLock) {
				if (Status == DownloaderStatus.Running && !pauseRequested)
					pauseRequested = true;
			}
		}
		
		public static void StartDownloader ()
		{
			lock (serviceLock) {
				if (Status != DownloaderStatus.Running) {
					Status = DownloaderStatus.Running;
					ThreadPool.QueueUserWorkItem (Download);
				}
				else if (pauseRequested) {
					pauseRequested = false;
					Status = DownloaderStatus.Running;
				}
			}
		}
		
		static void Download (object o)
		{
			byte[] buffer = new byte [16384];
			var list = ReadDownloads ();
			bool allDone = false;
			
			while (!pauseRequested)
			{
				var next = list.FirstOrDefault (d => d.Status == UpdateStatus.Downloading);
				if (next == null)
					next = list.FirstOrDefault (d => d.Status == UpdateStatus.PendingDownload);
				if (next == null) {
					lock (serviceLock) {
						pauseRequested = true;
					}
					allDone = true;
					break;
				}
				next.Status = UpdateStatus.Downloading;
				SaveDownloads (list);
				
				StatusMessage = GettextCatalog.GetString ("Downloading {0}", next.Name);
				
				HttpWebRequest req = (HttpWebRequest) HttpWebRequest.Create (next.Url);
				
				FileStream fs = null;
				long lenDownloaded = 0;
				
				try {
					if (!string.IsNullOrEmpty (next.File) && File.Exists (next.File)) {
						FileInfo fi = new FileInfo (next.File);
						lenDownloaded = fi.Length;
						req.AddRange ((int)fi.Length);
						fs = new FileStream (next.File, FileMode.Open, FileAccess.Write);
						fs.Seek (0,SeekOrigin.End);
					}
					
					using (var resp = req.GetResponse ()) {
						
						if (fs != null && resp.ContentLength == -1) {
							// Not resumable
							fs.Close ();
							fs = null;
							lenDownloaded = 0;
						}
						
						if (fs == null) {
							next.File = GetFileName (next, resp);
							SaveDownloads (list);
							fs = new FileStream (next.File, FileMode.Create, FileAccess.Write);
						}
						
						long totalLen = next.Size;
						var s = resp.GetResponseStream ();
						int nr = -1;
						while (!pauseRequested && (nr = s.Read (buffer, 0, buffer.Length)) > 0) {
							fs.Write (buffer, 0, nr);
							lenDownloaded += nr;
							if (totalLen != -1)
								StatusMessage = GettextCatalog.GetString ("Downloading {0} ({1}%)", next.Name, (lenDownloaded * 100) / totalLen);
							else
								StatusMessage = GettextCatalog.GetString ("Downloading {0}", next.Name);
						}
						fs.Flush ();
						Console.WriteLine ("READ " + lenDownloaded);
						if (nr == 0) {
							fs.Close ();
							if (next.Hash != CalcHash (next.File))
								throw new Exception ("MD5 hashes don't match");
							next.Status = UpdateStatus.Downloaded;
						}
					}
				}
				catch (Exception ex) {
					next.Status = UpdateStatus.Failed;
					LoggingService.LogError ("File download failed: " + next.Url, ex);
				}
				finally {
					fs.Dispose ();
					SaveDownloads (list);
				}
			}
			lock (serviceLock) {
				if (!pauseRequested) {
					// Service has been resumed
					ThreadPool.QueueUserWorkItem (Download);
				}
				else if (allDone) {
					Status = DownloaderStatus.Stopped;
					OnDownloadFinished ();
				}
				else
					Status = DownloaderStatus.Paused;
				pauseRequested = false;
			}
		}
		
		static void SaveDownloads (List<Update> list)
		{
			XmlSerializer ser = new XmlSerializer (typeof(List<Update>));
			using (var sr = new StreamWriter (DownloadIndex)) {
				ser.Serialize (sr, list);
			}
		}
		
		static List<Update> ReadDownloads ()
		{
			if (File.Exists (DownloadIndex)) {
				try {
					XmlSerializer ser = new XmlSerializer (typeof(List<Update>));
					using (var sr = new StreamReader (DownloadIndex)) {
						return (List<Update>) ser.Deserialize (sr);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not load download index", ex);
				}
			}
			return new List<Update> ();
		}
		
		static FilePath GetFileName (Update t, WebResponse resp)
		{
			string disp = resp.Headers.Get ("Content-Disposition");
			if (disp != null) {
				foreach (string v in disp.Split (';')) {
					if (v != null && v.Trim ().StartsWith ("filename")) {
						int i = v.IndexOf ('=');
						if (i != -1) {
							string n = v.Substring (i+1).Trim ();
							return DownloadDir.Combine (t.Id + Path.GetExtension (n));
						}
						break;
					}
				}
			}
			int j = t.Url.LastIndexOf ('/');
			string file = t.Url.Substring (j+1);
			j = file.LastIndexOf ('?');
			if (j != -1)
				file = file.Substring (0, j);
			return DownloadDir.Combine (t.Id + Path.GetExtension (file));
		}

		static string CalcHash (string file)
		{
			using (var fs = File.OpenRead (file)) {
				MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
				var data = x.ComputeHash (fs);
				StringBuilder sb = new StringBuilder ();
				for (int i = 0; i < data.Length; i++)
					sb.Append (data[i].ToString("x2").ToLower());
				return sb.ToString ();
			}
		}
		
		static void OnStatusChanged ()
		{
			Gtk.Application.Invoke (delegate {
				if (statusBarIcon != null) {
					statusBarIcon.Dispose ();
					statusBarIcon = null;
				}
				switch (Status) {
				case DownloaderStatus.Paused: statusBarIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (pausedIcon); break;
				case DownloaderStatus.Running: statusBarIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (runningIcon); break;
				}
				
				if (statusBarIcon != null)
					statusBarIcon.EventBox.ButtonPressEvent += HandleStatusBarIconEventBoxButtonPressEvent;
				
				if (StatusChanged != null)
					StatusChanged (null, EventArgs.Empty);
			});
		}

		static void HandleStatusBarIconEventBoxButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (Status == DownloaderStatus.Running)
				PauseDownloader ();
			else if (Status == DownloaderStatus.Paused)
				StartDownloader ();
		}
		
		static void OnDownloadFinished ()
		{
			Gtk.Application.Invoke (delegate {
				if (DownloadFinished != null)
					DownloadFinished (null, EventArgs.Empty);
			});
		}
	}
	
	public enum DownloaderStatus
	{
		Running,
		Stopped,
		Paused
	}
	
	public enum UpdateStatus
	{
		PendingDownload,
		Downloading,
		Downloaded,
		Failed,
		PendingInstall
	}
}

