//
// SingleFileDescriptionTemplate.cs
//
// Author:
//   Lluis Sanchez Gual
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

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.StandardHeaders;

namespace MonoDevelop.Ide.Templates
{
	public class SingleFileDescriptionTemplate: FileDescriptionTemplate
	{
		string name;
		string defaultName;
		string defaultExtension;
		string generatedFile;
		bool suppressAutoOpen = false;
		bool addStandardHeader = false;
		string dependsOn;
		string buildAction;
		List<string> references = new List<string> ();
		
		public override void Load (XmlElement filenode)
		{
			name = filenode.GetAttribute ("name");
			defaultName = filenode.GetAttribute ("DefaultName");
			defaultExtension = filenode.GetAttribute ("DefaultExtension");
			dependsOn = filenode.GetAttribute ("DependsOn");
			
			buildAction = BuildAction.Compile;
			buildAction = filenode.GetAttribute ("BuildAction");
			if (string.IsNullOrEmpty (buildAction))
				buildAction = null;
			
			string suppressAutoOpenStr = filenode.GetAttribute ("SuppressAutoOpen");
			if (!string.IsNullOrEmpty (suppressAutoOpenStr)) {
				try {
					suppressAutoOpen = bool.Parse (suppressAutoOpenStr);
				} catch (FormatException) {
					throw new InvalidOperationException ("Invalid value for SuppressAutoOpen in template.");
				}
			}
			
			string addStandardHeaderStr = filenode.GetAttribute ("AddStandardHeader");
			if (!string.IsNullOrEmpty (addStandardHeaderStr)) {
				try {
					addStandardHeader = bool.Parse (addStandardHeaderStr);
				} catch (FormatException) {
					throw new InvalidOperationException ("Invalid value for AddStandardHeader in template.");
				}
			}
			
			foreach (XmlElement elem in filenode.SelectNodes ("AssemblyReference")) {
				string aref = elem.InnerText.Trim ();
				if (!string.IsNullOrEmpty (aref))
					references.Add (aref);
			}
		}
		
		public override string Name {
			get { return name; } 
		}
		
		public bool AddStandardHeader {
			get { return addStandardHeader; }
			set { addStandardHeader = value; }
		}
		
		public sealed override bool AddToProject (Project project, string language, string directory, string name)
		{
			return AddFileToProject (project, language, directory, name) != null;
		}
		
		public ProjectFile AddFileToProject (Project project, string language, string directory, string name)
		{
			generatedFile = GetFileName (project, language, directory, name);
			if (project != null && project.IsFileInProject (generatedFile))
				throw new UserException (GettextCatalog.GetString ("The file '{0}' already exists in the project.", Path.GetFileName (generatedFile)));
			
			generatedFile = SaveFile (project, language, directory, name);
			if (generatedFile != null) {		
				string buildAction = this.buildAction ?? project.GetDefaultBuildAction (generatedFile);
				ProjectFile projectFile = new ProjectFile (generatedFile, buildAction);
				
				if (!string.IsNullOrEmpty (dependsOn)) {
					Hashtable tags = new Hashtable ();
					ModifyTags (project, language, null, generatedFile, ref tags);
					string parsedDepName = StringParserService.Parse (dependsOn, HashtableToStringArray (tags));
					projectFile.DependsOn = parsedDepName;
				}
				project.AddFile (projectFile);
				
				DotNetProject netProject = project as DotNetProject;
				if (netProject != null) {
					// Add required references
					foreach (string aref in references) {
						string res = Runtime.SystemAssemblyService.GetAssemblyFullName (aref);
						res = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (res, netProject.TargetFramework);
						if (!ContainsReference (netProject, res))
							netProject.References.Add (new ProjectReference (ReferenceType.Gac, aref));
					}
				}
				
				return projectFile;
			} else
				return null;
		}
		
		bool ContainsReference (DotNetProject project, string aref)
		{
			string aname;
			int i = aref.IndexOf (',');
			if (i == -1)
				aname = aref;
			else
				aname = aref.Substring (0, i);
			foreach (ProjectReference pr in project.References) {
				if (pr.ReferenceType == ReferenceType.Gac && (pr.Reference == aref || pr.Reference.StartsWith (aref + ",")))
					return true;
			}
			return false;
		}
		
		public override void Show ()
		{
			if (!suppressAutoOpen)
				IdeApp.Workbench.OpenDocument (generatedFile);
		}
		
		// Creates a file and saves it to disk. Returns the path to the new file
		// All parameters are optional (can be null)
		public string SaveFile (Project project, string language, string baseDirectory, string entryName)
		{
			string file = GetFileName (project, language, baseDirectory, entryName);
			
			if (File.Exists (file)) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("File already exists"),
				                                GettextCatalog.GetString ("File {0} already exists. Do you want to overwrite\nthe existing file?", file),
				                                AlertButton.OverwriteFile)) {
					return null;
				}
			}
			
			if (!Directory.Exists (Path.GetDirectoryName (file)))
				Directory.CreateDirectory (Path.GetDirectoryName (file));
					
			Stream stream = CreateFile (project, language, file);
			
			byte[] buffer = new byte [2048];
			int nr;
			FileStream fs = null;
			try {
				fs = File.Create (file);
				while ((nr = stream.Read (buffer, 0, 2048)) > 0)
					fs.Write (buffer, 0, nr);
			} finally {
				stream.Close ();
				if (fs != null)
					fs.Close ();
			}
			return file;
		}
		
		// Returns the name of the file that this template generates.
		// All parameters are optional (can be null)
		public virtual string GetFileName (Project project, string language, string baseDirectory, string entryName)
		{
			if (string.IsNullOrEmpty (entryName) && !string.IsNullOrEmpty (defaultName))
				entryName = defaultName;
			
			string fileName = entryName;
			
			//substitute tags
			if ((name != null) && (name.Length > 0)) {
				Hashtable tags = new Hashtable ();
				ModifyTags (project, language, entryName ?? name, null, ref tags);
				fileName = StringParserService.Parse (name, HashtableToStringArray (tags));
			}
			
			if (fileName == null)
				throw new InvalidOperationException ("File name not provided in template");
			
			//give it an extension if it didn't get one (compatibility with pre-substition behaviour)
			if (Path.GetExtension (fileName).Length == 0) {
				if (!string.IsNullOrEmpty  (defaultExtension)) {
					fileName = fileName + defaultExtension;
				}
				else if (!string.IsNullOrEmpty  (language)) {
					ILanguageBinding languageBinding = GetLanguageBinding (language);
					fileName = languageBinding.GetFileName (fileName);
				} 
			}
			
			if (baseDirectory != null)
				fileName = Path.Combine (baseDirectory, fileName);

			return fileName;
		}
		
		// Returns a stream with the content of the file.
		// project and language parameters are optional
		public virtual Stream CreateFile (Project project, string language, string fileName)
		{
			
			if (project != null && project.IsFileInProject (fileName))
				throw new UserException (GettextCatalog.GetString ("The file '{0}' already exists in the project.", Path.GetFileName (fileName)));
			
			Hashtable tags = new Hashtable ();
			ModifyTags (project, language, null, fileName, ref tags);
			
			string content = CreateContent (language);
			content = StringParserService.Parse (content, HashtableToStringArray (tags));
			
			MemoryStream ms = new MemoryStream ();
			byte[] data;
			if (StandardHeaderService.EmitStandardHeader && AddStandardHeader) { 
				string header = StandardHeaderService.GetHeader (language, fileName); 
				data = System.Text.Encoding.UTF8.GetBytes (header);
				ms.Write (data, 0, data.Length);
			}
			
			data = System.Text.Encoding.UTF8.GetBytes (content);
			ms.Write (data, 0, data.Length);
			ms.Position = 0;
			return ms;
		}
		
		// Creates the text content of the file
		// The Language parameter is optional

		public virtual string CreateContent (string language)
		{
			return string.Empty;
		}
		
		// Can add tags for substitution based on project, language or filename.
		// If overriding but still want base implementation's tags, should invoke base method.
		// We supply defaults whenever it is possible, to avoid having unsubstituted tags. However,
		// do not substitute blanks when a sensible default cannot be guessed, because they result
		//in less obvious errors.
		public virtual void ModifyTags (Project project, string language, string identifier, string fileName, ref Hashtable tags)
		{
			DotNetProject netProject = project as DotNetProject;
			string languageExtension = "";
			ILanguageBinding binding = null;
			if (!string.IsNullOrEmpty (language)) {
				binding = GetLanguageBinding (language);
				if (binding != null)
					languageExtension = Path.GetExtension (binding.GetFileName ("Default")).Remove (0, 1);
			}
			
			//need a default namespace or if there is no project, substitutions can get very messed up
			string ns = netProject != null ? netProject.GetDefaultNamespace (fileName) : "Application";
			
			//need an 'identifier' for tag substitution, e.g. class name or page name
			//if not given an identifier, use fileName
			if ((identifier == null) && (fileName != null))
				identifier = Path.GetFileName (fileName);
			 
			 if (identifier != null) {
			 	//remove all extensions
				while (Path.GetExtension (identifier).Length > 0)
					identifier = Path.GetFileNameWithoutExtension (identifier);
			 	
				tags ["Name"] = identifier;
				tags ["FullName"] = ns.Length > 0 ? ns + "." + identifier : identifier;
				
				//some .NET languages may be able to use keywords as identifiers if they're escaped
				//for simplicity, we escape the identifier in "Name" and provide "UnescapedName"
				IDotNetLanguageBinding dnb = binding as IDotNetLanguageBinding;
				if (dnb != null) {
					System.CodeDom.Compiler.CodeDomProvider provider = dnb.GetCodeDomProvider ();
					if (provider != null) {
						tags ["EscapedIdentifier"] = provider.CreateEscapedIdentifier (identifier);
					}
				}
			}
			
			if (ns.Length > 0)
				tags ["Namespace"] = ns;
			if (project != null)
				tags ["ProjectName"] = project.Name;
			if ((language != null) && (language.Length > 0))
				tags ["Language"] = language;
			if (languageExtension.Length > 0)
				tags ["LanguageExtension"] = languageExtension;
			
			if (fileName != null) {
				string fileDirectory = Path.GetDirectoryName (fileName);
				if (project != null && project.BaseDirectory != null && fileDirectory.Length >= project.BaseDirectory.Length && fileDirectory.Substring (0, project.BaseDirectory.Length) == fileDirectory )
					tags ["ProjectRelativeDirectory"] = fileDirectory.Substring (project.BaseDirectory.Length);
				else
					tags ["ProjectRelativeDirectory"] = fileDirectory;
				
				tags ["FileNameWithoutExtension"] = Path.GetFileNameWithoutExtension (fileName); 
				tags ["Directory"] = fileDirectory;
				tags ["FileName"] = fileName;
			}
		}
		
		protected ILanguageBinding GetLanguageBinding (string language)
		{
			ILanguageBinding binding = MonoDevelop.Projects.Services.Languages.GetBindingPerLanguageName (language);
			if (binding == null)
				throw new InvalidOperationException ("Language '" + language + "' not found");
			return binding;
		}
		
		protected string[,] HashtableToStringArray (Hashtable tags)
		{			
			string[,] tagsArr = new string [tags.Count, 2];
			int i = 0;
			foreach (string key in tags.Keys) {
				tagsArr [i, 0] = key;
				tagsArr [i, 1] = (string) tags [key];
				i++;
			}
			
			return tagsArr;
		}
	}
}
