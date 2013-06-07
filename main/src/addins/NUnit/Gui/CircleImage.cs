//
// CircleImage.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.NUnit
{
	static class CircleImage
	{
		internal static Gdk.Pixbuf Running;
		internal static Gdk.Pixbuf None;
		internal static Gdk.Pixbuf NotRun;
		internal static Gdk.Pixbuf Loading;

		internal static Gdk.Pixbuf Failure;
		internal static Gdk.Pixbuf Success;
		internal static Gdk.Pixbuf SuccessAndFailure;
		internal static Gdk.Pixbuf Inconclusive;
		
		internal static Gdk.Pixbuf OldFailure;
		internal static Gdk.Pixbuf OldSuccess;
		internal static Gdk.Pixbuf OldSuccessAndFailure;
		internal static Gdk.Pixbuf OldInconclusive;

		static CircleImage ()
		{
			try {
				Running = Gdk.Pixbuf.LoadFromResource ("NUnit.Running.png");
				Failure = Gdk.Pixbuf.LoadFromResource ("NUnit.Failed.png");
				None = Gdk.Pixbuf.LoadFromResource ("NUnit.None.png");
				NotRun = Gdk.Pixbuf.LoadFromResource ("NUnit.NotRun.png");
				Success = Gdk.Pixbuf.LoadFromResource ("NUnit.Success.png");
				SuccessAndFailure = Gdk.Pixbuf.LoadFromResource ("NUnit.SuccessAndFailed.png");
				Loading = Gdk.Pixbuf.LoadFromResource ("NUnit.Loading.png");
				Inconclusive = Gdk.Pixbuf.LoadFromResource ("NUnit.Inconclusive.png");
				OldFailure = ImageService.MakeTransparent (Failure, 0.4);
				OldSuccess = ImageService.MakeTransparent (Success, 0.4);
				OldSuccessAndFailure = ImageService.MakeTransparent (SuccessAndFailure, 0.4);
				OldInconclusive = ImageService.MakeTransparent (Inconclusive, 0.4);
			} catch (Exception e) {
				LoggingService.LogError ("Error while loading icons.", e);
			}
		}
	}
}

