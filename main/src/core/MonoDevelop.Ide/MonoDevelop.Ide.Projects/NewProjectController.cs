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
using ProjectConfiguration = MonoDevelop.Ide.Templates.ProjectConfiguration;

namespace MonoDevelop.Ide.Projects
{
	/// <summary>
	/// To be renamed to NewProjectDialog
	/// </summary>
	public class NewProjectDialogController : INewProjectDialogController
	{
		string chooseTemplateBannerText =  GettextCatalog.GetString ("Choose a template for your new project");
		string configureYourProjectBannerText = GettextCatalog.GetString ("Configure your new project");
		string configureYourWorkspaceBannerText = GettextCatalog.GetString ("Configure your new workspace");
		string configureYourSolutionBannerText = GettextCatalog.GetString ("Configure your new solution");

		List<TemplateCategory> templateCategories;
		INewProjectDialogBackend dialog;
		FinalProjectConfigurationPage finalConfigurationPage;
		TemplateWizardProvider wizardProvider;

		ProjectConfiguration projectConfiguration = new ProjectConfiguration () {
			CreateProjectDirectoryInsideSolutionDirectory = true
		};

		public bool OpenSolution { get; set; }
		public bool IsNewItemCreated { get; private set; }
		public IWorkspaceFileObject NewItem { get; private set; }
		public SolutionFolder ParentFolder { get; set; }
		public string BasePath { get; set; }
		public string SelectedTemplateId { get; set; }

		public bool IsNewSolution {
			get { return projectConfiguration.CreateSolution; }
		}

		ProcessedTemplateResult processedTemplate;
		SolutionItem currentEntry;
		bool disposeNewItem = true;

		public NewProjectDialogController ()
		{
			SelectedLanguage = "C#";
			IsFirstPage = true;
			LoadTemplateCategories ();
		}

		public bool Show ()
		{
			projectConfiguration.CreateSolution = ParentFolder == null;
			SetDefaultLocation ();
			SelectTemplate ();

			CreateFinalConfigurationPage ();
			CreateWizardProvider ();

			dialog = CreateNewProjectDialog ();
			dialog.RegisterController (this);

			dialog.ShowDialog ();

			if (disposeNewItem && NewItem != null)
				NewItem.Dispose ();

			return IsNewItemCreated;
		}

		void SetDefaultLocation ()
		{
			if (BasePath == null)
				BasePath = IdeApp.ProjectOperations.ProjectsDefaultPath;

			projectConfiguration.Location = FileService.ResolveFullPath (BasePath);
		}

		INewProjectDialogBackend CreateNewProjectDialog ()
		{
			return new GtkNewProjectDialogBackend ();
		}

		void CreateFinalConfigurationPage ()
		{
			finalConfigurationPage = new FinalProjectConfigurationPage (projectConfiguration);
			finalConfigurationPage.ParentFolder = ParentFolder;
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

		void LoadTemplateCategories ()
		{
			templateCategories = IdeApp.Services.TemplatingService.GetProjectTemplateCategories ().ToList ();
		}

		void SelectTemplate ()
		{
			foreach (TemplateCategory topLevelCategory in templateCategories) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						SolutionTemplate matchedTemplate = thirdLevelCategory
							.Templates
							.FirstOrDefault (MatchesSelectedTemplateId);
						if (matchedTemplate != null) {
							SelectedSecondLevelCategory = secondLevelCategory;
							SelectedTemplate = matchedTemplate;
							return;
						}
					}
				}
			}
		}

		bool MatchesSelectedTemplateId (SolutionTemplate template)
		{
			return (SelectedTemplateId == null) || (template.Id == SelectedTemplateId);
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
			} else if (IsLastPage && wizardProvider.HasWizard) {
				IsLastPage = false;
				return;
			}

			IsFirstPage = true;
			IsLastPage = false;
		}

		public void Create ()
		{
//			if (!btn_new.Sensitive)
//				return;
//			btn_new.Sensitive = false;
//
//			if (notebook.Page == 0) {
				if (!CreateProject ())
					return;

				Solution parentSolution = null;

				if (ParentFolder == null) {
					WorkspaceItem item = (WorkspaceItem) NewItem;
					parentSolution = item as Solution;
					if (parentSolution != null) {
						if (parentSolution.RootFolder.Items.Count > 0)
							currentEntry = parentSolution.RootFolder.Items [0] as SolutionItem;
						ParentFolder = parentSolution.RootFolder;
					}
				} else {
					SolutionItem item = (SolutionItem) NewItem;
					parentSolution = ParentFolder.ParentSolution;
					currentEntry = item;
				}

//				if (btn_new.Label == Gtk.Stock.GoForward) {
//					// There are features to show. Go to the next page
//					if (currentEntry != null) {
//						try {
//							featureList.Fill (parentFolder, currentEntry, SolutionItemFeatures.GetFeatures (parentFolder, currentEntry));
//						}
//						catch (Exception ex) {
//							LoggingService.LogError (ex.ToString ());
//						}
//					}
//					notebook.Page++;
//					btn_new.Sensitive = true;
//					btn_new.Label = Gtk.Stock.Ok;
//					return;
//				}

//			} else {
//				// Already in fetatures page
//				if (!featureList.Validate ())
//					return;
//			}

			// New combines (not added to parent combines) already have the project as child.
			if (!projectConfiguration.CreateSolution) {
				// Make sure the new item is saved before adding. In this way the
				// version control add-in will be able to put it under version control.
				if (currentEntry is SolutionEntityItem) {
					// Inherit the file format from the solution
					SolutionEntityItem eitem = (SolutionEntityItem) currentEntry;
					eitem.FileFormat = ParentFolder.ParentSolution.FileFormat;
					IdeApp.ProjectOperations.Save (eitem);
				}
				ParentFolder.AddItem (currentEntry, true);
			}

//			if (notebook.Page == 1)
//				featureList.ApplyFeatures ();

			if (ParentFolder != null)
				IdeApp.ProjectOperations.Save (ParentFolder.ParentSolution);
			else
				IdeApp.ProjectOperations.Save (NewItem);

			if (OpenSolution) {
				var op = OpenCreatedSolution (processedTemplate); // FIXME
				op.Completed += delegate {
					if (op.Success) {
						var sol = IdeApp.Workspace.GetAllSolutions ().FirstOrDefault ();
						if (sol != null)
							InstallProjectTemplatePackages (sol);
					}
				};
			}
			else {
				// The item is not a solution being opened, so it is going to be added to
				// an existing item. In this case, it must not be disposed by the dialog.
				disposeNewItem = false;
				if (ParentFolder != null)
					InstallProjectTemplatePackages (ParentFolder.ParentSolution);
			}

			IsNewItemCreated = true;
			dialog.CloseDialog ();
			wizardProvider.Dispose ();
		}

		public WizardPage CurrentWizardPage {
			get {
				if (IsFirstPage || IsLastPage) {
					return null;
				}
				return wizardProvider.CurrentWizardPage;
			}
		}

		bool CreateProject ()
		{
//			if (templateView.CurrentlySelected != null) {
//				PropertyService.Set ("Dialogs.NewProjectDialog.LastSelectedCategory",  ((ProjectTemplate)templateView.CurrentlySelected).Category);
//				string template;
//				// keep the old format if the language is not specified
//				if (String.IsNullOrEmpty(templateView.CurrentlySelected.LanguageName)) {
//					template = templateView.CurrentlySelected.Id;
//				} else { // use the newer format with language before id
//					template = templateView.CurrentlySelected.LanguageName + "/" + templateView.CurrentlySelected.Id;
//				}
//				recentTemplates.Remove (template);
//				recentTemplates.Insert (0, template);
//				if (recentTemplates.Count > 15)
//					recentTemplates.RemoveAt (recentTemplates.Count - 1);
//				string strRecent = string.Join (",", recentTemplates.ToArray ());
//				PropertyService.Set ("Dialogs.NewProjectDialog.RecentTemplates", strRecent);
//				PropertyService.SaveProperties ();
//				//PropertyService.Set("Dialogs.NewProjectDialog.LargeImages", ((RadioButton)ControlDictionary["largeIconsRadioButton"]).Checked);
//			}

			if (!projectConfiguration.IsValid ()) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, '.' or '_'."));
				return false;
			}

			if (ParentFolder != null && ParentFolder.ParentSolution.FindProjectByName (projectConfiguration.ProjectName) != null) {
				MessageService.ShowError (GettextCatalog.GetString ("A Project with that name is already in your Project Space"));
				return false;
			}

//			PropertyService.Set (
//				"MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.AutoCreateProjectSubdir",
//				CreateSolutionDirectory);

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

			if (NewItem != null) {
				NewItem.Dispose ();
				NewItem = null;
			}

			try {
				result = IdeApp.Services.TemplatingService.ProcessTemplate (GetSelectedTemplateForSelectedLanguage (), projectConfiguration, ParentFolder);
				NewItem = result.WorkspaceItem;
				if (NewItem == null)
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
					foreach (string action in templateResult.Actions) {
						IdeApp.Workbench.OpenDocument (Path.Combine (templateResult.ProjectBasePath, action));
					}
				}
			};
			return asyncOperation;
		}
	}
}

