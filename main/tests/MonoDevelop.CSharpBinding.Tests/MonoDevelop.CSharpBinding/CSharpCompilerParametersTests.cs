//
// CSharpCompilerParametersTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2020 Microsoft Corporation
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

using MonoDevelop.CSharp.Project;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class CSharpCompilerParametersTests : TestBase
	{
		/// <summary>
		/// Ensure OutputType has a default value that allows the compilation options to be created when the
		/// project is not loaded from a file.
		/// </summary>
		[Test]
		public void OutputType_NewProjectNotLoadedFromDisk_CompilationOptionsCanBeCreated ()
		{
			using (var solution = new Solution ()) {
				var project = Services.ProjectService.CreateDotNetProject ("C#");
				solution.RootFolder.AddItem (project);
				project.CompileTarget = CompileTarget.Library;
				var config = project.CreateConfiguration ("Debug", ConfigurationKind.Debug) as DotNetProjectConfiguration;
				project.Configurations.Add (config);
				var parameters = config.CompilationParameters as CSharpCompilerParameters;

				var options = parameters.CreateCompilationOptions ();

				Assert.AreEqual (Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary, options.OutputKind);

				// Change project's compile target to Exe.
				project.CompileTarget = CompileTarget.Exe;
				options = parameters.CreateCompilationOptions ();

				Assert.AreEqual (Microsoft.CodeAnalysis.OutputKind.ConsoleApplication, options.OutputKind);
			}
		}
	}
}
