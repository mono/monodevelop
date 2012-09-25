//
// TestNodeBuilder.cs
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

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.NUnit.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.NUnit
{
	public class TestNodeBuilder: TypeNodeBuilder
	{
		EventHandler testChanged;
		EventHandler testStatusChanged;
		
		public TestNodeBuilder ()
		{
			testChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnTestChanged));
			testStatusChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnTestStatusChanged));
		}
		
		public override Type CommandHandlerType {
			get { return typeof(TestNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/NUnit/ContextMenu/TestPad"; }
		}
			
		public override Type NodeDataType {
			get { return typeof(UnitTest); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((UnitTest)dataObject).Name;
		}
		
/*		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.UseMarkup;
		}
*/
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			UnitTest test = dataObject as UnitTest;
			
			if (test.Status == TestStatus.Running) {
				icon = CircleImage.Running;
				label = test.Title;
				return;
			} else if (test.Status == TestStatus.Loading) {
				icon = CircleImage.Loading;
				label = test.Title + GettextCatalog.GetString (" (Loading)");
				return;
			} else if (test.Status == TestStatus.LoadError) {
				icon = CircleImage.Failure;
				label = test.Title + GettextCatalog.GetString (" (Load failed)");
				return;
			} else {
				label = test.Title;

				UnitTestResult res = test.GetLastResult ();
				if (res == null)
					icon = CircleImage.None;
				else if (res.ErrorsAndFailures > 0 && res.Passed > 0)
					icon = CircleImage.SuccessAndFailure;
				else if (res.IsInconclusive)
					icon = CircleImage.Inconclusive;
				else if (res.IsFailure)
					icon = CircleImage.Failure;
				else if (res.IsSuccess) {
					icon = CircleImage.Success;

				} else if (res.IsNotRun)
					icon = CircleImage.NotRun;
				else
					icon = CircleImage.None;

				if (res != null && treeBuilder.Options ["ShowTestCounters"] && (test is UnitTestGroup)) {
					label += string.Format (GettextCatalog.GetString (" ({0} passed, {1} failed, {2} not run)"), res.Passed, res.ErrorsAndFailures, res.TestsNotRun);
				}

				if (treeBuilder.Options ["ShowTestTime"]) {
					label += "   Time: {0}ms" + (res.Time.TotalMilliseconds);
				}
			}
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			UnitTestGroup test = dataObject as UnitTestGroup;
			if (test == null)
				return;
				
			foreach (UnitTest t in test.Tests)
				builder.AddChild (t);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			UnitTestGroup test = dataObject as UnitTestGroup;
			return test != null && test.Tests.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			UnitTest test = (UnitTest) dataObject;
			test.TestChanged += testChanged;
			test.TestStatusChanged += testStatusChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			UnitTest test = (UnitTest) dataObject;
			test.TestChanged -= testChanged;
			test.TestStatusChanged -= testStatusChanged;
		}
		
		public void OnTestChanged (object sender, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) tb.UpdateAll ();
		}
		
		public void OnTestStatusChanged (object sender, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) tb.Update ();
		}
	}
	
	class TestNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (TestCommands.ShowTestCode)]
		protected void OnShowTest ()
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			SourceCodeLocation loc = null;
//			UnitTestResult res = test.GetLastResult ();
			loc = test.SourceCodeLocation;
			if (loc != null)
				IdeApp.Workbench.OpenDocument (loc.FileName, loc.Line, loc.Column);
		}
		
		[CommandHandler (TestCommands.GoToFailure)]
		protected void OnShowFailure ()
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			SourceCodeLocation loc = null;
			UnitTestResult res = test.GetLastResult ();
			if (res != null && res.IsFailure)
				loc = res.GetFailureLocation ();
			if (loc == null)
				loc = test.SourceCodeLocation;
			if (loc != null)
				IdeApp.Workbench.OpenDocument (loc.FileName, loc.Line, loc.Column);
		}
		
		[CommandUpdateHandler (TestCommands.ShowTestCode)]
		protected void OnUpdateRunTest (CommandInfo info)
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			info.Enabled = test.SourceCodeLocation != null;
		}
		
		[CommandUpdateHandler (ProjectCommands.Options)]
		protected void OnUpdateShowOptions (CommandInfo info)
		{
			info.Visible = !(CurrentNode.DataItem is SolutionFolderTestGroup);
		}
		
		[CommandHandler (ProjectCommands.Options)]
		protected void OnShowOptions ()
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			NUnitService.ShowOptionsDialog (test);
		}
	}
}
