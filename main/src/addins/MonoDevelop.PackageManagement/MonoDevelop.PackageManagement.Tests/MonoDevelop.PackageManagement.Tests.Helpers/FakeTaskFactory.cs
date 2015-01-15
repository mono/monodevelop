//
// FakeTaskFactory.cs
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
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeTaskFactory : ITaskFactory
	{
		public bool IsCreateTaskCalled;
		public bool RunTasksSynchronously;

		public FakeTask<PackagesForSelectedPageResult> FirstFakeTaskCreated { 
			get { return FakeTasksCreated [0] as FakeTask<PackagesForSelectedPageResult>; }
		}

		public List<object> FakeTasksCreated = new List<object> ();

		public ITask<TResult> CreateTask<TResult> (
			Func<TResult> function,
			Action<ITask<TResult>> continueWith)
		{
			IsCreateTaskCalled = true;
			var task = new FakeTask<TResult> (function, continueWith, RunTasksSynchronously);
			FakeTasksCreated.Add (task);
			return task;
		}

		public void ExecuteAllFakeTasks ()
		{
			foreach (FakeTask<PackagesForSelectedPageResult> task in FakeTasksCreated) {
				task.ExecuteTaskCompletely ();
			}
		}

		public void ExecuteAllTasks<T> ()
		{
			foreach (FakeTask<T> task in FakeTasksCreated) {
				task.ExecuteTaskCompletely ();
			}
		}

		public void ExecuteTask (int index)
		{
			var task = FakeTasksCreated [index] as FakeTask<PackagesForSelectedPageResult>;
			task.ExecuteTaskCompletely ();
		}

		public void ClearAllFakeTasks ()
		{
			FakeTasksCreated.Clear ();
		}
	}
}
