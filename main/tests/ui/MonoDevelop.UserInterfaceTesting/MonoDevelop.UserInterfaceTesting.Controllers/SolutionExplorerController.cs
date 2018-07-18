//
// SolutionExplorerController.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System.Linq;
using MonoDevelop.Components.AutoTest;
using System.Collections.Generic;

namespace MonoDevelop.UserInterfaceTesting.Controllers
{
	public static class SolutionExplorerController
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		readonly static Func<AppQuery, AppQuery> topLevel = c => c.Window ().Children ().TreeView ().Marked (
			"MonoDevelop.Ide.Gui.Components.ExtensibleTreeView+ExtensibleTreeViewTree").Model ();

		public static Func<AppQuery, AppQuery> GetSolutionQuery (string solutionLabel)
		{
			return c => topLevel (c).Children (false).Index (0).Property ("Label", solutionLabel);
		}

		public static Func<AppQuery, AppQuery> GetProjectQuery (string solutionLabel, string projectLabel)
		{
			return c => topLevel (c).Children (false).Index (0).Property ("Label", solutionLabel).Children (false).Property ("Label", projectLabel).Index (0);
		}

		public static bool Select (params string[] selectionTree)
		{
			string.Join (" > ", selectionTree).PrintData ();
			Func<AppQuery, AppQuery> query = GetNodeQuery (selectionTree);
			return Session.SelectElement (GetNodeQuery (selectionTree)) && Session.WaitForElement (c => query (c).Selected ()).Any ();
		}

		public static Func<AppQuery, AppQuery> GetNodeQuery (params string[] selectionTree)
		{
			var funcs = new List<Func<AppQuery, AppQuery>> ();
			funcs.Add (topLevel);
			foreach (var nodeName in selectionTree) {
				var lastFunc = funcs.Last ();
				funcs.Add (c => lastFunc (c).Children (false).Property ("Label", nodeName).Index (0));
			}
			return funcs.Last ();
		}

		public static bool SelectSolution (string solutionName, UITestBase testContext = null)
		{
			LogReproSteps (testContext, string.Format ("Under Solution Explorer, select Solution '{0}'", solutionName.StripBold ()));
			return Select (solutionName);
		}

		public static bool SelectProject (string solutionName, string projectName, UITestBase testContext = null)
		{
			LogReproSteps (testContext, string.Format ("Under Solution Explorer, select Project '{0}' under '{1}'", projectName.StripBold (), solutionName.StripBold ()));
			return Select (solutionName, projectName);
		}

		public static bool SelectReferenceFolder (string solutionName, string projectName, UITestBase testContext = null)
		{
			LogReproSteps (testContext, string.Format ("Under Solution Explorer, expand References node under '{0}'> '{1}'", projectName.StripBold (), solutionName.StripBold ()));
			return Select (solutionName, projectName, "References");
		}

		public static bool SelectSingleReference (string solutionName, string projectName, string referenceName, bool fromPackage = false, UITestBase testContext = null)
		{
			LogReproSteps (testContext, string.Format ("Under Solution Explorer, select NuGet package '{0}' under '{1}' > '{2}' > From Packages",
				referenceName, projectName.StripBold (), solutionName.StripBold ()));
			return fromPackage ? Select (solutionName, projectName, "From Packages", referenceName) : Select (solutionName, projectName, referenceName);
		}

		public static bool SelectPackage (string solutionName, string projectName, string package, UITestBase testContext = null)
		{
			LogReproSteps (testContext, string.Format ("Under Solution Explorer, select package '{0}' under '{1}' > '{2}' > 'Packages'", package, projectName.StripBold (), solutionName.StripBold ()));
			return Select (solutionName, projectName, "Packages", package);
		}

		static void LogReproSteps (UITestBase testContext, string message, params object[] info)
		{
			if (testContext != null) {
				testContext.ReproStep (message, info);
			}
		}
	}
}

