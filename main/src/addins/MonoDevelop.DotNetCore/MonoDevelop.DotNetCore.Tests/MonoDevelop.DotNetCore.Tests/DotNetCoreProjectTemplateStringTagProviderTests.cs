//
// DotNetCoreProjectTemplateStringTagProviderTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Collections.Generic;
using System.IO;
using MonoDevelop.DotNetCore.Templating;
using NUnit.Framework;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreProjectTemplateStringTagProviderTests : DotNetCoreVersionsRestorerTestBase
	{
		DotNetCoreProjectTemplateStringTagProvider provider;
		HashSet<string> directories;
		List<string> templateFiles;
		string sdkRootPath;

		[SetUp]
		public void Init ()
		{
			directories = new HashSet<string> ();
			templateFiles = new List<string> ();

			sdkRootPath = ToNativePath ("/usr/local/share/dotnet/sdk");
			DotNetCoreSdk.SetSdkRootPath (sdkRootPath);

			provider = new DotNetCoreProjectTemplateStringTagProvider ();
			provider.DirectoryExists = DirectoryExists;
			provider.EnumerateFiles = EnumerateFiles;
		}

		bool DirectoryExists (string directory)
		{
			return directories.Contains (directory);
		}

		IEnumerable<string> EnumerateFiles (string directory)
		{
			return templateFiles;
		}

		static string ToNativePath (string filePath)
		{
			if (Path.DirectorySeparatorChar == '\\')
				return filePath;

			if (filePath.Contains (":")) {
				filePath = filePath.Replace (":", "_drive");
				filePath = "/" + filePath;
			}

			return filePath.Replace ('\\', Path.DirectorySeparatorChar);
		}

		string GetTagValue (string tag)
		{
			return provider.GetTagValue (null, tag) as string;
		}

		string GetDotNetCoreSdk21CommonProjectTemplatesTagValue ()
		{
			return GetTagValue ("DotNetCoreSdk.2.1.Templates.Common.ProjectTemplates.nupkg");
		}

		string GetDotNetCoreSdk21TestProjectTemplatesTagValue ()
		{
			return GetTagValue ("DotNetCoreSdk.2.1.Templates.Test.ProjectTemplates.nupkg");
		}

		string GetDotNetCoreSdk21WebProjectTemplatesTagValue ()
		{
			return GetTagValue ("DotNetCoreSdk.2.1.Templates.Web.ProjectTemplates.nupkg");
		}

		void AddProjectTemplateFile (string sdkVersion, params string[] fileNames)
		{
			foreach (string fileName in fileNames) {
				string directory = Path.Combine (sdkRootPath, sdkVersion, "Templates");
				directories.Add (directory);

				string fullPath = Path.Combine (directory, fileName);
				templateFiles.Add (fullPath);
			}
		}

		[Test]
		public void DotNetCoreNotInstalled_UnknownTag ()
		{
			DotNetCoreSdksNotInstalled ();
			string result = GetTagValue ("Unknown");

			Assert.AreEqual (string.Empty, result);
		}

		[Test]
		public void DotNetCoreNotInstalled_CommonProjectTemplates ()
		{
			DotNetCoreSdksNotInstalled ();
			string result = GetDotNetCoreSdk21CommonProjectTemplatesTagValue ();

			Assert.AreEqual (string.Empty, result);
		}

		[Test]
		public void NetCore21Installed_NoTemplatesDirectory_CommonProjectTemplates ()
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174");

			string result = GetDotNetCoreSdk21CommonProjectTemplatesTagValue ();

			Assert.AreEqual (string.Empty, result);
		}

		[Test]
		public void NetCore21Installed_CommonProjectTemplates ()
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174");
			AddProjectTemplateFile (
				"2.1.300-preview1-008174",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg",
				"microsoft.dotnet.test.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg",
				"microsoft.dotnet.web.projecttemplates.2.1.2.1.0-preview1-final.nupkg");
			string expectedResult = Path.Combine (
				sdkRootPath,
				"2.1.300-preview1-008174",
				"Templates",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg");

			string result = GetDotNetCoreSdk21CommonProjectTemplatesTagValue ();

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void NetCore21Installed_TestProjectTemplates ()
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174");
			AddProjectTemplateFile (
				"2.1.300-preview1-008174",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg",
				"microsoft.dotnet.test.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg",
				"microsoft.dotnet.web.projecttemplates.2.1.2.1.0-preview1-final.nupkg");
			string expectedResult = Path.Combine (
				sdkRootPath,
				"2.1.300-preview1-008174",
				"Templates",
				"microsoft.dotnet.test.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg");

			string result = GetDotNetCoreSdk21TestProjectTemplatesTagValue ();

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void NetCore21Installed_WebProjectTemplates ()
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174");
			AddProjectTemplateFile (
				"2.1.300-preview1-008174",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg",
				"microsoft.dotnet.test.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg",
				"microsoft.dotnet.web.projecttemplates.2.1.2.1.0-preview1-final.nupkg");
			string expectedResult = Path.Combine (
				sdkRootPath,
				"2.1.300-preview1-008174",
				"Templates",
				"microsoft.dotnet.web.projecttemplates.2.1.2.1.0-preview1-final.nupkg");

			string result = GetDotNetCoreSdk21WebProjectTemplatesTagValue ();

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void NetCore21Installed_MatchingFileWithDifferentExtension_CommonProjectTemplates ()
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174");
			AddProjectTemplateFile (
				"2.1.300-preview1-008174",
				"microsoft.dotnet.common.projecttemplates.2.0.1.0.0-beta3-20171110-312.aaa",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg");
			string expectedResult = Path.Combine (
				sdkRootPath,
				"2.1.300-preview1-008174",
				"Templates",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg");

			string result = GetDotNetCoreSdk21CommonProjectTemplatesTagValue ();

			Assert.AreEqual (expectedResult, result);
		}

		[TestCase ("DotNetCoreSdk.2.1.Templates.Common.ProjectTemplates.txt")]
		[TestCase ("DotNetCoreSdk.2.1.Common.ProjectTemplates.nupkg")]
		[TestCase ("DotNetCoreSdk.2.0.Templates.Common.ProjectTemplates.nupkg")]
		public void NetCore21Installed_TemplatesAvailable_InvalidTag (string tag)
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174");
			AddProjectTemplateFile (
				"2.1.300-preview1-008174",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg");

			string result = GetTagValue (tag);

			Assert.AreEqual (string.Empty, result);
		}

		/// <summary>
		/// .NET Core 2.0 SDK is not supported.
		/// </summary>
		[TestCase ("DotNetCoreSdk.2.0.Templates.Common.ProjectTemplates.nupkg")]
		public void NetCore21AndNetCore20Installed_TemplatesAvailable_InvalidTag (string tag)
		{
			DotNetCoreSdksInstalled ("2.1.300-preview1-008174", "2.0.0");
			AddProjectTemplateFile (
				"2.1.300-preview1-008174",
				"microsoft.dotnet.common.projecttemplates.2.1.1.0.1-beta3-20180215-1392068.nupkg");
			AddProjectTemplateFile (
				"2.0.0",
				"microsoft.dotnet.common.projecttemplates.2.0.1.0.0-beta3-20171110-312.nupkg");

			string result = GetTagValue (tag);

			Assert.AreEqual (string.Empty, result);
		}

		/// <summary>
		/// .NET Core 2.1.4 SDK is not supported.
		/// </summary>
		[Test]
		public void NetCore214_TemplatesAvailable_NotSupported ()
		{
			DotNetCoreSdksInstalled ("2.1.4");
			AddProjectTemplateFile (
				"2.1.4",
				"microsoft.dotnet.common.projecttemplates.2.0.1.0.0-beta3-20171110-312.nupkg");
			string result = GetDotNetCoreSdk21CommonProjectTemplatesTagValue ();

			Assert.AreEqual (string.Empty, result);
		}
	}
}
