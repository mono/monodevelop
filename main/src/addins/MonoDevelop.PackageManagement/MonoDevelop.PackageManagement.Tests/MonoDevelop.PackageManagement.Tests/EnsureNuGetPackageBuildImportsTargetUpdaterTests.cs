//
// EnsureNuGetPackageBuildImportsTargetUpdaterTests.cs
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
using System.Linq;
using MonoDevelop.Projects.Formats.MSBuild;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class EnsureNuGetPackageBuildImportsTargetUpdaterTests
	{
		MSBuildProject msbuildProject;
		PackageManagementMSBuildExtension msbuildExtension;
		EnsureNuGetPackageBuildImportsTargetUpdater updater;

		void CreateMSBuildProject (string xml)
		{
			msbuildProject = new MSBuildProject ();
			msbuildProject.Document.LoadXml (xml);
		}

		void CreateUpdaterWithImportToRemove (string import)
		{
			updater = new EnsureNuGetPackageBuildImportsTargetUpdater ();
			updater.RemoveImport (import);
		}

		void SaveProject ()
		{
			using (updater) {
				PackageManagementMSBuildExtension.Updater = updater;
				msbuildExtension = new PackageManagementMSBuildExtension ();
				msbuildExtension.SaveProject (null, null, msbuildProject);
			}
		}

		[Test]
		public void SaveProject_ProjectHasOneImportInNuGetImportAndItIsBeingRemoved_NuGetImportTargetIsRemoved ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <Target Name=\"EnsureNuGetPackageBuildImports\" BeforeTargets=\"PrepareForBuild\">\r\n" +
				"    <PropertyGroup>\r\n" +
				"      <ErrorText>Error.</ErrorText>\r\n" +
				"    </PropertyGroup>\r\n" +
				"    <Error Condition=\"!Exists('packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets')\" Text=\"$([System.String]::Format('$(ErrorText)', 'packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets'))\" />\r\n" +
				"  </Target>\r\n" +
				"</Project>");
			int targetCountBeforeSave = msbuildProject.Targets.Count ();
			string import = @"packages\Xamarin.Forms.1.2.3.6257\build\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\Xamarin.Forms.targets";
			CreateUpdaterWithImportToRemove (import);

			SaveProject ();

			MSBuildTarget target = msbuildProject.Targets.FirstOrDefault ();
			Assert.AreEqual (1, targetCountBeforeSave);
			Assert.AreEqual (0, msbuildProject.Targets.Count ());
		}

		[Test]
		public void SaveProject_ProjectHasOneImportInNuGetImportTargetHasDifferentCaseAndItIsBeingRemoved_NuGetImportTargetIsRemoved ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <Target Name=\"ENSUREnugetpackagebuildIMPORTS\" BeforeTargets=\"PrepareForBuild\">\r\n" +
				"    <PropertyGroup>\r\n" +
				"      <ErrorText>Error.</ErrorText>\r\n" +
				"    </PropertyGroup>\r\n" +
				"    <Error Condition=\"!Exists('packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets')\" Text=\"$([System.String]::Format('$(ErrorText)', 'packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets'))\" />\r\n" +
				"  </Target>\r\n" +
				"</Project>");
			int targetCountBeforeSave = msbuildProject.Targets.Count ();
			string import = @"packages\Xamarin.Forms.1.2.3.6257\build\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\Xamarin.Forms.targets";
			CreateUpdaterWithImportToRemove (import);

			SaveProject ();

			MSBuildTarget target = msbuildProject.Targets.FirstOrDefault ();
			Assert.AreEqual (1, targetCountBeforeSave);
			Assert.AreEqual (0, msbuildProject.Targets.Count ());
		}

		[Test]
		public void SaveProject_ProjectHasTwoImportsInNuGetImportAndOneBeingRemoved_ImportRemovedFromNuGetImport ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <Target Name=\"EnsureNuGetPackageBuildImports\" BeforeTargets=\"PrepareForBuild\">\r\n" +
				"    <PropertyGroup>\r\n" +
				"      <ErrorText>Error.</ErrorText>\r\n" +
				"    </PropertyGroup>\r\n" +
				"    <Error Condition=\"!Exists('packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets')\" Text=\"$([System.String]::Format('$(ErrorText)', 'packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets'))\" />\r\n" +
				"    <Error Condition=\"!Exists('packages\\Other.1.1.0\\build\\Other.targets')\" Text=\"$([System.String]::Format('$(ErrorText)', 'packages\\Other.1.1.0\\build\\Other.targets'))\" />\r\n" +
				"  </Target>\r\n" +
				"</Project>");
			int taskCountBeforeSave = msbuildProject.Targets.FirstOrDefault ().Tasks.Count ();
			SaveProject ();

			MSBuildTarget target = msbuildProject.Targets.FirstOrDefault ();
			Assert.AreEqual (2, taskCountBeforeSave);
			Assert.AreEqual (1, target.Tasks.Count ());
			Assert.IsTrue (target.Tasks.Any (t => t.Condition == @"!Exists('packages\Other.1.1.0\build\Other.targets')"));
			Assert.IsFalse (target.Tasks.Any (t => t.Condition == @"!Exists('packages\Xamarin.Forms.1.2.3.6257\build\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\Xamarin.Forms.targets')"));
		}
	}
}

