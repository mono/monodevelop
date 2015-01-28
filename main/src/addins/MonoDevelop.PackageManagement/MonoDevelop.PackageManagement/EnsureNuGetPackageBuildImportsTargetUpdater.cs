//
// EnsureNuGetPackageBuildImportsTargetUpdater.cs
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
using System.Xml;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Visual Studio adds an extra EnsureNuGetPackageBuildImports target to the project when
	/// a NuGet package uses a custom MSBuild target.
	/// 
	/// This EnsureNuGetPackageBuildImports target has a set of Error tasks which will cause
	/// build errors if the MSBuild target file does not exist.
	/// 
	/// This class updates that target so to prevent build errors occurring. When a NuGet
	/// package is uninstalled the MSBuild import is removed from the 
	/// EnsureNuGetPackageBuildImports target. If there are Error items left inside the
	/// EnsureNuGetPackageBuildImports target element then the 
	/// EnsureNuGetPackageBuildImports is removed.
	/// </summary>
	public class EnsureNuGetPackageBuildImportsTargetUpdater : IDisposable
	{
		static readonly string NuGetTargetName = "EnsureNuGetPackageBuildImports";

		string importToRemove;

		public EnsureNuGetPackageBuildImportsTargetUpdater ()
		{
			PackageManagementMSBuildExtension.Updater = this;
		}

		public void RemoveImport (string import)
		{
			importToRemove = import;
		}

		public void UpdateProject (MSBuildProject project)
		{
			if (importToRemove == null)
				return;

			MSBuildTarget nugetImportTarget = FindNuGetImportTarget (project);
			if (nugetImportTarget == null)
				return;

			MSBuildTask msbuildTask = FindErrorTaskForImport (nugetImportTarget, importToRemove);
			if (msbuildTask == null)
				return;

			RemoveFromProject (msbuildTask.Element);

			if (nugetImportTarget.Tasks.Count () == 0) {
				RemoveFromProject (nugetImportTarget.Element);
			}
		}

		static MSBuildTarget FindNuGetImportTarget (MSBuildProject project)
		{
			return project.Targets
				.FirstOrDefault (target => IsMatchIgnoringCase (target.Name, NuGetTargetName));
		}

		static MSBuildTask FindErrorTaskForImport (MSBuildTarget target, string import)
		{
			string condition = String.Format ("!Exists('{0}')", import);
			return target.Tasks
				.FirstOrDefault (task => IsMatchIgnoringCase (task.Condition, condition));
		}

		static void RemoveFromProject (XmlElement element)
		{
			element.ParentNode.RemoveChild (element);
		}

		static bool IsMatchIgnoringCase (string a, string b)
		{
			return String.Equals (a, b, StringComparison.OrdinalIgnoreCase);
		}

		public void Dispose ()
		{
			PackageManagementMSBuildExtension.Updater = null;
		}
	}
}

