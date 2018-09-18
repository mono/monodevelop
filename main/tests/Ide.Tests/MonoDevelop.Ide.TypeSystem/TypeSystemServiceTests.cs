//
// TypeSystemServiceTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class TypeSystemServiceTests : IdeTestBase
	{
		[Test]
		public async Task SymlinkedFilesHaveProperdocumentRegistration ()
		{
			if (Platform.IsWindows)
				Assert.Ignore ("Symlinks not supported on Windows");

			FilePath solFile = Util.GetSampleProjectPath ("symlinked-source-file", "test-symlinked-file", "test-symlinked-file.sln");

			var solutionDirectory = Path.GetDirectoryName (solFile);
			var dataFile = Path.Combine (solutionDirectory, "SymlinkedFileData.txt");
			var data = File.ReadAllLines (dataFile);
			var symlinkFileName = Path.Combine (solutionDirectory, data [0]);
			var symlinkFileSource = Path.GetFullPath (Path.Combine (solutionDirectory, data [1]));

			File.Delete (symlinkFileName);
			Process.Start ("ln", $"-s '{symlinkFileSource}' '{symlinkFileName}'").WaitForExit ();

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				var project = sol.GetAllProjects ().Single ();

				foreach (var file in project.Files) {
					Assert.IsNotNull (TypeSystemService.GetDocumentId (project, file.FilePath.ResolveLinks ()));
					if (file.FilePath.FileName.EndsWith ("SymlinkedFile.cs", StringComparison.Ordinal))
						Assert.IsNull (TypeSystemService.GetDocumentId (project, file.FilePath));
				}
			}
		}
	}
}
