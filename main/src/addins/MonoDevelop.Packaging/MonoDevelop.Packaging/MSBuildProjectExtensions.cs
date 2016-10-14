// 
// MSBuildProjectExtensions.cs
// 
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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


using System.Linq;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Packaging
{
	internal static class MSBuildProjectExtensions
	{
		public static void AddImportIfMissing (
			this MSBuildProject project,
			string importedProjectFile,
			bool importAtTop,
			string condition)
		{
			if (project.ImportExists (importedProjectFile))
				return;
			
			project.AddImport (importedProjectFile, importAtTop, condition);
		}
		
		public static void AddImport (
			this MSBuildProject project,
			string importedProjectFile,
			bool importAtTop,
			string condition)
		{
			MSBuildObject before = GetInsertBeforeObject (project, importAtTop);
			project.AddNewImport (importedProjectFile, condition, before);
		}

		static MSBuildObject GetInsertBeforeObject (MSBuildProject project, bool importAtTop)
		{
			if (importAtTop) {
				return project.GetAllObjects ().FirstOrDefault ();
			}

			// Return an unknown MSBuildItem instead of null so the MSBuildProject adds the import as the last
			// child in the project.
			return new MSBuildItem ();
		}
		
		public static void RemoveImportIfExists (this MSBuildProject project, string importedProjectFile)
		{
			project.RemoveImport (importedProjectFile);
		}
		
		public static bool ImportExists (this MSBuildProject project, string importedProjectFile)
		{
			return project.GetImport (importedProjectFile) != null;
		}

		public static MSBuildPropertyGroup GetNuGetMetadataPropertyGroup (this MSBuildProject project)
		{
			MSBuildPropertyGroup propertyGroup = project.GetExistingNuGetMetadataPropertyGroup ();
			if (propertyGroup != null)
				return propertyGroup;

			return project.GetGlobalPropertyGroup ();
		}

		static MSBuildPropertyGroup GetExistingNuGetMetadataPropertyGroup (this MSBuildProject project)
		{
			foreach (MSBuildPropertyGroup propertyGroup in project.PropertyGroups) {
				if (propertyGroup.HasProperty (NuGetPackageMetadata.PackageIdPropertyName))
					return propertyGroup;
			}

			return null;
		}

		public static bool HasNuGetMetadata (this MSBuildProject project)
		{
			return project.GetExistingNuGetMetadataPropertyGroup () != null;
		}
	}
}
