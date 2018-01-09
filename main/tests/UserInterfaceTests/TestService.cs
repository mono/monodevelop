//
// TestService.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2014 Xamarin Inc.
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
using MonoDevelop.Components.AutoTest;
using System.Collections.Generic;

namespace UserInterfaceTests
{
	public static class TestService
	{
		public static AutoTestClientSession Session { get; private set; }

		public static void StartSession (string file = null, string profilePath = null, string args = null)
		{
			Session = new AutoTestClientSession ();

			Session.StartApplication (file: file, args: args, environment: new Dictionary<string, string> {
				{ "MONODEVELOP_PROFILE", profilePath ?? Util.CreateTmpDir ("profile") }
			});

			Session.SetGlobalValue ("MonoDevelop.Core.Instrumentation.InstrumentationService.Enabled", true);
			Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.GrabDesktopFocus");
		}

		public static void EndSession ()
		{
			Session.Stop ();
		}
	}
}
