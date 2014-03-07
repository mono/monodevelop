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
using MonoDevelop.Ide;
using NuGet;
using ICSharpCode.PackageManagement;
using Xwt.Drawing;
using System.Collections.Generic;

namespace MonoDevelop.PackageManagement
{
	public class ImageLoader
	{
		public event EventHandler<ImageLoadedEventArgs> Loaded;

		PackageManagementTaskFactory taskFactory = new PackageManagementTaskFactory ();
		Dictionary<Uri, List<object>> callersWaitingForImageLoad = new Dictionary<Uri, List<object>> ();
		static readonly ImageCache imageCache = new ImageCache ();

		public void LoadFrom (string uri, object state)
		{
			LoadFrom (new Uri (uri), state);
		}

		public void LoadFrom (Uri uri, object state)
		{
			Image image = imageCache.GetImage (uri);
			if (image != null) {
				OnLoaded (new ImageLoadedEventArgs (image, uri, state));
				return;
			}

			if (AddToCallersWaitingForImageLoad (uri, state))
				return;

			ITask<ImageLoadedEventArgs> loadTask = taskFactory.CreateTask (
				() => LoadImage (uri, state),
				(task) => OnLoaded (task, uri, state));

			loadTask.Start ();
		}

		bool AddToCallersWaitingForImageLoad (Uri uri, object state)
		{
			List<object> callers = GetCallersWaitingForImageLoad (uri);
			if (callers != null) {
				callers.Add (state);
				return true;
			} else {
				callersWaitingForImageLoad.Add (uri, new List<object> ());
			}
			return false;
		}

		List<object> GetCallersWaitingForImageLoad (Uri uri)
		{
			List<object> callers = null;
			if (callersWaitingForImageLoad.TryGetValue (uri, out callers)) {
				return callers;
			}
			return null;
		}

		ImageLoadedEventArgs LoadImage (Uri uri, object state)
		{
			try {
				var httpClient = new HttpClient (uri);
				Stream stream = httpClient.GetResponse ().GetResponseStream ();
				Image image = Image.FromStream (stream);

				return new ImageLoadedEventArgs (image, uri, state);
			} catch (Exception ex) {
				return new ImageLoadedEventArgs (ex, uri, state);
			}
		}

		void OnLoaded (ITask<ImageLoadedEventArgs> task, Uri uri, object state)
		{
			if (task.IsFaulted) {
				OnError (task.Exception, uri, state);
			} else if (task.IsCancelled) {
				// Do nothing.
			} else {
				OnLoaded (task.Result);
			}
		}

		void OnLoaded (ImageLoadedEventArgs eventArgs)
		{
			if (eventArgs.Image != null) {
				imageCache.AddImage (eventArgs.Uri, eventArgs.Image);
			}

			OnLoaded (this, eventArgs);

			List<object> callers = GetCallersWaitingForImageLoad (eventArgs.Uri);
			if (callers != null) {
				OnLoaded (callers, eventArgs);
				callersWaitingForImageLoad.Remove (eventArgs.Uri);
			}
		}

		void OnError (Exception ex, Uri uri, object state)
		{
			OnLoaded (new ImageLoadedEventArgs (ex, uri, state));
		}

		void OnLoaded (object sender, ImageLoadedEventArgs eventArgs)
		{
			if (Loaded != null) {
				Loaded (this, eventArgs);
			}
		}

		void OnLoaded (IEnumerable<object> states, ImageLoadedEventArgs eventArgs)
		{
			foreach (object state in states) {
				OnLoaded (this, eventArgs.WithState (state));
			}
		}

		public void ShrinkImageCache ()
		{
			imageCache.ShrinkImageCache ();
		}

		public void Dispose ()
		{
			ShrinkImageCache ();
		}
	}
}

