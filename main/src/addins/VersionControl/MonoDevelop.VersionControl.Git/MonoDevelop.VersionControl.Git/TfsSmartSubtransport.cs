//
// NtlmSmartSubtransport.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Net.Http;
using LibGit2Sharp;
using System.IO;

namespace MonoDevelop.VersionControl.Git
{
	public class TfsSmartSession : IDisposable
	{
		readonly SmartSubtransportRegistration<TfsSmartSubtransport> httpRegistration;
		readonly SmartSubtransportRegistration<TfsSmartSubtransport> httpsRegistration;

		public TfsSmartSession ()
		{
			httpRegistration = GlobalSettings.RegisterSmartSubtransport<TfsSmartSubtransport> ("http");
			httpsRegistration = GlobalSettings.RegisterSmartSubtransport<TfsSmartSubtransport> ("https");
		}

		public bool Disposed { get; private set; }
		public void Dispose ()
		{
			if (!Disposed) {
				GlobalSettings.UnregisterSmartSubtransport<TfsSmartSubtransport> (httpRegistration);
				GlobalSettings.UnregisterSmartSubtransport<TfsSmartSubtransport> (httpsRegistration);
				Disposed = true;
			}
		}
	}

	public class TfsSmartSubtransport : RpcSmartSubtransport
	{
		protected override SmartSubtransportStream Action(string url, GitSmartSubtransportAction action)
		{
			string postContentType = null;
			string serviceUri;

			switch (action) {
			case GitSmartSubtransportAction.UploadPackList:
				serviceUri = url + "/info/refs?service=git-upload-pack";
				break;
			case GitSmartSubtransportAction.UploadPack:
				serviceUri = url + "/git-upload-pack";
				postContentType = "application/x-git-upload-pack-request";
				break;
			case GitSmartSubtransportAction.ReceivePackList:
				serviceUri = url + "/info/refs?service=git-receive-pack";
				break;
			case GitSmartSubtransportAction.ReceivePack:
				serviceUri = url + "/git-receive-pack";
				postContentType = "application/x-git-receive-pack-request";
				break;
			default:
				throw new InvalidOperationException();
			}

			// Grab the credentials from the user.
			var httpClient = new HttpClient {
				Timeout = TimeSpan.FromMinutes (1.0),
			};

			var res = httpClient.GetAsync (serviceUri).Result;
			if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
				var cred = (UsernamePasswordCredentials)GitCredentials.TryGet (url, "", SupportedCredentialTypes.UsernamePassword, GitCredentialsType.Tfs);

				httpClient = new HttpClient (new HttpClientHandler { Credentials = new System.Net.NetworkCredential (cred.Username, cred.Password) }) {
					Timeout = TimeSpan.FromMinutes (1.0),
				};
			}

			return new TfsSmartSubtransportStream(this) {
				HttpClient = httpClient,
				ServiceUri = new Uri (serviceUri),
				PostContentType = postContentType,
			};
		}

		class TfsSmartSubtransportStream : SmartSubtransportStream
		{
			public HttpClient HttpClient;
			public Uri ServiceUri;
			public string PostContentType;

			Lazy<Stream> responseStream;
			MemoryStream requestStream;

			public TfsSmartSubtransportStream(SmartSubtransport smartSubtransport) : base(smartSubtransport)
			{
				responseStream = new Lazy<Stream> (CreateResponseStream);
			}

			Stream CreateResponseStream()
			{
				HttpResponseMessage result;

				if (requestStream == null)
					result = HttpClient.GetAsync (ServiceUri, HttpCompletionOption.ResponseHeadersRead).Result;
				else {
					requestStream.Seek (0, SeekOrigin.Begin);

					var streamContent = new StreamContent (requestStream);
					if (!string.IsNullOrEmpty (PostContentType))
						streamContent.Headers.Add ("Content-Type", PostContentType);

					result = HttpClient.SendAsync (new HttpRequestMessage (HttpMethod.Post, ServiceUri) {
						Content = streamContent,
					}, HttpCompletionOption.ResponseHeadersRead).Result;
				}

				return result.EnsureSuccessStatusCode().Content.ReadAsStreamAsync().Result;
			}

			public override int Read(Stream dataStream, long length, out long bytesRead)
			{
				bytesRead = 0L;
				var buffer = new byte[64 * 1024];
				int count;

				while (length > 0 && (count = responseStream.Value.Read(buffer, 0, (int)Math.Min(buffer.Length, length))) > 0) {
					dataStream.Write(buffer, 0, count);
					bytesRead += (long)count;
					length -= (long)count;
				}
				return 0;
			}

			public override int Write(Stream dataStream, long length)
			{
				requestStream = requestStream ?? new MemoryStream ();

				var buffer = new byte[64 * 1024];
				int count;
				while (length > 0 && (count = dataStream.Read(buffer, 0, (int)Math.Min(buffer.Length, length))) > 0) {
					requestStream.Write(buffer, 0, count);
					length -= (long)count;
				}
				return 0;
			}
		}
	}
}

