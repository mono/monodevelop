//  ProjectTemplate.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

using MonoDevelop.Core;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;

using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	internal class OpenFileAction
	{
		string fileName;
		
		public OpenFileAction(string fileName)
		{
			this.fileName = fileName;
		}
		
		public void Run(ProjectCreateInformation projectCreateInformation)
		{
			IdeApp.Workbench.OpenDocument (projectCreateInformation.ProjectBasePath + Path.DirectorySeparatorChar + fileName);
		}
	}
	
	/// <summary>
	/// This class defines and holds the new project templates.
	/// </summary>
	internal class ProjectTemplate
	{
		public static ArrayList ProjectTemplates = new ArrayList();
		
		string    id           = null;
		string    originator   = null;
		string    created      = null;
		string    lastmodified = null;
		string    name         = null;
		string    category     = null;
		string    languagename = null;
		string    description  = null;
		string    icon         = null;
		string    wizardpath   = null;
		ArrayList actions      = new ArrayList();

		
		CombineDescriptor combineDescriptor = null;
		
#region Template Properties
		public string WizardPath {
			get {
				return wizardpath;
			}
		}
		
		public string Id {
			get { return id; }
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

		[Browsable(false)]
		public CombineDescriptor CombineDescriptor
		{
			get 
			{
				return combineDescriptor;
			}
		}
#endregion
		
		protected ProjectTemplate (RuntimeAddin addin, string id, ProjectTemplateCodon codon)
		{
			XmlDocument doc = codon.GetTemplate ();
						
			XmlElement config = doc.DocumentElement["TemplateConfiguration"];
			
			string category;
			if (config["_Category"] != null)
				category = addin.Localizer.GetString (config["_Category"].InnerText);
			else
				throw new InvalidOperationException ("Missing element '_Category' in file template: " + codon.Id);
			
			string languageElement  = (config["LanguageName"] == null)? null : config["LanguageName"].InnerText;
			
			//Find all of the languages that the template supports
			if (languageElement != null) {
				ArrayList templateLangs = new ArrayList ();
				foreach (string s in languageElement.Split (','))
					templateLangs.Add (s.Trim ());
				ExpandLanguageWildcards (templateLangs);
				
				//initialise this template (the first language)
				string language = (string) templateLangs [0];
				
				if (templateLangs.Count > 1)
					Initialise (addin, id, doc, language, language+"/"+category);
				else
					Initialise (addin, id, doc, language, category);
				
				//then add new templates for all other languages
				//Yes, creating more of the same type of object in the constructor is weird,
				//but it allows templates to specify multiple languages without changing the public API
				for (int i = 1; i < templateLangs.Count; i++) {
					try {
						language = (string) templateLangs [i];
						// per-language template instances should not share the XmlDocument
						ProjectTemplates.Add (new ProjectTemplate (addin, id, (XmlDocument) doc.Clone (), language, language+"/"+category));
					} catch (Exception e) {
						LoggingService.LogFatalError (GettextCatalog.GetString ("Error loading template {0}", codon.Id), e);
					}
				}
			} else {
				Initialise (addin, id, doc, null, category);
			}
		}
		
		
		private ProjectTemplate (RuntimeAddin addin, string id, XmlDocument doc, string languagename, string category)
		{
			Initialise (addin, id, doc, languagename, category);
		}
		
		private void Initialise (RuntimeAddin addin, string id, XmlDocument doc, string languagename, string category)
		{
			this.id = id;
			
			originator   = doc.DocumentElement.GetAttribute ("originator");
			created      = doc.DocumentElement.GetAttribute ("created");
			lastmodified = doc.DocumentElement.GetAttribute ("lastModified");
			
			XmlElement config = doc.DocumentElement["TemplateConfiguration"];
			
			if (config["Wizard"] != null) {
				wizardpath = config["Wizard"].InnerText;
			}
			
			this.name         = addin.Localizer.GetString (config["_Name"].InnerText);
			this.category     = category;
			this.languagename = languagename;
			
			if (config["_Description"] != null) {
				description  = addin.Localizer.GetString (config["_Description"].InnerText);
			}
			
			if (config["Icon"] != null) {
				icon = ResourceService.GetStockId (addin, config["Icon"].InnerText);
			}
			
			if (doc.DocumentElement["Combine"] != null) {
				combineDescriptor = CombineDescriptor.CreateCombineDescriptor(doc.DocumentElement["Combine"]);
			} else {
				throw new InvalidOperationException ("Combine element not found");
			}
			
			// Read Actions;
			if (doc.DocumentElement["Actions"] != null) {
				foreach (XmlElement el in doc.DocumentElement["Actions"]) {
					actions.Add(new OpenFileAction(el.Attributes["filename"].InnerText));
				}
			}
		}
		
		void ExpandLanguageWildcards (ArrayList list)
		{
			//Template can match all CodeDom .NET languages with a "*"
			if (list.Contains ("*")) {
				ILanguageBinding [] bindings = MonoDevelop.Projects.Services.Languages.GetLanguageBindings ();
				foreach (ILanguageBinding lb in bindings) {
					IDotNetLanguageBinding dnlang = lb as IDotNetLanguageBinding;
					if (dnlang != null && dnlang.GetCodeDomProvider () != null)
						list.Add (dnlang.Language);
				list.Remove ("*");
				}
			}
		}
		
		string lastCombine    = null;
		ProjectCreateInformation projectCreateInformation;
		
		public WorkspaceItem CreateWorkspaceItem (ProjectCreateInformation projectCreateInformation)
		{
			this.projectCreateInformation = projectCreateInformation;
			WorkspaceItem item = combineDescriptor.CreateEntry (projectCreateInformation, this.languagename);
			lastCombine = item.FileName;
			return item;
		}
		
		public SolutionEntityItem CreateProject (SolutionItem policyParent, ProjectCreateInformation projectCreateInformation)
		{
			this.projectCreateInformation = projectCreateInformation;
			
			// Create a project using the first child template of the combine template
			
			ISolutionItemDescriptor[] entries = combineDescriptor.EntryDescriptors;
			if (entries.Length == 0)
				throw new InvalidOperationException ("Combine template does not contain any project template");

			lastCombine = null;
			SolutionEntityItem it = entries[0].CreateItem (projectCreateInformation, this.languagename);
			entries[0].InitializeItem (policyParent, projectCreateInformation, this.languagename, it);
			return it;
		}
		
		public bool HasItemFeatures (SolutionFolder parentFolder, ProjectCreateInformation cinfo)
		{
			ISolutionItemDescriptor sid = combineDescriptor.EntryDescriptors [0];
			SolutionEntityItem sampleItem = sid.CreateItem (cinfo, languagename);
			return (SolutionItemFeatures.GetFeatures (parentFolder, sampleItem).Length > 0);
		}
		
		public void OpenCreatedCombine()
		{
			IAsyncOperation op = IdeApp.Workspace.OpenWorkspaceItem (lastCombine);
			op.WaitForCompleted ();
			if (op.Success) {
				foreach (OpenFileAction action in actions)
					action.Run(projectCreateInformation);
			}
		}

		static ProjectTemplate()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ProjectTemplates", OnExtensionChanged);
		}

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				ProjectTemplateCodon codon = (ProjectTemplateCodon) args.ExtensionNode;
				try {
					ProjectTemplates.Add (new ProjectTemplate (codon.Addin, codon.Id, codon));
				} catch (Exception e) {
					LoggingService.LogFatalError (e.ToString ());
				}
			}
			else {
				foreach (ProjectTemplate pt in ProjectTemplates) {
					ProjectTemplateCodon codon = (ProjectTemplateCodon) args.ExtensionNode;
					if (pt.Id == codon.Id) {
						ProjectTemplates.Remove (pt);
						break;
					}
				}
			}
		}
	}
}
