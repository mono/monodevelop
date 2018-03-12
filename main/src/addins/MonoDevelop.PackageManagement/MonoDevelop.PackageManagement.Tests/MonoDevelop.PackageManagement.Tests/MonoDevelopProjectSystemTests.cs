//
// MonoDevelopMSBuildNuGetProjectSystemTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.ProjectManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MonoDevelopMSBuildNuGetProjectSystemTests
	{
		TestableMonoDevelopProjectSystem projectSystem;
		FakeDotNetProject project;

		void CreateProjectSystem (IDotNetProject dotNetProject)
		{
			projectSystem = new TestableMonoDevelopProjectSystem (dotNetProject);
		}

		void CreateTestProject ()
		{
			project = new FakeDotNetProject (@"d:\Projects\Test\MyProject.csproj");
		}

		void CreateTestWebApplicationProject ()
		{
			project = ProjectHelper.CreateTestWebApplicationProject ();
		}

		void CreateTestWebSiteProject ()
		{
			project = ProjectHelper.CreateTestWebSiteProject ();
		}

		void CreateTestProject (string fileName)
		{
			project = new FakeDotNetProject (fileName);
		}

		void AddFileToProject (string fileName)
		{
			ProjectHelper.AddFile (project, fileName);
		}

		void AddDefaultCustomToolForFileName (string fileName, string customTool)
		{
			projectSystem.FakeProjectService.AddDefaultCustomToolForFileName (fileName, customTool);
		}

		void AddFile (string fileName)
		{
			projectSystem.AddFile (fileName.ToNativePath (), (Stream)null);
		}

		void AssertLastMSBuildChildElementHasProjectAttributeValue (string expectedImport)
		{
			ImportAndCondition import = projectSystem.NewImportsHandler.ImportsAdded.Last ();
			Assert.AreEqual (expectedImport, import.Name);
		}

		void AssertLastImportHasCondition (string expectedCondition)
		{
			ImportAndCondition import = projectSystem.NewImportsHandler.ImportsAdded.Last ();
			Assert.AreEqual (expectedCondition, import.Condition);
		}

		void AssertLastImportHasImportLocation (ImportLocation expectedLocation)
		{
			ImportAndCondition import = projectSystem.NewImportsHandler.ImportsAdded.Last ();
			Assert.AreEqual (expectedLocation, import.Location);
		}

		void AssertFirstMSBuildChildElementHasProjectAttributeValue (string expectedImport)
		{
			ImportAndCondition import = projectSystem.NewImportsHandler.ImportsAdded.First ();
			Assert.AreEqual (expectedImport, import.Name);
		}

		void AssertFirstImportHasCondition (string expectedCondition)
		{
			ImportAndCondition import = projectSystem.NewImportsHandler.ImportsAdded.First ();
			Assert.AreEqual (expectedCondition, import.Condition);
		}

		void AssertFirstImportHasImportLocation (ImportLocation expectedLocation)
		{
			ImportAndCondition import = projectSystem.NewImportsHandler.ImportsAdded.First ();
			Assert.AreEqual (expectedLocation, import.Location);
		}

		void AssertImportRemoved (string expectedImportRemoved)
		{
			Assert.That (project.ImportsRemoved, Contains.Item (expectedImportRemoved));
		}

		MSBuildProject CreateMSBuildProject (string xml)
		{
			var msbuildProject = new MSBuildProject ();
			msbuildProject.LoadXml (xml);
			return msbuildProject;
		}

		[Test]
		public void Root_NewInstanceCreated_ReturnsProjectDirectory ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);

			string expectedRoot = @"d:\projects\MyProject".ToNativePath ();
			Assert.AreEqual (expectedRoot, projectSystem.ProjectFullPath);
		}

		[Test]
		public void ProjectName_NewInstanceCreated_ReturnsProjectName ()
		{
			CreateTestProject ();
			project.Name = "MyProjectName";
			CreateProjectSystem (project);

			Assert.AreEqual ("MyProjectName", projectSystem.ProjectName);
		}

		[Test]
		[Ignore ("Arbitrary properties are not implemented")]
		public void GetPropertyValue_PassedDefinedPropertyName_ReturnsExpectedPropertyValue ()
		{
			CreateTestProject ();
			string expectedPropertyValue = "Test";
			string propertyName = "TestProperty";
			//project.SetEvaluatedProperty (propertyName, expectedPropertyValue);
			CreateProjectSystem (project);

			string propertyValue = projectSystem.GetPropertyValue (propertyName);

			Assert.AreEqual (expectedPropertyValue, propertyValue);
		}

		[Test]
		public void GetPropertyValue_PassedRootNamespacePropertyName_ReturnsRootNamespace ()
		{
			CreateTestProject ();
			project.DefaultNamespace = "Test";
			CreateProjectSystem (project);

			string propertyValue = projectSystem.GetPropertyValue ("RootNamespace");

			Assert.AreEqual ("Test", propertyValue);
		}

		[Test]
		public void TargetFramework_TargetFrameworkVersion40DefinedInProject_ReturnsFullDotNetFramework40 ()
		{
			CreateTestProject ();
			project.TargetFrameworkMoniker = new TargetFrameworkMoniker ("v4.0");
			CreateProjectSystem (project);

			FrameworkName expectedName = new FrameworkName (".NETFramework, Version=v4.0");

			Assert.AreEqual (expectedName.ToString (), projectSystem.TargetFramework.ToString ());
		}

		[Test]
		public void TargetFramework_TargetFrameworkVersion35ClientProfileDefinedInProject_ReturnsClientProfileDotNetFramework35 ()
		{
			CreateTestProject ();
			project.TargetFrameworkMoniker = new TargetFrameworkMoniker (".NETFramework", "v3.5", "Client");
			CreateProjectSystem (project);

			FrameworkName expectedName = new FrameworkName (".NETFramework, Profile=Client, Version=v3.5");

			Assert.AreEqual (expectedName.ToString (), projectSystem.TargetFramework.ToString ());
		}

		[Test]
		public void TargetFramework_TargetFrameworkVersionIsSilverlight20DefinedInProject_ReturnsSilverlight ()
		{
			CreateTestProject ();
			project.TargetFrameworkMoniker = new TargetFrameworkMoniker ("Silverlight", "v2.0");
			CreateProjectSystem (project);

			FrameworkName expectedName = new FrameworkName ("Silverlight, Version=v2.0");

			Assert.AreEqual (expectedName.ToString (), projectSystem.TargetFramework.ToString ());
		}

		[Test]
		public void IsSupportedFile_PassedCSharpFileName_ReturnsTrue ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\abc.cs".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsWebProjectAndPassedAppConfigFileName_ReturnsFalse ()
		{
			CreateTestWebApplicationProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\app.config".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsFalse (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsWebProjectAndPassedAppConfigFileNameInUpperCase_ReturnsFalse ()
		{
			CreateTestWebApplicationProject ();
			CreateProjectSystem (project);

			string fileName = @"c:\projects\APP.CONFIG".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsFalse (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsWebApplicationProjectAndPassedWebConfigFileName_ReturnsTrue ()
		{
			CreateTestWebApplicationProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\web.config".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsWebSiteProjectAndPassedWebConfigFileName_ReturnsTrue ()
		{
			CreateTestWebSiteProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\web.config".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsCSharpProjectAndPassedWebConfigFileName_ReturnsFalse ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\web.config".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsFalse (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsCSharpProjectAndPassedWebConfigFileNameInUpperCase_ReturnsFalse ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\WEB.CONFIG".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsFalse (result);
		}

		[Test]
		public void IsSupportedFile_ProjectIsCSharpProjectAndPassedAppConfigFileName_ReturnsTrue ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string fileName = @"d:\temp\app.config".ToNativePath ();
			bool result = projectSystem.IsSupportedFile (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task ReferenceExists_ProjectHasReferenceAndFullPathToAssemblyPassedToMethod_ReturnsTrue ()
		{
			CreateTestProject (@"D:\Projects\Test\MyProject.csproj");
			ProjectHelper.AddReference (project, "MyAssembly", @"d:\Projects\Test\MyAssembly.dll".ToNativePath ());
			CreateProjectSystem (project);
			string fileName = @"D:\Projects\Test\MyAssembly.dll".ToNativePath ();

			bool result = await projectSystem.ReferenceExistsAsync (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task ReferenceExists_ProjectHasNoReferences_ReturnsFalse ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			string fileName = @"D:\Projects\Test\MyAssembly.dll".ToNativePath ();

			bool result = await projectSystem.ReferenceExistsAsync (fileName);

			Assert.IsFalse (result);
		}

		[Test]
		public async Task ReferenceExistsAsync_ProjectReferenceNameHasDifferentCase_ReturnsTrue ()
		{
			CreateTestProject (@"D:\Projects\Test\MyProject.csproj");
			ProjectHelper.AddReference (project, "myassembly", @"d:\Projects\Test\myassembly.dll".ToNativePath ());
			CreateProjectSystem (project);
			string fileName = @"D:\Projects\Test\MYASSEMBLY.dll".ToNativePath ();

			bool result = await projectSystem.ReferenceExistsAsync (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task ReferenceExistsAsync_ReferenceNamePassedIsInProjectAndIsReferenceNameWithNoFileExtension_ReturnsTrue ()
		{
			CreateTestProject ();
			ProjectHelper.AddGacReference (project, "System.ComponentModel.Composition");
			CreateProjectSystem (project);
			string referenceName = "System.ComponentModel.Composition";

			bool result = await projectSystem.ReferenceExistsAsync (referenceName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task ReferenceExistsAsync_ReferenceIsInProjectAndProjectReferenceSearchedForHasExeFileExtension_ReturnsTrue ()
		{
			CreateTestProject (@"D:\Projects\Test\MyProject.csproj");
			ProjectHelper.AddReference (project, "myassembly", @"d:\Projects\Test\myassembly.exe".ToNativePath ());
			CreateProjectSystem (project);
			string fileName = @"D:\Projects\Test\myassembly.exe".ToNativePath ();

			bool result = await projectSystem.ReferenceExistsAsync (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task ReferenceExistsAsync_ReferenceIsInProjectAndProjectReferenceSearchedForHasExeFileExtensionInUpperCase_ReturnsTrue ()
		{
			CreateTestProject (@"D:\Projects\Test\MyProject.csproj");
			ProjectHelper.AddReference (project, "myassembly", @"d:\Projects\Test\myassembly.exe".ToNativePath ());
			CreateProjectSystem (project);
			string fileName = @"D:\Projects\Test\MYASSEMBLY.EXE".ToNativePath ();

			bool result = await projectSystem.ReferenceExistsAsync (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task AddReferenceAsync_AddReferenceToNUnitFramework_ProjectIsSavedAfterAddingReference ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			project.IsSaved = false;

			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();
			await projectSystem.AddReferenceAsync (fileName);

			Assert.AreEqual (1, project.ReferencesWhenSavedCount);
		}

		[Test]
		public async Task AddReferenceAsync_AddReferenceToNUnitFramework_ReferenceAddedToProject ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			project.IsSaved = false;

			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();
			await projectSystem.AddReferenceAsync (fileName);

			ProjectReference actualReference = project.References [0];
			Assert.AreEqual ("nunit.framework", actualReference.Reference);
			Assert.AreEqual (fileName, actualReference.HintPath.ToString());
		}

		[Test]
		public async Task AddReferenceAsync_ReferenceFileNameIsRelativePath_ReferenceAddedToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			project.IsSaved = false;
			string relativeFileName = @"packages\nunit\nunit.framework.dll".ToNativePath ();
			string fullFileName = @"d:\projects\MyProject\packages\nunit\nunit.framework.dll".ToNativePath ();
			await projectSystem.AddReferenceAsync (relativeFileName);

			ProjectReference actualReference = project.References [0];
			Assert.AreEqual ("nunit.framework", actualReference.Reference);
			Assert.AreEqual (fullFileName, actualReference.HintPath.ToString());
		}

		[Test]
		public async Task AddReferenceAsync_AddReferenceToNUnitFramework_AddingReferenceIsLogged ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			project.Name = "MyTestProject";

			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();
			await projectSystem.AddReferenceAsync (fileName);

			var expectedReferenceAndProjectName = new ReferenceAndProjectName () {
				Reference = "nunit.framework",
				Project = "MyTestProject"
			};

			Assert.AreEqual (expectedReferenceAndProjectName, projectSystem.ReferenceAndProjectNamePassedToLogAddedReferenceToProject);
		}

		[Test]
		public async Task RemoveReferenceAsync_ReferenceBeingRemovedHasFileExtension_ReferenceRemovedFromProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\packages\nunit\nunit.framework.dll".ToNativePath ();
			ProjectHelper.AddReference (project, fileName);
			CreateProjectSystem (project);

			await projectSystem.RemoveReferenceAsync (fileName);

			ProjectReference referenceItem = ProjectHelper.GetReference (project, "nunit.framework");

			Assert.IsNull (referenceItem);
		}

		[Test]
		public async Task RemoveReferenceAsync_ReferenceCaseAddedToProjectDifferentToReferenceNameBeingRemoved_ReferenceRemovedFromProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			ProjectHelper.AddReference (project, @"d:\projects\MyProject\packages\nunit\nunit.framework.dll".ToNativePath ());
			CreateProjectSystem (project);

			string fileName = @"NUNIT.FRAMEWORK.DLL";
			await projectSystem.RemoveReferenceAsync (fileName);

			ProjectReference referenceItem = ProjectHelper.GetReference (project, "nunit.framework");

			Assert.IsNull (referenceItem);
		}

		[Test]
		public async Task RemoveReferenceAsync_ProjectHasNoReference_ArgumentNullExceptionNotThrown ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string fileName = @"NUNIT.FRAMEWORK.DLL";
			try {
				await projectSystem.RemoveReferenceAsync (fileName);
			} catch (Exception ex) {
				Assert.Fail ("Exception thrown: {0}", ex);
			}
		}

		[Test]
		public async Task RemoveReferenceAsync_ReferenceExistsInProject_ProjectIsSavedAfterReferenceRemoved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();
			ProjectHelper.AddReference (project, fileName);
			CreateProjectSystem (project);

			await projectSystem.RemoveReferenceAsync (fileName);

			Assert.AreEqual (0, project.ReferencesWhenSavedCount);
		}

		[Test]
		public async Task RemoveReferenceAsync_ReferenceBeingRemovedHasFileExtension_ReferenceRemovalIsLogged ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			project.Name = "MyTestProject";
			string fileName = @"d:\projects\MyProject\packages\nunit\nunit.framework.dll".ToNativePath ();
			ProjectHelper.AddReference (project, fileName);
			CreateProjectSystem (project);

			await projectSystem.RemoveReferenceAsync (fileName);

			var expectedReferenceAndProjectName = new ReferenceAndProjectName {
				Reference = "nunit.framework",
				Project = "MyTestProject"
			};

			Assert.AreEqual (expectedReferenceAndProjectName, projectSystem.ReferenceAndProjectNamePassedToLogRemovedReferenceFromProject);
		}

		[Test]
		public void AddFile_NewFile_AddsFileToFileSystem ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string expectedPath = @"d:\temp\abc.cs".ToNativePath ();
			Stream expectedStream = new MemoryStream ();
			projectSystem.AddFile (expectedPath, expectedStream);

			Assert.AreEqual (expectedPath, projectSystem.PathPassedToPhysicalFileSystemAddFile);
			Assert.AreEqual (expectedStream, projectSystem.StreamPassedToPhysicalFileSystemAddFile);
		}

		[Test]
		public void AddFile_NewFile_AddsFileToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.cs".ToNativePath ();
			project.AddDefaultBuildAction (BuildAction.Compile, fileName);
			CreateProjectSystem (project);
			AddFile (fileName);

			ProjectFile fileItem = ProjectHelper.GetFile (project, fileName);
			FilePath expectedFileName = new FilePath (fileName);
			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (BuildAction.Compile, fileItem.BuildAction);
		}

		[Test]
		public void AddFile_NewResxFile_AddsFileToProjectWithCorrectItemType ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.resx".ToNativePath ();
			project.AddDefaultBuildAction (BuildAction.EmbeddedResource, fileName);
			CreateProjectSystem (project);

			AddFile (fileName);
			ProjectFile fileItem = ProjectHelper.GetFile (project, fileName);

			FilePath expectedFileName = new FilePath (fileName);

			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (BuildAction.EmbeddedResource, fileItem.BuildAction);
		}

		[Test]
		public void AddFile_RelativeFileNameUsed_AddsFileToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.cs".ToNativePath ();
			project.AddDefaultBuildAction (BuildAction.Compile, fileName);
			CreateProjectSystem (project);

			string relativeFileName = @"src\NewFile.cs".ToNativePath ();
			AddFile (relativeFileName);
			ProjectFile fileItem = ProjectHelper.GetFile (project, fileName);

			FilePath expectedFileName = new FilePath (fileName);

			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (BuildAction.Compile, fileItem.BuildAction);
		}

		[Test]
		public void AddFile_RelativeFileNameWithNoPathUsed_AddsFileToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\NewFile.cs".ToNativePath ();
			project.AddDefaultBuildAction (BuildAction.Compile, fileName);
			CreateProjectSystem (project);

			string relativeFileName = @"NewFile.cs";
			AddFile (relativeFileName);
			ProjectFile fileItem = ProjectHelper.GetFile (project, fileName);

			FilePath expectedFileName = new FilePath (fileName);

			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (BuildAction.Compile, fileItem.BuildAction);
		}

		[Test]
		public void AddFile_NewFile_ProjectIsSavedAfterFileAddedToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			project.IsSaved = false;
			CreateProjectSystem (project);

			string fileName = @"d:\projects\MyProject\src\NewFile.cs";
			AddFile (fileName);

			Assert.AreEqual (1, project.FilesAddedWhenSavedCount);
		}

		[Test]
		public void AddFile_NewFileToBeAddedInBinFolder_FileIsNotAddedToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);

			string fileName = @"bin\NewFile.dll";
			AddFile (fileName);

			Assert.AreEqual (0, project.FilesAdded.Count);
		}

		[Test]
		public void AddFile_NewFileToBeAddedInBinFolderWithBinFolderNameInUpperCase_FileIsNotAddedToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);

			string fileName = @"BIN\NewFile.dll";
			AddFile (fileName);

			Assert.AreEqual (0, project.FilesAdded.Count);
		}

		[Test]
		public void AddFile_FileAlreadyExistsInProject_FileIsNotAddedToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			project.AddDefaultBuildAction (BuildAction.Compile, @"d:\projects\MyProject\src\NewFile.cs");
			CreateProjectSystem (project);
			AddFileToProject (@"d:\projects\MyProject\src\NewFile.cs");

			AddFile (@"src\NewFile.cs");

			int projectItemsCount = project.FilesAdded.Count;
			Assert.AreEqual (0, projectItemsCount);
		}

		[Test]
		public void AddFile_NewFile_FileAddedToProjectIsLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			project.Name = "MyTestProject";
			CreateProjectSystem (project);

			AddFile (@"src\files\abc.cs");

			var expectedFileNameAndProjectName = new FileNameAndProjectName {
				FileName = @"src\files\abc.cs".ToNativePath (),
				ProjectName = "MyTestProject"
			};

			Assert.AreEqual (expectedFileNameAndProjectName, projectSystem.FileNameAndProjectNamePassedToLogAddedFileToProject);
		}

		[Test]
		public void AddFile_NewFileAlreadyExistsInProject_FileIsStillLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			project.Name = "MyTestProject";
			AddFileToProject (@"src\files\abc.cs");
			CreateProjectSystem (project);

			AddFile (@"src\files\abc.cs");

			var expectedFileNameAndProjectName = new FileNameAndProjectName {
				FileName = @"src\files\abc.cs".ToNativePath (),
				ProjectName = "MyTestProject"
			};

			Assert.AreEqual (expectedFileNameAndProjectName, projectSystem.FileNameAndProjectNamePassedToLogAddedFileToProject);
		}

		[Test]
		public void RemoveFile_DeletesFileFromFileSystem_CallsFileServiceRemoveFile ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			AddFileToProject (@"d:\temp\test.cs");
			CreateProjectSystem (project);

			projectSystem.RemoveFile ("test.cs");

			Assert.AreEqual (@"d:\temp\test.cs".ToNativePath (), projectSystem.FakeFileService.PathPassedToRemoveFile);
		}

		[Test]
		public void RemoveFile_DeletesFileFromFileSystem_ProjectIsSavedAfterFileRemoved ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			project.IsSaved = false;
			AddFileToProject (@"d:\temp\test.cs");
			CreateProjectSystem (project);

			projectSystem.RemoveFile ("test.cs");

			Assert.AreEqual (0, project.FilesInProjectWhenSavedCount);
		}

		[Test]
		public void RemoveFile_DeletesFileFromFileSystem_FileDeletionLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			AddFileToProject (@"d:\temp\test.cs");
			CreateProjectSystem (project);

			projectSystem.RemoveFile ("test.cs");

			Assert.AreEqual ("test.cs", projectSystem.FileNamePassedToLogDeletedFile);
		}

		[Test]
		public void RemoveFile_DeletesFileFromFileSystem_FolderInformationNotLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			AddFileToProject (@"d:\temp\test.cs");
			CreateProjectSystem (project);

			projectSystem.RemoveFile ("test.cs");

			Assert.IsNull (projectSystem.FileNameAndDirectoryPassedToLogDeletedFileFromDirectory);
		}

		[Test]
		public void RemoveFile_DeletesFileFromSubFolder_FileDeletionLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			AddFileToProject (@"d:\temp\src\Files\test.cs");
			CreateProjectSystem (project);

			projectSystem.RemoveFile (@"src\Files\test.cs".ToNativePath ());

			var expectedFileNameAndFolder = new FileNameAndDirectory () {
				FileName = "test.cs",
				Folder = @"src\Files".ToNativePath ()
			};

			var actualFileNameAndFolder = projectSystem.FileNameAndDirectoryPassedToLogDeletedFileFromDirectory;

			Assert.AreEqual (expectedFileNameAndFolder, actualFileNameAndFolder);
		}

		[Test]
		public void RemoveFile_DeletesFileFromSubFolder_FileDeletionWithoutFolderInformationIsNotLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			AddFileToProject (@"d:\temp\src\Files\test.cs");
			CreateProjectSystem (project);

			projectSystem.RemoveFile (@"src\Files\test.cs".ToNativePath ());

			Assert.IsNull (projectSystem.FileNamePassedToLogDeletedFile);
		}

		[Test]
		public void DeleteDirectory_DeletesDirectoryFromFileSystem_CallsFileServiceRemoveDirectory ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			AddFileToProject (@"d:\temp\test\test.cs");
			CreateProjectSystem (project);

			projectSystem.DeleteDirectory ("test", false);

			string path = @"d:\temp\test".ToNativePath ();
			Assert.AreEqual (path, projectSystem.FakeFileService.PathPassedToRemoveDirectory);
		}

		[Test]
		public void DeleteDirectory_DeletesDirectoryFromFileSystem_ProjectIsSavedAfterDirectoryDeleted ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			project.IsSaved = false;
			AddFileToProject (@"d:\temp\test\test.cs");
			CreateProjectSystem (project);

			projectSystem.DeleteDirectory ("test", false);

			Assert.AreEqual (0, project.FilesInProjectWhenSavedCount);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public void DeleteDirectory_DeletesDirectoryFromFileSystem_DirectoryIsLogged ()
		{
			CreateTestProject (@"d:\temp\MyProject.csproj");
			project.IsSaved = false;
			AddFileToProject (@"d:\temp\test\test.cs");
			CreateProjectSystem (project);

			projectSystem.DeleteDirectory ("test", false);

			Assert.AreEqual ("test", projectSystem.DirectoryPassedToLogDeletedDirectory);
		}

		[Test]
		public async Task AddFrameworkReferenceAsync_SystemXmlToBeAdded_ReferenceAddedToProject ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			await projectSystem.AddFrameworkReferenceAsync ("System.Xml", "MyPackage");

			ProjectReference referenceItem = ProjectHelper.GetReference (project, "System.Xml");

			Assert.AreEqual ("System.Xml", referenceItem.Reference);
			Assert.AreEqual (ReferenceType.Package, referenceItem.ReferenceType);
		}

		[Test]
		public async Task AddFrameworkReferenceAsync_SystemXmlToBeAdded_ProjectIsSaved ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			await projectSystem.AddFrameworkReferenceAsync ("System.Xml", "MyPackage");

			bool saved = project.IsSaved;

			Assert.IsTrue (saved);
		}

		[Test]
		public async Task AddFrameworkReferenceAsync_SystemXmlToBeAdded_AddedReferenceIsLogged ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			project.Name = "MyTestProject";	

			await projectSystem.AddFrameworkReferenceAsync ("System.Xml", "MyPackage");

			var expectedReferenceAndProjectName = new ReferenceAndProjectName () {
				Reference = "System.Xml",
				Project = "MyTestProject"
			};

			Assert.AreEqual (expectedReferenceAndProjectName, projectSystem.ReferenceAndProjectNamePassedToLogAddedReferenceToProject);
		}

		[Test]
		public void ResolvePath_PathPassed_ReturnsPathUnchanged ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			string expectedPath = @"d:\temp".ToNativePath ();

			string path = projectSystem.ResolvePath (expectedPath);

			Assert.AreEqual (expectedPath, path);
		}

		[Test]
		[Ignore ("Custom tool not implemented")]
		public void AddFile_NewTextTemplateFileWithAssociatedDefaultCustomTool_AddsFileToProjectWithDefaultCustomTool ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			string path = @"d:\temp\abc.tt";
			AddDefaultCustomToolForFileName (path, "TextTemplatingFileGenerator");
			Stream stream = new MemoryStream ();

			projectSystem.AddFile (path, stream);

			//ProjectFile fileItem = ProjectHelper.GetFile (project, path);
			//string customTool = fileItem.CustomTool;
			//Assert.AreEqual ("TextTemplatingFileGenerator", customTool);
		}

		[Test]
		[Ignore ("Custom tool not implemented")]
		public void AddFile_NewFileWithNoAssociatedDefaultCustomTool_AddsFileToProjectWithNoDefaultCustomTool ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			string path = @"d:\temp\abc.tt".ToNativePath ();
			AddDefaultCustomToolForFileName (path, null);
			Stream stream = new MemoryStream ();

			projectSystem.AddFile (path, stream);

			//ProjectFile fileItem = ProjectHelper.GetFile (project, path);
			//string customTool = fileItem.CustomTool;
			//Assert.AreEqual (String.Empty, customTool);
		}

		[Test]
		public void AddImport_FullImportFilePathAndBottomOfProject_PathRelativeToProjectAddedAsLastImportInProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			AssertLastMSBuildChildElementHasProjectAttributeValue (@"..\packages\Foo.0.1\build\Foo.targets");
			AssertLastImportHasImportLocation (ImportLocation.Bottom);
		}

		[Test]
		public void AddImport_SolutionAndProjectInSameDirectory_PathRelativeToProjectAddedAsLastImportInProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			AssertLastMSBuildChildElementHasProjectAttributeValue (@"packages\Foo.0.1\build\Foo.targets");
			AssertLastImportHasImportLocation (ImportLocation.Bottom);
		}

		[Test]
		public void AddImport_AddImportToBottomOfProject_ImportAddedWithConditionThatChecksForExistenceOfTargetsFile ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			AssertLastImportHasCondition ("Exists('..\\packages\\Foo.0.1\\build\\Foo.targets')");
			AssertLastImportHasImportLocation (ImportLocation.Bottom);
		}

		[Test]
		public void AddImport_AddSameImportTwice_ImportOnlyAddedTwiceToProjectSinceProjectRemovesDuplicates ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);
			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);
			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);
		}

		[Test]
		public void AddImport_AddSameImportTwiceButWithDifferentCase_ImportOnlyAddedTwiceToProjectSinceProjectRemovesDuplicates ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath1 = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			string targetPath2 = @"d:\projects\MyProject\packages\Foo.0.1\BUILD\FOO.TARGETS".ToNativePath ();

			projectSystem.AddImport (targetPath1, ImportLocation.Bottom);
			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);

			projectSystem.AddImport (targetPath2, ImportLocation.Bottom);
			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);
		}

		[Test]
		public void AddImport_FullImportFilePathAndBottomOfProject_ProjectIsSaved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public void RemoveImport_ImportAlreadyAddedToBottomOfProject_ImportRemoved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			projectSystem.RemoveImport (targetPath);

			AssertImportRemoved (@"..\packages\Foo.0.1\build\Foo.targets");
		}

		[Test]
		public void RemoveImport_ImportAlreadyWithDifferentCaseAddedToBottomOfProject_ImportRemoved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath1 = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath1, ImportLocation.Bottom);
			string targetPath2 = @"d:\projects\MyProject\packages\Foo.0.1\BUILD\FOO.TARGETS".ToNativePath ();

			projectSystem.RemoveImport (targetPath2);

			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);
			Assert.AreEqual (2, project.ImportsRemoved.Count);
		}

		[Test]
		public void RemoveImport_NoImportsAdded_ProjectIsSaved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);

			projectSystem.RemoveImport ("Unknown.targets");

			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public void AddImport_AddToTopOfProject_ImportAddedAsFirstChildElement ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Top);

			AssertFirstMSBuildChildElementHasProjectAttributeValue (@"..\packages\Foo.0.1\build\Foo.targets");
			AssertFirstImportHasImportLocation (ImportLocation.Top);
		}

		[Test]
		public void AddImport_AddImportToTopOfProject_ImportAddedWithConditionThatChecksForExistenceOfTargetsFile ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Top);

			AssertFirstImportHasCondition ("Exists('..\\packages\\Foo.0.1\\build\\Foo.targets')");
			AssertFirstImportHasImportLocation (ImportLocation.Top);
		}

		[Test]
		public void AddImport_AddToTopOfProjectTwice_ImportAddedTwiceSinceProjectRemovesDuplicates ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Top);
			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);

			projectSystem.AddImport (targetPath, ImportLocation.Top);

			Assert.AreEqual (1, projectSystem.NewImportsHandler.ImportsAdded.Count);
		}

		[Test]
		public async Task ReferenceExistsAsync_ReferenceIsInProjectButIncludesAssemblyVersion_ReturnsTrue ()
		{
			CreateTestProject ();
			string include = "MyAssembly, Version=0.1.0.0, Culture=neutral, PublicKeyToken=8cc8392e8503e009";
			ProjectHelper.AddGacReference (project, include);
			CreateProjectSystem (project);
			string fileName = @"D:\Projects\Test\myassembly.dll".ToNativePath ();

			bool result = await projectSystem.ReferenceExistsAsync (fileName);

			Assert.IsTrue (result);
		}

		[Test]
		public async Task RemoveReferenceAsync_ReferenceBeingRemovedHasFileExtensionAndProjectHasReferenceIncludingAssemblyVersion_ReferenceRemovedFromProject ()
		{
			CreateTestProject ();
			string include = "nunit.framework, Version=2.6.2.0, Culture=neutral, PublicKeyToken=8cc8392e8503e009";
			ProjectHelper.AddGacReference (project, include);
			CreateProjectSystem (project);
			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();

			await projectSystem.RemoveReferenceAsync (fileName);

			ProjectReference referenceItem = ProjectHelper.GetReference (project, "nunit.framework");
			Assert.IsNull (referenceItem);
		}

		[Test]
		public void RemoveImport_EnsureMacImportsWithWrongSeparatorAreRemoved_RemovesTwoImportsOneWithForwardSlashesAndOneWithBackslashesOnBothWindowsAndMac ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath, ImportLocation.Bottom);
			string expectedImportRemoved = @"..\packages\Foo.0.1\build\Foo.targets";

			projectSystem.RemoveImport (targetPath.ToNativePath ());

			AssertImportRemoved (expectedImportRemoved);
			AssertImportRemoved (expectedImportRemoved.Replace ('\\', '/'));
			Assert.AreEqual (2, project.ImportsRemoved.Count);
		}

		[Test]
		public void RemoveImport_ProjectAndSolutionInSameDirectory_ImportRemoved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			projectSystem.RemoveImport (targetPath);

			AssertImportRemoved (@"packages\Foo.0.1\build\Foo.targets");
		}

		[Test]
		public void RemoveImport_ImportAlreadyAddedToBottomOfProject_ImportRemovedEventIsFired ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath, ImportLocation.Bottom);
			const string expectedImportRemoved = @"..\packages\Foo.0.1\build\Foo.targets";
			DotNetProjectImportEventArgs eventArgs = null;
			projectSystem.PackageManagementEvents.ImportRemoved += (sender, e) => {
				eventArgs = e;
			};

			projectSystem.RemoveImport (targetPath);

			AssertImportRemoved (expectedImportRemoved);
			Assert.IsFalse (project.IsProjectBuilderDisposed);
			Assert.AreEqual (project, eventArgs.Project);
			Assert.AreEqual (expectedImportRemoved, eventArgs.Import);
		}

		[Test]
		public void RemoveImport_ProjectHasNuGetImportTargetAndSingleImportIsBeingRemoved_NuGetImportTargetIsRemoved ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath, ImportLocation.Bottom);
			MSBuildProject msbuildProject = CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <Target Name=\"EnsureNuGetPackageBuildImports\" BeforeTargets=\"PrepareForBuild\">\r\n" +
				"    <PropertyGroup>\r\n" +
				"      <ErrorText>Error.</ErrorText>\r\n" +
				"    </PropertyGroup>\r\n" +
				"    <Error Condition=\"!Exists('packages\\Foo.0.1\\build\\Foo.targets')\" Text=\"$([System.String]::Format('$(ErrorText)', 'packages\\Foo.0.1\\build\\Foo.targets'))\" />\r\n" +
				"  </Target>\r\n" +
				"</Project>");
			int targetCountBeforeSave = msbuildProject.Targets.Count ();
			project.SaveAction = () => {
				var msbuildExtension = new PackageManagementMSBuildExtension ();
				msbuildExtension.UpdateProject (msbuildProject);
			};

			projectSystem.RemoveImport (targetPath);

			AssertImportRemoved (@"packages\Foo.0.1\build\Foo.targets");
			Assert.AreEqual (1, targetCountBeforeSave);
			Assert.AreEqual (0, msbuildProject.Targets.Count ());
		}

		[Test]
		public void AddImport_ImportAlreadyAddedToBottomOfProject_ImportAddedEventIsFired ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();
			projectSystem.AddImport (targetPath, ImportLocation.Bottom);
			const string expectedImportAdded = @"..\packages\Foo.0.1\build\Foo.targets";
			DotNetProjectImportEventArgs eventArgs = null;
			projectSystem.PackageManagementEvents.ImportAdded += (sender, e) => {
				eventArgs = e;
			};

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			AssertLastMSBuildChildElementHasProjectAttributeValue (@"..\packages\Foo.0.1\build\Foo.targets");
			AssertLastImportHasImportLocation (ImportLocation.Bottom);
			Assert.IsFalse (project.IsProjectBuilderDisposed);
			Assert.AreEqual (project, eventArgs.Project);
			Assert.AreEqual (expectedImportAdded, eventArgs.Import);
		}

		[Test]
		public async Task AddReferenceAsync_AddReferenceToNUnitFramework_ReferenceAddingEventIsFired ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			ProjectReference referenceBeingAdded = null;
			projectSystem.PackageManagementEvents.ReferenceAdding += (sender, e) => {
				referenceBeingAdded = e.Reference;
			};

			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();
			await projectSystem.AddReferenceAsync (fileName);

			Assert.AreEqual (fileName, referenceBeingAdded.HintPath.ToString ());
			Assert.IsTrue (referenceBeingAdded.LocalCopy);
		}

		[Test]
		public async Task RemoveReferenceAsync_RemoveReferenceToNUnitFramework_ReferenceRemovingEventIsFired ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);
			string fileName = @"d:\projects\packages\nunit\nunit.framework.dll".ToNativePath ();
			ProjectHelper.AddReference (project, fileName);
			ProjectReference referenceBeingRemoved = null;
			bool projectIsSaved = false;
			projectSystem.PackageManagementEvents.ReferenceRemoving += (sender, e) => {
				referenceBeingRemoved = e.Reference;
				projectIsSaved = project.IsSaved;
			};

			await projectSystem.RemoveReferenceAsync (fileName);

			Assert.AreEqual (fileName, referenceBeingRemoved.HintPath.ToString ());
			Assert.IsFalse (projectIsSaved);
		}

		[Test]
		public void AddImport_FullImportFilePathAndBottomOfProject_ImportHandlerIsDisposed ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			string targetPath = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets".ToNativePath ();

			projectSystem.AddImport (targetPath, ImportLocation.Bottom);

			Assert.IsTrue (projectSystem.NewImportsHandler.IsDisposed);
		}

		[Test]
		public void AddExistingFile_FileExists_AddsFileToProject ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.cs".ToNativePath ();
			project.AddDefaultBuildAction (BuildAction.Compile, fileName);
			CreateProjectSystem (project);

			projectSystem.AddExistingFile (fileName);

			ProjectFile fileItem = ProjectHelper.GetFile (project, fileName);
			var expectedFileName = new FilePath (fileName);
			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (BuildAction.Compile, fileItem.BuildAction);
			Assert.IsNull (projectSystem.PathPassedToPhysicalFileSystemAddFile);
			Assert.IsTrue (project.IsSaved);
			Assert.AreEqual (1, project.FilesAddedWhenSavedCount);
		}

		[Test]
		public void GetDirectories_TwoDirectories_ReturnsTwoDirectories ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			var expectedDirectories = new string[] {
				@"d:\projects\MyProject\Scripts\One".ToNativePath (),
				@"d:\projects\MyProject\Scripts\Two".ToNativePath (),
			};
			projectSystem.AddDirectoriesForPath (@"d:\projects\MyProject\Scripts".ToNativePath (), expectedDirectories);

			string[] directories = projectSystem.GetDirectories (@"Scripts").ToArray ();

			CollectionAssert.AreEqual (expectedDirectories, directories);
		}

		[Test]
		public void GetFiles_Recursive_ThrowsNotImplementedException ()
		{
			CreateTestProject ();
			CreateProjectSystem (project);

			Assert.Throws<NotImplementedException> (() => projectSystem.GetFiles ("", "*.*", true));
		}

		[Test]
		public void GetFiles_TwoFiles_ReturnsTwoFiles ()
		{
			CreateTestProject (@"d:\projects\MyProject\MyProject.csproj");
			CreateProjectSystem (project);
			var expectedFiles = new string[] {
				@"d:\projects\MyProject\Scripts\one.js".ToNativePath (),
				@"d:\projects\MyProject\Scripts\two.js".ToNativePath (),
			};
			projectSystem.AddFilesForPath (
				@"d:\projects\MyProject\Scripts".ToNativePath (),
				"*.*",
				SearchOption.TopDirectoryOnly,
				expectedFiles);

			string[] files = projectSystem.GetFiles (@"Scripts", "*.*", false).ToArray ();

			CollectionAssert.AreEqual (expectedFiles, files);
		}
	}
}


