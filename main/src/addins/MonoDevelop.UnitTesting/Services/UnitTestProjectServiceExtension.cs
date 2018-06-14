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
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.UnitTesting
{
	public class UnitTestProjectServiceExtension: ProjectExtension
	{
		object canExecuteCheckLock = new object ();

		bool unitTestChecked;
		UnitTest unitTestFound;

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return IdeApp.IsInitialized && base.SupportsObject (item);
		}

		protected override void Initialize ()
		{
			base.Initialize ();
			if (IdeApp.IsInitialized)
				UnitTestService.TestSuiteChanged += TestSuiteChanged;
		}

		public override void Dispose ()
		{
			base.Dispose ();
			if (IdeApp.IsInitialized)
				UnitTestService.TestSuiteChanged -= TestSuiteChanged;
		}

		void TestSuiteChanged (object sender, System.EventArgs e)
		{
			unitTestChecked = false;
			unitTestFound = null;
		}

		UnitTest FindRootTest ()
		{
			if (!unitTestChecked) {
				unitTestFound = UnitTestService.FindRootTest (Project);
				unitTestChecked = true;
			}
			return unitTestFound;
		}

		static UnitTestingRunConfiguration unitTestingRunConfigurationInstance = new UnitTestingRunConfiguration ();

		protected override IEnumerable<SolutionItemRunConfiguration> OnGetRunConfigurations (OperationContext ctx)
		{
			var configs = base.OnGetRunConfigurations (ctx);

			// If the project has unit tests, add a configuration for running the tests
			if (FindRootTest () != null)
				configs = configs.Concat (unitTestingRunConfigurationInstance);
			
			return configs;
		}

		protected override async Task OnExecute (MonoDevelop.Core.ProgressMonitor monitor, MonoDevelop.Projects.ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (runConfiguration == unitTestingRunConfigurationInstance) {
				// The user selected to run the tests
				UnitTest test = FindRootTest ();
				if (test != null) {
					var cs = new CancellationTokenSource ();
					using (monitor.CancellationToken.Register (cs.Cancel))
						await UnitTestService.RunTest (test, context, false, false, cs);
				}
			} else
				await base.OnExecute (monitor, context, configuration, runConfiguration);
		}

		protected override bool OnGetCanExecute (MonoDevelop.Projects.ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (runConfiguration == unitTestingRunConfigurationInstance) {
				UnitTest test = FindRootTest ();
				return (test != null) && test.CanRun (context.ExecutionHandler);
			}
			return base.OnGetCanExecute (context, configuration, runConfiguration);
		}
	}
}
