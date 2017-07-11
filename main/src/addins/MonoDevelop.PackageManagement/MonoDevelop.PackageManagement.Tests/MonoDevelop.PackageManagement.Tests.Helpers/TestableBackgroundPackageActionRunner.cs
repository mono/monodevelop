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
		public Queue<Action> BackgroundActionsQueued = new Queue<Action> ();

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
			CreateEventMonitorAction = (monitor, packageManagementEvents, completionSource) => {
				EventsMonitor = new TestablePackageManagementEventsMonitor (monitor, packageManagementEvents, completionSource);
				return EventsMonitor;
			};
		}

		public void ExecuteSingleBackgroundDispatch ()
		{
			var action = BackgroundActionsQueued.Dequeue ();
			action.Invoke ();
		}

		public void ExecuteBackgroundDispatch ()
		{
			while (BackgroundActionsQueued.Count > 0) {
				ExecuteSingleBackgroundDispatch ();
			}
		}

		protected override void BackgroundDispatch (Action action)
		{
			BackgroundActionsQueued.Enqueue (action);
		}

		protected override void GuiDispatch (Action action)
		{
			action ();
		}

		protected override void ClearDispatcher ()
		{
			BackgroundActionsQueued.Clear ();
		}

		public bool DispatcherIsDispatchingReturns;

		protected override bool DispatcherIsDispatching ()
		{
			return DispatcherIsDispatchingReturns;
		}

		public Func<ProgressMonitor,
			IPackageManagementEvents,
			TaskCompletionSource<bool>,
			PackageManagementEventsMonitor> CreateEventMonitorAction;

		protected override PackageManagementEventsMonitor CreateEventMonitor (
			ProgressMonitor monitor,
			IPackageManagementEvents packageManagementEvents,
			TaskCompletionSource<bool> taskCompletionSource)
		{
			return CreateEventMonitorAction (monitor, packageManagementEvents, taskCompletionSource);
		}

		public TestablePackageManagementEventsMonitor EventsMonitor;
	}
}

