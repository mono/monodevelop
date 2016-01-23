//
// MonodevelopWorkspaceTests.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class MonodevelopWorkspaceTests : TestBase
	{
		[Test]
		public async Task TestLoadingSolution()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			var sol = (Projects.Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			await TypeSystemServiceTestExtensions.LoadSolution (sol);
			var compilation = await TypeSystemService.GetCompilationAsync (sol.GetAllProjects ().First());
			var programType = compilation.GetTypeByMetadataName ("ConsoleProject.Program");
			Assert.IsNotNull (programType);
		}

		[Test]
		public async Task TestLoadingSolutionWithUnsupportedType()
		{
			string solFile = Util.GetSampleProject ("unsupported-project-roundtrip", "TestApp.WinPhone.sln");
			var sol = (Projects.Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			await TypeSystemServiceTestExtensions.LoadSolution (sol);
		}
	}
}

