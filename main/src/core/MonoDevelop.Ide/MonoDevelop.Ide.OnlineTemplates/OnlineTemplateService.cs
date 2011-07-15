// 
// OnlineTemplateService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.OnlineTemplates
{
	static class OnlineTemplateService
	{
		//FIXME: get a URL for the template repo
		const string PROJECT_TEMPLATE_INDEX_URL = "";
		
		static FilePath ProjectTemplateIndexFile {
			get {
				return UserProfile.Current.CacheDir.Combine ("OnlineTemplates", "ProjectTemplateIndex.xml");
			}
		}
		
		internal static string GetOnlineTemplatesRoot ()
		{
			string platform;
			if (Platform.IsWindows)
				platform = "win32";
			else if (Platform.IsMac)
				platform = "mac";
			else
				platform = "linux";
			
			var version = Mono.Addins.AddinManager.CurrentAddin.Version;
			
			return "http://monodevelop.com/online-templates/" + platform + "/" + version + "/";
		}
		
		public static Task<ProjectTemplateIndex> GetProjectTemplates ()
		{
			string url = GetOnlineTemplatesRoot () + "project-template-index.xml";
			var up = UpdateTemplateIndex (url, ProjectTemplateIndexFile, TimeSpan.FromDays (1));
			if (up != null) {
				return up.ContinueWith (t => ProjectTemplateIndex.Load (ProjectTemplateIndexFile));
			} else {
				return Task.Factory.StartNew (() => ProjectTemplateIndex.Load (ProjectTemplateIndexFile));
			}
		}
		
		static Task UpdateTemplateIndex (string url, FilePath file, TimeSpan? updateIfOlderThan)
		{
			LoggingService.LogInfo ("Updating online template index '{0}'.", url);
			
			if (updateIfOlderThan.HasValue) {
				if (File.Exists (file) && (DateTime.Now - File.GetLastWriteTime (file)) < updateIfOlderThan.Value) {
					LoggingService.LogInfo ("Skipped updating online template index because cache is too recent");
					return null;
				}
			}
			
			try {
				return DownloadTemplateIndex (url, file);
			} catch (Exception ex) {
				string message = string.Format ("Online template index '{0}' could not be downloaded.", url);
				LoggingService.LogWarning (message, ex);
				throw;
			}
		}
		
		static Task DownloadTemplateIndex (string url, FilePath file)
		{
			var request = (HttpWebRequest) WebRequest.Create (url);
			
			var fileInfo = new FileInfo (file);
			if (fileInfo.Exists) {
				var lastWrite = fileInfo.LastWriteTime;
				request.IfModifiedSince = lastWrite;
			}
				
			var t = Task_Factory_FromAsync<WebResponse> (request.BeginGetResponse, request.EndGetResponse, null);
			return t.ContinueWith ((Task<WebResponse> twr) => {
				try {
					var response = (HttpWebResponse) twr.Result;
					if (response.StatusCode == HttpStatusCode.OK) {
						if (!Directory.Exists (file.ParentDirectory))
							Directory.CreateDirectory (file.ParentDirectory);
						using (var fs = File.Create (file))
							response.GetResponseStream ().CopyTo (fs);
						LoggingService.LogInfo ("Updated online template index '{0}'.", url);
					}
				} catch (WebException wex) {
					var httpResp = wex.Response as HttpWebResponse;
					if (httpResp != null && httpResp.StatusCode == HttpStatusCode.NotModified) {
						File.SetLastWriteTime (file, DateTime.Now);
						LoggingService.LogInfo ("Online template index is up-to-date.");
					} else {
						string message = string.Format ("Online template index '{0}' could not be downloaded.", url);
						LoggingService.LogWarning (message, wex);
						throw;
					}
				} catch (Exception ex) {
					string message = string.Format ("Online template index '{0}' could not be downloaded.", url);
					LoggingService.LogWarning (message, ex);
					throw;
				} 
			});
		}
		
		//Mono 2.8 doesn't implement Task.Factory.FromAsync
		static Task<TResult> Task_Factory_FromAsync<TResult> (Func<AsyncCallback,object,IAsyncResult> beginMethod,
			Func<IAsyncResult,TResult> endMethod, object state)
		{
			var completionSource = new TaskCompletionSource<TResult> (TaskCreationOptions.None);
			beginMethod ((ar) => {
				try {
					completionSource.SetResult (endMethod (ar));
				} catch (Exception e) {
					completionSource.SetException (e);
				}
			}, state);
			return completionSource.Task;
		}
	}
}
