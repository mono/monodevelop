//
// PackageManagementCanReferenceProjectExtensionTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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

using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	class PackageManagementCanReferenceProjectExtensionTests : RestoreTestBase
	{
		/// <summary>
		/// Ensures that all frameworks are considered when referencing a multi-target project or a
		/// multi-target project references another project.
		/// </summary>
		[Test]
		public async Task MultiTargetProjects ()
		{
			string solutionFileName = Util.GetSampleProject ("CanReferenceProjectTests", "CanReferenceProjectTests.sln");
			using (solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName)) {
				CreateNuGetConfigFile (solution.BaseDirectory);

				await RestoreNuGetPackages (solution);

				var netstandard11 = (DotNetProject)solution.FindProjectByName ("NetStandard11");
				var netcore21net472 = (DotNetProject)solution.FindProjectByName ("NetCore21Net472");
				var net472 = (DotNetProject)solution.FindProjectByName ("Net472");
				var net46netstandard10 = (DotNetProject)solution.FindProjectByName ("Net46NetStandard10");

				// Multi-target frameworks included in reason why project cannot be referenced
				string reason = null;
				bool result = netstandard11.CanReferenceProject (netcore21net472, out reason);
				Assert.IsFalse (result);
				Assert.AreEqual ("Incompatible target frameworks: .NETCoreApp,Version=v2.1, .NETFramework,Version=v4.7.2", reason);

				// netstandard11 cannot reference net472
				result = netstandard11.CanReferenceProject (net472, out reason);
				Assert.IsFalse (result);
				Assert.AreEqual ("Incompatible target framework: .NETFramework,Version=v4.7.2", reason);

				// .NET 4.7.2 can reference a multi-target project that includes .NET 4.7.2
				result = net472.CanReferenceProject (netcore21net472, out reason);
				Assert.IsTrue (result);

				// netcoreapp2.1 - net472 multi-target project can reference a net472 project.
				result = netcore21net472.CanReferenceProject (net472, out reason);
				Assert.IsTrue (result);

				// netcoreapp2.1 - net472 can reference net46 - netstandard10 multi-target project.
				result = netcore21net472.CanReferenceProject (net46netstandard10, out reason);
				Assert.IsTrue (result);

				// net46 - netstandard10 cannot reference netcoreapp2.1 - net472 multi-target project.
				result = net46netstandard10.CanReferenceProject (netcore21net472, out reason);
				Assert.IsFalse (result);
				Assert.AreEqual ("Incompatible target frameworks: .NETCoreApp,Version=v2.1, .NETFramework,Version=v4.7.2", reason);
			}
		}
	}
}
