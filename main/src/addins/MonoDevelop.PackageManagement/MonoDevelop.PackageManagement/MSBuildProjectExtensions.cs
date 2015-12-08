// 
// MSBuildProjectExtensions.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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
using System.Xml;

using MonoDevelop.Projects.Formats.MSBuild;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public static class MSBuildProjectExtensions
	{
		static readonly XmlNamespaceManager namespaceManager =
			new XmlNamespaceManager (new NameTable ());
		
		static MSBuildProjectExtensions ()
		{
			namespaceManager.AddNamespace ("tns", MSBuildProject.Schema);
		}
		
		public static void AddImportIfMissing (
			this MSBuildProject project,
			string importedProjectFile,
			ProjectImportLocation importLocation,
			string condition)
		{
			if (project.ImportExists (importedProjectFile))
				return;
			
			project.AddImport (importedProjectFile, importLocation, condition);
		}
		
		public static void AddImport (
			this MSBuildProject project,
			string importedProjectFile,
			ProjectImportLocation importLocation,
			string condition)
		{
			var before = importLocation == ProjectImportLocation.Top ? project.GetAllObjects ().FirstOrDefault () : null;
			project.AddNewImport (importedProjectFile, condition, before);
		}
		
		public static void RemoveImportIfExists (this MSBuildProject project, string importedProjectFile)
		{
			project.RemoveImport (importedProjectFile);
		}
		
		public static bool ImportExists (this MSBuildProject project, string importedProjectFile)
		{
			return project.GetImport (importedProjectFile) != null;
		}
	}
}
