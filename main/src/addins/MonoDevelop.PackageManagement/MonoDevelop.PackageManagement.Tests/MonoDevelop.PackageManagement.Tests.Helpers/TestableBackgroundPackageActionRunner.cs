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
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class TestableBackgroundPackageActionRunner : BackgroundPackageActionRunner
	{
		public List<Action> BackgroundActionsQueued = new List<Action> ();

		public TestableBackgroundPackageActionRunner (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			PackageManagementInstrumentationService instrumentationService)
			: base (
				progressMonitorFactory,
				packageManagementEvents,
				instrumentationService)
		{
			Init ();
		}

		void Init ()
		{
			CreateEventMonitorAction = (monitor, packageManagementEvents) => {
				EventsMonitor = new TestablePackageManagementEventsMonitor (monitor, packageManagementEvents);
				return EventsMonitor;
			};
		}

		public void ExecuteSingleBackgroundDispatch ()
		{
			BackgroundActionsQueued [0].Invoke ();
			BackgroundActionsQueued.RemoveAt (0);
		}

		public void ExecuteBackgroundDispatch ()
		{
			foreach (Action action in BackgroundActionsQueued) {
				action ();
			}
			BackgroundActionsQueued.Clear ();
		}

		protected override void BackgroundDispatch (Action action)
		{
			BackgroundActionsQueued.Add (action);
		}

		protected override void GuiDispatch (Action action)
		{
			action ();
		}

		public Func<ProgressMonitor,
			IPackageManagementEvents,
			PackageManagementEventsMonitor> CreateEventMonitorAction;

		protected override PackageManagementEventsMonitor CreateEventMonitor (
			ProgressMonitor monitor,
			IPackageManagementEvents packageManagementEvents,
			TaskCompletionSource<bool> taskCompletionSource)
		{
			return CreateEventMonitorAction (monitor, packageManagementEvents);
		}

		public TestablePackageManagementEventsMonitor EventsMonitor;
	}
}

