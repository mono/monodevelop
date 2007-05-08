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

using MonoDevelop.Core;
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
		
		public override void Load (XmlElement filenode)
		{
			name = filenode.GetAttribute ("name");
			defaultName = filenode.GetAttribute ("DefaultName");
			defaultExtension = filenode.GetAttribute ("DefaultExtension");
		}
		
		public override string Name {
			get { return name; } 
		}
		
		public sealed override void AddToProject (Project project, string language, string directory, string name)
		{
			AddFileToProject (project, language, directory, name);
		}
		
		public ProjectFile AddFileToProject (Project project, string language, string directory, string name)
		{
			generatedFile = GetFileName (project, language, directory, name);
			if (project != null && project.IsFileInProject (generatedFile))
				throw new UserException (GettextCatalog.GetString ("The file '{0}' already exists in the project.", Path.GetFileName (generatedFile)));
			
			generatedFile = SaveFile (project, language, directory, name);
			if (generatedFile != null) {
				project.AddFile (generatedFile, BuildAction.Compile);
				return project.GetProjectFile (generatedFile);
			} else
				return null;
		}
		
		public override void Show ()
		{
			IdeApp.Workbench.OpenDocument (generatedFile);
		}
		
		// Creates a file and saves it to disk. Returns the path to the new file
		// All parameters are optional (can be null)
		public string SaveFile (Project project, string language, string baseDirectory, string entryName)
		{
			string file = GetFileName (project, language, baseDirectory, entryName);
			
			if (File.Exists (file)) {
				if (!Services.MessageService.AskQuestion (GettextCatalog.GetString (
					"File {0} already exists, do you want to overwrite\nthe existing file?", file),
					GettextCatalog.GetString ("File already exists"))) {
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
				StringParserService sps = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
				Hashtable tags = new Hashtable ();
				ModifyTags (project, language, entryName, null, ref tags);
				fileName = sps.Parse (name, HashtableToStringArray (tags));
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
			StringParserService sps = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
			
			if (project != null && project.IsFileInProject (fileName))
				throw new UserException (GettextCatalog.GetString ("The file '{0}' already exists in the project.", Path.GetFileName (fileName)));
			
			Hashtable tags = new Hashtable ();
			ModifyTags (project, language, null, fileName, ref tags);
			
			string content = CreateContent (language);
			content = sps.Parse (content, HashtableToStringArray (tags));
			
			MemoryStream ms = new MemoryStream ();
			StringParserService stringParserService = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
			string header = stringParserService.Parse(StandardHeaderService.GetHeader(language), new string[,] { { "FileName", fileName } } );
			
			byte[] data = System.Text.Encoding.UTF8.GetBytes (header);
			ms.Write (data, 0, data.Length);
			
			data = System.Text.Encoding.UTF8.GetBytes (content);
			ms.Write (data, 0, data.Length);
			ms.Position = 0;
			return ms;
		}
		
		// Creates the text content of the file
		// The Language parameter is optional

		public virtual string CreateContent (string language)
		{
			return "";
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
			if (!string.IsNullOrEmpty (language)) {
				ILanguageBinding binding = GetLanguageBinding (language);
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
			}			
			
			if (ns.Length > 0)
				tags ["Namespace"] = ns;
			if (project != null)
				tags ["ProjectName"] = project.Name;
			if ((language != null) && (language.Length > 0))
				tags ["Language"] = language;
			if (languageExtension.Length > 0)
				tags ["LanguageExtension"] = languageExtension;
			tags ["FileName"] = fileName;
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
		
		public override bool IsValidName (string name, string language)
		{
			if (name.Length > 0) {
				if (language != null && language.Length > 0) {
					IDotNetLanguageBinding binding = GetLanguageBinding (language) as IDotNetLanguageBinding;
					if (binding != null) {
						System.CodeDom.Compiler.CodeDomProvider provider = binding.GetCodeDomProvider ();
						if (provider != null)
							return provider.IsValidIdentifier (name);
					}
				}
				return name.IndexOfAny (Path.InvalidPathChars) == -1;
			}
			else
				return false;
		}
	}
}
