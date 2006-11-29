//
// MSBuildFileFormat.cs
//
// Author:
//   Ankit Jain
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
		const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";

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

			XmlDocument doc = project.ExtendedProperties ["xml_doc"] as XmlDocument;

			//Write out the Configurations
			//FIXME: Not touching global config right now,
			//instead just writing out everything in the individual configs
			XmlNode globalConfigNode = project.ExtendedProperties ["global_config"] as XmlNode;

			//Set active config
			//FIXME: >1 <Configuration elements?
			XmlNode ac = MoveToChild (globalConfigNode, "Configuration");
			if (ac != null) {
				if (ac.Attributes ["Condition"] != null) {
					Regex regex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
					StringDictionary dic = ParseCondition (
							regex,
							ac.Attributes ["Condition"].Value);
					if (dic.ContainsKey ("CONFIGURATION") && String.IsNullOrEmpty (dic ["CONFIGURATION"]))
						// '$(Configuration)' == ''
						ac.InnerText = project.ActiveConfiguration.Name;
					else
						//New Element needs to be added for active config
						ac = null;
				} else {
					//New Element needs to be added for active config
					ac = null;
				}
			}

			if (ac == null) {
				//Add new xml element for active config
				ac = doc.CreateElement ("Configuration", ns);
				globalConfigNode.AppendChild (ac);
				ac.InnerText = project.ActiveConfiguration.Name;

				((XmlElement) ac).SetAttribute ("Condition", "'$(Configuration)' == ''");
			}

			foreach (DotNetProjectConfiguration config in project.Configurations) {
				XmlElement configNode = project.ExtendedProperties [config] as XmlElement;
				if (configNode == null) {
					//Create node for new configuration
					configNode = doc.CreateElement ("PropertyGroup", ns);
					doc.DocumentElement.AppendChild (configNode);

					configNode.SetAttribute ("Condition", 
						String.Format (" '$(Configuration)' == '{0}'", config.Name));
				}

				EnsureChildValue (configNode, "OutputType", ns, config.CompileTarget);
				EnsureChildValue (configNode, "AssemblyName", ns, config.OutputAssembly);
				EnsureChildValue (configNode, "OutputPath", ns, 
					Runtime.FileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, config.OutputDirectory));
				EnsureChildValue (configNode, "DebugSymbols", ns, config.DebugMode);

				if (project.LanguageName == "VBNet") {
					VBCompilerParameters vbparams = 
						(VBCompilerParameters) config.CompilationParameters;

					EnsureChildValue (configNode, "RootNamespace", ns, vbparams.RootNamespace);
					EnsureChildValue (configNode, "AllowUnsafeBlocks", ns, vbparams.UnsafeCode);
					EnsureChildValue (configNode, "Optimize", ns, vbparams.Optimize);
					EnsureChildValue (configNode, "CheckForOverflowUnderflow", ns, vbparams.GenerateOverflowChecks);
					EnsureChildValue (configNode, "DefineConstants", ns, vbparams.DefineSymbols);
					EnsureChildValue (configNode, "WarningLevel", ns, vbparams.WarningLevel);
					EnsureChildValue (configNode, "OptionExplicit", ns, vbparams.OptionExplicit);
					EnsureChildValue (configNode, "OptionStrict", ns, vbparams.OptionStrict);

					//FIXME: VB.net Imports
				}

				if (project.LanguageName == "C#") {
					CSharpCompilerParameters csparams =
						(CSharpCompilerParameters) config.CompilationParameters;

					EnsureChildValue (configNode, "AllowUnsafeBlocks", ns, csparams.UnsafeCode);
					EnsureChildValue (configNode, "Optimize", ns, csparams.Optimize);
					EnsureChildValue (configNode, "CheckForOverflowUnderflow", ns, csparams.GenerateOverflowChecks);
					EnsureChildValue (configNode, "DefineConstants", ns, csparams.DefineSymbols);
					EnsureChildValue (configNode, "WarningLevel", ns, csparams.WarningLevel);
				}
			}

			//FIXME: Set ActiveConfiguration

			CleanUpEmptyItemGroups (doc);
			doc.Save (file);

			return;
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
			DotNetProject project = new DotNetProject (lang);
			project.FileName = fname;
			//Default project name
			project.Name = Path.GetFileNameWithoutExtension (fname);
			project.FileFormat = new MSBuildFileFormat (lang);
			project.ClrVersion = ClrVersion.Net_2_0;

			project.ExtendedProperties ["xml_doc"] = doc;

			//Read the global config
			XPathNodeIterator iter = nav.Select ("/tns:Project/tns:PropertyGroup[not(@Condition)]", NamespaceManager);

			DotNetProjectConfiguration globalConfig = (DotNetProjectConfiguration) project.CreateConfiguration ("Temp");
			globalConfig.ClrVersion = ClrVersion.Net_2_0;

			string str_tmp = String.Empty;
			string active_config = String.Empty;
			string guid = null;
			while (iter.MoveNext ()) {
				if (guid == null && 
					ReadAsString (iter.Current, "ProjectGuid", ref str_tmp, false))
					guid = str_tmp;

				ReadConfig (iter.Current, globalConfig, project.LanguageName, basePath, ref active_config);
				//FIXME: Handle case when >1 global PropertyGroups exist,
				//Note: No IConfiguration object is saved for 'global config'
				//so the nodes are saved with string 'global_config' as the key
				project.ExtendedProperties ["global_config"] = iter.Current.UnderlyingObject;
			}

			if (guid != null)
				project.ExtendedProperties ["guid"] = guid.Trim (new char [] {'{', '}'});

			//ReadItemGroups : References, Source files etc
			ReadItemGroups (doc, project, globalConfig, basePath);

			//Load configurations
			iter = nav.Select ("/tns:Project/tns:PropertyGroup[@Condition]", NamespaceManager);
			Regex regex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
			while (iter.MoveNext ()) {
				string tmp = null;
				StringDictionary dic = ParseCondition (
						regex, 
						iter.Current.GetAttribute ("Condition", NamespaceManager.DefaultNamespace));

				if (!dic.ContainsKey ("CONFIGURATION") || 
					String.IsNullOrEmpty (dic ["CONFIGURATION"]))
					continue;

				DotNetProjectConfiguration config = 
					(DotNetProjectConfiguration) project.GetConfiguration (dic ["CONFIGURATION"]);

				if (config == null) {
					config = (DotNetProjectConfiguration) globalConfig.Clone ();
					config.Name = dic ["CONFIGURATION"];
					project.Configurations.Add (config);
				}

				ReadConfig (iter.Current, config, project.LanguageName, basePath, ref tmp);

				project.ExtendedProperties [config] = iter.Current.UnderlyingObject;
			}

			if (project.Configurations [active_config] != null)
				project.ActiveConfiguration = project.Configurations [active_config];

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
			//Create nodes for this in Writefile
			//project.ConfigurationAdded += new ConfigurationEventHandler (HandleConfigurationAdded);
			project.ConfigurationRemoved += new ConfigurationEventHandler (HandleConfigurationRemoved);

			project.NameChanged += new CombineEntryRenamedEventHandler (HandleRename);

			return project;
		}

		static void HandleRename (object sender, CombineEntryRenamedEventArgs e)
		{
			string oldfname = e.CombineEntry.FileName;
			string extn = Path.GetExtension (oldfname);
			string dir = Path.GetDirectoryName (oldfname);
			string newfname = Path.Combine (dir, e.NewName + extn);

			File.Move (oldfname, newfname);
			e.CombineEntry.FileName = newfname;
		}

		//Event handlers
		static void HandleConfigurationRemoved (object sender, ConfigurationEventArgs e)
		{
			DotNetProject project = sender as DotNetProject;
			if (project == null)
				return;

			XmlNode node = project.ExtendedProperties [e.Configuration] as XmlNode;
			if (node == null) {
				Console.WriteLine ("node not found");
				return;
			}

			node.ParentNode.RemoveChild (node);
		}

		//References

		static void HandleReferenceRemoved (object sender, ProjectReferenceEventArgs e)
		{
			XmlNode node = e.Project.ExtendedProperties [e.ProjectReference] as XmlNode;

			node.ParentNode.RemoveChild (node);
		}

		static void HandleReferenceAdded (object sender, ProjectReferenceEventArgs e)
		{
			XmlDocument doc = e.Project.ExtendedProperties ["xml_doc"] as XmlDocument;

			ProjectReference projectRef = e.ProjectReference;
			ReferenceType refType = e.ProjectReference.ReferenceType;

			string elemName;
			if (refType == ReferenceType.Project)
				elemName = "ProjectReference";
			else
				elemName = "Reference";

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
					Runtime.FileUtilityService.AbsoluteToRelativePath (e.Project.BaseDirectory, projectRef.Reference));
				AppendChild (elem, "SpecificVersion", ns, "False");
				break;
			case ReferenceType.Project:
				Combine c = e.Project.ParentCombine;
				while (c.ParentCombine != null)
					c = c.ParentCombine;

				Project p = c.FindProject (projectRef.Reference);
				//FIXME: if (p == null) : This should not happen!
				reference = Runtime.FileUtilityService.AbsoluteToRelativePath (
					e.Project.BaseDirectory, p.FileName);

				if (p.ExtendedProperties.Contains ("guid"))
					AppendChild (elem, "Project", ns, (string) p.ExtendedProperties ["guid"]);

				AppendChild (elem, "Name", ns, p.Name);

				break;
			case ReferenceType.Custom:
				break;
			}

			//Add the Include attribute
			elem.SetAttribute ("Include", reference);

			e.Project.ExtendedProperties [projectRef] = elem;
		}

		//ProjectFile-s

		static void HandleFileRemoved (object sender, ProjectFileEventArgs e)
		{
			XmlNode node = e.Project.ExtendedProperties [e.ProjectFile] as XmlNode;

			node.ParentNode.RemoveChild (node);
		}

		static void HandleFileAdded (object sender, ProjectFileEventArgs e)
		{
			XmlDocument doc = e.Project.ExtendedProperties ["xml_doc"] as XmlDocument;

			string name = BuildActionToString (e.ProjectFile.BuildAction);
			if (name == null) {
				Console.WriteLine ("BuildAction.{0} not supported!", e.ProjectFile.BuildAction);
				return;
			}

			//FIXME: Subtype

			XmlElement elem = doc.CreateElement (name, ns);
			elem.SetAttribute ("Include", e.ProjectFile.RelativePath);

			XmlNode node = doc.SelectSingleNode (String.Format (
					"/tns:Project/tns:ItemGroup/tns:{0}", name), NamespaceManager);

			if (node == null) {
				node = doc.CreateElement ("ItemGroup", ns);
				doc.DocumentElement.AppendChild (node);
				node.AppendChild (elem);
			} else {
				node.ParentNode.AppendChild (elem);
			}

			e.Project.ExtendedProperties [e.ProjectFile] = elem;
		}

		void HandleFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			XmlElement node = e.Project.ExtendedProperties [e.ProjectFile] as XmlElement;
			node.SetAttribute ("Include", e.ProjectFile.RelativePath);
		}

		void HandleFilePropertyChanged (object sender, ProjectFileEventArgs e)
		{
			//Subtype, BuildAction, DependsOn, Data

			XmlNode node = e.Project.ExtendedProperties [e.ProjectFile] as XmlNode;
			XmlDocument doc = e.Project.ExtendedProperties ["xml_doc"] as XmlDocument;

			//BuildAction
			string buildAction = BuildActionToString (e.ProjectFile.BuildAction);
			if (buildAction == null) {
				Console.WriteLine ("BuildAction.{0} not supported!", e.ProjectFile.BuildAction);
				return;
			}

			if (node.LocalName != buildAction) {
				XmlElement elem = doc.CreateElement (buildAction, ns);
				XmlNode parent = node.ParentNode;

				List<XmlNode> list = new List<XmlNode> ();
				foreach (XmlNode n in node.ChildNodes)
					list.Add (n);
				foreach (XmlNode n in list)
					elem.AppendChild (node.RemoveChild (n));

				list.Clear ();
				foreach (XmlAttribute a in node.Attributes)
					list.Add (a);

				foreach (XmlAttribute a in list)
					elem.Attributes.Append (node.Attributes.Remove (a));

				parent.RemoveChild (node);
				parent.AppendChild (elem);

				e.Project.ExtendedProperties [e.ProjectFile] = elem;
			}

			//FIXME: Subtype, DependsOn, Data
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
			case BuildAction.Exclude:
				//FIXME:
				break;
			}

			return null;
		}

		//Reading

		void ReadItemGroups (XmlDocument doc, DotNetProject project, 
				DotNetProjectConfiguration globalConfig, string basePath)
		{
			//FIXME: This can also be Config/Platform specific, handle it?
			XmlNodeList itemList = doc.SelectNodes ("/tns:Project/tns:ItemGroup", NamespaceManager);

			StringBuilder importsBuilder = null;
			if (project.LanguageName == "VBNet")
				importsBuilder = new StringBuilder ();

			ProjectFile pf;
			ProjectReference pr;
			foreach (XmlNode itemGroup in itemList) {
				foreach (XmlNode node in itemGroup.ChildNodes) {
					if (node.Attributes ["Include"] == null)
						//FIXME: warning/error?
						continue;

					string include = node.Attributes ["Include"].Value;
					pf = null;
					pr = null;
					if (include.Length == 0)
						//FIXME: Ignore, error??
						continue;

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
						project.ExtendedProperties [pr] = node;
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
						project.ExtendedProperties [pr] = node;

						break;
					case "Compile":
						pf = project.AddFile (MapAndResolvePath (basePath, include), BuildAction.Compile);
						project.ExtendedProperties [pf] = node;
						break;
					case "None":
						pf = project.AddFile (MapAndResolvePath (basePath, include), BuildAction.Nothing);
						project.ExtendedProperties [pf] = node;
						break;
					case "EmbeddedResource":
						pf = project.AddFile (MapAndResolvePath (basePath, include), BuildAction.EmbedAsResource);
						project.ExtendedProperties [pf] = node;
						break;
					case "Import":
						//FIXME: Keep nodes for each import? List of imports?
						//This will probably have to be written back in WriteFile
						importsBuilder.AppendFormat ("{0},", include);
						break;
					default:
						Console.WriteLine ("Unrecognised ItemGroup element '{0}'", node.LocalName);
						break;
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
				string lang, string basePath, ref string active_config)
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
				Regex regex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
				StringDictionary dic = ParseCondition (
						regex, 
						nav.GetAttribute ("Condition", NamespaceManager.DefaultNamespace));

				if (dic.ContainsKey ("CONFIGURATION") && String.IsNullOrEmpty (dic ["CONFIGURATION"]))
					// '$(Configuration)' == ''
					active_config = nav.Value;

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

			//FIXME: if (left_parts.Count != right_parts.Count)

			for (int i = 0; i < left_parts.Length; i ++) {
				if (left_parts [i].StartsWith ("$(") &&
					left_parts [i].EndsWith (")")) {
					//FIXME: Yuck!
					string key = left_parts [i].Substring (2, left_parts [i].Length - 3);
					dic [key.ToUpper ()] = right_parts [i].Trim ();
				}
			}

			return dic;
		}

		//Utility methods

		XmlNode MoveToChild (XmlNode node, string localName)
		{
			if (!node.HasChildNodes)
				return null;

			foreach (XmlNode n in node.ChildNodes)
				if (n.LocalName == localName)
					return n;

			return null;
		}

		void EnsureChildValue (XmlNode node, string localName, string ns, object val)
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
		static XmlElement AppendChild (XmlElement e, string localName, string ns, string value)
		{
			XmlElement elem = e.OwnerDocument.CreateElement (localName, ns);
			elem.InnerText = value;
			e.AppendChild (elem);

			return elem;
		}

		string MapAndResolvePath (string basePath, string relPath)
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
