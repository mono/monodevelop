//
// DotNetCoreMSBuildProjectTests.cs
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

using System.Linq;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreMSBuildProjectTests
	{
		DotNetCoreMSBuildProject project;
		MSBuildProject msbuildProject;

		void CreateMSBuildProject (string xml, string fileName = @"MyProject.csproj")
		{
			msbuildProject = new MSBuildProject ();
			msbuildProject.FileName = fileName;
			msbuildProject.LoadXml (xml);

			project = new DotNetCoreMSBuildProject ();
		}

		void AddGlobalPropertyToMSBuildProject (string name, string value, string defaultValue = null)
		{
			msbuildProject
				.GetGlobalPropertyGroup ()
				.SetValue (name, value, defaultValue);
		}

		string GetPropertyValueFromMSBuildProject (string name)
		{
			return msbuildProject
				.GetGlobalPropertyGroup ()
				.GetValue (name);
		}

		bool MSBuildProjectHasGlobalProperty (string name)
		{
			return msbuildProject
				.GetGlobalPropertyGroup ()
				.HasProperty (name);
		}

		void ReadProject (string frameworkMoniker = ".NETCoreApp,Version=v1.0")
		{
			var moniker = TargetFrameworkMoniker.Parse (frameworkMoniker);
			project.ReadProjectHeader (msbuildProject);
			project.ReadProject (msbuildProject, moniker);
		}

		void WriteProject (string frameworkMoniker = ".NETCoreApp,Version=v1.0")
		{
			var moniker = TargetFrameworkMoniker.Parse (frameworkMoniker);
			project.WriteProject (msbuildProject, moniker);
		}

		[Test]
		public void ReadProject_ToolsVersionDefined ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"</Project>");

			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";

			Assert.AreEqual ("15.0", project.ToolsVersion);
			Assert.IsFalse (project.IsOutputTypeDefined);
			Assert.AreEqual ("Microsoft.NET.Sdk", project.Sdk);
			Assert.IsTrue (project.HasSdk);
		}

		[Test]
		public void ReadProject_OutputTypeDefined ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();

			ReadProject ();

			Assert.IsTrue (project.IsOutputTypeDefined);
			Assert.AreEqual ("netcoreapp1.0", project.TargetFrameworks.Single ());
		}

		[Test]
		public void ReadProject_ExplicityReferences ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				" <Import Sdk=\"Microsoft.NET.Sdk\" Project=\"Sdk.targets\" />" +
				"</Project>");
			msbuildProject.Evaluate ();

			ReadProject ();

			Assert.That (msbuildProject.GetReferencedSDKs (), Is.Not.Empty);
		}

		[Test]
		public void WriteProject_ProjectGuidAddedAndToolsVersionChanged_ProjectGuidIsRemovedAndToolsVersionReset ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();

			msbuildProject.ToolsVersion = "4.0";
			AddGlobalPropertyToMSBuildProject ("ProjectGuid", "{111}");
			WriteProject ();

			Assert.AreEqual ("15.0", msbuildProject.ToolsVersion);
			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("ProjectGuid"));
		}

		[Test]
		public void WriteProject_OutputTypeLibraryIsAdded_OutputTypeIsRemoved ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();

			msbuildProject.ToolsVersion = "4.0";
			AddGlobalPropertyToMSBuildProject ("OutputType", "Library");
			WriteProject ();

			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("OutputType"));
		}

		[Test]
		public void WriteProject_OutputTypeLibraryIsDefinedWhenRead_OutputTypeIsNotRemoved ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();

			WriteProject ();

			Assert.IsTrue (MSBuildProjectHasGlobalProperty ("OutputType"));
		}

		[Test]
		public void WriteProject_DefaultTargetsAdded_DefaultTargetsIsSetToNull ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();
			msbuildProject.DefaultTargets = "Build";

			WriteProject ();

			Assert.IsNull (msbuildProject.DefaultTargets);
		}

		[Test]
		public void WriteProject_DescriptionAdded_RemovedOnWritingSinceDefaultIsUsed ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();
			AddGlobalPropertyToMSBuildProject ("Description", "Package Description", string.Empty);

			WriteProject ();

			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("Description"));
		}

		[Test]
		public void WriteProject_DescriptionInOriginalProjectFile_NotRemovedOnWriting ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"      <Description>Test</Description>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();

			WriteProject ();

			Assert.IsTrue (MSBuildProjectHasGlobalProperty ("Description"));
		}

		[Test]
		public void WriteProject_DescriptionNotInOriginalProjectFileAndNonDefaultValueUsed_NotRemovedOnWriting ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();
			AddGlobalPropertyToMSBuildProject ("Description", "Test", string.Empty);

			WriteProject ();

			Assert.AreEqual ("Test", GetPropertyValueFromMSBuildProject ("Description"));
		}

		[Test]
		public void WriteProject_TargetFrameworkInformationAdded_RemovedOnWriting ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();
			AddGlobalPropertyToMSBuildProject ("TargetFrameworkVersion", "1.0");
			AddGlobalPropertyToMSBuildProject ("TargetFrameworkIdentifier", ".NETCoreApp");

			WriteProject ();

			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("TargetFrameworkVersion"));
			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("TargetFrameworkIdentifier"));
		}

		[Test]
		public void WriteProject_AssemblyNameAndRootNamespaceAddedButSameAsProjectName_RemovedOnWriting ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>",
				"Test.csproj"
			);
			ReadProject ();
			AddGlobalPropertyToMSBuildProject ("AssemblyName", "Test");
			AddGlobalPropertyToMSBuildProject ("RootNamespace", "Test");

			WriteProject ();

			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("AssemblyName"));
			Assert.IsFalse (MSBuildProjectHasGlobalProperty ("RootNamespace"));
		}

		[Test]
		public void WriteProject_AssemblyNameAndRootNamespaceInOriginalProjectFile_NotRemovedOnWriting ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"      <AssemblyName>Test</AssemblyName>\r\n" +
				"      <RootNamespace>Test</RootNamespace>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();

			WriteProject ();

			Assert.IsTrue (MSBuildProjectHasGlobalProperty ("AssemblyName"));
			Assert.IsTrue (MSBuildProjectHasGlobalProperty ("RootNamespace"));
		}

		[Test]
		public void WriteProject_SdkProjectHasToolsVersionSetAfterReading_ToolsVersionRemovedOnWriting ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";
			msbuildProject.ToolsVersion = "4.0";

			WriteProject ();

			Assert.IsNull (msbuildProject.ToolsVersion);
		}

		[Test]
		public void WriteProject_NewProjectReferenceAddedWithNameAndProjectMetadata_ProjectReferenceSavedWithJustIncludeNotNameAndProject ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";
			var projectReferenceItem = msbuildProject.AddNewItem ("ProjectReference", @"Lib\Lib.csproj");
			projectReferenceItem.Metadata.SetValue ("Name", "Lib");
			projectReferenceItem.Metadata.SetValue ("Project", "{F109E7DF-F561-4CD6-A46E-CFB27A8B6F2C}");

			WriteProject ();

			var projectReferenceSaved = msbuildProject.GetAllItems ()
				.FirstOrDefault (item => item.Name == "ProjectReference");

			Assert.IsFalse (projectReferenceSaved.Metadata.HasProperty ("Name"));
			Assert.IsFalse (projectReferenceSaved.Metadata.HasProperty ("Project"));
			Assert.AreEqual (@"Lib\Lib.csproj", projectReferenceSaved.Include);
		}

		[Test]
		public void WriteProject_TargetFrameworkVersionChanged_TargetFrameworkUpdated ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";

			WriteProject (".NETCoreApp,Version=v1.1");

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				.GetValue ("TargetFramework");
			Assert.AreEqual ("netcoreapp1.1", savedFramework);
		}

		[Test]
		public void WriteProject_TargetFrameworkVersionChangedThenChangedBackAgain_OriginalTargetFrameworkUsedInProject ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";

			WriteProject (".NETCoreApp,Version=v1.1");
			WriteProject (".NETCoreApp,Version=v1.0");

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				.GetValue ("TargetFramework");
			Assert.AreEqual ("netcoreapp1.0", savedFramework);
		}

		[Test]
		public void WriteProject_NetStandardTargetFrameworkVersionChanged_TargetFrameworkUpdated ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netstandard1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject (".NET Standard,Version=v1.0");
			project.Sdk = "Microsoft.NET.Sdk";

			WriteProject (".NETStandard,Version=v1.6");

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				.GetValue ("TargetFramework");
			Assert.AreEqual ("netstandard1.6", savedFramework);
		}

		[Test]
		public void WriteProject_NetFrameworkVersionChanged_TargetFrameworkUpdated ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>net45</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";

			WriteProject (".NETFramework,Version=v4.6");

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				.GetValue ("TargetFramework");
			Assert.AreEqual ("net46", savedFramework);
		}

		[Test]
		public void WriteProject_ProjectDefinesMultipleTargetFrameworksAndTargetFrameworkVersionChanged_TargetFrameworksUpdated ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFrameworks>netcoreapp1.0;net45</TargetFrameworks>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject ();
			project.Sdk = "Microsoft.NET.Sdk";

			WriteProject (".NETCoreApp,Version=v1.1");

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				 .GetValue ("TargetFrameworks");
			Assert.AreEqual ("netcoreapp1.1;net45", savedFramework);
		}

		[Test]
		public void WriteProject_AssemblyNameAndRootNamespaceAddedDifferentToProjectName_AssemblyNameAndRootNamespaceSaved ()
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\" ToolsVersion=\"15.0\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"      <OutputType>Exe</OutputType>\r\n" +
				"      <TargetFramework>netcoreapp1.0</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>",
			"MyProject.csproj");
			ReadProject ();
			AddGlobalPropertyToMSBuildProject ("AssemblyName", "NewAssemblyName");
			AddGlobalPropertyToMSBuildProject ("RootNamespace", "NewRootNamespace");

			WriteProject ();

			Assert.AreEqual ("NewAssemblyName", GetPropertyValueFromMSBuildProject ("AssemblyName"));
			Assert.AreEqual ("NewRootNamespace", GetPropertyValueFromMSBuildProject ("RootNamespace"));
		}

		[TestCase ("netcoreapp1.0", ".NETCoreApp,Version=v1.0")]
		[TestCase ("netcoreapp10", ".NETCoreApp,Version=v1.0")]
		[TestCase ("NetCoreApp1.0", ".NETCoreApp,Version=v1.0")]
		[TestCase ("net461", ".NETFramework,Version=v4.6.1")]
		[TestCase ("net4.6.1", ".NETFramework,Version=v4.6.1")]
		[TestCase ("Net461", ".NETFramework,Version=v4.6.1")]
		[TestCase ("NET461", ".NETFramework,Version=v4.6.1")]
		[TestCase ("netstandard2.0", ".NETStandard,Version=v2.0")]
		[TestCase ("netstandard20", ".NETStandard,Version=v2.0")]
		[TestCase ("NetStandard2.0", ".NETStandard,Version=v2.0")]
		[TestCase ("tizen40", "Tizen,Version=v4.0")]
		[TestCase ("tizen4.0", "Tizen,Version=v4.0")]
		[TestCase ("Tizen4.0", "Tizen,Version=v4.0")]
		public void WriteProject_ProjectTargetFrameworkUnchanged_TargetFrameworkPropertyNotModified (
			string shortFrameworkName,
			string fullFrameworkName)
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>" + shortFrameworkName + "</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject (fullFrameworkName);

			WriteProject (fullFrameworkName);

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				.GetValue ("TargetFramework");
			Assert.AreEqual (shortFrameworkName, savedFramework);
		}

		[TestCase ("netcoreapp1.0", ".NETCoreApp,Version=v1.0", ".NETCoreApp,Version=v1.1", "netcoreapp1.1")]
		[TestCase ("netcoreapp10", ".NETCoreApp,Version=v1.0", ".NETCoreApp,Version=v1.1", "netcoreapp11")]
		[TestCase ("NetCoreApp1.0", ".NETCoreApp,Version=v1.0", ".NETCoreApp,Version=v1.1", "NetCoreApp1.1")]
		[TestCase ("net461", ".NETFramework,Version=v4.6.1", ".NETFramework,Version=v4.7.1", "net471")]
		[TestCase ("net4.6.1", ".NETFramework,Version=v4.6.1", ".NETFramework,Version=v4.7.1", "net4.7.1")]
		[TestCase ("Net461", ".NETFramework,Version=v4.6.1", ".NETFramework,Version=v4.7.1", "Net471")]
		[TestCase ("NET461", ".NETFramework,Version=v4.6.1", ".NETFramework,Version=v4.7.1", "NET471")]
		[TestCase ("netstandard2.0", ".NETStandard,Version=v2.0", ".NETStandard,Version=v1.1", "netstandard1.1")]
		[TestCase ("netstandard20", ".NETStandard,Version=v2.0", ".NETStandard,Version=v1.1", "netstandard11")]
		[TestCase ("NetStandard2.0", ".NETStandard,Version=v2.0", ".NETStandard,Version=v1.1", "NetStandard1.1")]
		[TestCase ("tizen40", "Tizen,Version=v4.0", "Tizen,Version=v4.1", "tizen41")]
		[TestCase ("tizen4.0", "Tizen,Version=v4.0", "Tizen,Version=v4.1", "tizen4.1")]
		[TestCase ("Tizen4.0", "Tizen,Version=v4.0", "Tizen,Version=v4.1", "Tizen4.1")]

		// Changing the target framework name should not happen in practice.
		[TestCase ("netcoreapp1.0", ".NETCoreApp,Version=v1.0", ".NETFramework,Version=v4.6.1", "net461")] // Use default dotted version format for .NETFramework
		[TestCase ("net461", ".NETFramework,Version=v4.6.1", ".NETCoreApp,Version=v1.1", "netcoreapp1.1")] // Use default dotted format for .NET Core.
		[TestCase ("netcoreapp1.0", ".NETCoreApp,Version=v1.0", ".NETStandard,Version=v1.1", "netstandard1.1")]
		[TestCase ("netcoreapp10", ".NETCoreApp,Version=v1.0", ".NETStandard,Version=v1.0", "netstandard1.0")]
		public void WriteProject_ProjectTargetFrameworkChanged_TargetFrameworkPropertyModified (
			string shortFrameworkName,
			string originalFullFrameworkName,
			string finalFullFrameworkName,
			string expectedShortFrameworkName)
		{
			CreateMSBuildProject (
				"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <TargetFramework>" + shortFrameworkName + "</TargetFramework>\r\n" +
				"  </PropertyGroup>\r\n" +
				"</Project>");
			msbuildProject.Evaluate ();
			ReadProject (originalFullFrameworkName);

			WriteProject (finalFullFrameworkName);

			string savedFramework = msbuildProject.GetGlobalPropertyGroup ()
				.GetValue ("TargetFramework");
			Assert.AreEqual (expectedShortFrameworkName, savedFramework);
		}
	}
}
