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
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;

namespace MonoDevelop.NUnit
{
	public class TestNodeBuilder: TypeNodeBuilder
	{
		EventHandler testChanged;
		EventHandler testStatusChanged;
		
		public TestNodeBuilder ()
		{
			testChanged = (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnTestChanged));
			testStatusChanged = (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnTestStatusChanged));
		}
		
		public override Type CommandHandlerType {
			get { return typeof(TestNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/TestPad/ContextMenu"; }
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
				label = test.Title + " (Loading)";
				return;
			} else if (test.Status == TestStatus.LoadError) {
				icon = CircleImage.Failure;
				label = test.Title + " (Load failed)";
				return;
			} else {
				label = test.Title;

				UnitTestResult res = test.GetLastResult ();
				if (res == null)
					icon = CircleImage.None;
				else if (res.IsFailure && res.IsSuccess)
					icon = CircleImage.SuccessAndFailure;
				else if (res.IsFailure)
					icon = CircleImage.Failure;
				else if (res.IsSuccess) {
					icon = CircleImage.Success;
					if (treeBuilder.Options ["ShowTestTime"]) {
						label += " (" + (res.Time.TotalMilliseconds) + " ms)";
					}
				}
				else if (res.IsIgnored)
					icon = CircleImage.NotRun;
				else
					icon = CircleImage.None;
				
				if (res != null && treeBuilder.Options ["ShowTestCounters"] && (test is UnitTestGroup)) {
					label += string.Format (" ({0} success, {1} failed, {2} ignored)", res.TotalSuccess, res.TotalFailures, res.TotalIgnored);
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
			SourceCodeLocation loc = test.SourceCodeLocation;
			if (loc != null)
				Runtime.FileService.OpenFile (loc.FileName, loc.Line, loc.Column, true);
		}
		
		[CommandUpdateHandler (TestCommands.ShowTestCode)]
		protected void OnUpdateRunTest (CommandInfo info)
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			info.Enabled = test.SourceCodeLocation != null;
		}
		
		[CommandHandler (ProjectCommands.Options)]
		protected void OnShowOptions ()
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			NUnitService.ShowOptionsDialog (test);
		}
	}
}
