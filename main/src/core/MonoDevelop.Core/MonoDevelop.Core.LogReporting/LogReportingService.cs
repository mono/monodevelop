// 
// LogReportingService.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011 Alan McGovern
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
using System.Threading;
using System.Net;
using System.Web;
using System.IO.Compression;

namespace MonoDevelop.Core.LogReporting
{
	public static class LogReportingService
	{
		public static readonly FilePath CrashLogDirectory = UserProfile.Current.LogDir.Combine ("LogAgent");
		
		const string ReportCrashesKey = "MonoDevelop.LogAgent.ReportCrashes";
		const string ReportUsageKey = "MonoDevelop.LogAgent.ReportUsage";
		
		static int CrashId;
		static int Processing;
		
		public static bool? ReportCrashes {
			get { return PropertyService.Get<bool?> (ReportCrashesKey); }
			set { PropertyService.Set (ReportCrashesKey, value); }
		}
			
		public static bool? ReportUsage {
			get { return PropertyService.Get<bool?> (ReportUsageKey); }
			set { PropertyService.Set (ReportUsageKey, value); }
		}
		
		public static void ReportUnhandledException (Exception ex)
		{
			// If crash reporting has been explicitly disabled, disregard this
			if (ReportCrashes.HasValue && !ReportCrashes.Value)
				return;

			var data = System.Text.Encoding.UTF8.GetBytes (ex.ToString ());
			var filename = string.Format ("{0}.{1}.crashlog", SystemInformation.SessionUuid, Interlocked.Increment (ref CrashId));
			if (!TryUploadReport (filename, data)) {
				if (!Directory.Exists (CrashLogDirectory))
					Directory.CreateDirectory (CrashLogDirectory);

				File.WriteAllBytes (CrashLogDirectory.Combine (filename), data);
			}
		}
		
		public static void ProcessCache ()
		{
			int origValue = -1;
			try {
				// Ensure only 1 thread at a time attempts to upload cached reports
				origValue = Interlocked.CompareExchange (ref Processing, 1, 0);
				if (origValue != 0)
					return;
				
				// Uploading is not enabled, so bail out
				if (!ReportCrashes.GetValueOrDefault ())
					return;
				
				// Definitely no crash reports if this doesn't exist
				if (!Directory.Exists (CrashLogDirectory))
					return;
				
				foreach (var file in Directory.GetFiles (CrashLogDirectory)) {
					if (TryUploadReport (file, File.ReadAllBytes (file)))
						File.Delete (file);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Exception processing cached crashes", ex);
			} finally {
				if (origValue == 0)
					Interlocked.CompareExchange (ref Processing, 0, 1);
			}
		}

		static bool TryUploadReport (string filename, byte[] data)
		{
			try {
				var server = Environment.GetEnvironmentVariable ("MONODEVELOP_CRASHREPORT_SERVER");
				if (string.IsNullOrEmpty (server))
					server = "monodevlog.xamarin.com:35162";

				var request = (HttpWebRequest) WebRequest.Create (string.Format ("http://{0}/logagentreport/", server));
				request.Headers.Add ("LogAgentVersion", "1");
				request.Headers.Add ("LogAgent_Filename", Path.GetFileName (filename));
				request.Headers.Add ("Content-Encoding", "gzip");
				request.Method = "POST";
				
				request.ContentLength = data.Length;
				using (var requestStream = new GZipStream (request.GetRequestStream (), CompressionMode.Compress))
					requestStream.Write (data, 0, data.Length);
				
				LoggingService.LogDebug ("CrashReport sent to server, awaiting response...");

				// Ensure the server has correctly processed everything.
				using (var response = (HttpWebResponse) request.GetResponse ()) {
					if (response.StatusCode != HttpStatusCode.OK) {
						LoggingService.LogError ("Server responded with status code {1} and error: {0}", response.StatusDescription, response.StatusCode);
						return false;
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to upload report to the server", ex);
				return false;
			}

			LoggingService.LogDebug ("Successfully uploaded crash report");
			return true;
		}
	}
}

