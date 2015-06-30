// 
// DotNetProjectExtensions.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public static class DotNetProjectExtensions
	{
		public static readonly Guid WebApplication = Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}");
		public static readonly Guid WebSite = Guid.Parse("{E24C65DC-7377-472B-9ABA-BC803B73C61A}");

		public static Func<string, bool> FileExists = File.Exists;

		public static bool IsWebProject(this IDotNetProject project)
		{
			return project.HasProjectType(WebApplication) || project.HasProjectType(WebSite);
		}
		
		public static bool HasProjectType(this IDotNetProject project, Guid projectTypeGuid)
		{
			foreach (string guid in project.GetProjectTypeGuids()) {
				if (IsMatch(projectTypeGuid, guid)) {
					return true;
				}
			}
			return false;
		}
		
		public static string[] GetProjectTypeGuids(this IDotNetProject project)
		{
			string projectTypeGuids = project.GetProjectTypeGuidPropertyValue();
			return projectTypeGuids.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
		}
		
		static bool IsMatch(Guid guid, string guidStringToMatch)
		{
			Guid result;
			if (Guid.TryParse(guidStringToMatch, out result)) {
				return guid == result;
			}
			return false;
		}
		
		public static string GetProjectTypeGuidPropertyValue (this IDotNetProject project)
		{
			string propertyValue = null;
			if (project.ExtendedProperties.Contains("ProjectTypeGuids")) {
				propertyValue = project.ExtendedProperties["ProjectTypeGuids"] as String;
			}
			return propertyValue ?? String.Empty;
		}

		public static bool HasPackages (this DotNetProject project)
		{
			return HasPackages (project.BaseDirectory, project.Name);
		}

		public static string GetPackagesConfigFilePath (this DotNetProject project)
		{
			return GetPackagesConfigFilePath (project.BaseDirectory, project.Name);
		}

		public static bool HasPackages (this IDotNetProject project)
		{
			return AnyFileExists (GetPossiblePackagesConfigFilePaths (project.BaseDirectory, project.Name));
		}

		static bool HasPackages (string projectDirectory, string projectName)
		{
			return AnyFileExists (GetPossiblePackagesConfigFilePaths (projectDirectory, projectName));
		}

		static bool AnyFileExists (IEnumerable<string> files)
		{
			return files.Any (FileExists);
		}

		static IEnumerable<string> GetPossiblePackagesConfigFilePaths (string projectDirectory, string projectName)
		{
			yield return GetNonDefaultProjectPackagesConfigFilePath (projectDirectory, projectName);
			yield return GetDefaultPackagesConfigFilePath (projectDirectory);
		}

		static string GetNonDefaultProjectPackagesConfigFilePath (string projectDirectory, string projectName)
		{
			return Path.Combine (projectDirectory, GetNonDefaultProjectPackagesConfigFileName (projectName));
		}

		static string GetNonDefaultProjectPackagesConfigFileName (string projectName)
		{
			return "packages." + projectName.Replace (' ', '_') + ".config";
		}

		static string GetDefaultPackagesConfigFilePath (string projectDirectory)
		{
			return Path.Combine (projectDirectory, Constants.PackageReferenceFile);
		}

		public static string GetPackagesConfigFilePath (this IDotNetProject project)
		{
			return GetPackagesConfigFilePath (project.BaseDirectory, project.Name);
		}

		static string GetPackagesConfigFilePath (string projectDirectory, string projectName)
		{
			string nonDefaultPackagesConfigFilePath = GetNonDefaultProjectPackagesConfigFilePath (projectDirectory, projectName);
			if (FileExists (nonDefaultPackagesConfigFilePath)) {
				return nonDefaultPackagesConfigFilePath;
			}
			return GetDefaultPackagesConfigFilePath (projectDirectory);
		}
	}
}
