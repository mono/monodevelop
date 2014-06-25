//
// DotNetTestProvider.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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

using System;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.NUnit
{
	public class DotNetTestProvider: ExtensibleTestProvider<DotNetProject>
	{
		protected override UnitTest CreateUnitTest (DotNetProject project, string discovererId,
			ITestDiscoverer discoverer, ITestExecutionDispatcher dispatcher)
		{
			return new DotNetProjectTestSuite (project, discovererId, discoverer, dispatcher);
		}
	}

	public class DotNetProjectTestSuite: UnitTestTree
	{
		public DotNetProjectTestSuite (DotNetProject owner, string tag, ITestDiscoverer discoverer,
			ITestExecutionDispatcher dispatcher) : base (owner, tag, discoverer, dispatcher)
		{
			owner.NameChanged += new SolutionItemRenamedEventHandler (OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild += new BuildEventHandler (OnProjectBuilt);
		}

		void OnProjectRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			UnitTestGroup parent = Parent as UnitTestGroup;
			if (parent != null)
				parent.UpdateTests ();
		}

		void OnProjectBuilt (object sender, BuildEventArgs args)
		{
			foreach (var test in Tests) {
				test.Dispose ();
			}
			Tests.Clear ();
			OnCreateTests ();
			OnTestChanged ();
		}
	}
}

