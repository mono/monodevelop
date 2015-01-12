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
	static class TestStatusIcon
	{
		public static readonly Xwt.Drawing.Image Running;
		public static readonly Xwt.Drawing.Image None;
		public static readonly Xwt.Drawing.Image NotRun;
		public static readonly Xwt.Drawing.Image Loading;

		public static readonly Xwt.Drawing.Image Failure;
		public static readonly Xwt.Drawing.Image Success;
		public static readonly Xwt.Drawing.Image SuccessAndFailure;
		public static readonly Xwt.Drawing.Image Inconclusive;
		
		public static readonly Xwt.Drawing.Image OldFailure;
		public static readonly Xwt.Drawing.Image OldSuccess;
		public static readonly Xwt.Drawing.Image OldSuccessAndFailure;
		public static readonly Xwt.Drawing.Image OldInconclusive;

		static TestStatusIcon ()
		{
			try {
				Running = Xwt.Drawing.Image.FromResource ("unit-running-16.png");
				Failure = Xwt.Drawing.Image.FromResource ("unit-failed-16.png");
				None = Xwt.Drawing.Image.FromResource ("unit-not-yet-run-16.png");
				NotRun = Xwt.Drawing.Image.FromResource ("unit-skipped-16.png");
				Success = Xwt.Drawing.Image.FromResource ("unit-success-16.png");
				SuccessAndFailure = Xwt.Drawing.Image.FromResource ("unit-mixed-results-16.png");
				Loading = Xwt.Drawing.Image.FromResource ("unit-loading-16.png");
				Inconclusive = Xwt.Drawing.Image.FromResource ("unit-inconclusive-16.png");
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

