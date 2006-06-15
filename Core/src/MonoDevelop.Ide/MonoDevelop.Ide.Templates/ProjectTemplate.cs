// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;

using MonoDevelop.Core.AddIns;
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
		
		protected ProjectTemplate (AddIn addin, string id, string fileName)
		{
			this.id = id;
			
			Stream stream = addin.GetResourceStream (fileName);
			if (stream == null)
				throw new ApplicationException ("Template " + fileName + " not found");

			XmlDocument doc = new XmlDocument();
			try {
				doc.Load(stream);
			} finally {
				stream.Close ();
			}
			
			originator   = doc.DocumentElement.Attributes["originator"].InnerText;
			created      = doc.DocumentElement.Attributes["created"].InnerText;
			lastmodified = doc.DocumentElement.Attributes["lastModified"].InnerText;
			
			XmlElement config = doc.DocumentElement["TemplateConfiguration"];
			
			if (config["Wizard"] != null) {
				wizardpath = config["Wizard"].InnerText;
			}
			
			name         = GettextCatalog.GetString (config["_Name"].InnerText);
			category     = config["Category"].InnerText;
			
			if (config["LanguageName"] != null)
				languagename = config["LanguageName"].InnerText;
			
			if (config["_Description"] != null) {
				description  = GettextCatalog.GetString (config["_Description"].InnerText);
			}
			
			if (config["Icon"] != null) {
				icon = ResourceService.GetStockId (addin, config["Icon"].InnerText);
			}
			
			if (doc.DocumentElement["Combine"] != null) {
				combineDescriptor = CombineDescriptor.CreateCombineDescriptor(doc.DocumentElement["Combine"]);
			}
			
			// Read Actions;
			if (doc.DocumentElement["Actions"] != null) {
				foreach (XmlElement el in doc.DocumentElement["Actions"]) {
					actions.Add(new OpenFileAction(el.Attributes["filename"].InnerText));
				}
			}
		}
		
		string lastCombine    = null;
		ProjectCreateInformation projectCreateInformation;
		
		public string CreateCombine (ProjectCreateInformation projectCreateInformation)
		{
			this.projectCreateInformation = projectCreateInformation;
			lastCombine = combineDescriptor.CreateEntry (projectCreateInformation, this.languagename);
			return lastCombine;
		}
		
		public string CreateProject (ProjectCreateInformation projectCreateInformation)
		{
			this.projectCreateInformation = projectCreateInformation;
			
			// Create a project using the first child template of the combine template
			
			ICombineEntryDescriptor[] entries = combineDescriptor.EntryDescriptors;
			if (entries.Length == 0)
				throw new InvalidOperationException ("Combine template does not contain any project template");

			lastCombine = null;
			return entries[0].CreateEntry (projectCreateInformation, this.languagename);
		}
		
		public void OpenCreatedCombine()
		{
			IAsyncOperation op = IdeApp.ProjectOperations.OpenCombine (lastCombine);
			op.WaitForCompleted ();
			if (op.Success) {
				foreach (OpenFileAction action in actions)
					action.Run(projectCreateInformation);
			}
		}

		static ProjectTemplate()
		{
			Runtime.AddInService.RegisterExtensionItemListener ("/MonoDevelop/ProjectTemplates", OnExtensionChanged);
		}

		static void OnExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				ProjectTemplateCodon codon = (ProjectTemplateCodon) item;
				try {
					ProjectTemplates.Add (new ProjectTemplate (codon.AddIn, codon.ID, codon.Resource));
				} catch (Exception e) {
					Services.MessageService.ShowError (e, String.Format (GettextCatalog.GetString ("Error loading template from resource {0}"), codon.Resource));
				}
			}
		}
	}
}
