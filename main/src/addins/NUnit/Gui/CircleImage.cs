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
		internal static Xwt.Drawing.Image Running;
		internal static Xwt.Drawing.Image None;
		internal static Xwt.Drawing.Image NotRun;
		internal static Xwt.Drawing.Image Loading;

		internal static Xwt.Drawing.Image Failure;
		internal static Xwt.Drawing.Image Success;
		internal static Xwt.Drawing.Image SuccessAndFailure;
		internal static Xwt.Drawing.Image Inconclusive;
		
		internal static Xwt.Drawing.Image OldFailure;
		internal static Xwt.Drawing.Image OldSuccess;
		internal static Xwt.Drawing.Image OldSuccessAndFailure;
		internal static Xwt.Drawing.Image OldInconclusive;

		static CircleImage ()
		{
			try {
				Running = Xwt.Drawing.Image.FromResource ("NUnit.Running.png");
				Failure = Xwt.Drawing.Image.FromResource ("NUnit.Failed.png");
				None = Xwt.Drawing.Image.FromResource ("NUnit.None.png");
				NotRun = Xwt.Drawing.Image.FromResource ("NUnit.NotRun.png");
				Success = Xwt.Drawing.Image.FromResource ("NUnit.Success.png");
				SuccessAndFailure = Xwt.Drawing.Image.FromResource ("NUnit.SuccessAndFailed.png");
				Loading = Xwt.Drawing.Image.FromResource ("NUnit.Loading.png");
				Inconclusive = Xwt.Drawing.Image.FromResource ("NUnit.Inconclusive.png");
				OldFailure = Failure.WithAlpha (0.4);
				OldSuccess = Success.WithAlpha (0.4);
				OldSuccessAndFailure = SuccessAndFailure.WithAlpha (0.4);
				OldInconclusive = Inconclusive.WithAlpha (0.4);
			} catch (Exception e) {
				LoggingService.LogError ("Error while loading icons.", e);
			}
		}
	}
}

