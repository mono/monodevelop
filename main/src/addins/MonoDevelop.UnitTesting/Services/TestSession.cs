//
// TestSession.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using System.IO;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.UnitTesting
{
	class TestSession: AsyncOperation
	{
		UnitTest test;
		TestMonitor monitor;
		Projects.ExecutionContext context;
		TestResultsPad resultsPad;

		public TestSession (UnitTest test, Projects.ExecutionContext context, TestResultsPad resultsPad, CancellationTokenSource cs)
		{
			this.test = test;
			if (context != null)
				this.context = new Projects.ExecutionContext (context.ExecutionHandler, new CustomConsoleFactory (context.ConsoleFactory, cs), context.ExecutionTarget);
			CancellationTokenSource = cs;
			monitor = new TestMonitor (resultsPad, CancellationTokenSource);
			this.resultsPad = resultsPad;
			resultsPad.InitializeTestRun (test, cs);
			Task = new Task ((Action)RunTests);
		}
		
		public Task Start ()
		{
			Task.Start ();
			return Task;
		}

		void RunTests ()
		{
			try {
				UnitTestService.ResetResult (test);

				TestContext ctx = new TestContext (monitor, resultsPad, context, DateTime.Now);
				test.Run (ctx);
				test.SaveResults ();
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				monitor.ReportRuntimeError (null, ex);
			} finally {
				monitor.FinishTestRun ();
			}
		}
	}
}
