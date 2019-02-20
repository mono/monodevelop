//
// DotNetCoreMSBuildProject.cs
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
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreMSBuildProject
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
			IsOutputTypeDefined = project.IsOutputTypeDefined ();
			targetFrameworks = project.GetTargetFrameworks ().ToList ();
			hasRootNamespace = project.HasGlobalProperty ("RootNamespace");
			hasAssemblyName = project.HasGlobalProperty ("AssemblyName");
			hasDescription = project.HasGlobalProperty ("Description");

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
				globalPropertyGroup.RemovePropertyIfHasDefaultValue ("Description", "Package Description");

			project.DefaultTargets = null;

			project.RemoveExtraProjectReferenceMetadata ();

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
				globalPropertyGroup.RemovePropertyIfHasDefaultValue ("OutputType", outputType);
			}
		}

		void RemoveMSBuildProjectNameDerivedProperties (MSBuildPropertyGroup globalPropertyGroup)
		{
			string msbuildProjectName = globalPropertyGroup.ParentProject.FileName.FileNameWithoutExtension;

			if (!hasAssemblyName)
				globalPropertyGroup.RemovePropertyIfHasDefaultValue ("AssemblyName", msbuildProjectName);

			if (!hasRootNamespace)
				globalPropertyGroup.RemovePropertyIfHasDefaultValue ("RootNamespace", msbuildProjectName);
		}

		void UpdateTargetFramework (MSBuildProject project, TargetFrameworkMoniker framework)
		{
			if (targetFrameworkMoniker == framework)
				return;

			string shortFrameworkName = null;
			DotNetCoreShortTargetFramework shortFramework = null;

			string existingFramework = targetFrameworks.FirstOrDefault ();
			bool identifiersMatch = targetFrameworkMoniker.Identifier == framework.Identifier;

			if (identifiersMatch && DotNetCoreShortTargetFramework.TryParse (existingFramework, out shortFramework)) {
				shortFramework.Update (framework);
				shortFrameworkName = shortFramework.ToString ();
			} else {
				shortFrameworkName = framework.GetShortFrameworkName ();
			}

			if (existingFramework == shortFrameworkName)
				return;

			if (targetFrameworks.Count == 0)
				targetFrameworks.Add (shortFrameworkName);
			else
				targetFrameworks[0] = shortFrameworkName;

			targetFrameworkMoniker = framework;
			project.UpdateTargetFrameworks (targetFrameworks);
		}

		public void AddKnownItemAttributes (MSBuildProject project)
		{
			if (HasSdk)
				ProjectPackageReference.AddKnownItemAttributes (project);
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
	}
}
