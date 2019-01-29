//
// TestChart.cs
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

using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.UnitTesting.Commands
{
	public enum TestCommands
	{
		RunAllTests,
		RunTest,
		DebugTest,
		RunTestWith,
		ShowTestCode,
		SelectTestInTree,
		ShowTestDetails,
		GoToFailure,
		RerunTest,
	}

	public enum TestChartCommands
	{
		ShowResults,
		ShowTime,
		UseTimeScale,
		SingleDayResult,
		ShowSuccessfulTests,
		ShowFailedTests,
		ShowIgnoredTests
	}

	public enum NUnitProjectCommands
	{
		AddAssembly
	}

	public enum TextEditorCommands
	{
		RunTestAtCaret,
		DebugTestAtCaret,
		SelectTestAtCaret
	}

	class RunAllTestsHandler : CommandHandler
	{
		protected override void Run ()
		{
			WorkspaceObject ob = IdeApp.ProjectOperations.CurrentSelectedObject;
			if (ob != null) {
				UnitTest test = UnitTestService.FindRootTest (ob);
				if (test != null)
					UnitTestService.RunTest (test, null);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			WorkspaceObject ob = IdeApp.ProjectOperations.CurrentSelectedObject;
			if (ob != null) {
				UnitTest test = UnitTestService.FindRootTest (ob);
				info.Enabled = (test != null);
			} else
				info.Enabled = false;
		}
	}
}
