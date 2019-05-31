//
// MSBuildSdkProject.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Projects.MSBuild
{
	class MSBuildSdkProject
	{
		List<string> targetFrameworks;
		bool hasRootNamespace;
		bool hasAssemblyName;
		bool hasDescription;
		CompileTarget defaultCompileTarget = CompileTarget.Library;
		TargetFrameworkMoniker targetFrameworkMoniker;

		public string ToolsVersion { get; private set; }
		public bool IsOutputTypeDefined { get; private set; }

		public IEnumerable<string> TargetFrameworks => targetFrameworks;

		public bool HasSdk { get; set; }

		public bool HasToolsVersion () => !string.IsNullOrEmpty (ToolsVersion);

		public CompileTarget DefaultCompileTarget => defaultCompileTarget;

		/// <summary>
		/// Ensure MSBuildProject has ToolsVersion set to 15.0 so the correct
		/// MSBuild targets are imported.
		/// </summary>
		public void ReadProjectHeader (MSBuildProject project)
		{
			ToolsVersion = project.ToolsVersion;
			if (!HasToolsVersion ())
				project.ToolsVersion = "15.0";
		}

		public void ReadProject (MSBuildProject project, TargetFrameworkMoniker framework)
		{
			IsOutputTypeDefined = HasOutputType (project);
			targetFrameworks = GetTargetFrameworks (project).ToList ();
			hasRootNamespace = HasGlobalProperty (project, "RootNamespace");
			hasAssemblyName = HasGlobalProperty (project, "AssemblyName");
			hasDescription = HasGlobalProperty (project, "Description");

			targetFrameworkMoniker = framework;

			ReadDefaultCompileTarget (project);
		}

		public void WriteProject (MSBuildProject project, TargetFrameworkMoniker framework)
		{
			var globalPropertyGroup = project.GetGlobalPropertyGroup ();
			globalPropertyGroup.RemoveProperty ("ProjectGuid");
			globalPropertyGroup.RemoveProperty ("TargetFrameworkIdentifier");
			globalPropertyGroup.RemoveProperty ("TargetFrameworkVersion");

			if (!IsOutputTypeDefined)
				RemoveOutputTypeIfHasDefaultValue (project, globalPropertyGroup);

			RemoveMSBuildProjectNameDerivedProperties (globalPropertyGroup);

			if (!hasDescription)
				RemovePropertyIfHasDefaultValue (globalPropertyGroup, "Description", "Package Description");

			project.DefaultTargets = null;

			RemoveExtraProjectReferenceMetadata (project);

			UpdateTargetFramework (project, framework);

			if (HasToolsVersion ())
				project.ToolsVersion = ToolsVersion;

			if (HasSdk) {
				project.ToolsVersion = ToolsVersion;
			}
		}

		static void RemoveOutputTypeIfHasDefaultValue (MSBuildProject project, MSBuildPropertyGroup globalPropertyGroup)
		{
			string outputType = project.EvaluatedProperties.GetValue ("OutputType");
			if (string.IsNullOrEmpty (outputType)) {
				globalPropertyGroup.RemoveProperty ("OutputType");
			} else {
				RemovePropertyIfHasDefaultValue (globalPropertyGroup, "OutputType", outputType);
			}
		}

		void RemoveMSBuildProjectNameDerivedProperties (MSBuildPropertyGroup globalPropertyGroup)
		{
			string msbuildProjectName = globalPropertyGroup.ParentProject.FileName.FileNameWithoutExtension;

			if (!hasAssemblyName)
				RemovePropertyIfHasDefaultValue (globalPropertyGroup, "AssemblyName", msbuildProjectName);

			if (!hasRootNamespace)
				RemovePropertyIfHasDefaultValue (globalPropertyGroup, "RootNamespace", msbuildProjectName);
		}

		void UpdateTargetFramework (MSBuildProject project, TargetFrameworkMoniker framework)
		{
			if (targetFrameworkMoniker == framework)
				return;

			string shortFrameworkName = null;
			SdkProjectShortTargetFramework shortFramework = null;

			string existingFramework = targetFrameworks.FirstOrDefault ();
			bool identifiersMatch = targetFrameworkMoniker.Identifier == framework.Identifier;

			if (identifiersMatch && SdkProjectShortTargetFramework.TryParse (existingFramework, out shortFramework)) {
				shortFramework.Update (framework);
				shortFrameworkName = shortFramework.ToString ();
			} else {
				shortFrameworkName = framework.ShortName;
			}

			if (existingFramework == shortFrameworkName)
				return;

			if (targetFrameworks.Count == 0)
				targetFrameworks.Add (shortFrameworkName);
			else
				targetFrameworks [0] = shortFrameworkName;

			targetFrameworkMoniker = framework;
			UpdateTargetFrameworks (project, targetFrameworks);
		}

		public void AddKnownItemAttributes (MSBuildProject project)
		{
			if (HasSdk)
				project.AddKnownItemAttribute ("PackageReference", "Version");
		}

		public void ReadDefaultCompileTarget (MSBuildProject project)
		{
			string outputType = project.EvaluatedProperties.GetValue ("OutputType");
			if (!string.IsNullOrEmpty (outputType)) {
				if (!Enum.TryParse (outputType, out defaultCompileTarget)) {
					defaultCompileTarget = CompileTarget.Library;
				}
			}
		}

		static bool HasOutputType (MSBuildProject project)
		{
			return HasGlobalProperty (project, "OutputType");
		}

		static bool HasGlobalProperty (MSBuildProject project, string name)
		{
			var globalPropertyGroup = project.GetGlobalPropertyGroup ();
			if (globalPropertyGroup != null)
				return globalPropertyGroup.HasProperty (name);

			return false;
		}

		static void RemovePropertyIfHasDefaultValue (
			MSBuildPropertyGroup propertyGroup,
			string propertyName,
			string defaultPropertyValue)
		{
			if (!propertyGroup.HasProperty (propertyName))
				return;

			if (propertyGroup.GetValue (propertyName) == defaultPropertyValue) {
				propertyGroup.RemoveProperty (propertyName);
			}
		}

		static void RemoveExtraProjectReferenceMetadata (MSBuildProject project)
		{
			foreach (MSBuildItem item in project.GetAllItems ()) {
				if (item.Name == "ProjectReference") {
					item.Metadata.RemoveProperty ("Name");
					item.Metadata.RemoveProperty ("Project");
				}
			}
		}

		static IEnumerable<string> GetTargetFrameworks (MSBuildProject project)
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

			return new string [0];
		}

		static void UpdateTargetFrameworks (MSBuildProject project, IEnumerable<string> targetFrameworks)
		{
			var globalPropertyGroup = project.GetGlobalPropertyGroup ();
			if (targetFrameworks.Count () > 1) {
				string value = string.Join (";", targetFrameworks);
				globalPropertyGroup.SetValue ("TargetFrameworks", value);
			} else {
				globalPropertyGroup.SetValue ("TargetFramework", targetFrameworks.FirstOrDefault ());
			}
		}
	}
}
