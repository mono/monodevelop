//
// ImageLoader.cs
//
// Author:
//     Jeffrey Stedfast <jeff@xamarin.com>
//     Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2012-2014 Xamarin Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Core.Web;
using System.Net;
using System.Net.Http;

namespace MonoDevelop.Components
{
	public class ImageLoader
	{
		readonly object mutex = new object ();
		Xwt.Drawing.Image image = null;
		string cachePath, url;
		double scaleFactor;

		internal ImageLoader (string cacheFile, string url, double scaleFactor)
		{
			Downloading = true;
			this.cachePath = cacheFile;
			this.url = url;
			this.scaleFactor = scaleFactor;
			Load ();
		}

		public Xwt.Drawing.Image Image {
			get { return image; }
		}

		public bool Downloading { get; private set; }

		void Load ()
		{
			string tempPath = cachePath + ".tmp";

			LoadFromDisk (cachePath, false);

			var finfo = new FileInfo (cachePath);

			var client = HttpClientProvider.CreateHttpClient (url);
			if (finfo.Exists)
				client.DefaultRequestHeaders.IfModifiedSince = finfo.LastWriteTime;
			client.GetAsync (url).ContinueWith (async t => {
				try {
					// If the errorcode is NotModified the file we cached on disk is still the latest one.
					if (t.Result.StatusCode == HttpStatusCode.NotModified) {
						Cleanup ();
						return;
					}
					//if 404, there is no gravatar for the user
					if (t.Result.StatusCode == HttpStatusCode.NotFound) {
						image = null;
						Cleanup ();
						return;
					}
					using (var response = t.Result) {
						finfo.Directory.Create ();

						// Copy out the new file and reload it
						if (response.StatusCode == HttpStatusCode.OK) {
							using (var tempFile = File.Create (tempPath)) {
								await response.Content.CopyToAsync (tempFile);
							}
							FileService.SystemRename (tempPath, cachePath);
						}
						LoadFromDisk (cachePath, true);
					}
				} catch (Exception ex) {
					var aex = ex as AggregateException;
					if (aex != null)
						ex = aex.Flatten ().InnerException;
					var wex = ex?.InnerException as WebException;
					if (wex != null && wex.Status.IsCannotReachInternetError ())
						LoggingService.LogWarning ("Gravatar service could not be reached.");
					else
						LoggingService.LogError ("Error in Gravatar downloader.", ex);
					Cleanup ();
				} finally {
					try {
						client.Dispose ();
						if (File.Exists (tempPath))
							File.Delete (tempPath);
					} catch (Exception ex) {
						LoggingService.LogError ("Error deleting Gravatar temp file.", ex);
					}
				}
			});
		}

		void Cleanup ()
		{
			Xwt.Application.Invoke (() => UpdateImage (image, true));
		}

		void LoadFromDisk (string path, bool downloaded)
		{
			Xwt.Application.Invoke (delegate {
				Xwt.Drawing.Image newImage = null;
				try {
					if (File.Exists (path)) {
						using (var stream = File.OpenRead (path))
							newImage = Xwt.Drawing.Image.FromStream (stream);
						if (Math.Abs (scaleFactor - 1) > 0.2)
							newImage = newImage.Scale (1 / scaleFactor);
					}
					UpdateImage (newImage, downloaded);
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to load cached image", ex);
					try {
						File.Delete (path);
					} catch {
						LoggingService.LogError ("Failed to delete corrupt cached image", ex);
					}
					UpdateImage (null, downloaded);
				}
			});
		}

		void UpdateImage (Xwt.Drawing.Image value, bool final)
		{
			EventHandler handler;

			lock (mutex) {
				handler = completed;
				if (image == value) {
					handler = null;
				} else {
					image = value;
				}

				// Null out the event handlers once the download has completed so we don't
				// retain the objects the handler references forever.
				if (final) {
					completed = null;
					Downloading = false;
				}
			}

			if (handler != null)
				handler (this, EventArgs.Empty);
		}

		EventHandler completed;
		public event EventHandler Completed {
			add {
				lock (mutex) {
					if (Downloading)
						completed += value;
					else
						value (this, EventArgs.Empty);
				}
			}
			remove {
				lock (mutex) {
					if (completed != null)
						completed -= value;
				}
			}
		}
	}
}
