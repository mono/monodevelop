//
// SharedProjectMSBuildHandler.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc
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
using System.Linq;
using System.Xml;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects.SharedAssetsProjects
{
	class SharedAssetsProjectMSBuildHandler: MSBuildProjectHandler
	{
		string projitemsFile;

		public SharedAssetsProjectMSBuildHandler ()
		{
			// Shared projects use msbuild by default. Without this, referencing projects assume that a SAP
			// doesn't support msbuild and they fall back to MD's build system.
			UseMSBuildEngineByDefault = true;
		}

		protected override void LoadProject (IProgressMonitor monitor, MSBuildProject msproject)
		{
			var doc = msproject.Document;
			projitemsFile = null;
			foreach (var no in doc.DocumentElement.ChildNodes) {
				var im = no as XmlElement;
				if (im != null && im.LocalName == "Import" && im.GetAttribute ("Label") == "Shared") {
					projitemsFile = im.GetAttribute ("Project");
					break;
				}
			}
			if (projitemsFile == null)
				return;

			// TODO: load the type from msbuild
			((SharedAssetsProject)EntityItem).LanguageName = "C#";

			projitemsFile = Path.Combine (Path.GetDirectoryName (msproject.FileName), projitemsFile);

			MSBuildProject p = new MSBuildProject ();
			p.Load (projitemsFile);

			MSBuildSerializer ser = CreateSerializer ();
			ser.SerializationContext.BaseFile = EntityItem.FileName;
			ser.SerializationContext.ProgressMonitor = monitor;

			((SharedAssetsProject)Item).ProjItemsPath = projitemsFile;
			Item.SetItemHandler (this);

			var cp = p.PropertyGroups.FirstOrDefault (g => g.Label == "Configuration");
			if (cp != null)
				((SharedAssetsProject)EntityItem).DefaultNamespace = cp.GetPropertyValue ("Import_RootNamespace");

			LoadProjectItems (p, ser, ProjectItemFlags.None);
		}

		protected override MSBuildProject SaveProject (IProgressMonitor monitor)
		{
			MSBuildSerializer ser = CreateSerializer ();
			ser.SerializationContext.BaseFile = EntityItem.FileName;
			ser.SerializationContext.ProgressMonitor = monitor;

			MSBuildProject projitemsProject = new MSBuildProject ();
			MSBuildProject msproject = new MSBuildProject ();

			var newProject = EntityItem.FileName == null || !File.Exists (EntityItem.FileName);
			if (newProject) {
				var grp = msproject.GetGlobalPropertyGroup ();
				if (grp == null)
					grp = msproject.AddNewPropertyGroup (false);
				grp.SetPropertyValue ("ProjectGuid", EntityItem.ItemId, false);
				var import = msproject.AddNewImport (@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props");
				import.Condition = @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')";
				msproject.AddNewImport (@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.Default.props");
				msproject.AddNewImport (@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.props");
				import = msproject.AddNewImport (Path.ChangeExtension (EntityItem.FileName.FileName, ".projitems"));
				import.Label = "Shared";
				msproject.AddNewImport (@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.CSharp.targets");
			} else {
				msproject.Load (EntityItem.FileName);
			}

			// having no ToolsVersion is equivalent to 2.0, roundtrip that correctly
			if (ToolsVersion != "2.0")
				msproject.ToolsVersion = ToolsVersion;
			else if (string.IsNullOrEmpty (msproject.ToolsVersion))
				msproject.ToolsVersion = null;
			else
				msproject.ToolsVersion = "2.0";

			if (projitemsFile == null)
				projitemsFile = ((SharedAssetsProject)Item).ProjItemsPath;
			if (File.Exists (projitemsFile)) {
				projitemsProject.Load (projitemsFile);
			} else {
				var grp = projitemsProject.AddNewPropertyGroup (true);
				grp.SetPropertyValue ("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)", false);
				grp.SetPropertyValue ("HasSharedItems", "true", false);
				grp.SetPropertyValue ("SharedGUID", EntityItem.ItemId, false);
			}

			var configGrp = projitemsProject.PropertyGroups.FirstOrDefault (g => g.Label == "Configuration");
			if (configGrp == null) {
				configGrp = projitemsProject.AddNewPropertyGroup (true);
				configGrp.Label = "Configuration";
			}
			configGrp.SetPropertyValue ("Import_RootNamespace", ((SharedAssetsProject)EntityItem).DefaultNamespace, false);

			SaveProjectItems (monitor, new MSBuildFileFormatVS12 (), ser, projitemsProject, "$(MSBuildThisFileDirectory)");

			projitemsProject.Save (projitemsFile);

			return msproject;
		}
	}
}

