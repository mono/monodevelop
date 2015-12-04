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
using System.Linq;

namespace MonoDevelop.Ide.Templates
{
	internal class ProjectTemplate
	{
		public static List<ProjectTemplate> ProjectTemplates = new List<ProjectTemplate> ();

		static MonoDevelop.Core.Instrumentation.Counter TemplateCounter = MonoDevelop.Core.Instrumentation.InstrumentationService.CreateCounter ("Template Instantiated", "Project Model", id:"Core.Template.Instantiated");

		private List<string> actions = new List<string> ();

		private string createdSolutionName;
		IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects = new List<PackageReferencesForCreatedProject> ();
		private ProjectCreateInformation createdProjectInformation = null;

		internal string CreatedSolutionName {
			get { return createdSolutionName; }
		}

		internal IEnumerable<string> Actions {
			get { return actions; }
		}

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

		private string groupId;
		public string GroupId
		{
			get { return groupId; }
		}

		private string condition;
		public string Condition
		{
			get { return condition; }
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

		/// <summary>
		/// The name of the template before localization, used for the instantiation counter
		/// </summary>
		private string nonLocalizedName;
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

		private string fileExtension;
		public string FileExtension
		{
			get { return fileExtension; }
		}

		private string supportedParameters;
		public string SupportedParameters {
			get { return supportedParameters; }
		}

		private string defaultParameters;
		public string DefaultParameters {
			get { return defaultParameters; }
		}

		private string imageId;
		public string ImageId {
			get { return imageId; }
		}

		private string imageFile;
		public string ImageFile {
			get { return imageFile; }
		}

		private string visibility;
		public string Visibility {
			get { return visibility; }
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

			// Get legacy category.
			if (xmlConfiguration ["_Category"] != null) {
				category = xmlConfiguration ["_Category"].InnerText;
			}

			if (xmlConfiguration ["Category"] != null) {
				category = xmlConfiguration ["Category"].InnerText;
			} else if (category == null) {
				LoggingService.LogWarning (string.Format ("Category missing in project template {0}", codon.Id));
			}

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
				this.nonLocalizedName = xmlConfiguration ["_Name"].InnerText;
				this.name = addin.Localizer.GetString (this.nonLocalizedName);
			}

			if (xmlConfiguration ["_Description"] != null) {
				this.description = addin.Localizer.GetString (xmlConfiguration ["_Description"].InnerText);
			}

			if (xmlConfiguration ["Icon"] != null) {
				this.icon = ImageService.GetStockId (addin, xmlConfiguration ["Icon"].InnerText, Gtk.IconSize.Dnd);
			}

			if (xmlConfiguration ["GroupId"] != null) {
				this.groupId = xmlConfiguration ["GroupId"].InnerText;
				this.condition = xmlConfiguration ["GroupId"].GetAttribute ("condition");
			}

			if (xmlConfiguration ["FileExtension"] != null) {
				this.fileExtension = xmlConfiguration ["FileExtension"].InnerText;
			}

			if (xmlConfiguration ["SupportedParameters"] != null) {
				this.supportedParameters = xmlConfiguration ["SupportedParameters"].InnerText;
			}

			if (xmlConfiguration ["DefaultParameters"] != null) {
				this.defaultParameters = xmlConfiguration ["DefaultParameters"].InnerText;
			}

			if (xmlConfiguration ["Image"] != null) {
				XmlElement imageElement = xmlConfiguration ["Image"];
				imageId = imageElement.GetAttribute ("id");
				imageFile = imageElement.GetAttribute ("file");
				if (!String.IsNullOrEmpty (imageFile)) {
					imageFile = Path.Combine (codon.BaseDirectory, imageFile);
				}
			}

			if (xmlConfiguration ["Visibility"] != null) {
				visibility = xmlConfiguration ["Visibility"].InnerText;
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
		public IAsyncOperation OpenCreatedSolution ()
		{
			IAsyncOperation asyncOperation = IdeApp.Workspace.OpenWorkspaceItem (createdSolutionName);
			asyncOperation.Completed += delegate {
				if (asyncOperation.Success) {
					foreach (string action in actions) {
						IdeApp.Workbench.OpenDocument (Path.Combine (createdProjectInformation.ProjectBasePath, action));
					}
				}
			};
			return asyncOperation;
		}

		public WorkspaceItem CreateWorkspaceItem (ProjectCreateInformation cInfo)
		{
			WorkspaceItemCreatedInformation workspaceItemInfo = solutionDescriptor.CreateEntry (cInfo, this.languagename);

			this.createdSolutionName = workspaceItemInfo.WorkspaceItem.FileName;
			this.createdProjectInformation = cInfo;
			this.packageReferencesForCreatedProjects = workspaceItemInfo.PackageReferencesForCreatedProjects;

			var pDesc = this.solutionDescriptor.EntryDescriptors.OfType<ProjectDescriptor> ().ToList ();

			var metadata = new Dictionary<string, string> ();
			metadata ["Id"] = this.Id;
			metadata ["Name"] = this.nonLocalizedName;
			metadata ["Language"] = this.LanguageName;
			metadata ["Platform"] = pDesc.Count == 1 ? pDesc[0].ProjectType : "Multiple";
			TemplateCounter.Inc (1, null, metadata);

			return workspaceItemInfo.WorkspaceItem;
		}

		public IEnumerable<SolutionEntityItem> CreateProjects (SolutionItem policyParent, ProjectCreateInformation cInfo)
		{
			if (solutionDescriptor.EntryDescriptors.Length == 0)
				throw new InvalidOperationException ("Solution template doesn't have any project templates");

			var solutionEntryItems = new List<SolutionEntityItem> ();
			packageReferencesForCreatedProjects = new List<PackageReferencesForCreatedProject> ();

			foreach (ISolutionItemDescriptor solutionItemDescriptor in GetItemsToCreate (solutionDescriptor, cInfo)) {
				ProjectCreateInformation itemCreateInfo = GetItemSpecificCreateInfo (solutionItemDescriptor, cInfo);
				itemCreateInfo = new ProjectTemplateCreateInformation (itemCreateInfo, cInfo.ProjectName);

				SolutionEntityItem solutionEntryItem = solutionItemDescriptor.CreateItem (itemCreateInfo, this.languagename);
				if (solutionEntryItem != null) {
					solutionItemDescriptor.InitializeItem (policyParent, itemCreateInfo, this.languagename, solutionEntryItem);

					SavePackageReferences (solutionEntryItem, solutionItemDescriptor, itemCreateInfo);

					solutionEntryItems.Add (solutionEntryItem);
				}
			}

			var pDesc = this.solutionDescriptor.EntryDescriptors.OfType<ProjectDescriptor> ().FirstOrDefault ();
			var metadata = new Dictionary<string, string> ();
			metadata ["Id"] = this.Id;
			metadata ["Name"] = this.nonLocalizedName;
			metadata ["Language"] = this.LanguageName;
			metadata ["Platform"] = pDesc != null ? pDesc.ProjectType : "Unknown";
			TemplateCounter.Inc (1, null, metadata);

			createdProjectInformation = cInfo;

			return solutionEntryItems;
		}

		static IEnumerable<ISolutionItemDescriptor> GetItemsToCreate (SolutionDescriptor solutionDescriptor, ProjectCreateInformation cInfo)
		{
			foreach (ISolutionItemDescriptor descriptor in solutionDescriptor.EntryDescriptors) {
				var projectDescriptor = descriptor as ProjectDescriptor;
				if ((projectDescriptor != null) && !projectDescriptor.ShouldCreateProject (cInfo)) {
					// Skip.
				} else {
					yield return descriptor;
				}
			}
		}

		static ProjectCreateInformation GetItemSpecificCreateInfo (ISolutionItemDescriptor descriptor, ProjectCreateInformation cInfo)
		{
			var entry = descriptor as ICustomProjectCIEntry;
				if (entry != null)
					return entry.CreateProjectCI (cInfo);

			return cInfo;
		}

		void SavePackageReferences (SolutionEntityItem solutionEntryItem, ISolutionItemDescriptor descriptor, ProjectCreateInformation cInfo)
		{
			if ((solutionEntryItem is Project) && (descriptor is ProjectDescriptor)) {
				var projectPackageReferences = new PackageReferencesForCreatedProject (((Project)solutionEntryItem).Name, ((ProjectDescriptor)descriptor).GetPackageReferences (cInfo));
				packageReferencesForCreatedProjects.Add (projectPackageReferences);
			}
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

		public bool HasPackages ()
		{
			return solutionDescriptor.HasPackages ();
		}

		public IList<PackageReferencesForCreatedProject> PackageReferencesForCreatedProjects {
			get { return packageReferencesForCreatedProjects; }
		}
	}
}
