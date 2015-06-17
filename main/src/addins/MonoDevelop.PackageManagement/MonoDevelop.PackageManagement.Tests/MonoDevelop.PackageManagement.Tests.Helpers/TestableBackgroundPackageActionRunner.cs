//
// TestableBackgroundPackageActionRunner.cs
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
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestableBackgroundPackageActionRunner : BackgroundPackageActionRunner
	{
		public List<MessageHandler> BackgroundDispatchersQueued = new List<MessageHandler> ();

		public TestableBackgroundPackageActionRunner (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
			: base (progressMonitorFactory, packageManagementEvents, progressProvider)
		{
			Init ();
		}

		void Init ()
		{
			CreateEventMonitorAction = (monitor, packageManagementEvents, progressProvider) => {
				EventsMonitor = new TestablePackageManagementEventsMonitor (monitor, packageManagementEvents, progressProvider);
				return EventsMonitor;
			};
		}

		public void ExecuteSingleBackgroundDispatch ()
		{
			BackgroundDispatchersQueued [0].Invoke ();
			BackgroundDispatchersQueued.RemoveAt (0);
		}

		public void ExecuteBackgroundDispatch ()
		{
			foreach (MessageHandler dispatcher in BackgroundDispatchersQueued) {
				dispatcher.Invoke ();
			}
			BackgroundDispatchersQueued.Clear ();
		}

		protected override void BackgroundDispatch (MessageHandler handler)
		{
			BackgroundDispatchersQueued.Add (handler);
		}

		public bool InvokeBackgroundDispatchAndWaitImmediately = true;

		protected override void BackgroundDispatchAndWait (MessageHandler handler)
		{
			if (InvokeBackgroundDispatchAndWaitImmediately) {
				handler.Invoke ();
			} else {
				BackgroundDispatchersQueued.Add (handler);
			}
		}

		protected override void GuiDispatch (MessageHandler handler)
		{
			handler.Invoke ();
		}

		public Func<IProgressMonitor,
			IPackageManagementEvents,
			IProgressProvider,
			PackageManagementEventsMonitor> CreateEventMonitorAction;

		protected override PackageManagementEventsMonitor CreateEventMonitor (
			IProgressMonitor monitor,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			return CreateEventMonitorAction (monitor, packageManagementEvents, progressProvider);
		}

		public TestablePackageManagementEventsMonitor EventsMonitor;
	}
}

