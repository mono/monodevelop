//
// MSBuildFileFormat.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using VBBinding;
using CSharpBinding;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public class MSBuildFileFormat : IFileFormat
	{
		string language;
		internal const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";

		static XmlNamespaceManager manager;

		public MSBuildFileFormat ()
		{
		}

		public MSBuildFileFormat (string language)
		{
			this.language = language;
		}

		public string Name {
			get { return "MSBuild project"; }
		}

		public string GetValidFormatName (string fileName)
		{
			if (language == null || language == "C#")
				//default
				return Path.ChangeExtension (fileName, ".csproj");
			if (language == "VBNet")
				return Path.ChangeExtension (fileName, ".vbproj");
			return fileName;
		}

		public bool CanReadFile (string file)
		{
			return (GetLanguage (file) != null);
		}

		public bool CanWriteFile (object obj)
		{
			return obj is DotNetProject;
		}

		static XmlNamespaceManager NamespaceManager {
			get {
				if (manager == null) {
					manager = new XmlNamespaceManager (new NameTable ());
					manager.AddNamespace ("tns", ns);
				}

				return manager;
			}
		}

		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
			if (node == null)
				return;

			DotNetProject project = node as DotNetProject;
			if (project == null)
				//FIXME: Argument exception?
				return;
			
			string tmpfilename = String.Empty;
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving project: {0}", file), 1);
				try {
					if (File.Exists (file))
						tmpfilename = Path.GetTempFileName ();
				} catch (IOException) {
				}

				if (tmpfilename == String.Empty) {
					WriteFileInternal (file, project, monitor);
				} else {
					WriteFileInternal (tmpfilename, project, monitor);
					Runtime.FileService.DeleteFile (file);
					Runtime.FileService.MoveFile (tmpfilename, file);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save project: {0}", file), ex);
				Console.WriteLine ("Could not save project: {0}, {1}", file, ex);

				if (tmpfilename != String.Empty)
					Runtime.FileService.DeleteFile (tmpfilename);
				throw;
			} finally {
				monitor.EndTask ();
			}
		}

		void WriteFileInternal (string file, DotNetProject project, IProgressMonitor monitor)
		{
			string platform = "AnyCPU";
			bool newdoc = false;
			XmlDocument doc = null;

			MSBuildData data = (MSBuildData) project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (data == null) {
				//Create a new XmlDocument
				doc = new XmlDocument ();
				data = new MSBuildData ();
				data.Document = doc;
				project.ExtendedProperties [typeof (MSBuildFileFormat)] = data;
				newdoc = true;

				XmlElement e = doc.CreateElement ("Project", ns);
				doc.AppendChild (e);
				e.SetAttribute ("DefaultTargets", "Build");
			} else {
				doc = data.Document;
			}

			//Write out the Configurations
			//FIXME: Not touching global config right now,
			//instead just writing out everything in the individual configs
			XmlElement globalConfigElement = data.GlobalConfigElement;
			if (globalConfigElement == null) {
				globalConfigElement = doc.CreateElement ("PropertyGroup", ns);
				doc.DocumentElement.AppendChild (globalConfigElement);

				data.GlobalConfigElement = globalConfigElement;
				data.Guid = Guid.NewGuid ().ToString ().ToUpper ();

				//FIXME: EnsureChildValue for AssemblyName <-> OutputAssembly
				//	Get this from where? different configs could have different ones.. 
			}

			EnsureChildValue (globalConfigElement, "ProjectGuid", ns, 
				String.Concat ("{", data.Guid, "}"));

			//Default Config and platform
			//Note: Ignoring this, not relevant for MD, but might be useful for prj2make
			//For new projects, adding these elements in SaveProject
			//
			//string [] defaultActivePlatform = GetConfigPlatform (project.ActiveConfiguration.Name);
			//SetForNullCondition (doc, globalConfigElement, "Configuration", defaultActivePlatform [0]);
			//SetForNullCondition (doc, globalConfigElement, "Platform", defaultActivePlatform [1]);

			foreach (DotNetProjectConfiguration config in project.Configurations) {
				XmlElement configElement = null;

				if (data.ConfigElements.ContainsKey (config)) {
					configElement = data.ConfigElements [config];
				} else {
					//Create node for new configuration
					configElement = doc.CreateElement ("PropertyGroup", ns);
					doc.DocumentElement.AppendChild (configElement);

					string [] t = GetConfigPlatform (config.Name);
					configElement.SetAttribute ("Condition", 
						String.Format (" '$(Configuration)|$(Platform)' == '{0}|{1}' ", t [0], t [1]));
					data.ConfigElements [config] = configElement;
				}

				EnsureChildValue (configElement, "OutputType", ns, config.CompileTarget);
				EnsureChildValue (configElement, "AssemblyName", ns, config.OutputAssembly);
				EnsureChildValue (configElement, "OutputPath", ns, 
					Runtime.FileService.AbsoluteToRelativePath (project.BaseDirectory, config.OutputDirectory));
				EnsureChildValue (configElement, "DebugSymbols", ns, config.DebugMode);

				if (project.LanguageName == "VBNet") {
					VBCompilerParameters vbparams = 
						(VBCompilerParameters) config.CompilationParameters;

					EnsureChildValue (configElement, "RootNamespace", ns, vbparams.RootNamespace);
					EnsureChildValue (configElement, "AllowUnsafeBlocks", ns, vbparams.UnsafeCode);
					EnsureChildValue (configElement, "Optimize", ns, vbparams.Optimize);
					EnsureChildValue (configElement, "CheckForOverflowUnderflow", ns, vbparams.GenerateOverflowChecks);
					EnsureChildValue (configElement, "DefineConstants", ns, vbparams.DefineSymbols);
					EnsureChildValue (configElement, "WarningLevel", ns, vbparams.WarningLevel);
					EnsureChildValue (configElement, "OptionExplicit", ns, vbparams.OptionExplicit);
					EnsureChildValue (configElement, "OptionStrict", ns, vbparams.OptionStrict);
					if (vbparams.Win32Icon != null && vbparams.Win32Icon.Length > 0)
						EnsureChildValue (configElement, "ApplicationIcon", ns,
							Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, vbparams.Win32Icon));

					if (vbparams.Win32Resource != null && vbparams.Win32Resource.Length > 0)
						EnsureChildValue (configElement, "Win32Resource", ns,
							Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, vbparams.Win32Resource));

					//FIXME: VB.net Imports
				}

				if (project.LanguageName == "C#") {
					CSharpCompilerParameters csparams =
						(CSharpCompilerParameters) config.CompilationParameters;

					EnsureChildValue (configElement, "AllowUnsafeBlocks", ns, csparams.UnsafeCode);
					EnsureChildValue (configElement, "Optimize", ns, csparams.Optimize);
					EnsureChildValue (configElement, "CheckForOverflowUnderflow", ns, csparams.GenerateOverflowChecks);
					EnsureChildValue (configElement, "DefineConstants", ns, csparams.DefineSymbols);
					EnsureChildValue (configElement, "WarningLevel", ns, csparams.WarningLevel);
					if (csparams.Win32Icon != null && csparams.Win32Icon.Length > 0)
						EnsureChildValue (configElement, "ApplicationIcon", ns,
							Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, csparams.Win32Icon));

					if (csparams.Win32Resource != null && csparams.Win32Resource.Length > 0)
						EnsureChildValue (configElement, "Win32Resource", ns,
							Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, csparams.Win32Resource));
				}
			}

			//FIXME: Set ActiveConfiguration

			CleanUpEmptyItemGroups (doc);
			
			if (newdoc) {
				//MUST go at the end.. 
				XmlElement el = doc.CreateElement ("Import", ns);
				doc.DocumentElement.InsertAfter (el, doc.DocumentElement.LastChild);
				el.SetAttribute ("Project", @"$(MSBuildBinPath)\Microsoft.CSharp.Targets");
			}

			doc.Save (file);

			return;
		}

		/* Finds an element named @elementName, with a attribute Condition, which has "$(@elementName) = ''"
		 * and sets @value for that. Creates the element if its not found. */
		void SetForNullCondition (XmlDocument doc, XmlElement configElement, string elementName, string value)
		{
			XmlNodeList list = doc.SelectNodes (String.Format (
					"/tns:Project/tns:PropertyGroup/tns:{0}[@Condition]", elementName),
					NamespaceManager);
			foreach (XmlNode node in list) {
				if (CheckNullCondition (node as XmlElement, elementName)) {
					node.InnerText = value;
					return;
				}
			}

			//Add new xml element for active config
			XmlElement elem = doc.CreateElement (elementName, ns);
			configElement.AppendChild (elem);
			elem.InnerText = value;

			elem.SetAttribute ("Condition", " '$(" + elementName + ")' == '' ");
		}

		bool CheckNullCondition (XmlElement elem, string varName)
		{
			if (elem == null)
				return false;

			//FIXME: This will get instantiated repeatedly, save this
			Regex regex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
			StringDictionary dic = ParseCondition (
					regex,
					elem.Attributes ["Condition"].Value);

			string varUpper = varName.ToUpper ();
			if (dic.Keys.Count == 1 && 
				dic.ContainsKey (varUpper) && String.IsNullOrEmpty (dic [varUpper])) {
				// Eg. '$(Configuration)' == ''
				return true;
			}

			return false;
		}

		void CleanUpEmptyItemGroups (XmlDocument doc)
		{
			XmlNodeList list = doc.SelectNodes ("/tns:Project/tns:ItemGroup[count(child)=0]", NamespaceManager);
			List<XmlNode> del = new List<XmlNode> ();
			foreach (XmlNode n in list) {
				if (!n.HasChildNodes)
					del.Add (n);
			}

			foreach (XmlNode n in del)
				n.ParentNode.RemoveChild (n);
		}

		public void SaveProject (DotNetProject project, IProgressMonitor monitor)
		{
			WriteFile (project.FileName, project, monitor);

			MSBuildData d = (MSBuildData) project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null)
				throw new Exception (String.Format ("INTERNAL ERROR: 'data' object not found for {0}", project.Name));

			foreach (ProjectFile pfile in project.ProjectFiles)
				d.ProjectFileElements [pfile] = FileToXmlElement (d, project, pfile);

			foreach (ProjectReference pref in project.ProjectReferences)
				d.ProjectReferenceElements [pref] = ReferenceToXmlElement (d, project, pref);

			XmlElement elem = d.Document.CreateElement ("Configuration", ns);
			d.GlobalConfigElement.AppendChild (elem);
			elem.InnerText = "Debug";
			elem.SetAttribute ("Condition", " '$(Configuration)' == '' ");

			elem = d.Document.CreateElement ("Platform", ns);
			d.GlobalConfigElement.AppendChild (elem);
			elem.InnerText = "AnyCPU";
			elem.SetAttribute ("Condition", " '$(Platform)' == '' ");

			SetupHandlers (project);
			d.Document.Save (project.FileName);
		}

		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			Project project = null;
			if (monitor == null || fileName == null)
				//FIXME: Use NullProgressMonitor for monitor?
				return null;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Loading project: {0}", fileName), 1);
				project = LoadProject (fileName, monitor);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load project: {0}", fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
			}

			return project;
		}

		//FIXME: Use monitor to report warnings/errors
		DotNetProject LoadProject (string fname, IProgressMonitor monitor)
		{
			FileStream fs = new FileStream (fname, FileMode.Open, FileAccess.Read);
			XmlDocument doc = new XmlDocument ();
			doc.Load (fs);
			fs.Close ();

			XPathNavigator nav = doc.CreateNavigator ();
			nav.MoveToFirstChild ();

			if (nav.NamespaceURI != ns)
				throw new UnknownProjectVersionException (fname, nav.NamespaceURI);

			//Resolve ../'s 
			fname = Path.GetFullPath (fname);
			string lang = GetLanguage (fname);
			string basePath = Path.GetDirectoryName (fname);

			//Create the project
			MSBuildProject project = new MSBuildProject (lang);
			project.FileName = fname;
			project.Version = "0.1"; //FIXME:
			//Default project name
			project.Name = Path.GetFileNameWithoutExtension (fname);
			project.FileFormat = new MSBuildFileFormat (lang);
			project.ClrVersion = ClrVersion.Net_2_0;

			MSBuildData data = new MSBuildData ();
			data.Document = doc;
			project.ExtendedProperties [typeof (MSBuildFileFormat)] = data;

			//Read the global config
			XPathNodeIterator iter = nav.Select ("/tns:Project/tns:PropertyGroup[not(@Condition)]", NamespaceManager);

			DotNetProjectConfiguration globalConfig = (DotNetProjectConfiguration) project.CreateConfiguration ("Temp");
			globalConfig.ClrVersion = ClrVersion.Net_2_0;

			string str_tmp = String.Empty;
			string default_config = String.Empty;
			string default_platform = "AnyCPU";
			string guid = null;
			string rootNamespace = String.Empty;
			while (iter.MoveNext ()) {
				if (guid == null && 
					ReadAsString (iter.Current, "ProjectGuid", ref str_tmp, false))
					guid = str_tmp;

				ReadConfig (iter.Current, globalConfig, project.LanguageName, basePath, ref default_config, ref default_platform);
				//FIXME: Handle case when >1 global PropertyGroups exist,
				data.GlobalConfigElement = (XmlElement) iter.Current.UnderlyingObject;

				//FIXME: RootNamespace can be specified per-config, but we are 
				//taking the first occurrence
				if (rootNamespace == String.Empty &&
					ReadAsString (iter.Current, "RootNamespace", ref str_tmp, false)) {
					rootNamespace = str_tmp;
				}
			}
			project.DefaultNamespace = rootNamespace;

			if (guid != null)
				data.Guid = guid.Trim (new char [] {'{', '}'});

			//ReadItemGroups : References, Source files etc
			ReadItemGroups (data, project, globalConfig, basePath, monitor);

			//Load configurations
			iter = nav.Select ("/tns:Project/tns:PropertyGroup[@Condition]", NamespaceManager);
			Regex regex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
			while (iter.MoveNext ()) {
				string tmp = String.Empty;
				string tmp2 = String.Empty;
				StringDictionary dic = ParseCondition (
						regex, 
						iter.Current.GetAttribute ("Condition", NamespaceManager.DefaultNamespace));

				string configname = GetConfigName (dic);
				if (configname == null)
					continue;

				DotNetProjectConfiguration config = 
					(DotNetProjectConfiguration) project.GetConfiguration (configname);

				if (config == null) {
					config = (DotNetProjectConfiguration) globalConfig.Clone ();
					config.Name = configname;

					project.Configurations.Add (config);
				}

				ReadConfig (iter.Current, config, project.LanguageName, basePath, ref tmp, ref tmp2);

				data.ConfigElements [config] = (XmlElement) iter.Current.UnderlyingObject;
			}

			/* Note: Ignoring this, not required for MD, but might be useful in prj2make
			string confname = default_config + "|" + default_platform;
			if (project.Configurations [confname] != null)
				project.ActiveConfiguration = project.Configurations [confname]; */

			SetupHandlers (project);

			return project;
		}

		static void SetupHandlers (DotNetProject project)
		{
			//Setup handlers
			//References
			project.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (HandleReferenceRemoved);
			project.ReferenceAddedToProject += new ProjectReferenceEventHandler (HandleReferenceAdded);

			//Files
			project.FileRemovedFromProject += new ProjectFileEventHandler (HandleFileRemoved);
			project.FileAddedToProject += new ProjectFileEventHandler (HandleFileAdded);
			project.FilePropertyChangedInProject += new ProjectFileEventHandler (HandleFilePropertyChanged);
			project.FileRenamedInProject += new ProjectFileRenamedEventHandler (HandleFileRenamed);

			//Configurations
			project.ConfigurationRemoved += new ConfigurationEventHandler (HandleConfigurationRemoved);

			project.NameChanged += new CombineEntryRenamedEventHandler (HandleRename);
		}

		static void HandleRename (object sender, CombineEntryRenamedEventArgs e)
		{
			if (e.CombineEntry.ParentCombine == null)
				//Ignore if the project is not yet a part of a Combine
				return;

			string oldfname = e.CombineEntry.FileName;
			string extn = Path.GetExtension (oldfname);
			string dir = Path.GetDirectoryName (oldfname);
			string newfname = Path.Combine (dir, e.NewName + extn);

			Runtime.FileService.MoveFile (oldfname, newfname);
			e.CombineEntry.FileName = newfname;
		}

		//Event handlers
		static void HandleConfigurationRemoved (object sender, ConfigurationEventArgs e)
		{
			DotNetProject project = (DotNetProject) sender;
			MSBuildData d = (MSBuildData) project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null || !d.ConfigElements.ContainsKey ((DotNetProjectConfiguration) e.Configuration))
				return;

			XmlElement elem = d.ConfigElements [(DotNetProjectConfiguration)e.Configuration];
			elem.ParentNode.RemoveChild (elem);
			d.ConfigElements.Remove ((DotNetProjectConfiguration)e.Configuration);
		}

		//References

		static void HandleReferenceRemoved (object sender, ProjectReferenceEventArgs e)
		{
			MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null || !d.ProjectReferenceElements.ContainsKey (e.ProjectReference))
				return;

			XmlElement elem = d.ProjectReferenceElements [e.ProjectReference];
			elem.ParentNode.RemoveChild (elem);
			d.ProjectReferenceElements.Remove (e.ProjectReference);
		}

		static void HandleReferenceAdded (object sender, ProjectReferenceEventArgs e)
		{
			try {
				MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
				if (d == null)
					return;

				d.ProjectReferenceElements [e.ProjectReference] = 
					ReferenceToXmlElement (d, e.Project, e.ProjectReference);
			} catch (Exception ex) {
				Runtime.LoggingService.ErrorFormat ("{0}", ex.Message);
				Console.WriteLine ("{0}", ex.ToString ());
			}
		}

		internal static XmlElement ReferenceToXmlElement (MSBuildData d, Project project, ProjectReference projectRef)
		{
			ReferenceType refType = projectRef.ReferenceType;

			string elemName;
			if (refType == ReferenceType.Project)
				elemName = "ProjectReference";
			else
				elemName = "Reference";

			XmlDocument doc = d.Document;
			XmlElement elem = doc.CreateElement (elemName, ns);

			//Add the element to the document
			XmlNode node = doc.SelectSingleNode (String.Format ("/tns:Project/tns:ItemGroup/tns:{0}", elemName), NamespaceManager);
			if (node == null) {
				node = doc.CreateElement ("ItemGroup", ns);
				doc.DocumentElement.AppendChild (node);
				node.AppendChild (elem);
			} else {
				node.ParentNode.AppendChild (elem);
			}

			string reference = projectRef.Reference;
			switch (refType) {
			case ReferenceType.Gac:
				break;
			case ReferenceType.Assembly:
				//FIXME: netmodule? no assembly manifest?
				reference = Mono.Cecil.AssemblyFactory.GetAssembly (reference).Name.ToString ();

				AppendChild (elem, "HintPath", ns, 
					Runtime.FileService.AbsoluteToRelativePath (project.BaseDirectory, projectRef.Reference));
				AppendChild (elem, "SpecificVersion", ns, "False");
				break;
			case ReferenceType.Project:
				Combine c = project.RootCombine;
				if (c != null) {
					Project p = c.FindProject (projectRef.Reference);
					//FIXME: if (p == null) : This should not happen!
					reference = Runtime.FileService.AbsoluteToRelativePath (
						project.BaseDirectory, p.FileName);

					if (p.ExtendedProperties.Contains (typeof (MSBuildFileFormat))) {
						MSBuildData data = (MSBuildData) p.ExtendedProperties [typeof (MSBuildFileFormat)];
						if (data.Guid != null & data.Guid.Length != 0)
							EnsureChildValue (elem, "Project", ns, String.Concat ("{", data.Guid, "}"));
					}

					AppendChild (elem, "Name", ns, p.Name);
				}
				break;
			case ReferenceType.Custom:
				break;
			}

			//Add the Include attribute
			elem.SetAttribute ("Include", reference);

			return elem;
		}

		//ProjectFile-s

		static void HandleFileRemoved (object sender, ProjectFileEventArgs e)
		{
			MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			XmlElement elem = d.ProjectFileElements [e.ProjectFile];
			elem.ParentNode.RemoveChild (elem);
			d.ProjectFileElements.Remove (e.ProjectFile);
		}

		static void HandleFileAdded (object sender, ProjectFileEventArgs e)
		{
			MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null)
				return;

			d.ProjectFileElements [e.ProjectFile] = FileToXmlElement (d, e.Project, e.ProjectFile);
		}

		static XmlElement FileToXmlElement (MSBuildData d, Project project, ProjectFile projectFile)
		{
			string name = BuildActionToString (projectFile.BuildAction);
			if (name == null) {
				Runtime.LoggingService.WarnFormat ("BuildAction.{0} not supported!", projectFile.BuildAction);
				Console.WriteLine ("BuildAction.{0} not supported!", projectFile.BuildAction);
				return null;
			}

			//FIXME: Subtype

			XmlDocument doc = d.Document;
			XmlElement elem = doc.CreateElement (name, ns);
			elem.SetAttribute ("Include", projectFile.RelativePath);

			XmlNode n = doc.SelectSingleNode (String.Format (
					"/tns:Project/tns:ItemGroup/tns:{0}", name), NamespaceManager);

			if (n == null) {
				n = doc.CreateElement ("ItemGroup", ns);
				doc.DocumentElement.AppendChild (n);
				n.AppendChild (elem);
			} else {
				n.ParentNode.AppendChild (elem);
			}

			if (projectFile.BuildAction == BuildAction.EmbedAsResource) {
				MSBuildProject msproj = project as MSBuildProject;
				if (msproj == null || 
					msproj.GetDefaultResourceIdInternal (projectFile) != projectFile.ResourceId)
					//Emit LogicalName if we are writing elements for a Non-MSBuidProject,
					//(eg. when converting a gtk-sharp project, it might depend on non-vs
					// style resource naming)
					//Or when the resourceId is different from the default one
					EnsureChildValue (elem, "LogicalName", ns, projectFile.ResourceId);
				
				//DependentUpon is relative to the basedir of the 'pf' (resource file)
				EnsureChildValue (d.ProjectFileElements [projectFile], "DependentUpon", ns,
						Runtime.FileService.AbsoluteToRelativePath (
							Path.GetDirectoryName (projectFile.Name), projectFile.DependsOn));
			}
			
			return elem;
		}

		static void HandleFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			//FIXME: Check whether this file is a ApplicationIcon and accordingly update that?
			XmlElement elem = d.ProjectFileElements [e.ProjectFile];
			elem.SetAttribute ("Include", e.ProjectFile.RelativePath);
		}

		static void HandleFilePropertyChanged (object sender, ProjectFileEventArgs e)
		{
			//Subtype, BuildAction, DependsOn, Data

			MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			XmlElement elem = d.ProjectFileElements [e.ProjectFile];

			//BuildAction
			string buildAction = BuildActionToString (e.ProjectFile.BuildAction);
			if (buildAction == null) {
				Runtime.LoggingService.WarnFormat ("BuildAction.{0} not supported!", e.ProjectFile.BuildAction);
				Console.WriteLine ("BuildAction.{0} not supported!", e.ProjectFile.BuildAction);
				return;
			}

			if (elem.LocalName != buildAction) {
				XmlElement newElem = d.Document.CreateElement (buildAction, ns);
				XmlNode parent = elem.ParentNode;

				List<XmlNode> list = new List<XmlNode> ();
				foreach (XmlNode n in elem.ChildNodes)
					list.Add (n);
				foreach (XmlNode n in list)
					newElem.AppendChild (elem.RemoveChild (n));

				list.Clear ();
				foreach (XmlAttribute a in elem.Attributes)
					list.Add (a);

				foreach (XmlAttribute a in list)
					newElem.Attributes.Append (elem.Attributes.Remove (a));

				parent.RemoveChild (elem);
				parent.AppendChild (newElem);

				d.ProjectFileElements [e.ProjectFile] = newElem;
			}

			//DependentUpon is relative to the basedir of the 'pf' (resource file)
			EnsureChildValue (d.ProjectFileElements [e.ProjectFile], "DependentUpon", ns,
					Runtime.FileService.AbsoluteToRelativePath (
						Path.GetDirectoryName (e.ProjectFile.Name), e.ProjectFile.DependsOn));
			//FIXME: Subtype, Data
		}

		static string BuildActionToString (BuildAction ba)
		{
			switch (ba) {
			case BuildAction.Nothing:
				return "None";								
			case BuildAction.Compile:
				return "Compile";
			case BuildAction.EmbedAsResource:
				return "EmbeddedResource";
			case BuildAction.FileCopy:
				return "Content";
			case BuildAction.Exclude:
				//FIXME:
				break;
			}

			return null;
		}

		//Reading

		void ReadItemGroups (MSBuildData data, DotNetProject project, 
				DotNetProjectConfiguration globalConfig, string basePath, IProgressMonitor monitor)
		{
			//FIXME: This can also be Config/Platform specific
			XmlNodeList itemList = data.Document.SelectNodes ("/tns:Project/tns:ItemGroup", NamespaceManager);

			StringBuilder importsBuilder = null;
			if (project.LanguageName == "VBNet")
				importsBuilder = new StringBuilder ();

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
					pf = null;
					pr = null;
					if (include.Length == 0)
						//FIXME: Ignore, error??
						continue;

					string str_tmp = String.Empty;
					switch (node.LocalName) {
					case "Reference":
						string hintPath = String.Empty;
						string fullname = Runtime.SystemAssemblyService.GetAssemblyFullName (include);
						if ((fullname != null && 
							Runtime.SystemAssemblyService.FindInstalledAssembly (fullname) != null) ||
							!ReadAsString (node, "HintPath", ref hintPath, false)) {

							//If the assembly is from a package file
							//Or has _no_ HintPath, then add it as a Gac entry
							pr = new ProjectReference (ReferenceType.Gac, fullname ?? include);
							project.ProjectReferences.Add (pr);
						} else {
							//Not in the Gac, has HintPath
							pr = project.AddReference (MapAndResolvePath (basePath, hintPath));
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
							projName = node ["Name"].InnerText;

						if (String.IsNullOrEmpty (projName)) {
							//FIXME: Add support to load the project file from here
							Console.WriteLine ("Expected element <Name> for ProjectReference '{0}'", include);
							continue;
						}

						pr = new ProjectReference (ReferenceType.Project, projName);
						project.ProjectReferences.Add (pr);
						data.ProjectReferenceElements [pr] = (XmlElement) node;

						break;
					case "Compile":
						pf = project.AddFile (MapAndResolvePath (basePath, include), BuildAction.Compile);
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "None":
						pf = project.AddFile (MapAndResolvePath (basePath, include), BuildAction.Nothing);
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "Content":
						pf = project.AddFile (MapAndResolvePath (basePath, include), BuildAction.FileCopy);
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "EmbeddedResource":
						string fname = MapAndResolvePath (basePath, include);
						if (!fname.StartsWith (project.BaseDirectory)) {
							monitor.ReportWarning (GettextCatalog.GetString (
								"The specified path '{0}' for the EmbeddedResource is outside the project directory. Ignoring.", include));
							Console.WriteLine ("The specified path '{0}' for the EmbeddedResource is outside the project directory. Ignoring.", include);
							continue;
						}

						pf = project.AddFile (fname, BuildAction.EmbedAsResource);
						if (ReadAsString (node, "LogicalName", ref str_tmp, false))
							pf.ResourceId = str_tmp;
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "Import":
						//FIXME: Keep nodes for each import? List of imports?
						//This will probably have to be written back in WriteFile
						importsBuilder.AppendFormat ("{0},", include);
						break;
					default:
						Console.WriteLine ("Unrecognised ItemGroup element '{0}', Include = '{1}'. Ignoring.", node.LocalName, include);
						break;
					}

					if (pf != null) {
						if (ReadAsString (node, "DependentUpon", ref str_tmp, false))
							//DependentUpon is relative to the basedir of the 'pf' (resource file)
							pf.DependsOn = MapAndResolvePath (Path.GetDirectoryName (pf.Name), str_tmp);

						if (String.Compare (node.LocalName, "Content", true) != 0 && 
							ReadAsString (node, "CopyToOutputDirectory", ref str_tmp, false))
							Console.WriteLine ("Warning: CopyToOutputDirectory not supported for BuildAction '{0}', Include = '{1}'", node.LocalName, include);
					}
				}
			}

			if (project.LanguageName == "VBNet") {
				if (importsBuilder.Length > 0) {
					importsBuilder.Length --;
					VBCompilerParameters vbparams = (VBCompilerParameters) globalConfig.CompilationParameters;
					vbparams.Imports = importsBuilder.ToString ();
				}
			}
		}

		//FIXME: Too many params ?
		void ReadConfig (XPathNavigator nav, DotNetProjectConfiguration config,
				string lang, string basePath, ref string default_config, ref string default_platform)
		{
			if (nav.MoveToChild ("OutputType", ns)) {
				try {
					config.CompileTarget = (CompileTarget) Enum.Parse (typeof (CompileTarget), nav.Value, true);
				} catch (Exception e) {
					//Ignore
				}
				nav.MoveToParent ();
			}

			if (nav.MoveToChild ("Configuration", ns)) {
				if (CheckNullCondition (nav.UnderlyingObject as XmlElement, "Configuration"))
					default_config = nav.Value;

				nav.MoveToParent ();
			}
			
			if (nav.MoveToChild ("Platform", ns)) {
				if (CheckNullCondition (nav.UnderlyingObject as XmlElement, "Platform"))
					default_platform = nav.Value;

				nav.MoveToParent ();
			}

			string str_tmp = String.Empty;
			int int_tmp = 0;
			bool bool_tmp = false;

			if (ReadAsString (nav, "AssemblyName", ref str_tmp, false))
				config.OutputAssembly = str_tmp;

			if (ReadAsString (nav, "OutputPath", ref str_tmp, false))
				//FIXME: default path ?
				config.OutputDirectory = MapAndResolvePath (basePath, str_tmp);

			if (ReadAsBool (nav, "DebugSymbols", ref bool_tmp))
				//FIXME: <DebugType>?
				config.DebugMode = bool_tmp;

			if (lang == "VBNet") {
				VBCompilerParameters vbparams = 
					(VBCompilerParameters) config.CompilationParameters;

				if (ReadAsString (nav, "RootNamespace", ref str_tmp, false))
					vbparams.RootNamespace = str_tmp;

				if (ReadAsBool (nav, "AllowUnsafeBlocks", ref bool_tmp))
					vbparams.UnsafeCode = bool_tmp;

				if (ReadAsBool (nav, "Optimize", ref bool_tmp))
					vbparams.Optimize = bool_tmp;

				if (ReadAsBool (nav, "CheckForOverflowUnderflow", ref bool_tmp))
					vbparams.GenerateOverflowChecks = bool_tmp;

				if (ReadAsString (nav, "DefineConstants", ref str_tmp, true))
					vbparams.DefineSymbols = str_tmp;

				if (ReadAsInt (nav, "WarningLevel", ref int_tmp))
					vbparams.WarningLevel = int_tmp;

				if (ReadOffOnAsBool (nav, "OptionExplicit", ref bool_tmp))
					vbparams.OptionExplicit = bool_tmp;

				if (ReadOffOnAsBool (nav, "OptionStrict", ref bool_tmp))
					vbparams.OptionStrict = bool_tmp;

				if (ReadAsString (nav, "ApplicationIcon", ref str_tmp, false))
					vbparams.Win32Icon = MapAndResolvePath (basePath, str_tmp);

				if (ReadAsString (nav, "Win32Resource", ref str_tmp, false))
					vbparams.Win32Resource = MapAndResolvePath (basePath, str_tmp);
				//FIXME: OptionCompare, add support to VBnet binding, params etc
			}

			if (lang == "C#") {
				CSharpCompilerParameters csparams =
					(CSharpCompilerParameters) config.CompilationParameters;

				if (ReadAsBool (nav, "AllowUnsafeBlocks", ref bool_tmp))
					csparams.UnsafeCode = bool_tmp;

				if (ReadAsBool (nav, "Optimize", ref bool_tmp))
					csparams.Optimize = bool_tmp;

				if (ReadAsBool (nav, "CheckForOverflowUnderflow", ref bool_tmp))
					csparams.GenerateOverflowChecks = bool_tmp;

				if (ReadAsString (nav, "DefineConstants", ref str_tmp, true))
					csparams.DefineSymbols = str_tmp;

				if (ReadAsInt (nav, "WarningLevel", ref int_tmp))
					csparams.WarningLevel = int_tmp;

				if (ReadAsString (nav, "ApplicationIcon", ref str_tmp, false))
					csparams.Win32Icon = MapAndResolvePath (basePath, str_tmp);
				
				if (ReadAsString (nav, "Win32Resource", ref str_tmp, false))
					csparams.Win32Resource = MapAndResolvePath (basePath, str_tmp);
			}
		}

		StringDictionary ParseCondition (Regex regex, string condition)
		{
			StringDictionary dic = new StringDictionary ();

			if (condition == null || condition.Length == 0)
				return dic;

			Match m = regex.Match (condition);
			if (!m.Success)
				return dic;

			string left = m.Groups [1].Value;
			string right = m.Groups [2].Value;

			string [] left_parts = left.Split ('|');
			string [] right_parts = right.Split ('|');

			for (int i = 0; i < left_parts.Length; i ++) {
				if (left_parts [i].StartsWith ("$(") &&
					left_parts [i].EndsWith (")")) {
					//FIXME: Yuck!
					string key = left_parts [i].Substring (2, left_parts [i].Length - 3);
					if (i < right_parts.Length)
						dic [key.ToUpper ()] = right_parts [i].Trim ();
					else
						dic [key.ToUpper ()] = String.Empty;
				}

			}

			return dic;
		}

		//Utility methods

		static XmlNode MoveToChild (XmlNode node, string localName)
		{
			if (!node.HasChildNodes)
				return null;

			foreach (XmlNode n in node.ChildNodes)
				if (n.LocalName == localName)
					return n;

			return null;
		}

		internal static void EnsureChildValue (XmlNode node, string localName, string ns, object val)
		{
			XmlNode n = MoveToChild (node, localName);
			if (n == null) {
				//Child not found, create it
				XmlElement e = node.OwnerDocument.CreateElement (localName, ns);
				e.InnerText = val.ToString ();

				node.AppendChild (e);
			} else {
				n.InnerText = val.ToString ();
			}
		}

		bool ReadAsString (XmlNode node, string localName, ref string val, bool allowEmpty)
		{
			//Assumption: Number of child nodes is small enough
			//that xpath query would be more expensive than
			//linear traversal
			if (node == null || !node.HasChildNodes)
				return false;

			foreach (XmlNode n in node.ChildNodes) {
				//Case sensitive compare
				if (n.LocalName != localName)
					continue;

				//FIXME: Use XmlChar.WhitespaceChars ?
				string s= n.InnerText.Trim ();
				if (s.Length == 0 && !allowEmpty)
					return false;

				val = s;
				return true;
			}

			return false;
		}

		bool ReadAsString (XPathNavigator nav, string localName, ref string val, bool allowEmpty)
		{
			if (!nav.MoveToChild (localName, ns))
				return false;

			//FIXME: Use XmlChar.WhitespaceChars ?
			string s = nav.Value.Trim ();
			nav.MoveToParent ();

			if (s.Length == 0 && !allowEmpty)
				return false;

			val = s;
			return true;
		}

		bool ReadAsBool (XPathNavigator nav, string localName, ref bool val)
		{
			string str = String.Empty;
			if (!ReadAsString (nav, localName, ref str, false))
				return false;

			switch (str.ToUpper ()) {
			case "TRUE":
				val = true;
				break;
			case "FALSE":
				val = false;
				break;
			default:
				return false;
			}

			return true;
		}

		bool ReadOffOnAsBool (XPathNavigator nav, string localName, ref bool val)
		{
			string str = String.Empty;
			if (!ReadAsString (nav, localName, ref str, false))
				return false;

			switch (str.ToUpper ()) {
			case "ON":
				val = true;
				break;
			case "OFF":
				val = false;
				break;
			default:
				return false;
			}

			return true;
		}

		bool ReadAsInt (XPathNavigator nav, string localName, ref int val)
		{
			if (!nav.MoveToChild (localName, ns))
				return false;

			try {
				val = nav.ValueAsInt;
			} catch {
				return false;
			} finally {
				nav.MoveToParent ();
			}

			return true;
		}

		//Creates a <localName>Value</localName>
		internal static XmlElement AppendChild (XmlElement e, string localName, string ns, string value)
		{
			XmlElement elem = e.OwnerDocument.CreateElement (localName, ns);
			elem.InnerText = value;
			e.AppendChild (elem);

			return elem;
		}

		static string GetConfigName (StringDictionary dic)
		{
			if (!dic.ContainsKey ("CONFIGURATION") || 
				String.IsNullOrEmpty (dic ["CONFIGURATION"]))
				return null;

			string configname = dic ["CONFIGURATION"];
			if (dic.ContainsKey ("PLATFORM") && !String.IsNullOrEmpty (dic ["PLATFORM"])) {
				if (String.Compare (dic ["PLATFORM"], "AnyCPU", true) == 0)
					configname = configname + "|Any CPU";
				else
					configname = configname + "|" + dic ["PLATFORM"];
			}

			return configname;
		}

		/* Returns [0] : Config name, [1] : Platform */
		static string [] GetConfigPlatform (string name)
		{
			//FIXME: Handle Abc|Foo|x64 ? VS2005 doesn't allow a config
			//name with |
			string [] tmp = name.Split (new char [] {'|'}, 2);
			string [] ret = new string [2];
			ret [0] = tmp [0];
			if (tmp.Length < 2)
				ret [1] = "AnyCPU";
			else
				ret [1] = tmp [1];

			return ret;
		}

		internal static string MapAndResolvePath (string basePath, string relPath)
		{
			string ret = SlnMaker.MapPath (basePath, relPath);
			if (ret == null)
				return ret;
			return Path.GetFullPath (ret);
		}

		string GetLanguage (string fileName)
		{
			string extn = Path.GetExtension (fileName);
			if (String.Compare (extn, ".csproj", true) == 0)
				return "C#";
			if (String.Compare (extn, ".vbproj", true) == 0)
				return "VBNet";

			return null;
		}
	}

}
