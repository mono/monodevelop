// FileTemplate.cs
//
// Author:
//   Mike Krüger (mkrueger@novell.com)
//   Lluis Sanchez Gual (lluis@novell.com)
//   Michael Hutchinson (mhutchinson@novell.com)
//   Marek Sieradzki (marek.sieradzki@gmail.com)
//   Viktoria Dudka (viktoriad@remobjects.com)
//   Vincent Dondain (vincent@xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Gtk;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Ide.Templates
{
	class FileTemplate
	{
		public const string DefaultCategoryKey = "DefaultKey";

		public Dictionary<string, string> Categories { get; private set; } = new Dictionary<string, string> ();

		public List<FileTemplateCondition> Conditions { get; private set; } = new List<FileTemplateCondition> ();

		public string Created { get; private set; } = String.Empty;

		public string DefaultFilename { get; private set; } = String.Empty;

		public string Description { get; private set; } = String.Empty;

		public List<FileDescriptionTemplate> Files { get; private set; } = new List<FileDescriptionTemplate> ();

		public IconId Icon { get; private set; } = String.Empty;

		public string Id { get; private set; } = String.Empty;

		public bool IsFixedFilename { get; private set; }

		public string LanguageName { get; private set; } = String.Empty;

		public string LastModified { get; private set; } = String.Empty;

		public string Name { get; private set; } = String.Empty;

		public string Originator { get; private set; } = String.Empty;

		public List<string> ProjectTypes { get; private set; } = new List<string> ();

		public string WizardPath { get; private set; } = String.Empty;

		static FileTemplate LoadFileTemplate (RuntimeAddin addin, ProjectTemplateCodon codon)
		{
			return LoadFileTemplate (addin, codon.GetTemplate (), codon.BaseDirectory, codon.Id);
		}

		internal static FileTemplate LoadFileTemplate (RuntimeAddin addin, XmlDocument xmlDocument, FilePath baseDirectory = new FilePath (), string templateId = "")
		{
			//Configuration
			XmlElement xmlNodeConfig = xmlDocument.DocumentElement ["TemplateConfiguration"];

			FileTemplate fileTemplate;
			if (xmlNodeConfig ["Type"] != null) {
				Type configType = addin.GetType (xmlNodeConfig ["Type"].InnerText);

				if (typeof(FileTemplate).IsAssignableFrom (configType)) {
					fileTemplate = (FileTemplate)Activator.CreateInstance (configType);
				} else
					throw new InvalidOperationException (string.Format ("The file template class '{0}' must be a subclass of MonoDevelop.Ide.Templates.FileTemplate", xmlNodeConfig ["Type"].InnerText));
			} else
				fileTemplate = new FileTemplate ();

			fileTemplate.Originator = xmlDocument.DocumentElement.GetAttribute ("Originator");
			fileTemplate.Created = xmlDocument.DocumentElement.GetAttribute ("Created");
			fileTemplate.LastModified = xmlDocument.DocumentElement.GetAttribute ("LastModified");

			if (xmlNodeConfig ["_Name"] != null) {
				fileTemplate.Name = Localize (addin, xmlNodeConfig ["_Name"].InnerText);
			} else {
				throw new InvalidOperationException (string.Format ("Missing element '_Name' in file template: {0}", templateId));
			}

			if (xmlNodeConfig ["LanguageName"] != null) {
				fileTemplate.LanguageName = xmlNodeConfig ["LanguageName"].InnerText;
			}

			if (xmlNodeConfig ["ProjectType"] != null) {
				var projectTypeList = new List<string> ();
				foreach (var item in xmlNodeConfig["ProjectType"].InnerText.Split (','))
					projectTypeList.Add (item.Trim ());
				fileTemplate.ProjectTypes = projectTypeList;
			}

			fileTemplate.Categories = new Dictionary<string, string> ();
			if (xmlNodeConfig ["_Category"] != null) {
				foreach (XmlNode xmlNode in xmlNodeConfig.GetElementsByTagName ("_Category")) {
					if (xmlNode is XmlElement) {
						string projectType = "";
						if (xmlNode.Attributes ["projectType"] != null)
							projectType = xmlNode.Attributes ["projectType"].Value;

						if (!string.IsNullOrEmpty (projectType) && fileTemplate.ProjectTypes.Contains (projectType))
							fileTemplate.Categories.Add (projectType, xmlNode.InnerText);
						else if (!fileTemplate.Categories.ContainsKey (DefaultCategoryKey))
							fileTemplate.Categories.Add (DefaultCategoryKey, xmlNode.InnerText);
					}
				}
			} else {
				throw new InvalidOperationException (string.Format ("Missing element '_Category' in file template: {0}", templateId));
			}

			if (xmlNodeConfig ["_Description"] != null) {
				fileTemplate.Description = Localize (addin, xmlNodeConfig ["_Description"].InnerText);
			}

			if (xmlNodeConfig ["Icon"] != null) {
				fileTemplate.Icon = ImageService.GetStockId (addin, xmlNodeConfig ["Icon"].InnerText, IconSize.Dnd);
			}

			if (xmlNodeConfig ["Wizard"] != null) {
				fileTemplate.Icon = xmlNodeConfig ["Wizard"].Attributes ["path"].InnerText;
			}

			if (xmlNodeConfig ["DefaultFilename"] != null) {
				fileTemplate.DefaultFilename = xmlNodeConfig ["DefaultFilename"].InnerText;
				string isFixed = xmlNodeConfig ["DefaultFilename"].GetAttribute ("IsFixed");
				if (isFixed.Length > 0) {
					bool bFixed;
					if (bool.TryParse (isFixed, out bFixed))
						fileTemplate.IsFixedFilename = bFixed;
					else
						throw new InvalidOperationException ("Invalid value for IsFixed in template.");
				}
			}

			//Template files
			XmlNode xmlNodeTemplates = xmlDocument.DocumentElement ["TemplateFiles"];

			if (xmlNodeTemplates != null) {
				foreach (XmlNode xmlNode in xmlNodeTemplates.ChildNodes) {
					var xmlElement = xmlNode as XmlElement;
					if (xmlElement != null) {
						fileTemplate.Files.Add (
							FileDescriptionTemplate.CreateTemplate (xmlElement, baseDirectory));
					}
				}
			}

			//Conditions
			XmlNode xmlNodeConditions = xmlDocument.DocumentElement ["Conditions"];
			if (xmlNodeConditions != null) {
				foreach (XmlNode xmlNode in xmlNodeConditions.ChildNodes) {
					var xmlElement = xmlNode as XmlElement;
					if (xmlElement != null) {
						fileTemplate.Conditions.Add (FileTemplateCondition.CreateCondition (xmlElement));
					}
				}
			}

			return fileTemplate;
		}

		static FileTemplate LoadTemplate (ProjectTemplateCodon codon)
		{
			try {
				FileTemplate t = LoadFileTemplate (codon.Addin, codon);
				t.Id = codon.Id;
				return t;
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
			return null;
		}

		static string EXTENSION_PATH = "/MonoDevelop/Ide/FileTemplates";

		internal static List<FileTemplate> GetFileTemplates (Project project, string projectPath)
		{
			var extensionContext = AddinManager.CreateExtensionContext ();
			if (project != null) {
				extensionContext.RegisterCondition ("AppliesTo", new AppliesToCondition (project));
				extensionContext.RegisterCondition ("FlavorType", new FlavorTypeCondition (project));
				extensionContext.RegisterCondition ("ProjectTypeId", new ProjectTypeIdCondition (project));
			} else {
				extensionContext.RegisterCondition ("AppliesTo", new TrueCondition ());
				extensionContext.RegisterCondition ("FlavorType", new TrueCondition ());
				extensionContext.RegisterCondition ("ProjectTypeId", new TrueCondition ());
			}

			var list = new List<FileTemplate> ();
			foreach (var node in extensionContext.GetExtensionNodes<ProjectTemplateCodon> (EXTENSION_PATH)) {
				var template = LoadTemplate (node); 
				if (template != null && template.IsValidForProject (project, projectPath)) {
					list.Add (template);
				}
			}

			return list;
		}

		class TrueCondition : ConditionType
		{
			public override bool Evaluate (NodeElement conditionNode)
			{
				return true;
			}
		}

		internal static FileTemplate GetFileTemplateByID (string templateID)
		{
			var node = AddinManager.GetExtensionNodes<ProjectTemplateCodon> (EXTENSION_PATH)
								   .FirstOrDefault (n => n.Id == templateID);
			return node == null ? null : LoadTemplate (node);
		}

		public virtual bool Create (SolutionFolderItem policyParent, Project project, string directory, string language, string name)
		{
			if (!String.IsNullOrEmpty (WizardPath)) {
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
			if (IsFixedFilename)
				return (name == DefaultFilename);

			bool valid = true;
			foreach (FileDescriptionTemplate templ in Files)
				valid &= templ.IsValidName (name, language);

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
			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text";
			return mimeType;
		}

		public virtual bool CanCreateUnsavedFiles (FileDescriptionTemplate newfile, SolutionFolderItem policyParent, Project project, string directory, string language, string name)
		{
			if (project != null) {
				return true;
			} else {
				var singleFile = newfile as SingleFileDescriptionTemplate;
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

		protected virtual bool CreateFile (FileDescriptionTemplate newfile, SolutionFolderItem policyParent, Project project, string directory, string language, string name)
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
				var singleFile = newfile as SingleFileDescriptionTemplate;
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
				foreach (FileDescriptionTemplate f in Files)
					if (!(f is SingleFileDescriptionTemplate))
						return false;
			}

			// Filter on templates
			foreach (FileDescriptionTemplate f in Files)
				if (!f.SupportsProject (project, projectPath))
					return false;

			//filter on conditions
			if (project != null) {
				// When file template's project types don't match the current project's type.
				if (ProjectTypes.Any () && project.GetTypeTags ().All (p => !ProjectTypes.Contains (p)))
					return false;

				foreach (FileTemplateCondition condition in Conditions)
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
			var filtered = new List<string> ();
			foreach (string lang in langMatches) {
				bool shouldEnable = true;
				foreach (FileTemplateCondition condition in Conditions) {
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
				var templateLangs = new List<string> ();
				foreach (string s in LanguageName.Split (','))
					templateLangs.Add (s.Trim ());
				ExpandLanguageWildcards (templateLangs);
				return templateLangs;
			}
		}

		List<string> MatchLanguagesWithProject (Project project)
		{
			//The languages that the project supports
			var projectLangs = new List<string> (project.SupportedLanguages);
			ExpandLanguageWildcards (projectLangs);

			List<string> templateLangs = SupportedLanguages;

			//Find all matches between the language strings of project and template
			var langMatches = new List<string> ();

			foreach (string templLang in templateLangs)
				foreach (string projLang in projectLangs)
					if (templLang == projLang)
						langMatches.Add (projLang);

			//Eliminate duplicates
			int pos = 0;
			while (pos < langMatches.Count) {
				int next = langMatches.IndexOf (langMatches [pos], pos + 1);
				if (next != -1)
					langMatches.RemoveAt (next);
				else
					pos++;
			}

			return langMatches;
		}

		static void ExpandLanguageWildcards (ICollection<string> list)
		{
			//Template can match all CodeDom .NET languages with a "*"
			if (list.Contains ("*")) {
				foreach (var lb in LanguageBindingService.LanguageBindings) {
					if (lb.GetCodeDomProvider () != null)
						list.Add (lb.Language);
					list.Remove ("*");
				}
			}
		}

		/// <summary>
		/// The addin may be null if the file template is loaded by a unit test.
		/// </summary>
		static string Localize (RuntimeAddin addin, string s)
		{
			if (addin != null)
				return addin.Localizer.GetString (s);

			return GettextCatalog.GetString (s);
		}
	}
}
