// NUnitProjectServiceExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.NUnit
{
	public class NUnitProjectServiceExtension: ProjectServiceExtension
	{
		public override void Execute (MonoDevelop.Core.IProgressMonitor monitor, IBuildTarget item, ExecutionContext context, string configuration)
		{
			if (base.CanExecute (item, context, configuration)) {
				// It is executable by default
				base.Execute(monitor, item, context, configuration);
				return;
			} else if (item is IWorkspaceObject) {
				UnitTest test = NUnitService.Instance.FindRootTest ((IWorkspaceObject)item);
				if (test != null) {
					IAsyncOperation oper = null;
					DispatchService.GuiSyncDispatch (delegate {
						oper = NUnitService.Instance.RunTest (test, context.ExecutionHandler, false);
					});
					if (oper != null) {
						monitor.CancelRequested += delegate {
							oper.Cancel ();
						};
						oper.WaitForCompleted ();
					}
				}
			}
		}
		
		public override bool CanExecute (IBuildTarget item, ExecutionContext context, string configuration)
		{
			// We check for DefaultExecutionHandlerFactory because the tests can't run using any other execution mode
			
			bool res = base.CanExecute (item, context, configuration);
			if (!res && (item is IWorkspaceObject)) {
				UnitTest test = NUnitService.Instance.FindRootTest ((IWorkspaceObject)item);
				return (test != null) && test.CanRun (context.ExecutionHandler);
			} else
				return res;
		}
	}
}
