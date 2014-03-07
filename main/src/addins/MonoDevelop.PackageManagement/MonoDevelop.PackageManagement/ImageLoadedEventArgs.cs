//
// ImageLoadedEventArgs.cs
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
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public class ImageLoadedEventArgs : EventArgs
	{
		public ImageLoadedEventArgs (Image image, Uri uri, object state)
		{
			Uri = uri;
			Image = image;
			State = state;
		}

		public ImageLoadedEventArgs (Exception ex, Uri uri, object state)
		{
			Uri = uri;
			Exception = ex;
			State = state;
		}

		public Image Image { get; private set; }
		public Uri Uri { get; private set; }
		public Exception Exception { get; private set; }
		public object State { get; private set; }

		public bool HasError {
			get { return Exception != null; }
		}

		public ImageLoadedEventArgs WithState (object state)
		{
			var eventArgs = (ImageLoadedEventArgs)MemberwiseClone ();
			eventArgs.State = state;
			return eventArgs;
		}
	}
}

