// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Templates
{
	/// <summary>
	/// This class defines and holds the new file templates.
	/// </summary>
	public class FileTemplate
	{
		public static ArrayList FileTemplates = new ArrayList();
		
		string    originator   = null;
		string    created      = null;
		string    lastmodified = null;
		string    name         = null;
		string    category     = null;
		string    languagename = "";
		string    projecttype  = "";
		string    description  = null;
		string    icon         = null;
		
		string    wizardpath   = null;
		
		ArrayList files        = new ArrayList(); // contains FileDescriptionTemplate classes
		
		XmlElement fileoptions = null;
		
		public string WizardPath {
			get {
				return wizardpath;
			}
		}
		
		public string Originator {
			get {
				return originator;
			}
		}
		
		public string Created {
			get {
				return created;
			}
		}
		
		public string LastModified {
			get {
				return lastmodified;
			}
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public string Category {
			get {
				return category;
			}
		}
		
		public string LanguageName {
			get {
				return languagename;
			}
		}
		
		public string ProjectType {
			get {
				return projecttype;
			}
		}
		
		public string Description {
			get {
				return description;
			}
		}
		
		public string Icon {
			get {
				return icon;
			}
		}
		
		public XmlElement FileOptions {
			get {
				return fileoptions;
			}
		}
		
		public ArrayList Files {
			get {
				return files;
			}
		}
		
		static FileTemplate LoadFileTemplate (AddIn addin, string filename)
		{
			Stream stream = addin.GetResourceStream (filename);
			if (stream == null)
				throw new ApplicationException ("Template " + filename + " not found");

			XmlDocument doc = new XmlDocument();
			try {
				doc.Load(stream);
			} finally {
				stream.Close ();
			}
			
			XmlElement config = doc.DocumentElement["TemplateConfiguration"];
			
			FileTemplate fileTemplate;
			
			if (config["Type"] != null) {
				string hn = config["Type"].InnerText;
				Type type = addin.GetType (hn);
				if (!(typeof(FileTemplate).IsAssignableFrom (type)))
					throw new InvalidOperationException ("The file template class '" + hn + "' must be a subclass of MonoDevelop.Ide.Templates.FileTemplate.");
				fileTemplate = (FileTemplate) Activator.CreateInstance (type);
			} else {
				fileTemplate = new FileTemplate ();
			}
			
			fileTemplate.originator   = doc.DocumentElement.Attributes["Originator"].InnerText;
			fileTemplate.created      = doc.DocumentElement.Attributes["Created"].InnerText;
			fileTemplate.lastmodified = doc.DocumentElement.Attributes["LastModified"].InnerText;
			
			fileTemplate.name         = GettextCatalog.GetString (config["_Name"].InnerText);
			fileTemplate.category     = config["Category"].InnerText;
			
			if (config["LanguageName"] != null)
				fileTemplate.languagename = config["LanguageName"].InnerText;
			
			if (config["ProjectType"] != null)
				fileTemplate.projecttype  = config["ProjectType"].InnerText;
			
			if (config["_Description"] != null) {
				fileTemplate.description  = GettextCatalog.GetString (config["_Description"].InnerText);
			}
			
			if (config["Icon"] != null) {
				fileTemplate.icon = ResourceService.GetStockId (addin, config["Icon"].InnerText);
			}
			
			if (config["Wizard"] != null) {
				fileTemplate.wizardpath = config["Wizard"].Attributes["path"].InnerText;
			}
			
			fileTemplate.fileoptions = doc.DocumentElement["FileOptions"];
			
			// load the files
			XmlElement files  = doc.DocumentElement["TemplateFiles"];
			XmlNodeList nodes = files.ChildNodes;
			foreach (XmlElement filenode in nodes) {
				string tfname = filenode.GetAttribute ("DefaultName") + filenode.GetAttribute ("DefaultExtension");
				XmlElement domElem = filenode ["CompileUnit"];
				
				FileDescriptionTemplate template;
				if (domElem != null)
					template = new FileDescriptionTemplate (tfname, domElem);
				else
					template = new FileDescriptionTemplate (tfname, filenode.InnerText);
				fileTemplate.files.Add(template);
			}
			return fileTemplate;
		}
		
		static FileTemplate()
		{
			LoadTemplates ((FileTemplateCodon[]) Runtime.AddInService.GetTreeItems ("/MonoDevelop/FileTemplates", typeof (FileTemplateCodon)));
		}

		static void LoadTemplates (FileTemplateCodon[] codons)
		{
			foreach (FileTemplateCodon codon in codons) {
				try {
					FileTemplate t = LoadFileTemplate (codon.AddIn, codon.Resource);
					FileTemplates.Add (t);
				} catch (Exception e) {
					Services.MessageService.ShowError (e, String.Format (GettextCatalog.GetString ("Error loading template from resource {0}"), codon.Resource));
				}
			}
		}
		
		public virtual bool Create (Project project, string directory, string language, string name)
		{
			if (WizardPath != null) {
				//IProperties customizer = new DefaultProperties();
				//customizer.SetProperty("Template", item);
				//customizer.SetProperty("Creator",  this);
				//WizardDialog wizard = new WizardDialog("File Wizard", customizer, item.WizardPath);
				//if (wizard.ShowDialog() == DialogResult.OK) {
					//DialogResult = DialogResult.OK;
				//}
				return false;
			} else {
				foreach (FileDescriptionTemplate newfile in Files)
					CreateFile (newfile, project, directory, language, name);
				return true;
			}
		}
		
		protected virtual void CreateFile (FileDescriptionTemplate newfile, Project project, string directory, string language, string name)
		{
			string fileName = name;
			string defaultName = newfile.Name;
			IDotNetLanguageBinding languageBinding;
			StringParserService sps = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
			
			if (language != "") {
				languageBinding = GetDotNetLanguageBinding (language);
				defaultName = languageBinding.GetFileName (Path.GetFileNameWithoutExtension (defaultName));
			}
			
			if (fileName != null) {
				if (Path.GetExtension (name) != Path.GetExtension (defaultName))
					fileName = fileName + Path.GetExtension (defaultName);
			} else {
				fileName = defaultName;
			}
			
			if (directory != null)
				fileName = Path.Combine (directory, fileName);
			
			if (project != null && project.IsFileInProject (fileName))
				throw new UserException (GettextCatalog.GetString ("The file '{0}' already exists in the project.", Path.GetFileName (fileName)));
			
			string content = CreateFileContent (newfile, language);
			
			string ns = project != null ? project.Name : "Global";
			string[,] tags = { {"Name", Path.GetFileNameWithoutExtension (fileName)}, {"Namespace", ns} };
			content = sps.Parse (content, tags);
			
			if (directory != null) {
				CreateProjectFile (project, fileName, content);
				IdeApp.Workbench.OpenDocument (fileName);
			} else {
				// Guess the mime type of the new file
				string fn = Path.GetTempFileName ();
				string ext = Path.GetExtension (fileName);
				int n=0;
				while (File.Exists (fn + n + ext))
					n++;
				File.Move (fn, fn + n + ext);
				string mimeType = Gnome.Vfs.MimeType.GetMimeTypeForUri (fn + n + ext);
				File.Delete (fn + n + ext);
				if (mimeType == null || mimeType == "")
					mimeType = "text";
				
				IdeApp.Workbench.NewDocument (fileName, mimeType, content);
			}
		}
		
		protected virtual void CreateProjectFile (Project project, string fileName, string fileContent)
		{
			if (System.IO.File.Exists (fileName)) {
				if (!Services.MessageService.AskQuestion (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to replace it?.", Path.GetFileName (fileName)), GettextCatalog.GetString ("MonoDevelop")))
					throw new UserException (GettextCatalog.GetString ("The file '{0}' could not be created.", fileName));
			}
			StreamWriter sw = new StreamWriter (fileName);
			sw.Write (fileContent);
			sw.Close ();
			
			if (project != null) {
				ProjectFile pfile = new ProjectFile (fileName, BuildAction.Compile);
				project.ProjectFiles.Add (pfile);
			}
		}
		
		protected virtual string CreateFileContent (FileDescriptionTemplate newfile, string language)
		{
			if (newfile.CodeDomContent != null)
				return GenerateContent (newfile, language);
			else
				return newfile.Content;
		}
		
		string GenerateContent (FileDescriptionTemplate newfile, string language)
		{
			if (language == null || language == "")
				throw new InvalidOperationException ("Language not defined in CodeDom based template.");
			
			IDotNetLanguageBinding binding = GetDotNetLanguageBinding (language);
			
			CodeDomProvider provider = binding.GetCodeDomProvider ();
			if (provider == null)
				throw new InvalidOperationException ("The language '" + language + "' does not have support for CodeDom.");

			XmlCodeDomReader xcd = new XmlCodeDomReader ();
			CodeCompileUnit cu = xcd.ReadCompileUnit (newfile.CodeDomContent);
			
			ICodeGenerator generator = provider.CreateGenerator();
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.IndentString = "\t";
			options.BracingStyle = "C";
			
			StringWriter sw = new StringWriter ();
			generator.GenerateCodeFromCompileUnit (cu, sw, options);
			sw.Close();
			
			string txt = sw.ToString ();
			int i = txt.IndexOf ("</autogenerated>");
			if (i == -1) return txt;
			i = txt.IndexOf ('\n', i);
			if (i == -1) return txt;
			i = txt.IndexOf ('\n', i + 1);
			if (i == -1) return txt;
			
			return txt.Substring (i+1);
		}
		
		IDotNetLanguageBinding GetDotNetLanguageBinding (string language)
		{
			IDotNetLanguageBinding binding = MonoDevelop.Projects.Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
			if (binding == null)
				throw new InvalidOperationException ("Language '" + language + "' not found");
			return binding;
		}
	}
}
