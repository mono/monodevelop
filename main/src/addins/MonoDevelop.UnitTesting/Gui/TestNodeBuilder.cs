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
using System.Globalization;
using System.Text;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.UnitTesting.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.UnitTesting
{
	class TestNodeBuilder: TypeNodeBuilder
	{
		public override Type CommandHandlerType {
			get { return typeof(TestNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/UnitTesting/ContextMenu/TestPad"; }
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
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			UnitTest test = dataObject as UnitTest;
			nodeInfo.Icon = test.StatusIcon;

			var singleTestSuffix = String.Empty;
			if (test is UnitTestGroup unitTestGroup) 
				singleTestSuffix =  GetSuffix (unitTestGroup, treeBuilder.Options ["CombineTestNamespaces"] );

			var title = RemoveGenericArgument (test.Title);
			title =  test.Title + singleTestSuffix ;

			if (test.Status == TestStatus.Running) {
				nodeInfo.Label = Ambience.EscapeText (title);
				return;
			} else if (test.Status == TestStatus.Loading) {
				nodeInfo.Label = Ambience.EscapeText (title) + GettextCatalog.GetString (" (Loading)");
				return;
			} else if (test.Status == TestStatus.LoadError) {
				nodeInfo.Label = Ambience.EscapeText (title) + GettextCatalog.GetString (" (Load failed)");
				return;
			} else {
				nodeInfo.Label = Ambience.EscapeText (title);

				UnitTestResult res = test.GetLastResult ();
				if (res != null && treeBuilder.Options ["ShowTestCounters"] && (test is UnitTestGroup)) {
					nodeInfo.Label += string.Format (GettextCatalog.GetString (" ({0} passed, {1} failed, {2} not run)"), res.Passed, res.ErrorsAndFailures, res.TestsNotRun);
				}

				if (treeBuilder.Options ["ShowTestTime"]) {
					nodeInfo.Label += string.Format ("   Time: {0}ms", res.Time.TotalMilliseconds);
				}
			}
		}

		static string GetSuffix (UnitTestGroup unitTestGroup, bool combineNested )
		{
			var rootTitle = unitTestGroup?.Title;
			var stringBuilder = new StringBuilder ();
			while (unitTestGroup != null)
					if (ContainsUnitTestCanMerge (unitTestGroup) && 
				        !(unitTestGroup is SolutionFolderTestGroup)) {
							var testCollection = unitTestGroup.Tests;
							var singleChildTestGroup = testCollection [0] as UnitTestGroup;
							if(singleChildTestGroup.CanMergeWithParent && combineNested)
								stringBuilder.Append (".").Append (singleChildTestGroup.Title);
						unitTestGroup = singleChildTestGroup;
					} else
						unitTestGroup = null;
			return stringBuilder.ToString ();
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			UnitTestGroup test = dataObject as UnitTestGroup;
			if (test == null)
				return;

			if (ContainsUnitTestCanMerge (test)  ) {
				BuildChildNodes (test, builder);
				return;
			}
			builder.AddChildren (test.Tests);
		}

		void BuildChildNodes (UnitTestGroup test, ITreeBuilder builder)
		{
			var combineTestNamespaces = builder.Options ["CombineTestNamespaces"];
			bool isSolution = test is SolutionFolderTestGroup;
			if (!isSolution && ContainsUnitTestCanMerge(test) && combineTestNamespaces) {
				var unitTestGroup = test.Tests[0] as UnitTestGroup;
				BuildChildNodes (unitTestGroup, builder);
				return;
			}
			builder.AddChildren (test.Tests);
		}

		static bool ContainsUnitTestCanMerge(UnitTestGroup test) => 
						test.Tests.Count == 1 && test.Tests[0] is UnitTestGroup &&
		                test.Tests [0].CanMergeWithParent;

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			UnitTestGroup test = dataObject as UnitTestGroup;
			return test != null && test.Tests.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			UnitTest test = (UnitTest) dataObject;
			test.TestChanged += OnTestChanged;
			test.TestStatusChanged += OnTestStatusChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			UnitTest test = (UnitTest) dataObject;
			test.TestChanged -= OnTestChanged;
			test.TestStatusChanged -= OnTestStatusChanged;
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

		static string RemoveGenericArgument (string title)
		{
			var leftParen = title.LastIndexOf ('(', title.Length - 1);
			if (leftParen > -1) {
				var leftAngleIndex = title.LastIndexOf ('<', leftParen);
				if (leftAngleIndex > -1) {
					var rightAngleIndex = title.IndexOf ('>', leftAngleIndex);
					if (rightAngleIndex > -1) {
						title = title.Substring (0, leftAngleIndex) + title.Substring (rightAngleIndex + 1);
					}
				}
			}
			return title;
		}
	}
	
	class TestNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (TestCommands.ShowTestCode)]
		protected async void OnShowTest ()
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			SourceCodeLocation loc = null;
//			UnitTestResult res = test.GetLastResult ();
			loc = test.SourceCodeLocation;
			if (loc != null)
				await IdeApp.Workbench.OpenDocument (loc.FileName, null, loc.Line, loc.Column);
		}
		
		[CommandHandler (TestCommands.GoToFailure)]
		protected async void OnShowFailure ()
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			SourceCodeLocation loc = null;
			UnitTestResult res = test.GetLastResult ();
			if (res != null && res.IsFailure)
				loc = res.GetFailureLocation ();
			if (loc == null)
				loc = test.SourceCodeLocation;
			if (loc != null)
				await IdeApp.Workbench.OpenDocument (loc.FileName, null, loc.Line, loc.Column);
		}

		[CommandUpdateHandler (TestCommands.GoToFailure)]
		protected void OnUpdateGoToFailure (CommandInfo info)
		{
			UnitTest test = CurrentNode.DataItem as UnitTest;
			info.Enabled = IsGoToFailureEnabled (test);
		}

		bool IsGoToFailureEnabled (UnitTest test)
		{
			if (/*test.SourceCodeLocation == null ||*/ test is UnitTestGroup)
				return false;

			UnitTestResult res = test.GetLastResult ();
			return res != null && res.IsFailure;
		}
	}
}
