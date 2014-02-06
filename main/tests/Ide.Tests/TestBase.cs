//
// TestBase.cs
//
// Author:
//       Alan McGovern <alan@xamarin.com>
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
using System.Threading;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;
using NUnit.Framework;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public abstract class TestBase
	{
		static bool Initialized {
			get; set;
		}

		[SetUp]
		public virtual void Setup ()
		{
			// All this initialization was copied/pasted from IdeApp.Run. Hopefully i copied enough of it.
			if (!Initialized) {
				Initialized = true;

				// HERE BE DRAGONS
				// If we initialize the various Xamarin Studio services we hit many bugs with bad finalizers
				// or other shutdown related race conditions which cause this test to randomly fail. Rather
				// than trying to fix all of the races, i'm just disabling part of the test which creates a
				// Gtk# project so we can bypass all of the issues.

//				//ensure native libs initialized before we hit anything that p/invokes
//				MonoDevelop.Core.Platform.Initialize ();
//				// Set a synchronization context for the main gtk thread
//				SynchronizationContext.SetSynchronizationContext (new GtkSynchronizationContext ());
//				IdeApp.Customizer = new MonoDevelop.Ide.Extensions.IdeCustomizer ();
//
//				DispatchService.Initialize ();
//				InternalLog.Initialize ();
//				DesktopService.Initialize ();
//				ImageService.Initialize ();
//				IdeApp.Initialize (new NullProgressMonitor ());
			}
		}

		[TearDown]
		public virtual void Teardown ()
		{

		}
	}
}

