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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public class MSBuildFileFormat : IFileFormat
	{
		internal const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";

		static XmlNamespaceManager manager;
		SlnFileFormat solutionFormat = new SlnFileFormat ();

		public MSBuildFileFormat ()
		{
		}

		public string Name {
			get { return "Visual Studio .NET 2005"; }
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			if (solutionFormat.CanWriteFile (obj))
				return solutionFormat.GetValidFormatName (obj, fileName);
			
			if (obj is DotNetProject) {
				string lang = ((DotNetProject)obj).LanguageName;
				if (lang == null || lang == "C#")
					//default
					return Path.ChangeExtension (fileName, ".csproj");
				if (lang == "VBNet")
					return Path.ChangeExtension (fileName, ".vbproj");
			}
			return fileName;
		}

		public bool CanReadFile (string file)
		{
			if (solutionFormat.CanReadFile (file))
				return true;
			
			if (GetLanguage (file) == null)
				return false;

			//FIXME: Need a better way to check the rootelement
			XmlReader xr = null;
			try {
				xr = XmlReader.Create (file);
				xr.MoveToContent ();

				if (xr.NodeType == XmlNodeType.Element && String.Compare (xr.LocalName, "Project") == 0 &&
					String.Compare (xr.NamespaceURI, ns) == 0)
					return true;

			} catch (FileNotFoundException fex) {
				Console.WriteLine (GettextCatalog.GetString ("File not found {0} : ", file));
				return false;
			} catch (XmlException xe) {
				Console.WriteLine (GettextCatalog.GetString ("Error reading file {0} : ", xe.ToString ()));
				return false;
			} finally {
				if (xr != null)
					((IDisposable)xr).Dispose ();
			}

			return false;
		}

		public bool CanWriteFile (object obj)
		{
			return (obj is DotNetProject) || solutionFormat.CanWriteFile (obj);
		}

		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
		{
			if (obj is Combine)
				return solutionFormat.GetExportFiles (obj);
			return null;
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
			if (solutionFormat.CanWriteFile (node)) {
				solutionFormat.WriteFile (file, node, monitor);
				return;
			}
			
			if (node == null)
				return;

			DotNetProject project = node as DotNetProject;
			if (project == null)
				throw new InvalidOperationException ("The provided object is not a DotNetProject");
			
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
					File.Delete (file);
					File.Move (tmpfilename, file);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save project: {0}", file), ex);
				Console.WriteLine ("Could not save project: {0}, {1}", file, ex);

				if (tmpfilename != String.Empty)
					File.Delete (tmpfilename);
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
				EnsureChildValue (configElement, "AssemblyName", ns, CanonicalizePath (config.OutputAssembly));
				// VS2005 emits trailing \\ for folders
				EnsureChildValue (configElement, "OutputPath", ns, 
					CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						project.BaseDirectory, config.OutputDirectory)) + "\\");
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
					EnsureChildValue (configElement, "OptionExplicit", ns, vbparams.OptionExplicit ? "On" : "Off");
					EnsureChildValue (configElement, "OptionStrict", ns, vbparams.OptionStrict ? "On" : "Off");
					if (vbparams.Win32Icon != null && vbparams.Win32Icon.Length > 0)
						EnsureChildValue (configElement, "ApplicationIcon", ns,
							CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, vbparams.Win32Icon)));

					if (vbparams.Win32Resource != null && vbparams.Win32Resource.Length > 0)
						EnsureChildValue (configElement, "Win32Resource", ns,
							CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, vbparams.Win32Resource)));

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
							CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, csparams.Win32Icon)));

					if (csparams.Win32Resource != null && csparams.Win32Resource.Length > 0)
						EnsureChildValue (configElement, "Win32Resource", ns,
							CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
								project.BaseDirectory, csparams.Win32Resource)));
				}
			}

			// Always update the project references
			foreach (ProjectReference pref in project.ProjectReferences)
				data.ProjectReferenceElements [pref] = ReferenceToXmlElement (data, project, pref);
		
			//FIXME: Set ActiveConfiguration
			CleanUpEmptyItemGroups (doc);

			if (newdoc) {
				foreach (ProjectFile pfile in project.ProjectFiles) {
					if (pfile.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightGeneratedFile"] != null)
						//Ignore the generated %.xaml.g.cs files
						continue;

					XmlElement xe = FileToXmlElement (data, project, pfile);
					if (xe != null)
						data.ProjectFileElements [pfile] = xe;
				}

				XmlElement elem = doc.CreateElement ("Configuration", ns);
				data.GlobalConfigElement.AppendChild (elem);
				elem.InnerText = "Debug";
				elem.SetAttribute ("Condition", " '$(Configuration)' == '' ");

				elem = doc.CreateElement ("Platform", ns);
				data.GlobalConfigElement.AppendChild (elem);
				elem.InnerText = "AnyCPU";
				elem.SetAttribute ("Condition", " '$(Platform)' == '' ");

				//MUST go at the end.. 
				elem = doc.CreateElement ("Import", ns);
				doc.DocumentElement.InsertAfter (elem, doc.DocumentElement.LastChild);
				elem.SetAttribute ("Project", @"$(MSBuildBinPath)\Microsoft.CSharp.Targets");
			}

			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			settings.NewLineChars = "\r\n";
			settings.NewLineHandling = NewLineHandling.Replace;
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;

			using (XmlWriter xw = XmlWriter.Create (file, settings)) {
				doc.Save (xw);
				xw.Close ();
			}

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
			StringDictionary dic = ParseCondition (elem.Attributes ["Condition"].Value);

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
			SetupHandlers (project);
		}

		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			// If it is a solution, use the solution reader
			if (solutionFormat.CanReadFile (fileName)) {
				return solutionFormat.ReadFile (fileName, monitor);
			}
			
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
			XmlDocument doc = new XmlDocument ();
			doc.Load (fname);

			XPathNavigator nav = doc.CreateNavigator ();
			nav.MoveToFirstChild ();

			while (! (nav.UnderlyingObject is XmlElement))
				nav.MoveToNext ();

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
			project.FileFormat = new MSBuildFileFormat ();
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
			while (iter.MoveNext ()) {
				string tmp = String.Empty;
				string tmp2 = String.Empty;
				StringDictionary dic = ParseCondition (
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
				throw;
			}
		}

		internal static XmlElement ReferenceToXmlElement (MSBuildData d, Project project, ProjectReference projectRef)
		{
			ReferenceType refType = projectRef.ReferenceType;

			XmlDocument doc = d.Document;
			XmlElement elem;
			if (!d.ProjectReferenceElements.TryGetValue (projectRef, out elem)) {
				string elemName;
				if (refType == ReferenceType.Project)
					elemName = "ProjectReference";
				else
					elemName = "Reference";

				elem = doc.CreateElement (elemName, ns);

				//Add the element to the document
				XmlNode node = doc.SelectSingleNode (String.Format ("/tns:Project/tns:ItemGroup/tns:{0}", elemName), NamespaceManager);
				if (node == null) {
					node = doc.CreateElement ("ItemGroup", ns);
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
				reference = AssemblyName.GetAssemblyName (reference).ToString ();

				EnsureChildValue (elem, "HintPath", ns, 
					CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (project.BaseDirectory, projectRef.Reference)));
				EnsureChildValue (elem, "SpecificVersion", ns, "False");
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

					reference = CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						project.BaseDirectory, p.FileName));

					if (p.ExtendedProperties.Contains (typeof (MSBuildFileFormat))) {
						MSBuildData data = (MSBuildData) p.ExtendedProperties [typeof (MSBuildFileFormat)];
						if (data.Guid != null & data.Guid.Length != 0)
							EnsureChildValue (elem, "Project", ns, String.Concat ("{", data.Guid, "}"));
					}

					EnsureChildValue (elem, "Name", ns, Escape (p.Name));
				}
				break;
			case ReferenceType.Custom:
				break;
			}

			elem.SetAttribute ("Include", Escape (reference));

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

			XmlElement xe = FileToXmlElement (d, e.Project, e.ProjectFile);
			if (xe != null)
				d.ProjectFileElements [e.ProjectFile] = xe;
		}

		static XmlElement FileToXmlElement (MSBuildData d, Project project, ProjectFile projectFile)
		{
			if (projectFile.BuildAction == BuildAction.Compile && projectFile.Subtype != Subtype.Code)
				return null;

			string name = BuildActionToString (projectFile.BuildAction);
			if (name == null) {
				Runtime.LoggingService.WarnFormat ("BuildAction.{0} not supported!", projectFile.BuildAction);
				Console.WriteLine ("BuildAction.{0} not supported!", projectFile.BuildAction);
				return null;
			}

			//FIXME: Subtype

			XmlDocument doc = d.Document;
			XmlElement elem = doc.CreateElement (name, ns);
			elem.SetAttribute ("Include", CanonicalizePath (projectFile.RelativePath));

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
					MSBuildProject.GetDefaultResourceIdInternal (projectFile) != projectFile.ResourceId)
					//Emit LogicalName if we are writing elements for a Non-MSBuidProject,
					//(eg. when converting a gtk-sharp project, it might depend on non-vs
					// style resource naming)
					//Or when the resourceId is different from the default one
					EnsureChildValue (elem, "LogicalName", ns, Escape (projectFile.ResourceId));
				
				//DependentUpon is relative to the basedir of the 'pf' (resource file)
				if (!String.IsNullOrEmpty (projectFile.DependsOn))
					EnsureChildValue (elem, "DependentUpon", ns,
						CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
							Path.GetDirectoryName (projectFile.Name), projectFile.DependsOn)));
			}

			if (projectFile.BuildAction == BuildAction.FileCopy)
				EnsureChildValue (elem, "CopyToOutputDirectory", ns, "Always");

			if (projectFile.IsExternalToProject)
				EnsureChildValue (elem, "Link", ns, Path.GetFileName (projectFile.Name));
			
			return elem;
		}

		static void HandleFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			MSBuildData d = (MSBuildData) e.Project.ExtendedProperties [typeof (MSBuildFileFormat)];
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			//FIXME: Check whether this file is a ApplicationIcon and accordingly update that?
			XmlElement elem = d.ProjectFileElements [e.ProjectFile];
			elem.SetAttribute ("Include", CanonicalizePath (e.ProjectFile.RelativePath));
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
			if (!String.IsNullOrEmpty (e.ProjectFile.DependsOn))
				EnsureChildValue (d.ProjectFileElements [e.ProjectFile], "DependentUpon", ns,
					CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						Path.GetDirectoryName (e.ProjectFile.Name), e.ProjectFile.DependsOn)));
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

					string path = null;
					string include = node.Attributes ["Include"].Value;
					pf = null;
					pr = null;
					if (include.Length == 0)
						//FIXME: Ignore, error??
						continue;

					include = Unescape (include);

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

							if (AssemblyNamesTable.ContainsKey (asmname) && asmname != AssemblyNamesTable [asmname]) {
								// assembly name is in the table and case is different
								fullname = Runtime.SystemAssemblyService.GetAssemblyFullName (
									AssemblyNamesTable [asmname] + rest);
							}
						}
						if ((fullname != null && 
							Runtime.SystemAssemblyService.FindInstalledAssembly (fullname) != null) ||
							!ReadAsString (node, "HintPath", ref hintPath, false)) {

							//If the assembly is from a package file
							//Or has _no_ HintPath, then add it as a Gac entry
							pr = new ProjectReference (ReferenceType.Gac, fullname ?? include);
							project.ProjectReferences.Add (pr);
						} else {
							//Not in the Gac, has HintPath
							hintPath = Unescape (hintPath);
							path = MapAndResolvePath (basePath, hintPath);
							if (path == null) {
								Console.WriteLine (GettextCatalog.GetString (
									"HintPath ({0}) for Reference '{1}' is invalid. Ignoring.",
									hintPath, include));
								monitor.ReportWarning (GettextCatalog.GetString (
									"HintPath ({0}) for Reference '{1}' is invalid. Ignoring.",
									hintPath, include));

								continue;
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
							projName = Unescape (node ["Name"].InnerText);

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
						path = GetValidPath (monitor, basePath, include);
						if (path == null)
							continue;
						pf = project.AddFile (path, BuildAction.Compile);
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "None":
					case "Content":
						//FIXME: We don't support "CopyToOutputDirectory" for
						//other BuildActions
						path = GetValidPath (monitor, basePath, include);
						if (path == null)
							continue;
						if (ReadAsString (node, "CopyToOutputDirectory", ref str_tmp, false))
							pf = project.AddFile (path, BuildAction.FileCopy);
						else
							pf = project.AddFile (path, BuildAction.Nothing);
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "EmbeddedResource":
						path = GetValidPath (monitor, basePath, include);
						if (path == null)
							continue;

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
						if (ReadAsString (node, "LogicalName", ref str_tmp, false))
							pf.ResourceId = Unescape (str_tmp);
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					case "Import":
						//FIXME: Keep nodes for each import? List of imports?
						//This will probably have to be written back in WriteFile
						importsBuilder.AppendFormat ("{0},", include);
						break;
					case "SilverlightPage":
						//FIXME: this should be available only for 
						//<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>
						//
						//This tag also has a
						//	<Generator>MSBuild:Compile</Generator>
						path = GetValidPath (monitor, basePath, include);
						if (path == null)
							continue;

						pf = project.AddFile (path, BuildAction.EmbedAsResource);
						pf.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightPage"] = "";
						data.ProjectFileElements [pf] = (XmlElement) node;

						// Add the corresponding %.g.cs to the project, we'll skip this
						// when saving the project file
						pf = project.AddFile (path + ".g.cs", BuildAction.Compile);
						pf.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightGeneratedFile"] = "";
						data.ProjectFileElements [pf] = (XmlElement) node;
						break;
					default:
						Console.WriteLine ("Unrecognised ItemGroup element '{0}', Include = '{1}'. Ignoring.", node.LocalName, include);
						break;
					}

					if (pf != null) {
						if (ReadAsString (node, "DependentUpon", ref str_tmp, false))
							//DependentUpon is relative to the basedir of the 'pf' (resource file)
							pf.DependsOn = Unescape (MapAndResolvePath (Path.GetDirectoryName (pf.Name), str_tmp));

						if (String.Compare (node.LocalName, "Content", true) != 0 &&
							String.Compare (node.LocalName, "None", true) != 0 &&
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

		string GetValidPath (IProgressMonitor monitor, string basePath, string relPath)
		{
			string path = MapAndResolvePath (basePath, relPath);
			if (path != null)
				return path;

			Console.WriteLine (GettextCatalog.GetString ("File name '{0}' is invalid. Ignoring.", relPath));
			monitor.ReportWarning (GettextCatalog.GetString ("File name '{0}' is invalid. Ignoring.", relPath));
			return null;
		}

		//FIXME: Too many params ?
		void ReadConfig (XPathNavigator nav, DotNetProjectConfiguration config,
				string lang, string basePath, ref string default_config, ref string default_platform)
		{
			if (nav.MoveToChild ("OutputType", ns)) {
				try {
					config.CompileTarget = (CompileTarget) Enum.Parse (typeof (CompileTarget), nav.Value, true);
				} catch (ArgumentException) {
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
				config.OutputAssembly = Unescape (str_tmp);

			if (ReadAsString (nav, "OutputPath", ref str_tmp, false))
				config.OutputDirectory = MapAndResolvePath (basePath, Unescape (str_tmp));

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
					vbparams.Win32Icon = Unescape (MapAndResolvePath (basePath, str_tmp));

				if (ReadAsString (nav, "Win32Resource", ref str_tmp, false))
					vbparams.Win32Resource = Unescape (MapAndResolvePath (basePath, str_tmp));
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
					csparams.Win32Icon = Unescape (MapAndResolvePath (basePath, str_tmp));
				
				if (ReadAsString (nav, "Win32Resource", ref str_tmp, false))
					csparams.Win32Resource = Unescape (MapAndResolvePath (basePath, str_tmp));
			}
		}

		StringDictionary ParseCondition (string condition)
		{
			StringDictionary dic = new StringDictionary ();

			if (condition == null || condition.Length == 0)
				return dic;

			Match m = ConditionRegex.Match (condition);
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

		internal static void EnsureChildValue (XmlNode node, string localName, string ns, bool val)
		{
			EnsureChildValue (node, localName, ns, val.ToString ().ToLower ());
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

		static string CanonicalizePath (string path)
		{
			if (String.IsNullOrEmpty (path))
				return path;

			string ret = Runtime.FileService.NormalizeRelativePath (path);
			if (ret.Length == 0)
				return ".";

			return Escape (ret).Replace ('/', '\\');
		}

		static char [] charToEscapeArray = {'$', '%', '\'', '(', ')', '*', ';', '?', '@'};
		static string charsToEscapeString = "$%'()*;?@";

		// Escape and Unescape taken from : class/Microsoft.Build.Engine/Microsoft.Build.BuildEngine/Utilities.cs
		static string Escape (string unescapedExpression)
		{
			if (unescapedExpression.IndexOfAny (charToEscapeArray) < 0)
				return unescapedExpression;

			StringBuilder sb = new StringBuilder ();
			
			foreach (char c in unescapedExpression) {
				if (charsToEscapeString.IndexOf (c) < 0)
					sb.Append (c);
				else
					sb.AppendFormat ("%{0:x2}", (int) c);
			}
			
			return sb.ToString ();
		}
		
		static string Unescape (string escapedExpression)
		{
			if (escapedExpression.IndexOf ('%') < 0)
				return escapedExpression;

			StringBuilder sb = new StringBuilder ();
			
			int i = 0;
			while (i < escapedExpression.Length) {
				sb.Append (Uri.HexUnescape (escapedExpression, ref i));
			}
			
			return sb.ToString ();
		}

		static Regex conditionRegex = null;
		static Regex ConditionRegex {
			get {
				if (conditionRegex == null)
					conditionRegex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
				return conditionRegex;
			}
		}

		// Contains only assembly name like "System.Xml"
		// used to get the correct case of assembly names,
		// like System.XML
		static Dictionary<string, string> assemblyNamesTable = null;
		static Dictionary<string, string> AssemblyNamesTable {
			get {
				if (assemblyNamesTable == null) {
					assemblyNamesTable = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
					foreach (string fullname in Runtime.SystemAssemblyService.GetAssemblyFullNames ()) {
						string name = fullname;
						if (name.IndexOf (',') >= 0)
							name = name.Substring (0, name.IndexOf (',')).Trim ();
						assemblyNamesTable [name] = name;
					}
				}

				return assemblyNamesTable;
			}

		}
	}

}
