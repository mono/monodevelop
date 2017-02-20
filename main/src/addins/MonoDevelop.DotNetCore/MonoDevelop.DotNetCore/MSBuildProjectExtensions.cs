//
// MSBuildProjectExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	static class MSBuildProjectExtensions
	{
		static readonly string InternalDotNetCoreLabel = "__InternalDotNetCore__";

		public static MSBuildImport AddInternalImport (
			this MSBuildProject project,
			string importedProjectFile,
			bool importAtTop = false,
			string condition = null)
		{
			MSBuildObject before = GetInsertBeforeObject (project, importAtTop);
			MSBuildImport import = project.AddNewImport (importedProjectFile, condition, before);
			import.Label = InternalDotNetCoreLabel;
			return import;
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

		public static void AddInternalPropertyBefore (this MSBuildProject project, string name, string value, MSBuildObject beforeItem)
		{
			var propertyGroup = project.CreatePropertyGroup ();
			propertyGroup.Condition = string.Format ("'$({0})' == ''", name);
			propertyGroup.Label = InternalDotNetCoreLabel;
			propertyGroup.SetValue (name,value);

			project.AddPropertyGroup (propertyGroup, false, beforeItem);
		}

		public static void RemoveInternalElements (this MSBuildProject project)
		{
			foreach (var element in GetInternalElements (project).ToArray ()) {
				project.Remove (element);
			}
		}

		static IEnumerable<MSBuildObject> GetInternalElements (MSBuildProject project)
		{
			return project.GetAllObjects ().OfType<MSBuildElement> ().Where (HasInternalDotNetCoreLabel);
		}

		static IEnumerable<MSBuildPropertyGroup> GetInternalPropertyGroups (MSBuildProject project)
		{
			return project.PropertyGroups.Where (HasInternalDotNetCoreLabel);
		}

		static bool HasInternalDotNetCoreLabel (MSBuildElement element)
		{
			return element.Label == InternalDotNetCoreLabel;
		}

		public static bool IsOutputTypeDefined (this MSBuildProject project)
		{
			return project.HasGlobalProperty ("OutputType");
		}

		public static bool HasGlobalProperty (this MSBuildProject project, string name)
		{
			var globalPropertyGroup = project.GetGlobalPropertyGroup ();
			if (globalPropertyGroup != null)
				return globalPropertyGroup.HasProperty (name);

			return false;
		}

		public static IEnumerable<string> GetTargetFrameworks (this MSBuildProject project)
		{
			var properties = project.EvaluatedProperties;
			if (properties != null) {
				string targetFramework = properties.GetValue ("TargetFramework");
				if (targetFramework != null) {
					return new [] { targetFramework };
				}

				string targetFrameworks = properties.GetValue ("TargetFrameworks");
				if (targetFrameworks != null) {
					return targetFrameworks.Split (';');
				}
			}

			return new string[0];
		}

		public static void UpdateTargetFrameworks (this MSBuildProject project, IEnumerable<string> targetFrameworks)
		{
			var globalPropertyGroup = project.GetGlobalPropertyGroup ();
			if (targetFrameworks.Count () > 1) {
				string value = string.Join (";", targetFrameworks);
				globalPropertyGroup.SetValue ("TargetFrameworks", value);
			} else {
				globalPropertyGroup.SetValue ("TargetFramework", targetFrameworks.FirstOrDefault ());
			}
		}

		public static bool ImportExists (this MSBuildProject project, string importedProjectFile)
		{
			return project.GetImport (importedProjectFile) != null;
		}

		/// <summary>
		/// Use IntermediateOutputPath instead of BaseIntermediateOutputPath since the latter
		/// seems to have a full path so the exclude fails to match the include since these
		/// are relative paths. IntermediateOutputPath is a relative path however it is not
		/// as restrictive as BaseIntermediateOutputPath and will not exclude all files from
		/// this directory. A separate filtering is done in DotNetCoreProjectExtension's
		/// OnGetSourceFiles to remove files from the BaseIntermediateOutputPath.
		/// </summary>
		static string DefaultExcludes = @"$(BaseOutputPath)**;$(IntermediateOutputPath)**;**\*.*proj.user;**\*.*proj;**\*.sln;.*;**\.*\**";

		// HACK: Temporary workaround. Add wildcard items to the project otherwise the
		// solution window shows no files.
		public static void AddWebProjectWildcardItems (this MSBuildProject project)
		{
			MSBuildObject before = GetInsertBeforeObject (project, true);
			MSBuildItemGroup itemGroup = project.AddNewItemGroup (before);
			itemGroup.Label = InternalDotNetCoreLabel;

			MSBuildItem item = itemGroup.AddNewItem ("Content", @"**\*");
			item.Exclude = DefaultExcludes + @";**\*.cs;**\*.resx;Properties\**;";

			item = itemGroup.AddNewItem ("Compile", @"**\*.cs");
			item.Exclude = DefaultExcludes + @";wwwroot\**";

			item = itemGroup.AddNewItem ("EmbeddedResource", @"**\*.resx");
			item.Exclude = DefaultExcludes + @";wwwroot\**";
		}

		// HACK: Temporary workaround. Add wildcard items to the project otherwise the
		// solution window shows no files.
		public static void AddProjectWildcardItems (this MSBuildProject project)
		{
			MSBuildObject before = GetInsertBeforeObject (project, true);
			MSBuildItemGroup itemGroup = project.AddNewItemGroup (before);
			itemGroup.Label = InternalDotNetCoreLabel;

			// DefaultExcludesInProjectFolder contains "**/.*/**" which does not
			// exclude directories starting with '.'. Using "**\.*\**" works so
			// add it directly instead of using DefaultExcludesInProjectFolder in
			// the exclude.
			MSBuildItem item = itemGroup.AddNewItem ("None", @"**\*");
			item.Exclude = DefaultExcludes + @";**\*.cs;**\*.resx";

			item = itemGroup.AddNewItem ("Compile", @"**\*.cs");
			item.Exclude = DefaultExcludes;

			item = itemGroup.AddNewItem ("EmbeddedResource", @"**\*.resx");
			item.Exclude = DefaultExcludes;
		}

		/// <summary>
		/// Remove Name and Project from project references.
		/// </summary>
		public static void RemoveExtraProjectReferenceMetadata (this MSBuildProject project)
		{
			foreach (MSBuildItem item in project.GetAllItems ()) {
				if (item.Name == "ProjectReference") {
					item.Metadata.RemoveProperty ("Name");
					item.Metadata.RemoveProperty ("Project");
				}
			}
		}
	}
}
