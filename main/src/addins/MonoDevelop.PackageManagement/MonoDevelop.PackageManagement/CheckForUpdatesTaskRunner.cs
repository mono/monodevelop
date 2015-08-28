//
// CheckForUpdatesTaskRunner.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement
{
	public class CheckForUpdatesTaskRunner : IDisposable
	{
		ITaskFactory taskFactory;
		CheckForUpdatesTask currentCheckForUpdatesTask;
		ITask<CheckForUpdatesTask> task;

		public CheckForUpdatesTaskRunner (ITaskFactory taskFactory)
		{
			this.taskFactory = taskFactory;
		}

		public CheckForUpdatesTaskRunner ()
			: this (new PackageManagementTaskFactory ())
		{
		}

		public bool IsRunning {
			get { return currentCheckForUpdatesTask != null; }
		}

		public void Start (CheckForUpdatesTask checkForUpdatesTask)
		{
			Stop ();

			CreateCheckForUpdatesTask (checkForUpdatesTask);

			task.Start ();
		}

		void CreateCheckForUpdatesTask (CheckForUpdatesTask checkForUpdatesTask)
		{
			currentCheckForUpdatesTask = checkForUpdatesTask;

			task = taskFactory.CreateTask (
				() => CheckForUpdates (checkForUpdatesTask),
				OnCheckForUpdatesCompleted);
		}

		public void Dispose ()
		{
			Stop ();
		}

		public void Stop ()
		{
			if (task != null) {
				task.Cancel ();
				task = null;
			}
			if (currentCheckForUpdatesTask != null) {
				currentCheckForUpdatesTask.Dispose ();
				currentCheckForUpdatesTask = null;
			}
		}

		CheckForUpdatesTask CheckForUpdates (CheckForUpdatesTask currentTask)
		{
			currentTask.CheckForUpdates ();
			return currentTask;
		}

		void OnCheckForUpdatesCompleted (ITask<CheckForUpdatesTask> task)
		{
			if (task.IsFaulted) {
				if (IsCurrentTask (task)) {
					LogError ("Current check for updates task error.", task.Exception);
				} else {
					LogError ("Check for updates task error.", task.Exception);
				}
			} else if (task.IsCancelled) {
				// Ignore.
				return;
			} else if (!IsCurrentTask (task.Result)) {
				task.Result.Dispose ();
				return;
			} else {
				task.Result.CheckForUpdatesCompleted ();
				GuiBackgroundDispatch (() => {
					task.Result.Dispose ();
				});
			}

			currentCheckForUpdatesTask = null;
			this.task = null;
		}

		bool IsCurrentTask (CheckForUpdatesTask task)
		{
			return currentCheckForUpdatesTask == task;
		}

		protected virtual void LogError (string message, Exception ex)
		{
			LoggingService.LogError (message, ex);
		}

		bool IsCurrentTask (ITask<CheckForUpdatesTask> taskToCompare)
		{
			return taskToCompare == task;
		}

		protected virtual void GuiBackgroundDispatch (MessageHandler handler)
		{
			DispatchService.BackgroundDispatch (handler);
		}
	}
}

