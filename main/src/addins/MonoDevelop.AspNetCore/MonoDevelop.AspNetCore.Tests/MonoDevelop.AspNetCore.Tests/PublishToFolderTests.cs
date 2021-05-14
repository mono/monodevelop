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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdeUnitTests;
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
		}

		[Test]
		// .NET Core 2.2, regular
		[TestCase ("aspnetcore-empty-22", "publish --output bin/Release/netcoreapp/publish")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Debug --output bin/debug-22")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --output bin/release-22")]
		// .NET Core 2.2, self-contained
		// OSX
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime osx-x64 --output bin/release-22-self-contained-osx")]
		// Windows
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime win-x64 --output bin/release-22-self-contained-win-x64")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime win-x86 --output bin/release-22-self-contained-win-x86")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime win-arm --output bin/release-22-self-contained-win-arm")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime win-arm64 --output bin/release-22self-contained-win-arm64")]
		// Linux
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime linux-x64 --output bin/release-22-self-contained-linux-x64")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime linux-musl-x64 --output bin/release-22-self-contained-linux-musl-x64")]
		[TestCase ("aspnetcore-empty-22", "publish --configuration Release --self-contained --runtime linux-arm --output bin/release-22-self-contained-linux-arm")]
		// .NET Core 3.0, regular
		[TestCase ("aspnetcore-empty-30", "publish --output bin/Release/netcoreapp/publish")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Debug --output bin/debug-22")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --output bin/release-22")]
		// .NET Core 3.0, self-contained
		// OSX
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime osx-x64 --output bin/release-22-self-contained-osx")]
		// Windows
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime win-x64 --output bin/release-22-self-contained-win-x64")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime win-x86 --output bin/release-22-self-contained-win-x86")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime win-arm --output bin/release-22-self-contained-win-arm")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime win-arm64 --output bin/release-22self-contained-win-arm64")]
		// Linux
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime linux-x64 --output bin/release-22-self-contained-linux-x64")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime linux-musl-x64 --output bin/release-22-self-contained-linux-musl-x64")]
		[TestCase ("aspnetcore-empty-30", "publish --configuration Release --self-contained --runtime linux-arm --output bin/release-22-self-contained-linux-arm")]
		public async Task PublishToFolder (string solutionName, string publishArgs)
		{
			var solutionFileName = Util.GetSampleProject (solutionName, $"{solutionName}.sln");
			var solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = (DotNetProject)solution.GetAllProjects ().Single ();
			var operationConsole = new MockOperationConsole ();

			int exitCode = await publishToFolderCommandHandler.RunPublishCommand (publishArgs, project.BaseDirectory, operationConsole, CancellationToken.None);

			Assert.AreEqual (0, exitCode, "Publish to Folder command exit code must be 0");
		}
	}
}
