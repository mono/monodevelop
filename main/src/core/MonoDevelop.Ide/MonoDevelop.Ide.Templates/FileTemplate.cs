// FileTemplate.cs
//
// Author:
//   Mike Krüger (mkrueger@novell.com)
//   Lluis Sanchez Gual (lluis@novell.com)
//   Michael Hutchinson (mhutchinson@novell.com)
//   Marek Sieradzki (marek.sieradzki@gmail.com)
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


using System;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using System.IO;
using Gtk;
using System.Collections.Generic;
using System.Collections;
using MonoDevelop.Ide.Codons;
using System.Xml;
using System.Linq;

namespace MonoDevelop.Ide.Templates
{
    class FileTemplate
    {
        

        public static List<FileTemplate> fileTemplates = new List<FileTemplate> ();


        private List<FileDescriptionTemplate> files = new List<FileDescriptionTemplate> ();
        public List<FileDescriptionTemplate> Files
        {
            get { return files; }
        }

        private List<FileTemplateCondition> conditions = new List<FileTemplateCondition> ();
        public List<FileTemplateCondition> Сonditions
        {
            get { return conditions; }
        }

        private string id = String.Empty;
        public string Id
        {
            get { return id; }
        }

        private string icon = String.Empty;
        public IconId Icon
        {
            get { return icon; }
        }

        private string category = String.Empty;
        public string Category
        {
            get { return category; }
        }

        private string wizardPath = String.Empty;
        public string WizardPath
        {
            get { return wizardPath; }
        }

        private string description = String.Empty;
        public string Description
        {
            get { return description; }
        }

        private bool isFixedFilename = false;
        public bool IsFixedFilename
        {
            get { return isFixedFilename; }
        }

        private string defaultFilename = String.Empty;
        public string DefaultFilename
        {
            get { return defaultFilename; }
        }

        public string name = String.Empty;
        public string Name
        {
            get { return name; }
        }

        private string languageName = String.Empty;
        public string LanguageName
        {
            get { return languageName; }
        }

        private string originator = String.Empty;
        public string Originator
        {
            get { return originator; }
        }

        private string created = String.Empty;
        public  string Created
        {
            get { return created; }
        }

        private string lastModified = String.Empty;
        public  string LastModified
        {
            get { return lastModified; }
        }

        private string projecttype = String.Empty;
        public  string ProjectType
        {
            get { return projecttype; }
        }
		
		
        private static FileTemplate LoadFileTemplate (RuntimeAddin addin, ProjectTemplateCodon codon)
        {
			XmlDocument xmlDocument = codon.GetTemplate ();
			FilePath baseDirectory = codon.BaseDirectory;
			
            //Configuration
			XmlElement xmlNodeConfig = xmlDocument.DocumentElement["TemplateConfiguration"];

            FileTemplate fileTemplate = null;
            if (xmlNodeConfig["Type"] != null) {
                Type configType = addin.GetType (xmlNodeConfig["Type"].InnerText);

                if (typeof (FileTemplate).IsAssignableFrom (configType)) {
                    fileTemplate = (FileTemplate)Activator.CreateInstance (configType);
                }
                else
                    throw new InvalidOperationException (string.Format ("The file template class '{0}' must be a subclass of MonoDevelop.Ide.Templates.FileTemplate", xmlNodeConfig["Type"].InnerText));
            }
            else
                fileTemplate = new FileTemplate ();

            fileTemplate.originator = xmlDocument.DocumentElement.GetAttribute ("Originator");
            fileTemplate.created = xmlDocument.DocumentElement.GetAttribute ("Created");
            fileTemplate.lastModified = xmlDocument.DocumentElement.GetAttribute ("LastModified");

            if (xmlNodeConfig["_Name"] != null) {
                fileTemplate.name = xmlNodeConfig["_Name"].InnerText;
            }
            else {
                throw new InvalidOperationException (string.Format ("Missing element '_Name' in file template: {0}", codon.Id));
            }

            if (xmlNodeConfig["_Category"] != null) {
                fileTemplate.category = xmlNodeConfig["_Category"].InnerText;
            }
            else {
                throw new InvalidOperationException (string.Format ("Missing element '_Category' in file template: {0}", codon.Id));
            }

            if (xmlNodeConfig["LanguageName"] != null) {
                fileTemplate.languageName = xmlNodeConfig["LanguageName"].InnerText;
            }

            if (xmlNodeConfig["ProjectType"] != null) {
                fileTemplate.projecttype = xmlNodeConfig["ProjectType"].InnerText;
            }

            if (xmlNodeConfig["_Description"] != null) {
                fileTemplate.description = xmlNodeConfig["_Description"].InnerText;
            }

            if (xmlNodeConfig["Icon"] != null) {
                fileTemplate.icon = ImageService.GetStockId (addin, xmlNodeConfig["Icon"].InnerText, IconSize.Dnd); //xmlNodeConfig["_Description"].InnerText;
            }

            if (xmlNodeConfig["Wizard"] != null) {
                fileTemplate.icon = xmlNodeConfig["Wizard"].Attributes["path"].InnerText;
            }

            if (xmlNodeConfig["DefaultFilename"] != null) {
                fileTemplate.defaultFilename = xmlNodeConfig["DefaultFilename"].InnerText;
				string isFixed = xmlNodeConfig["DefaultFilename"].GetAttribute ("IsFixed");
				if (isFixed.Length > 0) {
					bool bFixed;
					if (bool.TryParse (isFixed, out bFixed))
						fileTemplate.isFixedFilename = bFixed;
					else
						throw new InvalidOperationException ("Invalid value for IsFixed in template.");
				}
            }

            //Template files
            XmlNode xmlNodeTemplates = xmlDocument.DocumentElement["TemplateFiles"];

			if(xmlNodeTemplates != null) {
				foreach(XmlNode xmlNode in xmlNodeTemplates.ChildNodes) {
					if(xmlNode is XmlElement) {
						fileTemplate.files.Add (
							FileDescriptionTemplate.CreateTemplate ((XmlElement)xmlNode, baseDirectory));
					}
				}
			}

            //Conditions
            XmlNode xmlNodeConditions = xmlDocument.DocumentElement["Conditions"];
			if(xmlNodeConditions != null) {
				foreach(XmlNode xmlNode in xmlNodeConditions.ChildNodes) {
					if(xmlNode is XmlElement) {
						fileTemplate.conditions.Add (FileTemplateCondition.CreateCondition ((XmlElement)xmlNode));
					}
				}
			}

            return fileTemplate;
        }

		static FileTemplate()
        {
            AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/FileTemplates", OnExtensionChanged);
        }

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
        {
            if (args.Change == ExtensionChange.Add) {
				ProjectTemplateCodon codon = (ProjectTemplateCodon) args.ExtensionNode;
                try {
                    FileTemplate t = LoadFileTemplate (codon.Addin, codon);
                    t.id = codon.Id;
                    fileTemplates.Add (t);
				} catch (Exception e) {
					string extId = null, addinId = null;
					if (codon != null) {
						if (codon.HasId)
							extId = codon.Id;
						if (codon.Addin != null)
							addinId = codon.Addin.Id;
					}
					LoggingService.LogError ("Error loading template id {0} in addin {1}:\n{2}",
					                         extId ?? "(null)", addinId ?? "(null)", e.ToString ());
				}
            }
            else {
				ProjectTemplateCodon codon = (ProjectTemplateCodon) args.ExtensionNode;
                foreach (FileTemplate t in fileTemplates) {
                    if (t.Id == codon.Id) {
                        fileTemplates.Remove (t);
                        break;
                    }
                }
            }
        }

		internal static List<FileTemplate> GetFileTemplates (Project project, string projectPath)
		{
			var list = new List<FileTemplate> ();
			foreach (var t in fileTemplates) {
				if (t.IsValidForProject (project, projectPath))
					list.Add (t);
			}
			return list;
		}

		internal static FileTemplate GetFileTemplateByID (string templateID)
        {
            foreach (FileTemplate t in fileTemplates)
                if (t.Id == templateID)
                    return t;

            return null;
        }

		public virtual bool Create (SolutionItem policyParent, Project project, string directory, string language, string name)
        {
            if (!String.IsNullOrEmpty(WizardPath)) {
                //Properties customizer = new Properties();
                //customizer.Set("Template", item);
                //customizer.Set("Creator",  this);
                //WizardDialog wizard = new WizardDialog("File Wizard", customizer, item.WizardPath);
                //if (wizard.ShowDialog() == DialogResult.OK) {
                //DialogResult = DialogResult.OK;
                //}
                return false;
			} else {
                foreach (FileDescriptionTemplate newfile in Files)
                    if (!CreateFile (newfile, policyParent, project, directory, language, name))
                        return false;
                return true;
            }
        }

		public virtual bool IsValidName (string name, string language)
        {
            if (isFixedFilename)
                return (name == defaultFilename);

            bool valid = true;
            foreach (FileDescriptionTemplate templ in Files)
                if (!templ.IsValidName (name, language))
                    valid = false;

            return valid;
        }

		public static string GuessMimeType (string fileName)
		{
			// Guess the mime type of the new file
			string fn = Path.GetTempFileName ();
			string ext = Path.GetExtension (fileName);
			int n = 0;
			while (File.Exists (fn + n + ext))
				n++;
			FileService.MoveFile (fn, fn + n + ext);
			string mimeType = DesktopService.GetMimeTypeForUri (fn + n + ext);
			FileService.DeleteFile (fn + n + ext);
			if (mimeType == null || mimeType == "")
				mimeType = "text";
			return mimeType;
		}
		
		public virtual bool CanCreateUnsavedFiles (FileDescriptionTemplate newfile, SolutionItem policyParent, Project project, string directory, string language, string name)
		{
			if (project != null) {
				return true;
			} else {
				SingleFileDescriptionTemplate singleFile = newfile as SingleFileDescriptionTemplate;
				if (singleFile == null)
					return false;

				if (directory != null) {
					return true;
				} else {
					string fileName = singleFile.GetFileName (policyParent, project, language, directory, name);
					string mimeType = GuessMimeType (fileName);
					return DisplayBindingService.GetDefaultViewBinding (null, mimeType, null) != null;
				}
			}
		}

		protected virtual bool CreateFile (FileDescriptionTemplate newfile, SolutionItem policyParent, Project project, string directory, string language, string name)
        {
            if (project != null) {
				var model = project.GetStringTagModel (new DefaultConfigurationSelector ());
				newfile.SetProjectTagModel (model);
				try {
	                if (newfile.AddToProject (policyParent, project, language, directory, name)) {
	                    newfile.Show ();
	                    return true;
					}
				} finally {
					newfile.SetProjectTagModel (null);
				}
			} else {
                SingleFileDescriptionTemplate singleFile = newfile as SingleFileDescriptionTemplate;
                if (singleFile == null)
                    throw new InvalidOperationException ("Single file template expected");

                if (directory != null) {
                    string fileName = singleFile.SaveFile (policyParent, project, language, directory, name);
                    if (fileName != null) {
						IdeApp.Workbench.OpenDocument (fileName, project);
                        return true;
                    }
				} else {
                    string fileName = singleFile.GetFileName (policyParent, project, language, directory, name);
                    Stream stream = singleFile.CreateFileContent (policyParent, project, language, fileName, name);

					string mimeType = GuessMimeType (fileName);
					IdeApp.Workbench.NewDocument (fileName, mimeType, stream);
					return true;
                }
            }
            return false;
        }

		protected virtual bool IsValidForProject (Project project, string projectPath)
        {
            // When there is no project, only single template files can be created.
            if (project == null) {
                foreach (FileDescriptionTemplate f in files)
                    if (!(f is SingleFileDescriptionTemplate))
                        return false;
            }

            // Filter on templates
            foreach (FileDescriptionTemplate f in files)
                if (!f.SupportsProject (project, projectPath))
                    return false;

            //filter on conditions
            if (project != null) {
				if (!string.IsNullOrEmpty (projecttype) && project.GetProjectTypes ().All (p => p != projecttype))
                    return false;

                foreach (FileTemplateCondition condition in conditions)
                    if (!condition.ShouldEnableFor (project, projectPath))
                        return false;
            }

            return true;
        }

		public virtual List<string> GetCompatibleLanguages (Project project, string projectPath)
        {
            if (project == null)
                return SupportedLanguages;

            //find the languages that both the template and the project support
            List<string> langMatches = MatchLanguagesWithProject (project);

            //filter on conditions
            List<string> filtered = new List<string> ();
            foreach (string lang in langMatches) {
                bool shouldEnable = true;
                foreach (FileTemplateCondition condition in conditions) {
                    if (!condition.ShouldEnableFor (project, projectPath, lang)) {
                        shouldEnable = false;
                        break;
                    }
                }
                if (shouldEnable)
                    filtered.Add (lang);
            }

            return filtered;
        }

        //The languages that the template supports
        //FIXME: would it be memory-effective to cache this?
		List<string> SupportedLanguages {
			get {
                List<string> templateLangs = new List<string> ();
                foreach (string s in this.LanguageName.Split (','))
                    templateLangs.Add (s.Trim ());
                ExpandLanguageWildcards (templateLangs);
                return templateLangs;
            }
        }

		List<string> MatchLanguagesWithProject (Project project)
        {
            //The languages that the project supports
            List<string> projectLangs = new List<string> (project.SupportedLanguages);
            ExpandLanguageWildcards (projectLangs);

            List<string> templateLangs = SupportedLanguages;

            //Find all matches between the language strings of project and template
            List<string> langMatches = new List<string> ();

            foreach (string templLang in templateLangs)
                foreach (string projLang in projectLangs)
                    if (templLang == projLang)
                        langMatches.Add (projLang);

            //Eliminate duplicates
            int pos = 0;
            while (pos < langMatches.Count) {
				int next = langMatches.IndexOf (langMatches [pos], pos +1);
                if (next != -1)
                    langMatches.RemoveAt (next);
                else
                    pos++;
            }

            return langMatches;
        }

		void ExpandLanguageWildcards (List<string> list)
        {
            //Template can match all CodeDom .NET languages with a "*"
            if (list.Contains ("*")) {
                foreach (var lb in LanguageBindingService.LanguageBindings) {
                    IDotNetLanguageBinding dnlang = lb as IDotNetLanguageBinding;
                    if (dnlang != null && dnlang.GetCodeDomProvider () != null)
                        list.Add (dnlang.Language);
                    list.Remove ("*");
                }
            }
        }
    }
}
