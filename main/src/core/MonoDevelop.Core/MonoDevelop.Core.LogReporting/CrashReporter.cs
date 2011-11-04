// 
// CrashReporter.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011, Xamarin Inc.
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
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Net;
using System.Web;

namespace MonoDevelop.Core.LogReporting
{
	public class CrashReporter
	{
		// The file which stores the list of reports which have to be uploaded
		string CacheFile {
			get { return Path.Combine (CacheDirectory, "crashlogs.xml"); }
		}
		
		// The directory where we cache all information for reports which
		// will be uploaded in the future.
		string CacheDirectory {
			get; set;
		}
		
		string Email {
			get; set;
		}
		
		StreamWriter Logger {
			get; set;
		}
		
		XmlSerializer Serializer {
			get; set;
		}
		
		public CrashReporter (string logDirectory, string email)
		{
			CacheDirectory = logDirectory;
			Email = email;
			if (!Directory.Exists (CacheDirectory))
				Directory.CreateDirectory (CacheDirectory);
			Logger = new StreamWriter (Path.Combine (CacheDirectory, "errors.log"));
			Logger.AutoFlush = true;
			Serializer = new XmlSerializer (typeof (List<CrashReport>));
		}
		
		public void ProcessCache ()
		{
			try {
				// If log only mode is enabled, don't try to process the cache
				if (!LogReportingService.ReportCrashes.GetValueOrDefault ())
					return;
				
				var reports = ReadCachedReports ();
				File.Delete (CacheFile);
				foreach (var v in reports)
					UploadOrCache (v);
			} catch (Exception ex) {
				Logger.WriteLine ("Exception processing stored cache: {0}", ex);
			}
		}
		
		public void UploadOrCache (string crashLog)
		{
			UploadOrCache (new CrashReport { Email = Email, CrashLogPath = crashLog  });
		}
		
		void UploadOrCache (CrashReport report)
		{
			if (!TryUploadReport (report)) {
				Logger.WriteLine ("Did not upload report, caching");
				AddReportToCache (report);
			}
		}
		
		List<CrashReport> ReadCachedReports ()
		{
			try {
				var fileInfo = new FileInfo (CacheFile);
				if (fileInfo.Exists)
					using (var s = fileInfo.OpenRead ())
						return (List<CrashReport>) Serializer.Deserialize (s);
			} catch {
				Logger.WriteLine ("Exception deserializing cached reports, ignoring");
			}
			return new List<CrashReport> ();
		}
		
		void AddReportToCache (CrashReport report)
		{
			try {
				// Ensure the log directory exists
				if (!Directory.Exists (CacheDirectory))
					Directory.CreateDirectory (CacheDirectory);
				
				// Make a copy of the crash log in our own cache and update the crash report info
				var newPath = Path.Combine (CacheDirectory, Path.GetFileName (report.CrashLogPath));
				if (report.CrashLogPath != newPath)
					File.Copy (report.CrashLogPath, newPath, true);
				report.CrashLogPath = newPath;
				
				// Update the list of pending crashes which we want to upload
				var reports = ReadCachedReports ();
				reports.Add (report);
				WriteReportsToCache (reports);
			} catch (Exception ex) {
				Logger.WriteLine ("Exception occurred when appending a crash report to the cache: {0}", ex);
			}
		}
		
		void WriteReportsToCache (List<CrashReport> reports)
		{
			using (var s = File.Open (CacheFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				Serializer.Serialize (s, reports);
		}
		
		bool TryUploadReport (CrashReport report)
		{
			try {
				if (!LogReportingService.ReportCrashes.GetValueOrDefault ()) {
					Logger.WriteLine ("CrashReporter is in log only mode. All crashes will be cached");
					return false;
				}
				
				var server = Environment.GetEnvironmentVariable ("MONODEVELOP_CRASHREPORT_SERVER");
				if (string.IsNullOrEmpty (server))
					server = "software.xamarin.com";

				var url = string.Format ("http://{0}/Service/IssueLogging?m={1}&n={2}", server, HttpUtility.UrlEncode (report.Email), HttpUtility.UrlEncode (Path.GetFileName (report.CrashLogPath)));
				Logger.WriteLine ("Trying to connect to: {0}", url);
				var request = WebRequest.Create (url);
				request.Method = "POST";
				using (var s = File.OpenRead (report.CrashLogPath)) {
					request.ContentLength = s.Length;
					// Write the log file to the request stream
					using (var requestStream = request.GetRequestStream ())
						s.CopyTo (requestStream);
				}
				Logger.WriteLine ("CrashReport sent to server, awaiting response...");
				
				// Ensure the server has correctly processed everything.
				using (var response = request.GetResponse ()) {
					var responseText = new StreamReader (response.GetResponseStream ()).ReadToEnd (); 
					if (responseText != "OK") {
						Logger.WriteLine ("Server responded with error: {0}", responseText);
						return false;
					}
				}
			} catch (Exception ex) {
				Logger.WriteLine ("Failed to upload report to the server.");
				Logger.WriteLine (ex);
				return false;
			}
			Logger.WriteLine ("Successfully uploaded");
			return true;
		}
	}
}