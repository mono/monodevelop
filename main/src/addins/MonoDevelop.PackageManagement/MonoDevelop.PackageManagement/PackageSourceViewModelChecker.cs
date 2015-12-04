//
// PackageSourceChecker.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Threading.Tasks;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageSourceViewModelChecker : IDisposable
	{
		PackageManagementTaskFactory taskFactory = new PackageManagementTaskFactory ();
		List<ITask<PackageSourceViewModelCheckedEventArgs>> tasks = new List<ITask<PackageSourceViewModelCheckedEventArgs>>();

		public event EventHandler<PackageSourceViewModelCheckedEventArgs> PackageSourceChecked;

		public void Dispose ()
		{
			foreach (PackageManagementTask<PackageSourceViewModelCheckedEventArgs> task in tasks) {
				if (task.IsCompleted || task.IsFaulted || task.IsCancelled) {
					// Do nothing.
				} else {
					task.Cancel ();
				}
			}
		}

		public void Check(IEnumerable<PackageSourceViewModel> packageSources)
		{
			foreach (PackageSourceViewModel packageSource in packageSources) {
				Check (packageSource);
			}
		}

		public void Check(PackageSourceViewModel packageSource)
		{
			ITask<PackageSourceViewModelCheckedEventArgs> task = taskFactory.CreateTask (
				() => CheckPackageSourceUrl (packageSource),
				(t) => OnPackageSourceChecked (this, t));

			tasks.Add (task);
			task.Start ();
		}

		PackageSourceViewModelCheckedEventArgs CheckPackageSourceUrl (PackageSourceViewModel packageSource)
		{
			if (IsHttpPackageSource (packageSource.SourceUrl)) {
				return CheckHttpPackageSource (packageSource);
			}
			return CheckFileSystemPackageSource (packageSource);
		}

		PackageSourceViewModelCheckedEventArgs CheckHttpPackageSource (PackageSourceViewModel packageSource)
		{
			HttpClient httpClient = CreateHttpClient (packageSource);
			try {
				using (var response = (HttpWebResponse)httpClient.GetResponse ()) {
					if (response.StatusCode == HttpStatusCode.OK) {
						return new PackageSourceViewModelCheckedEventArgs (packageSource);
					} else {
						LoggingService.LogInfo ("Status code {0} returned from package source url '{1}'", response.StatusCode, packageSource.SourceUrl);
						return new PackageSourceViewModelCheckedEventArgs (packageSource, GettextCatalog.GetString ("Unreachable"));
					}
				}
			} catch (WebException ex) {
				return CreatePackageSourceViewModelCheckedEventArgs (packageSource, ex);
			} catch (Exception ex) {
				LogPackageSourceException (packageSource, ex);
				return new PackageSourceViewModelCheckedEventArgs (packageSource, ex.Message);
			}
		}

		/// <summary>
		/// Do not use cached credentials for the first request sent to a package source.
		/// 
		/// Once NuGet has a valid credential for a package source it is used from that point
		/// onwards. So if the user changes a valid username/password in Preferences
		/// to a non-valid username/password they will not see the 'Invalid credentials'
		/// warning. To workaround this caching we remove the credentials for the request
		/// the first time it is sent. Also the UseDefaultCredentials is set to true.
		/// This forces NuGet to get the credentials from the IPackageSourceProvider,
		/// which is implemented by the RegisteredPackageSourcesViewModel, and will 
		/// contain the latest usernames and passwords for all package sources.
		/// </summary>
		HttpClient CreateHttpClient(PackageSourceViewModel packageSource)
		{
			var httpClient = new HttpClient (new Uri (packageSource.SourceUrl));

			bool resetCredentials = true;
			httpClient.SendingRequest += (sender, e) => {
				if (resetCredentials && packageSource.HasPassword ()) {
					resetCredentials = false;
					e.Request.Credentials = null;
					e.Request.UseDefaultCredentials = true;
				}
			};

			return httpClient;
		}

		PackageSourceViewModelCheckedEventArgs CheckFileSystemPackageSource (PackageSourceViewModel packageSource)
		{
			var dir = packageSource.SourceUrl;
			if (dir.StartsWith ("file://", StringComparison.OrdinalIgnoreCase)) {
				dir = new Uri (dir).LocalPath;
			}
			if (Directory.Exists (dir)) {
				return new PackageSourceViewModelCheckedEventArgs (packageSource);
			}
			return new PackageSourceViewModelCheckedEventArgs (packageSource, GettextCatalog.GetString ("Directory not found"));
		}

		PackageSourceViewModelCheckedEventArgs CreatePackageSourceViewModelCheckedEventArgs (PackageSourceViewModel packageSource, WebException ex)
		{
			string errorMessage = ex.Message;
			var response = ex.Response as HttpWebResponse;
			if (response != null) {
				switch (response.StatusCode) {
				case HttpStatusCode.Unauthorized:
					errorMessage = GettextCatalog.GetString ("Invalid credentials");
					break;
				case HttpStatusCode.NotFound:
					errorMessage = GettextCatalog.GetString ("Not found");
					break;
				case HttpStatusCode.GatewayTimeout:
				case HttpStatusCode.RequestTimeout:
					errorMessage = GettextCatalog.GetString ("Unreachable");
					break;
				case HttpStatusCode.ProxyAuthenticationRequired:
					errorMessage = GettextCatalog.GetString ("Proxy authentication required");
					break;
				}
			}

			LogPackageSourceException (packageSource, ex);

			switch (ex.Status) {
			case WebExceptionStatus.ConnectFailure:
			case WebExceptionStatus.ConnectionClosed:
			case WebExceptionStatus.NameResolutionFailure:
			case WebExceptionStatus.ProxyNameResolutionFailure:
			case WebExceptionStatus.Timeout:
				errorMessage = GettextCatalog.GetString ("Unreachable");
				break;
			}

			return new PackageSourceViewModelCheckedEventArgs (packageSource, errorMessage);
		}

		void LogPackageSourceException (PackageSourceViewModel packageSource, Exception ex)
		{
			LoggingService.LogInfo (String.Format ("Package source '{0}' returned exception.", packageSource.SourceUrl), ex);
		}

		void OnPackageSourceChecked (object sender, ITask<PackageSourceViewModelCheckedEventArgs> task)
		{
			PackageSourceViewModelCheckedEventArgs eventArgs = CreateEventArgs (task);
			if (eventArgs != null && PackageSourceChecked != null) {
				PackageSourceChecked (this, task.Result);
			}
		}

		PackageSourceViewModelCheckedEventArgs CreateEventArgs (ITask<PackageSourceViewModelCheckedEventArgs> task)
		{
			if (task.IsFaulted) {
				LoggingService.LogError ("Package source check failed.", task.Exception);
				return null;
			} else if (task.IsCancelled) {
				// Do nothing.
				return null;
			}

			return task.Result;
		}

		bool IsHttpPackageSource (string url)
		{
			if (string.IsNullOrEmpty (url))
				return false;

			Uri uri = null;
			if (Uri.TryCreate (url, UriKind.Absolute, out uri)) {
				return IsHttp (uri);
			}

			return false;
		}

		bool IsHttp (Uri uri)
		{
			return (uri.Scheme == Uri.UriSchemeHttp) || (uri.Scheme == Uri.UriSchemeHttps);
		}
	}
}
