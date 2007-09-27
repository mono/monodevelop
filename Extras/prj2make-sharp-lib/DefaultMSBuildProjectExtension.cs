//
// DefaultMSBuildProjectExtension.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Projects;

using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public class DefaultMSBuildProjectExtension : MSBuildProjectExtension
	{
		public override string TypeGuid {
			get { return null; }
		}

		public override string Name {
			get { return "DefaultMSBuildProjectExtension"; }
		}

		public override bool Supports (string type_guid, string filename, string type_guids)
		{
			//Should've been handled by now!
			return false;
		}

		public override DotNetProject CreateProject (string type_guid, string filename, string type_guids)
		{
			//Should've been handled by now!
			return null;
		}

		public override void ReadConfig (DotNetProject project, DotNetProjectConfiguration config, XPathNavigator nav, string basePath, IProgressMonitor monitor)
		{
			if (nav.MoveToChild ("OutputType",Utils.ns)) {
				try {
					config.CompileTarget = (CompileTarget) Enum.Parse (typeof (CompileTarget), nav.Value, true);
				} catch (ArgumentException) {
					//Ignore
				}
				nav.MoveToParent ();
			}

			if (nav.MoveToChild ("Configuration",Utils.ns)) {
				//if (CheckNullCondition (nav.UnderlyingObject as XmlElement, "Configuration"))
				//	default_config = nav.Value;

				nav.MoveToParent ();
			}

			if (nav.MoveToChild ("Platform",Utils.ns)) {
				//if (CheckNullCondition (nav.UnderlyingObject as XmlElement, "Platform"))
				//	default_platform = nav.Value;

				nav.MoveToParent ();
			}

			string str_tmp = String.Empty;
			int int_tmp = 0;
			bool bool_tmp = false;

			if (Utils.ReadAsString (nav, "AssemblyName", ref str_tmp, false))
				config.OutputAssembly = Utils.Unescape (str_tmp);

			if (Utils.ReadAsString (nav, "OutputPath", ref str_tmp, false))
				config.OutputDirectory = Utils.MapAndResolvePath (basePath, Utils.Unescape (str_tmp));

			if (Utils.ReadAsBool (nav, "DebugSymbols", ref bool_tmp))
				//FIXME: <DebugType>?
				config.DebugMode = bool_tmp;
		}

		public override void ReadItemGroups (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string basePath, IProgressMonitor monitor)
		{
			//FIXME: This can also be Config/Platform specific
			XmlNodeList itemList = data.Document.SelectNodes ("/tns:Project/tns:ItemGroup", MSBuildFileFormat.NamespaceManager);

			ProjectFile pf;
			ProjectReference pr;
			foreach (XmlNode itemGroup in itemList) {
				foreach (XmlNode node in itemGroup.ChildNodes) {
					if (node.NodeType != XmlNodeType.Element)
						continue;

					if (node.Attributes ["Include"] == null) {
						Console.WriteLine ("Warning: Expected 'Include' attribute not found for ItemGroup '{0}'",
							node.LocalName);
						continue;
					}

					string include = node.Attributes ["Include"].Value;
					if (include.Length == 0)
						//FIXME: Ignore, error??
						return;

					include = Utils.Unescape (include);
					data.ExtensionChain.ReadItemGroup (data, project, globalConfig, include, basePath, node, monitor);
				}
			}
		}

		public override void ReadItemGroup (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string include, string basePath, XmlNode node, IProgressMonitor monitor)
		{
			string path = null;
			ProjectFile pf = null;
			ProjectReference pr = null;

			string str_tmp = String.Empty;
			switch (node.LocalName) {
			case "Reference":
				string hintPath = String.Empty;
				string fullname = Runtime.SystemAssemblyService.GetAssemblyFullName (include);
				if (fullname == null) {
					// Check if the case of the assembly name might be incorrect
					// Eg. System.XML
					int commaPos = include.IndexOf (',');
					string asmname = include;
					string rest = String.Empty;

					if (commaPos >= 0) {
						asmname = include.Substring (0, commaPos).Trim ();
						rest = include.Substring (commaPos);
					}

					if (MSBuildFileFormat.AssemblyNamesTable.ContainsKey (asmname) && asmname != MSBuildFileFormat.AssemblyNamesTable [asmname]) {
						// assembly name is in the table and case is different
						fullname = Runtime.SystemAssemblyService.GetAssemblyFullName (
							MSBuildFileFormat.AssemblyNamesTable [asmname] + rest);
					}
				}
				if ((fullname != null && 
					Runtime.SystemAssemblyService.FindInstalledAssembly (fullname) != null) ||
					!Utils.ReadAsString (node, "HintPath", ref hintPath, false)) {

					//If the assembly is from a package file
					//Or has _no_ HintPath, then add it as a Gac entry
					pr = new ProjectReference (ReferenceType.Gac, fullname ?? include);
					project.ProjectReferences.Add (pr);
				} else {
					//Not in the Gac, has HintPath
					hintPath = Utils.Unescape (hintPath);
					path = Utils.MapAndResolvePath (basePath, hintPath);
					if (path == null) {
						Console.WriteLine (GettextCatalog.GetString (
							"HintPath ({0}) for Reference '{1}' is invalid. Ignoring.",
							hintPath, include));
						monitor.ReportWarning (GettextCatalog.GetString (
							"HintPath ({0}) for Reference '{1}' is invalid. Ignoring.",
							hintPath, include));

						return;
					}

					pr = project.AddReference (path);
				}
				data.ProjectReferenceElements [pr] = (XmlElement) node;

				break;
			case "ProjectReference":
				//Not using @Include currently, instead using the Name
				string projGuid = null;
				string projName = null;

				if (node ["Project"] != null)
					projGuid = node ["Project"].InnerText;
				if (node ["Name"] != null)
					projName = Utils.Unescape (node ["Name"].InnerText);

				if (String.IsNullOrEmpty (projName)) {
					//FIXME: Add support to load the project file from here
					Console.WriteLine ("Expected element <Name> for ProjectReference '{0}'", include);
					return;
				}

				pr = new ProjectReference (ReferenceType.Project, projName);
				project.ProjectReferences.Add (pr);
				data.ProjectReferenceElements [pr] = (XmlElement) node;

				break;
			case "Compile":
				path = Utils.GetValidPath (monitor, basePath, include);
				if (path == null)
					return;
				pf = project.AddFile (path, BuildAction.Compile);
				data.ProjectFileElements [pf] = (XmlElement) node;
				break;
			case "None":
			case "Content":
				//FIXME: We don't support "CopyToOutputDirectory" for
				//other BuildActions
				path = Utils.GetValidPath (monitor, basePath, include);
				if (path == null)
					return;
				if (Utils.ReadAsString (node, "CopyToOutputDirectory", ref str_tmp, false))
					pf = project.AddFile (path, BuildAction.FileCopy);
				else
					pf = project.AddFile (path, BuildAction.Nothing);
				data.ProjectFileElements [pf] = (XmlElement) node;
				break;
			case "EmbeddedResource":
				path = Utils.GetValidPath (monitor, basePath, include);
				if (path == null)
					return;

				/* IResourceBuilder, in this case will use just the
				 * filename to build the resource id. Ignoring <Link> here
				 *
				 * if (!path.StartsWith (project.BaseDirectory)) {
					monitor.ReportWarning (GettextCatalog.GetString (
						"The specified path '{0}' for the EmbeddedResource is outside the project directory. Ignoring.", include));
					Console.WriteLine ("The specified path '{0}' for the EmbeddedResource is outside the project directory. Ignoring.", include);
					continue;
				}*/

				pf = project.AddFile (path, BuildAction.EmbedAsResource);
				if (Utils.ReadAsString (node, "LogicalName", ref str_tmp, false))
					pf.ResourceId = Utils.Unescape (str_tmp);
				data.ProjectFileElements [pf] = (XmlElement) node;
				break;
			default:
				Console.WriteLine ("Unrecognised ItemGroup element '{0}', Include = '{1}'. Ignoring.", node.LocalName, include);
				break;
			}

			if (pf != null) {
				if (Utils.ReadAsString (node, "DependentUpon", ref str_tmp, false)) {
					//DependentUpon is relative to the basedir of the 'pf' (resource file)
					string resolvedPath = Utils.MapAndResolvePath (Path.GetDirectoryName (pf.Name), str_tmp);
					if (resolvedPath != null)
						pf.DependsOn = Utils.Unescape (resolvedPath);
				}

				if (String.Compare (node.LocalName, "Content", true) != 0 &&
					String.Compare (node.LocalName, "None", true) != 0 &&
					Utils.ReadAsString (node, "CopyToOutputDirectory", ref str_tmp, false))
					Console.WriteLine ("Warning: CopyToOutputDirectory not supported for BuildAction '{0}', Include = '{1}'", node.LocalName, include);
			}
		}

		public override void ReadFlavorProperties (MSBuildData data, DotNetProject project, XmlNode node, string guid)
		{
		}

		public override void OnFinishRead (MSBuildData data, DotNetProject project)
		{
		}

		//Writing methods

		public override void WriteConfig (DotNetProject project, DotNetProjectConfiguration config, XmlElement configElement, IProgressMonitor monitor)
		{
			Utils.EnsureChildValue (configElement, "OutputType", config.CompileTarget);
			Utils.EnsureChildValue (configElement, "AssemblyName", Utils.CanonicalizePath (config.OutputAssembly));
			// VS2005 emits trailing \\ for folders
			Utils.EnsureChildValue (configElement, "OutputPath", 
				Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
					project.BaseDirectory, config.OutputDirectory)) + "\\");
			Utils.EnsureChildValue (configElement, "DebugSymbols", config.DebugMode);
		}

		public override XmlElement FileToXmlElement (MSBuildData data, Project project, ProjectFile projectFile)
		{
			if (projectFile.BuildAction == BuildAction.Compile && projectFile.Subtype != Subtype.Code)
				return null;

			string name = MSBuildFileFormat.BuildActionToString (projectFile.BuildAction);
			if (name == null) {
				Runtime.LoggingService.WarnFormat ("BuildAction.{0} not supported!", projectFile.BuildAction);
				Console.WriteLine ("BuildAction.{0} not supported!", projectFile.BuildAction);
				return null;
			}

			//FIXME: Subtype

			bool newElement = false;
			XmlDocument doc = data.Document;
			XmlElement elem;
			if (!data.ProjectFileElements.TryGetValue (projectFile, out elem)) {
				newElement = true;
				elem = doc.CreateElement (name, Utils.ns);
				XmlNode n = doc.SelectSingleNode (String.Format (
					"/tns:Project/tns:ItemGroup/tns:{0}", name), MSBuildFileFormat.NamespaceManager);

				if (n == null) {
					n = doc.CreateElement ("ItemGroup", Utils.ns);
					doc.DocumentElement.AppendChild (n);
					n.AppendChild (elem);
				} else {
					n.ParentNode.AppendChild (elem);
				}

				bool notMSBuild = (Utils.GetMSBuildData (project) == null);
				if (projectFile.BuildAction == BuildAction.EmbedAsResource &&
					(notMSBuild || Services.ProjectService.GetDefaultResourceId (projectFile) != projectFile.ResourceId)) {
					//Emit LogicalName if we are writing elements for a Non-MSBuidProject,
					//  (eg. when converting a gtk-sharp project, it might depend on non-vs
					//  style resource naming )
					//Or when the resourceId is different from the default one
					Utils.EnsureChildValue (elem, "LogicalName", Utils.Escape (projectFile.ResourceId));

					if (notMSBuild)
						// explicitly set the resourceId, as once when it becomes a
						// msbuild project, .ResourceId will give resourceId by msbuild
						// rules, but we want to retain this value
						projectFile.ResourceId = projectFile.ResourceId;
				}

				if (projectFile.BuildAction == BuildAction.FileCopy)
					Utils.EnsureChildValue (elem, "CopyToOutputDirectory", "Always");

				if (projectFile.IsExternalToProject)
					Utils.EnsureChildValue (elem, "Link", Path.GetFileName (projectFile.Name));
			}

			elem.SetAttribute ("Include", Utils.CanonicalizePath (projectFile.RelativePath));

			if (projectFile.BuildAction == BuildAction.EmbedAsResource) {
				string projectResourceId = projectFile.ResourceId;

				if (!newElement) {
					if (Services.ProjectService.GetDefaultResourceId (projectFile) == projectResourceId)
						Utils.RemoveChild (elem, "LogicalName");
					else
						Utils.EnsureChildValue (elem, "LogicalName", Utils.Escape (projectResourceId));
				}

				//DependentUpon is relative to the basedir of the 'pf' (resource file)
				if (String.IsNullOrEmpty (projectFile.DependsOn)) {
					if (!newElement)
						Utils.RemoveChild (elem, "DependentUpon");
				} else {
					Utils.EnsureChildValue (elem, "DependentUpon",
						Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
							Path.GetDirectoryName (projectFile.Name), projectFile.DependsOn)));
				}
			}

			return elem;
		}

		public override XmlElement ReferenceToXmlElement (MSBuildData data, Project project, ProjectReference projectRef)
		{
			ReferenceType refType = projectRef.ReferenceType;

			bool newElement = false;
			XmlDocument doc = data.Document;
			XmlElement elem;
			if (!data.ProjectReferenceElements.TryGetValue (projectRef, out elem)) {
				string elemName;
				if (refType == ReferenceType.Project)
					elemName = "ProjectReference";
				else
					elemName = "Reference";

				elem = doc.CreateElement (elemName, Utils.ns);
				newElement = true;

				//Add the element to the document
				XmlNode node = doc.SelectSingleNode (String.Format ("/tns:Project/tns:ItemGroup/tns:{0}", elemName), MSBuildFileFormat.NamespaceManager);
				if (node == null) {
					node = doc.CreateElement ("ItemGroup", Utils.ns);
					doc.DocumentElement.AppendChild (node);
					node.AppendChild (elem);
				} else {
					node.ParentNode.AppendChild (elem);
				}
			}

			string reference = projectRef.Reference;
			switch (refType) {
			case ReferenceType.Gac:
				SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (projectRef.Reference);
				if (pkg != null && pkg.IsCorePackage && pkg.TargetVersion == ClrVersion.Net_2_0)
					// For core references like System.Data, emit only "System.Data" instead
					// of full names
					reference = reference.Substring (0, reference.IndexOf (','));
				break;
			case ReferenceType.Assembly:
				string asmname = null;
				try {
					asmname = AssemblyName.GetAssemblyName (reference).ToString ();
					reference = asmname;
				} catch (FileNotFoundException) {
				} catch (BadImageFormatException) {
				} catch (ArgumentException) {
				}

				if (asmname == null) {
					//Couldn't get assembly name
					if (!newElement && elem.Attributes ["Include"] != null)
						reference = elem.Attributes ["Include"].Value;
					else
						reference = Path.GetFileNameWithoutExtension (reference);
				}

				Utils.EnsureChildValue (elem, "HintPath",
					Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (project.BaseDirectory, projectRef.Reference)));
				Utils.EnsureChildValue (elem, "SpecificVersion", "False");
				break;
			case ReferenceType.Project:
				Combine c = project.RootCombine;
				if (c != null) {
					Project p = c.FindProject (projectRef.Reference);
					if (p == null) {
						Runtime.LoggingService.WarnFormat (GettextCatalog.GetString (
							"The project '{0}' referenced from '{1}' could not be found.",
							projectRef.Reference, project.Name));

						Console.WriteLine (GettextCatalog.GetString (
							"The project '{0}' referenced from '{1}' could not be found.",
							projectRef.Reference, project.Name));

						return elem;
					}

					reference = Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						project.BaseDirectory, p.FileName));

					if (p.ExtendedProperties.Contains (typeof (MSBuildFileFormat))) {
						MSBuildData d = (MSBuildData) p.ExtendedProperties [typeof (MSBuildFileFormat)];
						if (d.Guid != null & d.Guid.Length != 0)
							Utils.EnsureChildValue (elem, "Project", d.Guid);
					}

					if (newElement)
						//Set Name only for newly created elements, this could be
						//different from referenced project's Name
						Utils.EnsureChildValue (elem, "Name", Utils.Escape (p.Name));
				}
				break;
			case ReferenceType.Custom:
				break;
			}

			elem.SetAttribute ("Include", Utils.Escape (reference));

			return elem;
		}

		public override void OnFinishWrite (MSBuildData data, DotNetProject project)
		{
		}

		public override string GetGuidChain (DotNetProject project)
		{
			//Shouldn't get here!
			return null;
		}

	}
}
