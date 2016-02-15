//
// SystemTestProvider.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.UnitTesting.NUnit
{
	public class SystemTestProvider: ITestProvider
	{
		public SystemTestProvider ()
		{
			IdeApp.Workspace.ReferenceAddedToProject += OnReferenceChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferenceChanged;
		}

		public UnitTest CreateUnitTest (WorkspaceObject entry)
		{
			UnitTest test = null;
			
			if (entry is DotNetProject)
				test = NUnitProjectTestSuite.CreateTest ((DotNetProject)entry);
			
			UnitTestGroup grp = test as UnitTestGroup;
			if (grp != null && !grp.HasTests) {
				test.Dispose ();
				return null;
			}
			
			return test;
		}

		void OnReferenceChanged (object s, ProjectReferenceEventArgs args)
		{
			if (NUnitProjectTestSuite.IsNUnitReference (args.ProjectReference))
				UnitTestService.ReloadTests ();
		}

		public void Dispose ()
		{
			IdeApp.Workspace.ReferenceAddedToProject -= OnReferenceChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject -= OnReferenceChanged;
		}
	}
}

