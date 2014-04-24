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
			XmlElement import = project.AddImportElement (importedProjectFile, importLocation);
			import.SetAttribute ("Condition", condition);
		}
		
		static XmlElement AddImportElement(
			this MSBuildProject project,
			string importedProjectFile,
			ProjectImportLocation importLocation)
		{
			if (importLocation == ProjectImportLocation.Top) {
				return project.AddImportElementAtTop (importedProjectFile);
			}
			XmlElement import = project.CreateImportElement (importedProjectFile);
			project.Document.DocumentElement.AppendChild (import);
			return import;
		}
		
		static XmlElement CreateImportElement(this MSBuildProject project, string importedProjectFile)
		{
			XmlElement import = project.Document.CreateElement ("Import", MSBuildProject.Schema);
			import.SetAttribute ("Project", importedProjectFile);
			return import;
		}
		
		static XmlElement AddImportElementAtTop (this MSBuildProject project, string importedProjectFile)
		{
			XmlElement import = project.CreateImportElement (importedProjectFile);
			XmlElement projectRoot = project.Document.DocumentElement;
			projectRoot.InsertBefore (import, projectRoot.FirstChild);
			return import;
		}
		
		public static void RemoveImportIfExists (this MSBuildProject project, string importedProjectFile)
		{
			XmlElement import = project.FindImportElement (importedProjectFile);
			if (import != null) {
				import.ParentNode.RemoveChild (import);
			}
		}
		
		public static bool ImportExists (this MSBuildProject project, string importedProjectFile)
		{
			return project.FindImportElement (importedProjectFile) != null;
		}
		
		static XmlElement FindImportElement (this MSBuildProject project, string importedProjectFile)
		{
			return project
				.Imports ()
				.FirstOrDefault (import => String.Equals (import.GetAttribute ("Project"), importedProjectFile, StringComparison.OrdinalIgnoreCase));
		}
		
		static IEnumerable <XmlElement> Imports (this MSBuildProject project)
		{
			foreach (XmlElement import in project.Document.DocumentElement.SelectNodes ("tns:Import", namespaceManager)) {
				yield return import;
			}
		}
	}
}
