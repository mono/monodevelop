//
// PackageImageLoader.cs
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
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public class ImageLoader
	{
		public event EventHandler<ImageLoadedEventArgs> Loaded;

		public void LoadFrom (string uri, object state)
		{
			LoadFrom (new Uri (uri), state);
		}

		public void LoadFrom (Uri uri, object state)
		{
			var request = HttpWebRequest.Create (uri);
			Task<WebResponse> readTask = Task.Factory.FromAsync<WebResponse> (request.BeginGetResponse, request.EndGetResponse, null);
			readTask.ContinueWith (task => OnReadComplete (task, uri, state));
		}

		void OnReadComplete (Task<WebResponse> task, Uri uri, object state)
		{
			if (task.IsFaulted) {
				OnError (task.Exception, uri, state);
			} else if (task.IsCanceled) {
				// Do nothing.
			} else {
				LoadFrom (task.Result.GetResponseStream (), uri, state);
			}
		}

		void OnError (Exception ex, Uri uri, object state)
		{
			OnLoaded (new ImageLoadedEventArgs (ex, uri, state));
		}

		void LoadFrom (Stream stream, Uri uri, object state)
		{
			try {
				Image image = Image.FromStream (stream);
				OnLoaded (image, uri, state);
			} catch (Exception ex) {
				OnError (ex, uri, state);
			}
		}

		void OnLoaded (Image image, Uri uri, object state)
		{
			OnLoaded (new ImageLoadedEventArgs (image, uri, state));
		}

		void OnLoaded (ImageLoadedEventArgs eventArgs)
		{
			if (Loaded != null) {
				Loaded (this, eventArgs);
			}
		}
	}
}

