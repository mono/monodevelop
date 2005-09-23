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

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.Internal.Templates
{
	/// <summary>
	/// This class defines and holds the new file templates.
	/// </summary>
	internal class FileTemplate
	{
		public static ArrayList FileTemplates = new ArrayList();
		
		string    originator   = null;
		string    created      = null;
		string    lastmodified = null;
		string    name         = null;
		string    category     = null;
		string    languagename = null;
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
		
		public FileTemplate (AddIn addin, string filename)
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
			
			originator   = doc.DocumentElement.Attributes["Originator"].InnerText;
			created      = doc.DocumentElement.Attributes["Created"].InnerText;
			lastmodified = doc.DocumentElement.Attributes["LastModified"].InnerText;
			
			name         = GettextCatalog.GetString (config["_Name"].InnerText);
			category     = config["Category"].InnerText;
			languagename = config["LanguageName"].InnerText;
			
			if (config["_Description"] != null) {
				description  = GettextCatalog.GetString (config["_Description"].InnerText);
			}
			
			if (config["Icon"] != null) {
				icon         = ResourceService.GetStockId (addin, config["Icon"].InnerText);
			}
			
			if (config["Wizard"] != null) {
				wizardpath = config["Wizard"].Attributes["path"].InnerText;
			}
			
			fileoptions = doc.DocumentElement["FileOptions"];
			
			// load the files
			XmlElement files  = doc.DocumentElement["TemplateFiles"];
			XmlNodeList nodes = files.ChildNodes;
			foreach (XmlElement filenode in nodes) {
				FileDescriptionTemplate template = new FileDescriptionTemplate(filenode.Attributes["DefaultName"].InnerText + filenode.Attributes["DefaultExtension"].InnerText, filenode.InnerText);
				this.files.Add(template);
			}
		}
		
		static void LoadFileTemplate (AddIn addin, string filename)
		{
			FileTemplates.Add(new FileTemplate (addin, filename));
		}
		
		static FileTemplate()
		{
			LoadTemplates ((FileTemplateCodon[])(AddInTreeSingleton.AddInTree.GetTreeNode ("/MonoDevelop/FileTemplates").BuildChildItems (new object ()).ToArray (typeof (FileTemplateCodon))));
		}

		static void LoadTemplates (FileTemplateCodon[] codons)
		{
			foreach (FileTemplateCodon codon in codons) {
				try {
					LoadFileTemplate (codon.AddIn, codon.Resource);
				} catch (Exception e) {
					Runtime.MessageService.ShowError (e, String.Format (GettextCatalog.GetString ("Error loading template from resource {0}"), codon.Resource));
				}
			}
		}
	}
}
