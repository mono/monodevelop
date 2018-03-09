//
// DotNetCoreProjectTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Projects
{
	[TestFixture]
	public class DotNetCoreProjectTests : TestBase
	{
		[Test]
		public async Task MoveResourceFilesToFolder ()
		{
			FilePath projFile = Util.GetSampleProject ("DotNetCoreResources", "NetStandardProject", "NetStandardProject.csproj");

			using (var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				var resxFile = p.Files.Single (f => f.FilePath.FileName == "Resources.resx");

				var newFolder = p.BaseDirectory.Combine ("NewFolder");
				Directory.CreateDirectory (newFolder);

				var resxFileTarget = newFolder.Combine ("Resources.resx");

				bool move = true;
				ProjectOperations.TransferFilesInternal (Util.GetMonitor (), p, resxFile.FilePath, p, resxFileTarget, move, true);

				string expectedProjectXml = File.ReadAllText (p.FileName.ChangeName ("NetStandardProject-saved"));
				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (expectedProjectXml, projectXml);
			}
		}
	}
}
