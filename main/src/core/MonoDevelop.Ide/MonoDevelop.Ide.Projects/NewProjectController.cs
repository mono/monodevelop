﻿//
// NewProjectDialogController.cs
//
// Author:
//       Todd Berman  <tberman@off.net>
//       Lluis Sanchez Gual <lluis@novell.com>
//       Viktoria Dudka  <viktoriad@remobjects.com>
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using Xwt.Drawing;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.Projects
{
	/// <summary>
	/// To be renamed to NewProjectDialog
	/// </summary>
	class NewProjectDialogController : INewProjectDialogController
	{
		public event EventHandler ProjectCreationFailed;
		public event EventHandler ProjectCreated;

		string chooseTemplateBannerText =  GettextCatalog.GetString ("Choose a template for your new project");
		string configureYourWorkspaceBannerText = GettextCatalog.GetString ("Configure your new workspace");
		string configureYourSolutionBannerText = GettextCatalog.GetString ("Configure your new solution");

		const string UseGitPropertyName = "Dialogs.NewProjectDialog.UseGit";
		const string CreateGitIgnoreFilePropertyName = "Dialogs.NewProjectDialog.CreateGitIgnoreFile";
		internal const string CreateProjectSubDirectoryPropertyName = "MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.AutoCreateProjectSubdir";
		const string NewSolutionLastSelectedCategoryPropertyName = "Dialogs.NewProjectDialog.LastSelectedCategoryPath";
		const string NewSolutionLastSelectedTemplatePropertyName = "Dialogs.NewProjectDialog.LastSelectedTemplate";
		const string NewProjectLastSelectedCategoryPropertyName = "Dialogs.NewProjectDialog.AddNewProjectLastSelectedCategoryPath";
		const string NewProjectLastSelectedTemplatePropertyName = "Dialogs.NewProjectDialog.AddNewProjectLastSelectedTemplate";
		const string SelectedLanguagePropertyName = "Dialogs.NewProjectDialog.SelectedLanguage";

		List<TemplateCategory> templateCategories;
		List<SolutionTemplate> recentTemplates;
		INewProjectDialogBackend dialog;
		FinalProjectConfigurationPage finalConfigurationPage;
		TemplateWizardProvider wizardProvider;
		IVersionControlProjectTemplateHandler versionControlHandler;
		TemplateImageProvider imageProvider = new TemplateImageProvider ();

		NewProjectConfiguration projectConfiguration = new NewProjectConfiguration () {
			CreateProjectDirectoryInsideSolutionDirectory = true
		};

		public bool OpenSolution { get; set; }
		public bool IsNewItemCreated { get; private set; }

		public IWorkspaceFileObject NewItem {
			get { 
				if (processedTemplate != null) {
					return processedTemplate.WorkspaceItems.FirstOrDefault ();
				}
				return null;
			}
		}

		public SolutionFolder ParentFolder { get; set; }
		public string BasePath { get; set; }
		public string SelectedTemplateId { get; set; }
		public Workspace ParentWorkspace { get; set; }
		public bool ShowTemplateSelection { get; set; }

		string DefaultSelectedCategoryPath {
			get {
				return GetDefaultPropertyValue (NewProjectLastSelectedCategoryPropertyName,
					NewSolutionLastSelectedCategoryPropertyName);
			}
			set {
				SetDefaultPropertyValue (NewProjectLastSelectedCategoryPropertyName,
					NewSolutionLastSelectedCategoryPropertyName,
					value);
			}
		}

		string DefaultSelectedTemplate {
			get {
				return GetDefaultPropertyValue (NewProjectLastSelectedTemplatePropertyName,
					NewSolutionLastSelectedTemplatePropertyName);
			}
			set {
				SetDefaultPropertyValue (NewProjectLastSelectedTemplatePropertyName,
					NewSolutionLastSelectedTemplatePropertyName,
					value);
			}
		}

		string GetDefaultPropertyValue (string newProjectPropertyName, string newSolutionPropertyName)
		{
			if (!IsNewSolution) {
				string propertyValue = PropertyService.Get<string> (newProjectPropertyName, null);
				if (!string.IsNullOrEmpty (propertyValue))
					return propertyValue;
			}
			return PropertyService.Get<string> (newSolutionPropertyName, null);
		}

		void SetDefaultPropertyValue (string newProjectPropertyName, string newSolutionPropertyName, string value)
		{
			SolutionTemplateVisibility visibility = GetSelectedTemplateVisibility ();
			if (IsNewSolution || visibility != SolutionTemplateVisibility.NewProject) {
				PropertyService.Set (newSolutionPropertyName, value);
				PropertyService.Set (newProjectPropertyName, null);
			} else if (visibility == SolutionTemplateVisibility.NewProject) {
				PropertyService.Set (newProjectPropertyName, value);
			}
		}

		SolutionTemplateVisibility GetSelectedTemplateVisibility ()
		{
			if (SelectedTemplate != null)
				return SelectedTemplate.Visibility;
			return SolutionTemplateVisibility.All;
		}

		public bool IsNewSolution {
			get { return projectConfiguration.CreateSolution; }
		}

		ProcessedTemplateResult processedTemplate;
		List <SolutionItem> currentEntries;

		public NewProjectDialogController ()
		{
			IsFirstPage = true;
			ShowTemplateSelection = true;
			GetVersionControlHandler ();
		}

		public bool Show ()
		{
			projectConfiguration.CreateSolution = ParentFolder == null;
			LoadTemplateCategories ();
			SetDefaultSettings ();
			SelectDefaultTemplate ();

			CreateFinalConfigurationPage ();
			CreateWizardProvider ();

			dialog = CreateNewProjectDialog ();
			dialog.RegisterController (this);

			dialog.ShowDialog ();

			wizardProvider.Dispose ();
			imageProvider.Dispose ();

			return IsNewItemCreated;
		}

		void GetVersionControlHandler ()
		{
			versionControlHandler = AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/VersionControlProjectTemplateHandler", typeof(IVersionControlProjectTemplateHandler), true)
				.Select (extensionObject => (IVersionControlProjectTemplateHandler)extensionObject)
				.FirstOrDefault ();
		}

		void SetDefaultSettings ()
		{
			SetDefaultLocation ();
			SetDefaultGitSettings ();
			SelectedLanguage = PropertyService.Get (SelectedLanguagePropertyName, "C#");
			if (IsNewSolution)
				projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory = PropertyService.Get (CreateProjectSubDirectoryPropertyName, true);
		}

		void UpdateDefaultSettings ()
		{
			UpdateDefaultGitSettings ();
			if (IsNewSolution)
				PropertyService.Set (CreateProjectSubDirectoryPropertyName, projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			PropertyService.Set (SelectedLanguagePropertyName, GetLanguageForTemplateProcessing ());
			DefaultSelectedCategoryPath = GetSelectedCategoryPath ();
			DefaultSelectedTemplate = GetDefaultSelectedTemplateId ();
		}

		string GetSelectedCategoryPath ()
		{
			foreach (TemplateCategory topLevelCategory in templateCategories) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						SolutionTemplate matchedTemplate = thirdLevelCategory
							.Templates
							.FirstOrDefault (template => template == SelectedTemplate);
						if (matchedTemplate != null) {
							return String.Format ("{0}/{1}", topLevelCategory.Id, secondLevelCategory.Id);
						}
					}
				}
			}

			return null;
		}

		public string GetCategoryPathText (SolutionTemplate template)
		{
			foreach (TemplateCategory topLevelCategory in templateCategories) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						foreach (SolutionTemplate t in thirdLevelCategory.Templates) {
							if (t.GetTemplate (child => child == template) != null) 
								return String.Format ("{0} → {1}", topLevelCategory.Name, secondLevelCategory.Name);
						}
					}
				}
			}

			return null;
		}

		string GetDefaultSelectedTemplateId ()
		{
			if (SelectedTemplate != null) {
				return SelectedTemplate.Id;
			}
			return null;
		}

		void SetDefaultLocation ()
		{
			if (BasePath == null)
				BasePath = IdeApp.Preferences.ProjectsDefaultPath;

			projectConfiguration.Location = new FilePath (BasePath).ResolveLinks ();
		}

		void SetDefaultGitSettings ()
		{
			projectConfiguration.UseGit = PropertyService.Get (UseGitPropertyName, false);
			projectConfiguration.CreateGitIgnoreFile = PropertyService.Get (CreateGitIgnoreFilePropertyName, true);
		}

		void UpdateDefaultGitSettings ()
		{
			PropertyService.Set (UseGitPropertyName, projectConfiguration.UseGit);
			PropertyService.Set (CreateGitIgnoreFilePropertyName, projectConfiguration.CreateGitIgnoreFile);
		}

		protected virtual INewProjectDialogBackend CreateNewProjectDialog ()
		{
			return new GtkNewProjectDialogBackend ();
		}

		void CreateFinalConfigurationPage ()
		{
			finalConfigurationPage = new FinalProjectConfigurationPage (projectConfiguration);
			finalConfigurationPage.ParentFolder = ParentFolder;
			finalConfigurationPage.IsUseGitEnabled = IsNewSolution && (versionControlHandler != null);
			finalConfigurationPage.IsValidChanged += (sender, e) => {
				dialog.CanMoveToNextPage = finalConfigurationPage.IsValid;
			};
		}

		void CreateWizardProvider ()
		{
			wizardProvider = new TemplateWizardProvider ();
			wizardProvider.CanMoveToNextPageChanged += (sender, e) => {
				dialog.CanMoveToNextPage = wizardProvider.CanMoveToNextPage;
			};
		}

		public IEnumerable<TemplateCategory> TemplateCategories {
			get { return templateCategories; }
		}

		public List<SolutionTemplate> RecentTemplates {
			get { return recentTemplates; }
		}

		public TemplateCategory SelectedSecondLevelCategory { get; private set; }
		public SolutionTemplate SelectedTemplate { get; set; }
		public string SelectedLanguage { get; set; }

		public FinalProjectConfigurationPage FinalConfiguration {
			get { return finalConfigurationPage; }
		}

		public IEnumerable<ProjectConfigurationControl> GetFinalPageControls ()
		{
			return wizardProvider.GetFinalPageControls ();
		}

		void LoadTemplateCategories ()
		{
			Predicate<SolutionTemplate> templateMatch = GetTemplateFilter ();
			templateCategories = TemplatingService.GetProjectTemplateCategories (templateMatch).ToList ();
			if (IsNewSolution)
				recentTemplates = TemplatingService.RecentTemplates.GetTemplates (templateCategories).Where (t => t.IsMatch (SolutionTemplateVisibility.NewSolution)).ToList ();
			else
				recentTemplates = TemplatingService.RecentTemplates.GetTemplates (templateCategories).ToList ();
		}

		// Allow testing of the controller by allowing tests to specify the
		// TemplatingService. IdeApp.Services is not initialized during unit tests.
		TemplatingService templatingService;

		internal TemplatingService TemplatingService {
			get {
				if (templatingService != null)
					return templatingService;

				return IdeApp.Services.TemplatingService;
			}
			set { templatingService = value; }
		}

		Predicate<SolutionTemplate> GetTemplateFilter ()
		{
			if (IsNewSolution) {
				return ProjectTemplateCategorizer.MatchNewSolutionTemplates;
			}
			return ProjectTemplateCategorizer.MatchNewProjectTemplates;
		}

		void SelectDefaultTemplate ()
		{
			if (SelectedTemplateId != null) {
				SelectTemplate (SelectedTemplateId);
			} else if (RecentTemplates.Count > 0) { // select first recently used template if possible
				var lastUsedTemplate = RecentTemplates.First ();
				SelectTemplateInCategory (lastUsedTemplate.Category, lastUsedTemplate.Id);
				// SelectTemplateInCategory has selected the group containing the recent template,
				// make sure to select the actual recent template inside the group if the group exists
				if (SelectedTemplate != null)
					SelectedTemplate = lastUsedTemplate;
			} else if (DefaultSelectedCategoryPath != null) { // fallback to old DefaultSelected properties
				if (DefaultSelectedTemplate != null) {
					SelectTemplateInCategory (DefaultSelectedCategoryPath, DefaultSelectedTemplate);
				}

				if (SelectedTemplate == null) {
					SelectFirstTemplateInCategory (DefaultSelectedCategoryPath);
				}
			}

			if (SelectedSecondLevelCategory == null) {
				SelectFirstAvailableTemplate ();
			}
		}

		void SelectTemplate (string templateId)
		{
			SolutionTemplate matchedInGroup = null;
			SelectTemplate (template => {
				if (template.HasGroupId) {
					var inGroup = template.GetTemplate ((t) => t.Id == templateId);
					// check if the requested template is part of the current group
					// becasue it may be not referenced by a category directly.
					// in this case we match/select the group and change the selected
					// language if required.
					if (inGroup?.Id == templateId) {
						matchedInGroup = inGroup;
						return true;
					}
				}
				return template.Id == templateId;
			});

			// make sure that the requested language has been selected
			// if the requested template is part of a group
			if (matchedInGroup != null)
				SelectedLanguage = matchedInGroup.Language;
		}

		void SelectFirstAvailableTemplate ()
		{
			SelectTemplate (template => true);
		}

		void SelectFirstTemplateInCategory (string categoryPath)
		{
			SelectTemplateInCategory (categoryPath, template => true);
		}

		void SelectTemplateInCategory (string categoryPath, string templateId)
		{
			SelectTemplateInCategory (categoryPath, parentTemplate => {
				return parentTemplate.GetTemplate (template => template.Id == templateId) != null;
			});
		}

		void SelectTemplateInCategory (string categoryPath, Func<SolutionTemplate, bool> isTemplateMatch)
		{
			List<string> parts = new TemplateCategoryPath (categoryPath).GetParts ().ToList ();
			if (parts.Count < 2) {
				return;
			}

			string topLevelCategoryId = parts [0];
			string secondLevelCategoryId = parts [1];
			SelectTemplate (
				isTemplateMatch,
				category => category.Id == topLevelCategoryId,
				category => category.Id == secondLevelCategoryId);
		}

		void SelectTemplate (Func<SolutionTemplate, bool> isTemplateMatch)
		{
			SelectTemplate (isTemplateMatch, category => true, category => true);
		}

		void SelectTemplate (
			Func<SolutionTemplate, bool> isTemplateMatch,
			Func<TemplateCategory, bool> isTopLevelCategoryMatch,
			Func<TemplateCategory, bool> isSecondLevelCategoryMatch)
		{
			foreach (TemplateCategory topLevelCategory in templateCategories.Where (isTopLevelCategoryMatch)) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories.Where (isSecondLevelCategoryMatch)) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						SolutionTemplate matchedTemplate = thirdLevelCategory
							.Templates
							.FirstOrDefault (isTemplateMatch);
						if (matchedTemplate != null) {
							SelectedSecondLevelCategory = secondLevelCategory;
							SelectedTemplate = matchedTemplate;
							return;
						}
					}
				}
			}
		}

		public SolutionTemplate GetSelectedTemplateForSelectedLanguage ()
		{
			if (SelectedTemplate != null) {
				SolutionTemplate languageTemplate = SelectedTemplate.GetTemplate (SelectedLanguage);
				if (languageTemplate != null) {
					return languageTemplate;
				}
			}

			return SelectedTemplate;
		}

		SolutionTemplate GetTemplateForProcessing ()
		{
			if (SelectedTemplate.HasCondition) {
				SolutionTemplate template = GetConditionalTemplateForProcessing ();
				if (template != null) {
					return template;
				}
				throw new ApplicationException (GettextCatalog.GetString ("No template found matching condition '{0}'.", SelectedTemplate.Condition));
			}
			return GetSelectedTemplateForSelectedLanguage ();
		}

		/// <summary>
		/// Looks at the SelectedTemplate first to find a template that should be conditionally
		/// used. If there is no match then all templates in the same category that have the
		/// same template id are checked. This allows multiple templates with the same id in the
		/// same category to be supported. .NET Core 2.0 and .NET Core 1.0 project templates
		/// currently use the same template id so only one item is shown in the recently used
		/// items list but use different templates.
		/// </summary>
		SolutionTemplate GetConditionalTemplateForProcessing ()
		{
			string language = GetLanguageForTemplateProcessing ();

			SolutionTemplate template = SelectedTemplate.GetTemplate (language, finalConfigurationPage.Parameters);
			if (template != null)
				return template;

			// Fallback to checking all templates that match the template id in the same category
			// and support the condition.
			SolutionTemplate matchedTemplate = TemplatingService.GetTemplate (
				templateCategories,
				currentTemplate => IsTemplateMatch (currentTemplate, SelectedTemplate, language, finalConfigurationPage.Parameters),
				category => true,
				category => true);

			if (matchedTemplate != null)
				return matchedTemplate.GetTemplate (language, finalConfigurationPage.Parameters);

			return null;
		}

		static bool IsTemplateMatch (SolutionTemplate template, SolutionTemplate templateToMatch, string language, ProjectCreateParameters parameters)
		{
			return template.Id == templateToMatch.Id &&
				template.Category == templateToMatch.Category &&
				template.GetTemplate (language, parameters) != null;
		}

		string GetLanguageForTemplateProcessing ()
		{
			if (SelectedTemplate.AvailableLanguages.Contains (SelectedLanguage)) {
				return SelectedLanguage;
			}
			return SelectedTemplate.Language;
		}

		public string BannerText {
			get {
				if (IsLastPage) {
					return GetFinalConfigurationPageBannerText ();
				} else if (IsWizardPage) {
					return wizardProvider.CurrentWizardPage.Title;
				}
				return chooseTemplateBannerText;
			}
		}

		string GetFinalConfigurationPageBannerText ()
		{
			if (FinalConfiguration.IsWorkspace) {
				return configureYourWorkspaceBannerText;
			} else if (FinalConfiguration.HasProjects) {
				return GettextCatalog.GetString ("Configure your new {0}", FinalConfiguration.Template.Name);
			}
			return configureYourSolutionBannerText;
		}

		public bool CanMoveToNextPage {
			get {
				if (IsLastPage) {
					return finalConfigurationPage.IsValid;
				} else if (IsWizardPage) {
					return wizardProvider.CanMoveToNextPage;
				}
				return (SelectedTemplate != null);
			}
		}

		public bool CanMoveToPreviousPage {
			get { return !IsFirstPage; }
		}

		public string NextButtonText {
			get {
				if (IsLastPage) {
					return GettextCatalog.GetString ("Create");
				}
				return GettextCatalog.GetString ("Next");
			}
		}

		public bool IsFirstPage { get; private set; }
		public bool IsLastPage { get; private set; }

		public bool IsWizardPage {
			get { return wizardProvider.HasWizard && !IsFirstPage && !IsLastPage; }
		}

		public void MoveToNextPage ()
		{
			if (IsFirstPage) {
				IsFirstPage = false;

				FinalConfiguration.Template = GetSelectedTemplateForSelectedLanguage ();
				if (wizardProvider.MoveToFirstPage (FinalConfiguration.Template, finalConfigurationPage.Parameters)) {
					return;
				}
			} else if (wizardProvider.MoveToNextPage ()) {
				return;
			}

			IsLastPage = true;
		}

		public void MoveToPreviousPage ()
		{
			if (IsWizardPage) {
				if (wizardProvider.MoveToPreviousPage ()) {
					return;
				}
			} else if (IsLastPage && wizardProvider.HasWizard && wizardProvider.CurrentWizard.TotalPages != 0) {
				IsLastPage = false;
				return;
			}

			IsFirstPage = true;
			IsLastPage = false;
		}

		public async Task Create ()
		{
			if (wizardProvider.HasWizard)
				wizardProvider.BeforeProjectIsCreated ();

			if (!await CreateProject ()) {
				ProjectCreationFailed?.Invoke (this, EventArgs.Empty);
				return;
			}

			Solution parentSolution = null;

			if (ParentFolder == null) {
				//NOTE: we can only create one solution, so if the first item is a solution, it's the only item
				parentSolution = processedTemplate.WorkspaceItems.FirstOrDefault () as Solution;
				if (parentSolution != null) {
					if (parentSolution.RootFolder.Items.Count > 0)
						currentEntries = new List<SolutionItem> (parentSolution.GetAllSolutionItems ());
					ParentFolder = parentSolution.RootFolder;
				}
			} else {
				parentSolution = ParentFolder.ParentSolution;
				currentEntries = processedTemplate.WorkspaceItems.OfType<SolutionItem> ().ToList ();
			}

			// New combines (not added to parent combines) already have the project as child.
			if (!projectConfiguration.CreateSolution) {
				// Make sure the new item is saved before adding. In this way the
				// version control add-in will be able to put it under version control.
				foreach (SolutionItem currentEntry in currentEntries) {
					var eitem = currentEntry as SolutionItem;
					if (eitem != null) {
						// Inherit the file format from the solution
						eitem.ConvertToFormat (ParentFolder.ParentSolution.FileFormat);

						var project = eitem as Project;
						if (project != null) {
							// Remove any references to other projects and add them back after the
							// project is saved because a project reference cannot be resolved until
							// the project has a parent solution.
							List<ProjectReference> projectReferences = GetProjectReferences (project);
							if (projectReferences.Any ())
								project.Items.RemoveRange (projectReferences);

							await IdeApp.ProjectOperations.SaveAsync (eitem);

							if (projectReferences.Any ())
								project.Items.AddRange (projectReferences);
						}
					}
					ParentFolder.AddItem (currentEntry, true);
				}
			} else {
				string solutionFileName = Path.Combine (projectConfiguration.SolutionLocation, finalConfigurationPage.SolutionFileName);
				if (File.Exists (solutionFileName)) {
					if (!MessageService.Confirm (GettextCatalog.GetString ("File {0} already exists. Overwrite?", solutionFileName), AlertButton.OverwriteFile)) {
						ParentFolder = null;//Reset process of creating solution
						return;
					}
					File.Delete (solutionFileName);
				}
			}

			if (ParentFolder != null)
				await IdeApp.ProjectOperations.SaveAsync (ParentFolder.ParentSolution);
			else
				await IdeApp.ProjectOperations.SaveAsync (processedTemplate.WorkspaceItems);

			CreateVersionControlItems ();

			if (OpenSolution) {
				DisposeExistingNewItems ();
				TemplateWizard wizard = wizardProvider.CurrentWizard;
				if (await OpenCreatedSolution (processedTemplate)) {
					var sol = IdeApp.Workspace.GetAllSolutions ().FirstOrDefault ();
					if (sol != null) {
						if (wizard != null)
							wizard.ItemsCreated (new [] { sol });
						InstallProjectTemplatePackages (sol);
					}
				}
			}
			else {
				// The item is not a solution being opened, so it is going to be added to
				// an existing item. In this case, it must not be disposed by the dialog.
				RunTemplateActions (processedTemplate);
				if (wizardProvider.HasWizard)
					wizardProvider.CurrentWizard.ItemsCreated (processedTemplate.WorkspaceItems);
				if (ParentFolder != null)
					InstallProjectTemplatePackages (ParentFolder.ParentSolution);
			}

			IsNewItemCreated = true;
			UpdateDefaultSettings ();

			var tcs = new TaskCompletionSource<bool> ();
			Gtk.Application.Invoke ((sender, args) => {
				ProjectCreated?.Invoke (this, EventArgs.Empty);
				tcs.SetResult (true);
			});
			await tcs.Task;

			dialog.CloseDialog ();
		}

		public WizardPage CurrentWizardPage {
			get {
				if (IsFirstPage || IsLastPage) {
					return null;
				}
				return wizardProvider.CurrentWizardPage;
			}
		}

		List<ProjectReference> GetProjectReferences (Project solutionItem)
		{
			return solutionItem.Items.OfType<ProjectReference> ()
				.Where (item => item.ReferenceType == ReferenceType.Project)
				.ToList ();
		}

		async Task<bool> CreateProject ()
		{
			if (!projectConfiguration.IsValid ()) {
				MessageService.ShowError (projectConfiguration.GetErrorMessage ());
				return false;
			}

			if (ParentFolder != null && ParentFolder.ParentSolution.FindProjectByName (projectConfiguration.ProjectName) != null) {
				MessageService.ShowError (GettextCatalog.GetString ("A Project with that name is already in your Project Space"));
				return false;
			}

			if (ParentWorkspace != null && SolutionAlreadyExistsInParentWorkspace ()) {
				MessageService.ShowError (GettextCatalog.GetString ("A solution with that filename is already in your workspace"));
				return false;
			}

			SolutionTemplate template = GetTemplateForProcessing ();
			if (ProjectNameIsLanguageKeyword (template.Language, projectConfiguration.ProjectName)) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nName cannot contain a language keyword."));
				return false;
			}

			ProcessedTemplateResult result = null;

			try {
				if (Directory.Exists (projectConfiguration.ProjectLocation)) {
					var question = GettextCatalog.GetString ("Directory {0} already exists.\nDo you want to continue creating the project?", projectConfiguration.ProjectLocation);
					var btn = MessageService.AskQuestion (question, AlertButton.No, AlertButton.Yes);
					if (btn != AlertButton.Yes)
						return false;
				}

				Directory.CreateDirectory (projectConfiguration.ProjectLocation);
			} catch (IOException) {
				MessageService.ShowError (GettextCatalog.GetString ("Could not create directory {0}. File already exists.", projectConfiguration.ProjectLocation));
				return false;
			} catch (UnauthorizedAccessException) {
				MessageService.ShowError (GettextCatalog.GetString ("You do not have permission to create to {0}", projectConfiguration.ProjectLocation));
				return false;
			}

			DisposeExistingNewItems ();

			try {
				result = await TemplatingService.ProcessTemplate (template, projectConfiguration, ParentFolder);
				SetFirstBuildProperty (result.WorkspaceItems);
				if (!result.WorkspaceItems.Any ())
					return false;
			} catch (UserException ex) {
				MessageService.ShowError (ex.Message, ex.Details);
				return false;
			} catch (Exception ex) {
				MessageService.ShowError (GettextCatalog.GetString ("The project could not be created"), ex);
				return false;
			}	
			processedTemplate = result;
			return true;
		}

		bool SolutionAlreadyExistsInParentWorkspace ()
		{
			if (finalConfigurationPage.IsWorkspace)
				return false;

			string solutionFileName = Path.Combine (projectConfiguration.SolutionLocation, finalConfigurationPage.SolutionFileName);
			return ParentWorkspace.GetChildren ().OfType<Solution> ()
				.Any (solution => solution.FileName == solutionFileName);
		}

		void DisposeExistingNewItems ()
		{
			if (processedTemplate != null) {
				foreach (IDisposable item in processedTemplate.WorkspaceItems) {
					item.Dispose ();
				}
			}
		}

		/// <summary>
		/// Sets the FirstBuild user property to true for a new project. This will
		/// be removed when the first build of the project is run.
		/// </summary>
		static void SetFirstBuildProperty (IEnumerable<IWorkspaceFileObject> items)
		{
			foreach (var project in GetProjects (items)) {
				project.UserProperties.SetValue ("FirstBuild", true);
			}
		}

		static IEnumerable<Project> GetProjects (IEnumerable<IWorkspaceFileObject> items)
		{
			foreach (var item in items) {
				if (item is Solution solution) {
					foreach (var project in solution.GetAllProjects ()) {
						yield return project;
					}
				} else if (item is Project project) {
					yield return project;
				}
			}
		}

		static bool ProjectNameIsLanguageKeyword (string language, string projectName)
		{
			if (String.IsNullOrEmpty (language))
				return false;

			LanguageBinding binding = LanguageBindingService.GetBindingPerLanguageName (language);
			if (binding != null) {
				var codeDomProvider = binding.GetCodeDomProvider ();
				if (codeDomProvider != null) {
					projectName = SanitisePotentialNamespace (projectName);
					if (projectName.Contains ('.')) {
						return NameIsLanguageKeyword (codeDomProvider, projectName.Split ('.'));
					}
					return !codeDomProvider.IsValidIdentifier (projectName);
				}
			}

			return false;
		}

		static bool NameIsLanguageKeyword (CodeDomProvider codeDomProvider, string[] names)
		{
			return names.Any (name => !codeDomProvider.IsValidIdentifier (name));
		}

		/// <summary>
		/// Taken from DotNetProject. This is needed otherwise an invalid namespace
		/// can still be used if digits are used as the start of the project name
		/// (e.g. '2try').
		/// </summary>
		static string SanitisePotentialNamespace (string potential)
		{
			var sb = new StringBuilder ();
			foreach (char c in potential) {
				if (char.IsLetter (c) || c == '_' || (sb.Length > 0 && (char.IsLetterOrDigit (sb[sb.Length - 1]) || sb[sb.Length - 1] == '_') && (c == '.' || char.IsNumber (c)))) {
					sb.Append (c);
				}
			}
			if (sb.Length > 0) {
				if (sb[sb.Length - 1] == '.')
					sb.Remove (sb.Length - 1, 1);

				return sb.ToString ();
			} else
				return "Application";
		}

		void InstallProjectTemplatePackages (Solution sol)
		{
			if (!processedTemplate.HasPackages ())
				return;

			foreach (ProjectTemplatePackageInstaller installer in AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectTemplatePackageInstallers")) {
				installer.Run (sol, processedTemplate.PackageReferences);
			}
		}

		static async Task<bool> OpenCreatedSolution (ProcessedTemplateResult templateResult)
		{
			if (await IdeApp.Workspace.OpenWorkspaceItem (templateResult.SolutionFileName)) {
				RunTemplateActions (templateResult);
				return true;
			}
			return false;
		}

		static void RunTemplateActions (ProcessedTemplateResult templateResult)
		{
			foreach (string action in templateResult.Actions) {
				var fileName = Path.Combine (templateResult.ProjectBasePath, action);
				if (File.Exists (fileName))
					IdeApp.Workbench.OpenDocument (fileName, project: null);
			}

			// Notify supporting GettingStarted providers
			Project firstProject = null;
			if (templateResult.WorkspaceItems.OfType<Solution> ().Any ())
				// this is a solution that's been instantiated, lets just look for the first project
				firstProject = IdeApp.Workspace.GetAllProjects ().FirstOrDefault ();
			else
				firstProject = templateResult.WorkspaceItems.OfType<Project> ().FirstOrDefault ();
			if (firstProject != null) {
				var gettingStartedProvider = GettingStarted.GettingStarted.GetGettingStartedProvider (firstProject);
				gettingStartedProvider?.SupportedProjectCreated (templateResult);
			}
		}

		void CreateVersionControlItems ()
		{
			if (!projectConfiguration.CreateSolution) {
				return;
			}

			if (versionControlHandler != null) {
				versionControlHandler.Run (projectConfiguration);
			}
		}

		public Image GetImage (SolutionTemplate template)
		{
			return imageProvider.GetImage (template);
		}
	}
}

