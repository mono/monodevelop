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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.NUnit
{
	public class NUnitProjectServiceExtension: ProjectExtension
	{
		bool checkingCanExecute;
		object canExecuteCheckLock = new object ();

		protected override async Task OnExecute (MonoDevelop.Core.ProgressMonitor monitor, MonoDevelop.Projects.ExecutionContext context, ConfigurationSelector configuration)
		{
			bool defaultCanExecute;

			lock (canExecuteCheckLock) {
				try {
					checkingCanExecute = true;
					defaultCanExecute = Project.CanExecute (context, configuration);
				} finally {
					checkingCanExecute = false;
				}
			}
			if (defaultCanExecute) {
				// It is executable by default
				await base.OnExecute (monitor, context, configuration);
				return;
			}
			UnitTest test = NUnitService.Instance.FindRootTest (Project);
			if (test != null) {
				var cs = new CancellationTokenSource ();
				using (monitor.CancellationToken.Register (cs.Cancel))
					await NUnitService.Instance.RunTest (test, context.ExecutionHandler, false, false, cs);
			}
		}
		
		protected override bool OnGetCanExecute (MonoDevelop.Projects.ExecutionContext context, ConfigurationSelector configuration)
		{
			// We check for DefaultExecutionHandlerFactory because the tests can't run using any other execution mode
			
			var res = base.OnGetCanExecute (context, configuration);
			lock (canExecuteCheckLock) {
				if (checkingCanExecute)
					return res;
			}
			if (res)
				return true;
			UnitTest test = NUnitService.Instance.FindRootTest (Project);
			return (test != null) && test.CanRun (context.ExecutionHandler);
		}
	}
}
