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
