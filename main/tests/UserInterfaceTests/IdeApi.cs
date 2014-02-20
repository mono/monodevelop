//
// IdeApi.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Components.AutoTest;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using NUnit.Framework;

namespace UserInterfaceTests
{
	public static class IdeApi
	{
		public static void OpenFile (FilePath file)
		{
			TestService.Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.OpenDocument", (FilePath) file, true);
			Assert.AreEqual (file, IdeApi.GetActiveDocumentFilename ());
		}

		public static FilePath OpenTestSolution (string solution)
		{
			FilePath path = Util.GetSampleProject (solution);

			var op = TestService.Session.GlobalInvoke<AutoTestOperation> (
				"MonoDevelop.Ide.IdeApp.Workspace.OpenWorkspaceItem", (string) path
			);

			op.WaitForCompleted ();
			Assert.IsTrue (op.Success);

			return path;
		}

		public static void CloseAll ()
		{
			TestService.Session.ExecuteCommand (FileCommands.CloseWorkspace);
			TestService.Session.ExecuteCommand (FileCommands.CloseAllFiles);
		}

		public static FilePath GetActiveDocumentFilename ()
		{
			return TestService.Session.GetGlobalValue<FilePath> ("MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument.FileName");
		}

		public static void BuildSolution (bool expectedResult = true)
		{
			TestService.Session.ExecuteCommand (ProjectCommands.BuildSolution);

			var buildOp = TestService.Session.GetGlobalValue<AutoTestOperation> (
				"MonoDevelop.Ide.IdeApp.ProjectOperations.CurrentBuildOperation"
			);
			buildOp.WaitForCompleted ();
			Assert.AreEqual (buildOp.Success, expectedResult);
		}
	}
}
