//
// AspNetCoreProjectTests.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 
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
using System.Threading.Tasks;
using NUnit.Framework;
using MonoDevelop.Projects;
using UnitTests;

namespace MonoDevelop.AspNetCore.Tests
{
	[TestFixture]
	class AspNetCoreProjectTests : TestBase
	{
		Solution solution;

		[TearDown]
		public override void TearDown ()
		{
			solution?.Dispose ();
			solution = null;

			base.TearDown ();
		}

		[Test]
		public async Task RazorClassLib_Load_LoadsProject ()
		{
			// Just test, for now, that we can load a Razor Class Lib project
			string projectFileName = Util.GetSampleProject ("aspnetcore-razor-class-lib", "aspnetcore-razor-class-lib.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName)) {
				Assert.NotNull (project.Items.Single (item => item.Include == "Areas\\MyFeature\\Pages\\Page1.cshtml.cs"));
				Assert.NotNull (project.Items.Single (item => item.Include == "Areas\\MyFeature\\Pages\\Page1.cshtml"));
			}
		}
	}
}
