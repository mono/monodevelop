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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreMSBuildProject
	{
		List<string> targetFrameworks;
		bool hasRootNamespace;
		bool hasAssemblyName;
		bool hasDescription;

		public string ToolsVersion { get; private set; }
		public bool IsOutputTypeDefined { get; private set; }
		public string Sdk { get; set; }

		public IEnumerable<string> TargetFrameworks {
			get { return targetFrameworks; }
		}

		public bool HasSdk {
			get { return Sdk != null; }
		}

		public void ReadProject (MSBuildProject project)
		{
			ToolsVersion = project.ToolsVersion;
			IsOutputTypeDefined = project.IsOutputTypeDefined ();
			targetFrameworks = project.GetTargetFrameworks ().ToList ();
			hasRootNamespace = project.HasGlobalProperty ("RootNamespace");
			hasAssemblyName = project.HasGlobalProperty ("AssemblyName");
			hasDescription = project.HasGlobalProperty ("Description");
		}

		public void WriteProject (MSBuildProject project)
		{
			var globalPropertyGroup = project.GetGlobalPropertyGroup ();
			globalPropertyGroup.RemoveProperty ("ProjectGuid");
			globalPropertyGroup.RemoveProperty ("TargetFrameworkIdentifier");
			globalPropertyGroup.RemoveProperty ("TargetFrameworkVersion");

			if (!IsOutputTypeDefined)
				globalPropertyGroup.RemoveProperty ("OutputType");

			if (!hasAssemblyName)
				globalPropertyGroup.RemoveProperty ("AssemblyName");

			if (!hasRootNamespace)
				globalPropertyGroup.RemoveProperty ("RootNamespace");

			if (!hasDescription)
				globalPropertyGroup.RemoveProperty ("Description");

			project.DefaultTargets = null;

			if (!string.IsNullOrEmpty (ToolsVersion))
				project.ToolsVersion = ToolsVersion;

			if (HasSdk) {
				project.RemoveInternalElements ();
			}
		}

		public bool AddInternalSdkImports (MSBuildProject project, DotNetCoreSdkPaths sdkPaths)
		{
			return AddInternalSdkImports (project, sdkPaths.MSBuildSDKsPath, sdkPaths.ProjectImportProps, sdkPaths.ProjectImportTargets);
		}

		public bool AddInternalSdkImports (MSBuildProject project, string sdkPath, string sdkProps, string sdkTargets)
		{
			if (project.ImportExists (sdkProps))
				return false;

			if (Sdk == "Microsoft.NET.Sdk.Web") {
				// HACK: Add wildcard items to the project since they are not currently evaluated
				// properly which results in no files being displayed in the solution window.
				project.AddWebProjectWildcardItems ();
			} else {
				project.AddProjectWildcardItems ();
			}

			// HACK: The Sdk imports for web projects use the MSBuildSdksPath property to find
			// other files to import. So we define this in a property group at the top of the
			// project before the Sdk.props is imported so these other files can be found.
			MSBuildImport propsImport = project.AddInternalImport (sdkProps, importAtTop: true);
			project.AddInternalImport (sdkTargets);
			project.AddInternalPropertyBefore ("MSBuildSdksPath", sdkPath, propsImport);

			return true;
		}
	}
}
