//
// WebProjectTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide;
using NUnit.Framework;
using UnitTests;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class WebProjectTests : TestBase
	{
		/// <summary>
		/// Package Management addin depends on the project type guids in the DotNetProject
		/// so it can detect if a project is a web project or not.
		/// </summary>
		[Test]
		public async Task LoadedWebProjectContainsWebProjectTypeGuid ()
		{
			string solutionFileName = Util.GetSampleProject ("WebProjectTest", "WebProjectTest.sln");
			var solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			Project project = solution.GetAllProjects ().First ();

			Assert.That (project.FlavorGuids, Contains.Item ("{349C5851-65DF-11DA-9384-00065B846F21}"));

			solution.Dispose ();
		}
	}
}

