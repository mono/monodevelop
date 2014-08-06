﻿// 
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
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.MSBuild;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public static class DotNetProjectExtensions
	{
		public static readonly Guid WebApplication = Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}");
		public static readonly Guid WebSite = Guid.Parse("{E24C65DC-7377-472B-9ABA-BC803B73C61A}");
		
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
			return File.Exists (project.GetPackagesConfigFilePath ());
		}

		public static string GetPackagesConfigFilePath (this DotNetProject project)
		{
			return Path.Combine (project.BaseDirectory, Constants.PackageReferenceFile);
		}

		public static bool HasPackages (this IDotNetProject project)
		{
			return File.Exists (project.GetPackagesConfigFilePath ());
		}

		public static string GetPackagesConfigFilePath (this IDotNetProject project)
		{
			return Path.Combine (project.BaseDirectory, Constants.PackageReferenceFile);
		}
	}
}
