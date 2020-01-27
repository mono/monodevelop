//
// PublishToFolderTests.cs
//
// Author:
//       Oleksii Sachek <v-olsach@microsoft.com>
//
// Copyright (c) 2020
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

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Tests
{
	[TestFixture]
	public class PublishToFolderTests : TestBase
	{
		PublishToFolderCommandHandler publishToFolderCommandHandler;

		[SetUp]
		public async Task SetUp ()
		{
			publishToFolderCommandHandler = new PublishToFolderCommandHandler ();
			await Simulate ();
		}

		[Test]
		[TestCase ("aspnetcore-empty-22", "aspnetcore-empty-22.sln", "publish --output bin/Release/netcoreapp/publish")]
		public async Task PublishToFolder (string projectName, string projectItem, string publishArgs)
		{
			var projectFileName = Util.GetSampleProject (projectName, projectItem);
			using var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);

			int exitCode = await publishToFolderCommandHandler.RunPublishCommand (publishArgs, project.BaseDirectory, null, CancellationToken.None);

			Assert.AreEqual (0, exitCode, "Publish to Folder command exit code must be 0");
		}
	}
}
