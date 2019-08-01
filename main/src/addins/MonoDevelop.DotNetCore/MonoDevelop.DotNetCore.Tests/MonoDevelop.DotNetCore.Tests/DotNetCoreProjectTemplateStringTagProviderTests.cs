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
using System.Linq;
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
			provider.EnumerateDirectories = EnumerateDirectories;
		}

		bool DirectoryExists (string directory)
		{
			return directories.Contains (directory);
		}

		IEnumerable<string> EnumerateFiles (string directory)
		{
			return templateFiles;
		}

		IEnumerable<string> EnumerateDirectories (string directory)
		{
			return directories.Where (dir => Directory.GetParent (dir).FullName == directory);
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

		protected void DotNetCoreSdksInstalled (string sdkVersion, string templatesVersion, bool globalTemplates)
		{
			DotNetCoreSdksInstalled (sdkVersion);
			if (globalTemplates) {
				AddGlobalProjectTemplateFile (
					templatesVersion,
					$"microsoft.dotnet.common.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.common.projecttemplates.{templatesVersion}.nupkg",
					$"microsoft.dotnet.test.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.test.projecttemplates.{templatesVersion}.nupkg",
					$"microsoft.dotnet.web.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.web.projecttemplates.{templatesVersion}.nupkg",
					$"microsoft.dotnet.web.spa.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.web.spa.projecttemplates.{templatesVersion}.nupkg");
			} else {
				AddProjectTemplateFile (
					sdkVersion,
					$"microsoft.dotnet.common.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.common.projecttemplates.{templatesVersion}.nupkg",
					$"microsoft.dotnet.test.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.test.projecttemplates.{templatesVersion}.nupkg",
					$"microsoft.dotnet.web.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.web.projecttemplates.{templatesVersion}.nupkg",
					$"microsoft.dotnet.web.spa.projecttemplates.{templatesVersion}.aaa",
					$"microsoft.dotnet.web.spa.projecttemplates.{templatesVersion}.nupkg");
			}
		}

		string GetTagValue (string tag)
		{
			return provider.GetTagValue (null, tag) as string;
		}

		string GetDotNetCoreSdkCommonProjectTemplatesTagValue (string version)
		{
			return GetTagValue ($"DotNetCoreSdk.{version}.Templates.Common.ProjectTemplates.nupkg");
		}

		string GetDotNetCoreSdkTestProjectTemplatesTagValue (string version)
		{
			return GetTagValue ($"DotNetCoreSdk.{version}.Templates.Test.ProjectTemplates.nupkg");
		}

		string GetDotNetCoreSdkWebProjectTemplatesTagValue (string version)
		{
			return GetTagValue ($"DotNetCoreSdk.{version}.Templates.Web.ProjectTemplates.nupkg");
		}

		string GetDotNetCoreSdkSpaWebProjectTemplatesTagValue (string version)
		{
			return GetTagValue ($"DotNetCoreSdk.{version}.Templates.Web.Spa.ProjectTemplates.nupkg");
		}

		string GetExpectedTemplateTagValue (string sdkVersion, string templatesVersion, string filePrefix, bool globalTemplates)
		{
			if (globalTemplates) {
				return Path.Combine (
					Directory.GetParent (sdkRootPath).FullName,
					"templates",
					templatesVersion,
					$"{filePrefix}.{templatesVersion}.nupkg");
			} else {
				return Path.Combine (
					sdkRootPath,
					sdkVersion,
					"Templates",
					$"{filePrefix}.{templatesVersion}.nupkg");
			}
		}

		void AddGlobalProjectTemplateFile (string sdkVersion, params string [] fileNames)
		{
			string directory = Path.Combine (Directory.GetParent (sdkRootPath).FullName, "templates");
			directories.Add (directory);
			directory = Path.Combine (directory, sdkVersion);
			directories.Add (directory);
			foreach (string fileName in fileNames) {
				string fullPath = Path.Combine (directory, fileName);
				templateFiles.Add (fullPath);
			}
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

			Assert.AreEqual (string.Empty, GetDotNetCoreSdkCommonProjectTemplatesTagValue ("2.1"));
			Assert.AreEqual (string.Empty, GetDotNetCoreSdkCommonProjectTemplatesTagValue ("2.2"));
			Assert.AreEqual (string.Empty, GetDotNetCoreSdkCommonProjectTemplatesTagValue ("3.0"));
		}

		[TestCase ("2.1", "2.1.300-preview1-008174")]
		[TestCase ("2.2", "2.2.100")]
		public void NetCoreInstalled_NoTemplatesDirectory_CommonProjectTemplates (string sdkVersion, string installedVersion)
		{
			DotNetCoreSdksInstalled (installedVersion);

			string result = GetDotNetCoreSdkCommonProjectTemplatesTagValue (sdkVersion);

			Assert.AreEqual (string.Empty, result);
		}

		[TestCase ("3.0.100-preview8-013592", "3.0.0-preview8-013592", true)]
		[TestCase ("3.0.100-preview7-012821", "3.0.2.0.0-preview7.19365.3", false)]
		[TestCase ("2.2.100-preview2-009404", "2.2.1.0.2-beta4-20180904-2003790", false)]
		[TestCase ("2.1.701", "2.1.1.0.2-beta3", false)]
		[TestCase ("2.1.300-preview1-008174", "2.1.1.0.1-beta3-20180215-1392068", false)]
		public void NetCoreInstalled_CommonProjectTemplates (string sdkVersion, string templatesVersion, bool globalTemplates)
		{
			DotNetCoreSdksInstalled (sdkVersion, templatesVersion, globalTemplates);
			Assert.AreEqual (
				GetExpectedTemplateTagValue (sdkVersion, templatesVersion, "microsoft.dotnet.common.projecttemplates", globalTemplates),
				GetDotNetCoreSdkCommonProjectTemplatesTagValue (sdkVersion.Substring (0, 3)));
		}

		[TestCase ("3.0.100-preview8-013592", "3.0.0-preview8-013592", true)]
		[TestCase ("3.0.100-preview7-012821", "3.0.1.0.2-beta4.19155.2", false)]
		[TestCase ("2.2.100-preview2-009404", "2.2.1.0.2-beta4-20180904-2003790", false)]
		[TestCase ("2.1.701", "2.1.1.0.2-beta4-20181009-2100240", false)]
		[TestCase ("2.1.300-preview1-008174", "2.1.1.0.1-beta3-20180215-1392068", false)]
		public void NetCoreInstalled_TestProjectTemplates (string sdkVersion, string templatesVersion, bool globalTemplates)
		{
			DotNetCoreSdksInstalled (sdkVersion, templatesVersion, globalTemplates);
			Assert.AreEqual (
				GetExpectedTemplateTagValue (sdkVersion, templatesVersion, "microsoft.dotnet.test.projecttemplates", globalTemplates),
				GetDotNetCoreSdkTestProjectTemplatesTagValue (sdkVersion.Substring (0, 3)));
		}

		[TestCase ("3.0.100-preview8-013592", "3.0.0-preview8-013592", true)]
		[TestCase ("3.0.100-preview7-012821", "3.0.0-preview7.19365.7", false)]
		[TestCase ("2.2.100-preview2-009404", "2.2.2.2.0-preview2-35157", false)]
		[TestCase ("2.1.701", "2.1.2.1.12", false)]
		[TestCase ("2.1.300-preview1-008174", "2.1.2.1.0-preview1-final", false)]
		public void NetCoreInstalled_WebProjectTemplates (string sdkVersion, string templatesVersion, bool globalTemplates)
		{
			DotNetCoreSdksInstalled (sdkVersion, templatesVersion, globalTemplates);
			Assert.AreEqual (
				GetExpectedTemplateTagValue (sdkVersion, templatesVersion, "microsoft.dotnet.web.projecttemplates", globalTemplates),
				GetDotNetCoreSdkWebProjectTemplatesTagValue (sdkVersion.Substring (0, 3)));
		}

		[TestCase ("3.0.100-preview8-013592", "3.0.0-preview8-013592", true)]
		[TestCase ("3.0.100-preview7-012821", "3.0.0-preview7.19365.7", false)]
		[TestCase ("2.2.100-preview2-009404", "2.2.2.2.0-preview2-35157", false)]
		[TestCase ("2.1.701", "2.1.12", false)]
		[TestCase ("2.1.300-preview1-008174", "2.1.2.1.0-preview1-final", false)]
		public void NetCoreInstalled_SpaWebProjectTemplates (string sdkVersion, string templatesVersion, bool globalTemplates)
		{
			DotNetCoreSdksInstalled (sdkVersion, templatesVersion, globalTemplates);
			Assert.AreEqual (
				GetExpectedTemplateTagValue (sdkVersion, templatesVersion, "microsoft.dotnet.web.spa.projecttemplates", globalTemplates),
				GetDotNetCoreSdkSpaWebProjectTemplatesTagValue (sdkVersion.Substring (0, 3)));
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
			string result = GetDotNetCoreSdkCommonProjectTemplatesTagValue ("2.1");

			Assert.AreEqual (string.Empty, result);
		}
	}
}
