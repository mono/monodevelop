// ProjectTemplate.cs
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
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Templates
{
	internal class ProjectTemplate
	{
		public static List<ProjectTemplate> ProjectTemplates = new List<ProjectTemplate> ();

		private List<string> actions = new List<string> ();

		private string createdSolutionName;
		private ProjectCreateInformation createdProjectInformation = null;

		private SolutionDescriptor solutionDescriptor = null;
		public SolutionDescriptor SolutionDescriptor
		{
			get { return solutionDescriptor; }
		}

		private string languagename;
		public string LanguageName
		{
			get { return languagename; }
		}

		private string id;
		public string Id
		{
			get { return id; }
		}

		private string category;
		public string Category
		{
			get { return category; }
		}

		private string icon;
		public IconId Icon
		{
			get { return icon; }
		}

		private string description;
		public string Description
		{
			get { return description; }
		}

		private string name;
		public string Name
		{
			get { return name; }
		}

		private string originator;
		public string Originator
		{
			get { return originator; }
		}

		private string created;
		public string Created
		{
			get { return created; }
		}

		private string lastModified;
		public string LastModified
		{
			get { return lastModified; }
		}

		private string wizardPath;
		public string WizardPath
		{
			get { return wizardPath; }
		}



		//constructors
		static ProjectTemplate ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ProjectTemplates", OnExtensionChanged);
		}

		protected ProjectTemplate (RuntimeAddin addin, string id, ProjectTemplateCodon codon, string overrideLanguage)
		{
			XmlDocument xmlDocument = codon.GetTemplate ();

			XmlElement xmlConfiguration = xmlDocument.DocumentElement ["TemplateConfiguration"];

			if (xmlConfiguration ["_Category"] != null) {
				category = addin.Localizer.GetString (xmlConfiguration ["_Category"].InnerText);
			}
			else
				throw new InvalidOperationException (string.Format ("_Category missing in file template {0}", codon.Id));


			if (!string.IsNullOrEmpty (overrideLanguage)) {
				this.languagename = overrideLanguage;
				this.category = overrideLanguage + "/" + this.category;
			}
			else if (xmlConfiguration ["LanguageName"] != null) {

				List<string> listLanguages = new List<string> ();
				foreach (string item in xmlConfiguration ["LanguageName"].InnerText.Split (','))
					listLanguages.Add (item.Trim ());

				ExpandLanguageWildcards (listLanguages);

				this.languagename = listLanguages [0];
				
				if (listLanguages.Count > 1 && !String.IsNullOrEmpty (languagename) && !category.StartsWith (languagename + "/"))
					category = languagename + "/" + category;

				for (int i = 1; i < listLanguages.Count; i++) {
					string language = listLanguages[i];
					try {
						ProjectTemplates.Add (new ProjectTemplate (addin, id, codon, language));
					} catch (Exception e) {
						LoggingService.LogError (GettextCatalog.GetString ("Error loading template {0} for language {1}", codon.Id, language), e);
					}
				}
			}

			this.id = id;

			this.originator = xmlDocument.DocumentElement.GetAttribute ("originator");
			this.created = xmlDocument.DocumentElement.GetAttribute ("created");
			this.lastModified = xmlDocument.DocumentElement.GetAttribute ("lastModified");

			if (xmlConfiguration ["Wizard"] != null) {
				this.wizardPath = xmlConfiguration ["Wizard"].InnerText;
			}

			if (xmlConfiguration ["_Name"] != null) {
				this.name = addin.Localizer.GetString (xmlConfiguration ["_Name"].InnerText);
			}

			if (xmlConfiguration ["_Description"] != null) {
				this.description = addin.Localizer.GetString (xmlConfiguration ["_Description"].InnerText);
			}

			if (xmlConfiguration ["Icon"] != null) {
				this.icon = ImageService.GetStockId (addin, xmlConfiguration ["Icon"].InnerText, Gtk.IconSize.Dnd);
			}

			if (xmlDocument.DocumentElement ["Combine"] == null) {
				throw new InvalidOperationException ("Combine element not found");
			}
			else {
				solutionDescriptor = SolutionDescriptor.CreateSolutionDescriptor (addin, xmlDocument.DocumentElement ["Combine"],
					codon.BaseDirectory);
			}

			if (xmlDocument.DocumentElement ["Actions"] != null) {
				foreach (XmlNode xmlElement in xmlDocument.DocumentElement ["Actions"]) {
					if (xmlElement is XmlElement && xmlElement.Attributes ["filename"] != null)
						actions.Add (xmlElement.Attributes ["filename"].Value);
				}
			}
		}

		protected ProjectTemplate (RuntimeAddin addin, string id, ProjectTemplateCodon codon)
			: this (addin, id, codon, null)
		{
		}

		//methods
		public void OpenCreatedSolution ()
		{
			IAsyncOperation asyncOperation = IdeApp.Workspace.OpenWorkspaceItem (createdSolutionName);
			asyncOperation.WaitForCompleted ();

			if (asyncOperation.Success) {
				foreach (string action in actions) {
					IdeApp.Workbench.OpenDocument (Path.Combine (createdProjectInformation.ProjectBasePath, action));
				}
			}
		}

		public WorkspaceItem CreateWorkspaceItem (ProjectCreateInformation cInfo)
		{
			WorkspaceItem workspaceItem = solutionDescriptor.CreateEntry (cInfo, this.languagename);

			this.createdSolutionName = workspaceItem.FileName;
			this.createdProjectInformation = cInfo;

			return workspaceItem;
		}

		public SolutionEntityItem CreateProject (SolutionItem policyParent, ProjectCreateInformation cInfo)
		{
			if (solutionDescriptor.EntryDescriptors.Length == 0)
				throw new InvalidOperationException ("Solution template doesn't have any project templates");

			SolutionEntityItem solutionEntryItem = solutionDescriptor.EntryDescriptors [0].CreateItem (cInfo, this.languagename);
			solutionDescriptor.EntryDescriptors [0].InitializeItem (policyParent, cInfo, this.languagename, solutionEntryItem);


			this.createdProjectInformation = cInfo;


			return solutionEntryItem;
		}

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				ProjectTemplateCodon codon = (ProjectTemplateCodon) args.ExtensionNode;
				try {
					ProjectTemplates.Add (new ProjectTemplate (codon.Addin, codon.Id, codon, null));
				}
				catch (Exception e) {
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
				foreach (ProjectTemplate pt in ProjectTemplates) {
					ProjectTemplateCodon codon = (ProjectTemplateCodon) args.ExtensionNode;
					if (pt.Id == codon.Id) {
						ProjectTemplates.Remove (pt);
						break;
					}
				}
			}
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

		public bool HasItemFeatures (SolutionFolder parentFolder, ProjectCreateInformation cinfo)
		{
			// Disable solution item features. The project creation flow is awkward with
			// tacking on additional features that are otherwise accessible through other
			// means. It's especially awkward because there aren't many "features" to
			// begin with. Want GTK? Create a new GTK project through templates.

			return false;

			// ISolutionItemDescriptor sid = solutionDescriptor.EntryDescriptors [0];
			// SolutionEntityItem sampleItem = sid.CreateItem (cinfo, languagename);
			// return (SolutionItemFeatures.GetFeatures (parentFolder, sampleItem).Length > 0);
		}

	}
}
