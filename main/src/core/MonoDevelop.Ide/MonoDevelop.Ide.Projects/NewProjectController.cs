//
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using Xwt.Drawing;

namespace MonoDevelop.Ide.Projects
{
	/// <summary>
	/// To be renamed to NewProjectDialog
	/// </summary>
	class NewProjectDialogController : INewProjectDialogController
	{
		string chooseTemplateBannerText =  GettextCatalog.GetString ("Choose a template for your new project");
		string configureYourProjectBannerText = GettextCatalog.GetString ("Configure your new project");
		string configureYourWorkspaceBannerText = GettextCatalog.GetString ("Configure your new workspace");
		string configureYourSolutionBannerText = GettextCatalog.GetString ("Configure your new solution");

		const string UseGitPropertyName = "Dialogs.NewProjectDialog.UseGit";
		const string CreateGitIgnoreFilePropertyName = "Dialogs.NewProjectDialog.CreateGitIgnoreFile";
		const string CreateProjectSubDirectoryPropertyName = "MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.AutoCreateProjectSubdir";
		const string CreateProjectSubDirectoryInExistingSolutionPropertyName = "Dialogs.NewProjectDialog.AutoCreateProjectSubdirInExistingSolution";
		const string NewSolutionLastSelectedCategoryPropertyName = "Dialogs.NewProjectDialog.LastSelectedCategoryPath";
		const string NewSolutionLastSelectedTemplatePropertyName = "Dialogs.NewProjectDialog.LastSelectedTemplate";
		const string NewProjectLastSelectedCategoryPropertyName = "Dialogs.NewProjectDialog.AddNewProjectLastSelectedCategoryPath";
		const string NewProjectLastSelectedTemplatePropertyName = "Dialogs.NewProjectDialog.AddNewProjectLastSelectedTemplate";
		const string SelectedLanguagePropertyName = "Dialogs.NewProjectDialog.SelectedLanguage";

		List<TemplateCategory> templateCategories;
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
		bool disposeNewItem = true;

		public NewProjectDialogController ()
		{
			IsFirstPage = true;
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

			if (disposeNewItem)
				DisposeExistingNewItems ();

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
			projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory = GetDefaultCreateProjectDirectorySetting ();
		}

		bool GetDefaultCreateProjectDirectorySetting ()
		{
			if (IsNewSolution) {
				return PropertyService.Get (CreateProjectSubDirectoryPropertyName, true);
			}
			return PropertyService.Get (CreateProjectSubDirectoryInExistingSolutionPropertyName, true);
		}

		void UpdateDefaultSettings ()
		{
			UpdateDefaultGitSettings ();
			UpdateDefaultCreateProjectDirectorySetting ();
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

		string GetDefaultSelectedTemplateId ()
		{
			if (SelectedTemplate != null) {
				return SelectedTemplate.Id;
			}
			return null;
		}

		void UpdateDefaultCreateProjectDirectorySetting ()
		{
			if (IsNewSolution) {
				PropertyService.Set (CreateProjectSubDirectoryPropertyName, projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			} else {
				PropertyService.Set (CreateProjectSubDirectoryInExistingSolutionPropertyName, projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			}
		}

		void SetDefaultLocation ()
		{
			if (BasePath == null)
				BasePath = IdeApp.ProjectOperations.ProjectsDefaultPath;

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

		INewProjectDialogBackend CreateNewProjectDialog ()
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
			templateCategories = IdeApp.Services.TemplatingService.GetProjectTemplateCategories (templateMatch).ToList ();
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
			} else if (DefaultSelectedCategoryPath != null) {
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
			SelectTemplate (template => template.Id == templateId);
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
				string language = GetLanguageForTemplateProcessing ();
				SolutionTemplate template = SelectedTemplate.GetTemplate (language, finalConfigurationPage.Parameters);
				if (template != null) {
					return template;
				}
				throw new ApplicationException (String.Format ("No template found matching condition '{0}'.", SelectedTemplate.Condition));
			}
			return GetSelectedTemplateForSelectedLanguage ();
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
				return configureYourProjectBannerText;
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

		public void Create ()
		{
			if (wizardProvider.HasWizard)
				wizardProvider.BeforeProjectIsCreated ();

			if (!CreateProject ())
				return;

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
					var eitem = currentEntry as SolutionEntityItem;
					if (eitem != null) {
						// Inherit the file format from the solution
						eitem.FileFormat = ParentFolder.ParentSolution.FileFormat;

						// Remove any references to other projects and add them back after the
						// project is saved because a project reference cannot be resolved until
						// the project has a parent solution.
						List<ProjectReference> projectReferences = GetProjectReferences (eitem);
						if (projectReferences.Any ())
							eitem.Items.RemoveRange (projectReferences);

						IdeApp.ProjectOperations.Save (eitem);

						if (projectReferences.Any ())
							eitem.Items.AddRange (projectReferences);
					}
					ParentFolder.AddItem (currentEntry, true);
				}
			}

			if (ParentFolder != null)
				IdeApp.ProjectOperations.Save (ParentFolder.ParentSolution);
			else
				IdeApp.ProjectOperations.Save (processedTemplate.WorkspaceItems);

			CreateVersionControlItems ();

			if (OpenSolution) {
				DisposeExistingNewItems ();
				TemplateWizard wizard = wizardProvider.CurrentWizard;
				var op = OpenCreatedSolution (processedTemplate);
				op.Completed += delegate {
					if (op.Success) {
						var sol = IdeApp.Workspace.GetAllSolutions ().FirstOrDefault ();
						if (sol != null) {
							if (wizard != null)
								wizard.ItemsCreated (new [] { sol });
							InstallProjectTemplatePackages (sol);
						}
					}
				};
			}
			else {
				// The item is not a solution being opened, so it is going to be added to
				// an existing item. In this case, it must not be disposed by the dialog.
				disposeNewItem = false;
				RunTemplateActions (processedTemplate);
				if (wizardProvider.HasWizard)
					wizardProvider.CurrentWizard.ItemsCreated (processedTemplate.WorkspaceItems);
				if (ParentFolder != null)
					InstallProjectTemplatePackages (ParentFolder.ParentSolution);
			}

			IsNewItemCreated = true;
			UpdateDefaultSettings ();
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

		List<ProjectReference> GetProjectReferences (SolutionEntityItem solutionItem)
		{
			return solutionItem.Items.OfType<ProjectReference> ()
				.Where (item => item.ReferenceType == ReferenceType.Project)
				.ToList ();
		}

		bool CreateProject ()
		{
			if (!projectConfiguration.IsValid ()) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, '.' or '_'."));
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
				result = IdeApp.Services.TemplatingService.ProcessTemplate (GetTemplateForProcessing (), projectConfiguration, ParentFolder);
				if (!result.WorkspaceItems.Any ())
					return false;
			} catch (UserException ex) {
				MessageService.ShowError (ex.Message, ex.Details);
				return false;
			} catch (Exception ex) {
				MessageService.ShowException (ex, GettextCatalog.GetString ("The project could not be created"));
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
			return ParentWorkspace.GetAllSolutions ()
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

		void InstallProjectTemplatePackages (Solution sol)
		{
			if (!processedTemplate.HasPackages ())
				return;

			foreach (ProjectTemplatePackageInstaller installer in AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectTemplatePackageInstallers")) {
				installer.Run (sol, processedTemplate.PackageReferences);
			}
		}

		static IAsyncOperation OpenCreatedSolution (ProcessedTemplateResult templateResult)
		{
			IAsyncOperation asyncOperation = IdeApp.Workspace.OpenWorkspaceItem (templateResult.SolutionFileName);
			asyncOperation.Completed += delegate {
				if (asyncOperation.Success) {
					RunTemplateActions (templateResult);
				}
			};
			return asyncOperation;
		}

		static void RunTemplateActions (ProcessedTemplateResult templateResult)
		{
			foreach (string action in templateResult.Actions) {
				IdeApp.Workbench.OpenDocument (Path.Combine (templateResult.ProjectBasePath, action));
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

