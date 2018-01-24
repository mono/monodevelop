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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NuGet.Credentials;

namespace MonoDevelop.PackageManagement
{
	internal class PackageSourceViewModelChecker : IDisposable
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();

		public event EventHandler<PackageSourceViewModelCheckedEventArgs> PackageSourceChecked;

		public void Dispose ()
		{
			cancellationTokenSource.Cancel ();
		}

		public void Check(IEnumerable<PackageSourceViewModel> packageSources)
		{
			foreach (PackageSourceViewModel packageSource in packageSources) {
				Check (packageSource);
			}
		}

		public void Check(PackageSourceViewModel packageSource)
		{
			Task.Run (() => CheckPackageSourceUrl (packageSource), cancellationTokenSource.Token)
				.ContinueWith (OnPackageSourceChecked, TaskScheduler.FromCurrentSynchronizationContext ());
		}

		Task<PackageSourceViewModelCheckedEventArgs> CheckPackageSourceUrl (PackageSourceViewModel packageSource)
		{
			if (IsHttpPackageSource (packageSource.Source)) {
				return CheckHttpPackageSource (packageSource);
			}
			return Task.FromResult (CheckFileSystemPackageSource (packageSource));
		}

		async Task<PackageSourceViewModelCheckedEventArgs> CheckHttpPackageSource (PackageSourceViewModel packageSource)
		{
			try {
				using (HttpClient httpClient = CreateHttpClient (packageSource)) {
					var result = await httpClient.GetAsync (packageSource.Source);
					if (result.StatusCode == HttpStatusCode.OK) {
						return new PackageSourceViewModelCheckedEventArgs (packageSource);
					} else {
						return CreatePackageSourceViewModelCheckedEventArgs (packageSource, result.StatusCode);
					}
				}
			} catch (Exception ex) {
				return CreatePackageSourceViewModelCheckedEventArgs (packageSource, ex);
			}
		}

		PackageSourceViewModelCheckedEventArgs CreatePackageSourceViewModelCheckedEventArgs (PackageSourceViewModel packageSource, HttpStatusCode statusCode)
		{
			string errorMessage = GetErrorForStatusCode (statusCode);
			if (errorMessage == null) {
				LoggingService.LogInfo ("Status code {0} returned from package source url '{1}'", statusCode, packageSource.Source);
				errorMessage = GettextCatalog.GetString ("Unreachable");
			}
			return new PackageSourceViewModelCheckedEventArgs (packageSource, errorMessage);
		}

		PackageSourceViewModelCheckedEventArgs CreatePackageSourceViewModelCheckedEventArgs (PackageSourceViewModel packageSource, Exception ex)
		{
			if (ex is AggregateException) {
				ex = ex.GetBaseException ();
			}

			var webException = ex.InnerException as WebException;
			if (webException != null) {
				return CreatePackageSourceViewModelCheckedEventArgs (packageSource, webException);
			}

			LogPackageSourceException (packageSource, ex);
			return new PackageSourceViewModelCheckedEventArgs (packageSource, ex.Message);
		}

		HttpClient CreateHttpClient(PackageSourceViewModel packageSource)
		{
			var credentialService = new CredentialService (new ICredentialProvider[0], true);
			return HttpClientFactory.CreateHttpClient (
				packageSource.GetPackageSource (),
				credentialService);
		}

		PackageSourceViewModelCheckedEventArgs CheckFileSystemPackageSource (PackageSourceViewModel packageSource)
		{
			var dir = packageSource.Source;
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
				errorMessage = GetErrorForStatusCode (response.StatusCode, errorMessage);
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

		static string GetErrorForStatusCode (HttpStatusCode statusCode, string defaultErrorMessage = null)
		{
			switch (statusCode) {
				case HttpStatusCode.Unauthorized:
				return GettextCatalog.GetString ("Invalid credentials");

				case HttpStatusCode.NotFound:
				return GettextCatalog.GetString ("Not found");

				case HttpStatusCode.GatewayTimeout:
				case HttpStatusCode.RequestTimeout:
				return GettextCatalog.GetString ("Unreachable");

				case HttpStatusCode.ProxyAuthenticationRequired:
				return GettextCatalog.GetString ("Proxy authentication required");

				case HttpStatusCode.BadRequest:
				return GettextCatalog.GetString ("Bad request");
			}

			return defaultErrorMessage;
		}

		void LogPackageSourceException (PackageSourceViewModel packageSource, Exception ex)
		{
			LoggingService.LogInfo (String.Format ("Package source '{0}' returned exception.", packageSource.Source), ex);
		}

		void OnPackageSourceChecked (Task<PackageSourceViewModelCheckedEventArgs> task)
		{
			PackageSourceViewModelCheckedEventArgs eventArgs = CreateEventArgs (task);
			if (eventArgs != null && PackageSourceChecked != null) {
				PackageSourceChecked (this, task.Result);
			}
		}

		PackageSourceViewModelCheckedEventArgs CreateEventArgs (Task<PackageSourceViewModelCheckedEventArgs> task)
		{
			if (task.IsFaulted) {
				LoggingService.LogError ("Package source check failed.", task.Exception);
				return null;
			} else if (task.IsCanceled) {
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
