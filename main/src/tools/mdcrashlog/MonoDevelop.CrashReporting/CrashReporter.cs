// 
// CrashLogger.cs
//  
// Author:
//       alanmcgovern <>
// 
// Copyright (c) 2011 alanmcgovern
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

namespace MonoDevelop.CrashReporting
{
	public class CrashReporter {
		
		// The file which stores the list of reports which have to be uploaded
		string CacheFile {
			get { return Path.Combine (CacheDirectory, "crashlogs.xml"); }
		}
		
		// The directory where we cache all information for reports which
		// will be uploaded in the future.
		string CacheDirectory {
			get; set;
		}
		
		public CrashReporter (string logDirectory)
		{
			CacheDirectory = logDirectory;
		}
		
		public void UploadOrCache (string crashLog)
		{
			var report = new CrashReport { Email = "get_the_users@email.com", CrashLogPath = crashLog  };
			if (!TryUploadReport (report))
				AddReportToCache (report);
		}
		
		void AddReportToCache (CrashReport report)
		{
			try {
				// Ensure the log directory exists
				if (!Directory.Exists (CacheDirectory))
					Directory.CreateDirectory (CacheDirectory);
				
				// Make a copy of the crash log in our own cache and update the crash report info
				var newPath = Path.Combine (CacheDirectory, Path.GetFileName (report.CrashLogPath));
				File.Copy (report.CrashLogPath, newPath);
				report.CrashLogPath = newPath;
				
				// Update the list of pending crashes which we want to upload
				List<CrashReport> reports = null;
				var serializer = new XmlSerializer (typeof (List<CrashReport>));
				using (var s = File.Open (CacheFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
					try {
						reports = (List<CrashReport>) serializer.Deserialize (s);
					} catch {
						// If we can't deserialize, just ignore it. We don't care about corrupt lists.
						reports = new List<CrashReport> ();
					}
					
					reports.Add (report);
					s.SetLength (0);
					serializer.Serialize (s, reports);
				}
			} catch {
				Console.WriteLine ("Could not append crash information to the list of pending uploads.");
			}
		}
		
		bool TryUploadReport (CrashReport report)
		{
			try {
				var server = Environment.GetEnvironmentVariable ("CRASHREPORT_SERVER");
				if (string.IsNullOrEmpty (server))
					server = "software.xamarin.com";

				var url = string.Format ("http://{0}/Service/IssueLogging?m={1}&n={2}", server, HttpUtility.UrlEncode (report.Email), HttpUtility.UrlEncode (Path.GetFileName (report.CrashLogPath)));
				Console.WriteLine ("Trying to connect to: {0}", url);
				var request = WebRequest.Create (url);
				request.Method = "POST";
				
				// Write the log file to the request stream
				var requestStream = request.GetRequestStream ();
				using (var s = File.OpenRead (report.CrashLogPath))
					s.CopyTo (requestStream);
				
				// Ensure the server has correctly processed everything.
				using (var response = request.GetResponse ()) {
					if (new StreamReader (response.GetResponseStream ()).ReadToEnd () != "OK") {
						Console.WriteLine ("Server did not respond with success");
						return false;
					}
				}
			} catch (Exception ex) {
				Console.WriteLine ("Failed to upload report to the server.");
				Console.WriteLine (ex);
				return false;
			}
			Console.WriteLine ("Successfully uploaded");
			return true;
		}
	}
}

