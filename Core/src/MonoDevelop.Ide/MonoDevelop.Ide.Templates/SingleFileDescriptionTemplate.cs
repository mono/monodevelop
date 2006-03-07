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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Templates
{
	public class SingleFileDescriptionTemplate: FileDescriptionTemplate
	{
		string name;
		string generatedFile;
		
		public override void Load (XmlElement filenode)
		{
			name = filenode.GetAttribute ("name");
			if (name == "")
				name = filenode.GetAttribute ("DefaultName") + filenode.GetAttribute ("DefaultExtension");
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
				if (!Services.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("File {0} already exists, do you want to overwrite\nthe existing file ?"), file), GettextCatalog.GetString ("File already exists"))) {
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
		public string GetFileName (Project project, string language, string baseDirectory, string entryName)
		{
			string fileName = entryName;
			string defaultName = name;
			
			if (language != "") {
				IDotNetLanguageBinding languageBinding = GetDotNetLanguageBinding (language);
				defaultName = languageBinding.GetFileName (Path.GetFileNameWithoutExtension (defaultName));
			}
			
			if (fileName != null) {
				if (Path.GetExtension (name) != Path.GetExtension (defaultName))
					fileName = fileName + Path.GetExtension (defaultName);
			} else {
				fileName = defaultName;
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
			
			string content = CreateContent (language);
			
			DotNetProject netProject = project as DotNetProject;
			string ns = netProject != null ? netProject.GetDefaultNamespace (fileName) : "";
			string cname = Path.GetFileNameWithoutExtension (fileName);
			string[,] tags = { 
				{"Name", cname}, 
				{"Namespace", ns},
				{"FullName", ns.Length > 0 ? ns + "." + cname : cname},
				{"ProjectName", project != null ? project.Name : ""}
			};
				
			content = sps.Parse (content, tags);
			
			MemoryStream ms = new MemoryStream ();
			byte[] data = System.Text.Encoding.UTF8.GetBytes (content);
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
		
		protected IDotNetLanguageBinding GetDotNetLanguageBinding (string language)
		{
			IDotNetLanguageBinding binding = MonoDevelop.Projects.Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
			if (binding == null)
				throw new InvalidOperationException ("Language '" + language + "' not found");
			return binding;
		}
	}
}
