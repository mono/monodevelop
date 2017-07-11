//
// GenerateTypeDialog.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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

using System;
using System.Security;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Threading;

namespace MonoDevelop.Refactoring.GenerateType
{
	partial class GenerateTypeDialog : Gtk.Dialog
	{
		readonly Document document;
		readonly GenerateTypeDialogOptions generateTypeDialogOptions;
		readonly ISyntaxFactsService syntaxFactsService;

		// reserved names that cannot be a folder name or filename
		static readonly string[] reservedKeywords = { "con", "prn", "aux", "nul",
			"com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
			"lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9", "clock$"
		};

		ListStore projectStore = new ListStore (typeof (string), typeof (Project));
		ListStore documentStore = new ListStore (typeof (string), typeof (Document));

		internal GenerateTypeOptionsResult GenerateTypeOptionsResult {
			get {
				string defaultNamespace = "";

				return new GenerateTypeOptionsResult (
					accessibilityList [comboboxAccess.Active],
					typeKindList [comboboxType.Active],
					entryName.Text,
					SelectedProject,
					radiobuttonNewFile.Active,
					FileName,
					Folders,
					FullFilePath,
					SelectedDocument,
					areFoldersValidIdentifiers,
					defaultNamespace
				);
			}
		}

		Project SelectedProject {
			get {
				if (!comboboxProject.GetActiveIter (out TreeIter iter))
					return document.Project;
				return (Project)projectStore.GetValue (iter, 1);
			}
		}

		readonly IProjectManagementService projectManagementService;

		internal GenerateTypeDialog (string className, GenerateTypeDialogOptions generateTypeDialogOptions, Document document, INotificationService notificationService, IProjectManagementService projectManagementService, ISyntaxFactsService syntaxFactsService)
		{
			this.generateTypeDialogOptions = generateTypeDialogOptions;
			this.projectManagementService = projectManagementService;
			this.syntaxFactsService = syntaxFactsService;
			this.document = document;
			this.Build ();
			PopulateAccessibilty ();
			PopulateTypeKinds ();

			entryName.Text = className;
			FileName = className + ".cs";

			PopulateProjectList ();
			comboboxProject.Model = projectStore;
			comboboxProject.Changed += delegate {
				PopulateDocumentList ();
			};
			comboboxProject.Active = 0;

			comboboxExistingFile.Model = documentStore;
			comboboxExistingFile.Changed += delegate {
				if (comboboxExistingFile.GetActiveIter (out TreeIter iter)) {
					SelectedDocument = (Document)documentStore.GetValue (iter, 1);
				}
			};
			PopulateDocumentList ();
			radiobuttonToExistingFile.Active = true;
			this.buttonOk.Clicked += delegate {
				if (TrySubmit ())
					Respond (ResponseType.Ok);
			};
		}

		void PopulateDocumentList ()
		{
			var selectedProject = SelectedProject;
			documentStore.Clear ();
			if (selectedProject == document.Project) {
				SelectedDocument = document;
				documentStore.AppendValues (GettextCatalog.GetString ("<Current File>"), document);
				foreach (var doc in document.Project.Documents.Where (d => d.FilePath != SelectedDocument.FilePath && !d.IsGeneratedCode (default (CancellationToken)))) {
					documentStore.AppendValues (GetDocumentName (document), document);
				}
				comboboxExistingFile.Active = 0;
				return;
			}

			bool first = true;
			foreach (var doc in document.Project.Documents.Where (d => !d.IsGeneratedCode (default (CancellationToken)))) {
				if (first) {
					SelectedDocument = doc;
					first = false;
				}
				documentStore.AppendValues (GetDocumentName (document), document);
			}
			comboboxExistingFile.Active = 0;
		}

		string GetDocumentName (Document document) 
		{
			if (document.Folders.Count <= 2) {
				return document.Name;
			}
			return string.Join (System.IO.Path.DirectorySeparatorChar.ToString (), document.Folders.Take (2)) + System.IO.Path.DirectorySeparatorChar + document.Name;
		}

		void PopulateProjectList ()
		{
			projectStore.Clear ();
			projectStore.AppendValues (document.Project.Name, document.Project);
			var dependencyGraph = document.Project.Solution.GetProjectDependencyGraph ();
			foreach (var project in document.Project.Solution.Projects.Where (p => p.Name != document.Project.Name && !dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn (p.Id).Contains (document.Project.Id))) {
				projectStore.AppendValues (project.Name, project);
			}
		}

		List<Accessibility> accessibilityList = new List<Accessibility> ();
		void PopulateAccessibilty ()
		{
			if (!generateTypeDialogOptions.IsPublicOnlyAccessibility) {
				comboboxAccess.AppendText ("Default");
				accessibilityList.Add (Accessibility.NotApplicable);

				comboboxAccess.AppendText ("internal");
				accessibilityList.Add (Accessibility.Internal);
			}

			comboboxAccess.AppendText ("public");
			accessibilityList.Add (Accessibility.Public);
			comboboxAccess.Active = 0;
		}

		List<TypeKind> typeKindList = new List<TypeKind> ();
		string FullFilePath;

		string FileName {
			get {
				return entryNewFile.Text;
			}
			set {
				entryNewFile.Text = value;
			}
		}

		List<string> Folders;
		bool areFoldersValidIdentifiers;
		Document SelectedDocument;

		void PopulateTypeKinds ()
		{
			if (TypeKindOptionsHelper.IsClass (generateTypeDialogOptions.TypeKindOptions)) {
				comboboxType.AppendText ("Class");
				typeKindList.Add (TypeKind.Class);
			}
			if (TypeKindOptionsHelper.IsEnum (generateTypeDialogOptions.TypeKindOptions)) {
				comboboxType.AppendText ("Enum");
				typeKindList.Add (TypeKind.Enum);
			}

			if (TypeKindOptionsHelper.IsStructure (generateTypeDialogOptions.TypeKindOptions)) {
				comboboxType.AppendText ("Structure");
				typeKindList.Add (TypeKind.Structure);
			}
			if (TypeKindOptionsHelper.IsInterface (generateTypeDialogOptions.TypeKindOptions)) {
				comboboxType.AppendText ("Interface");
				typeKindList.Add (TypeKind.Interface);
			}

			if (TypeKindOptionsHelper.IsDelegate (generateTypeDialogOptions.TypeKindOptions)) {
				comboboxType.AppendText ("Delegate");
				typeKindList.Add (TypeKind.Delegate);
			}
			comboboxType.Active = 0;
		}
	
	
		bool TrySubmit()
		{
			if (radiobuttonNewFile.Active) {
				var trimmedFileName = entryNewFile.Text;

				if (string.IsNullOrWhiteSpace (trimmedFileName) || trimmedFileName.EndsWith (System.IO.Path.DirectorySeparatorChar.ToString (), StringComparison.Ordinal)) {
					MessageService.ShowError (GettextCatalog.GetString ("Path cannot have empty filename."));
					return false;
				}

				if (trimmedFileName.IndexOfAny (System.IO.Path.GetInvalidPathChars ()) >= 0 || trimmedFileName.StartsWith (@"\\", StringComparison.Ordinal) || trimmedFileName.StartsWith (@"//", StringComparison.Ordinal)) {
					MessageService.ShowError (GettextCatalog.GetString ("Illegal characters in path."));
					return false;
				}

				var isRootOfTheProject = trimmedFileName.StartsWith (System.IO.Path.DirectorySeparatorChar.ToString (), StringComparison.Ordinal);
				string implicitFilePath = null;

				// Construct the implicit file path
				if (isRootOfTheProject || this.SelectedProject != document.Project) {
					if (!TryGetImplicitFilePath (this.SelectedProject.FilePath ?? string.Empty, GettextCatalog.GetString ("Project Path is illegal."), out implicitFilePath)) {
						return false;
					}
				} else {
					if (!TryGetImplicitFilePath (document.FilePath, GettextCatalog.GetString ("DocumentPath is illegal."), out implicitFilePath)) {
						return false;
					}
				}

				// Remove the '\' at the beginning if present
				trimmedFileName = trimmedFileName.StartsWith (System.IO.Path.DirectorySeparatorChar.ToString (), StringComparison.Ordinal) ? trimmedFileName.Substring (1) : trimmedFileName;

				// Construct the full path of the file to be created
				FullFilePath = implicitFilePath + @"\" + trimmedFileName;

				try {
					this.FullFilePath = System.IO.Path.GetFullPath (this.FullFilePath);
				} catch (ArgumentNullException e) {
					MessageService.ShowError (e.Message);
					return false;
				} catch (ArgumentException e) {
					MessageService.ShowError (e.Message);
					return false;
				} catch (SecurityException e) {
					MessageService.ShowError (e.Message);
					return false;
				} catch (NotSupportedException e) {
					MessageService.ShowError (e.Message);
					return false;
				} catch (System.IO.PathTooLongException e) {
					MessageService.ShowError (e.Message);
					return false;
				}

				string projectRootPath = null;
				if (this.SelectedProject.FilePath == null) {
					projectRootPath = string.Empty;
				} else if (!TryGetImplicitFilePath (this.SelectedProject.FilePath, GettextCatalog.GetString ("Project Path is illegal."), out projectRootPath)) {
					return false;
				}

				areFoldersValidIdentifiers = true;
				if (this.FullFilePath.StartsWith (projectRootPath, StringComparison.Ordinal)) {
					// The new file will be within the root of the project
					var folderPath = this.FullFilePath.Substring (projectRootPath.Length);
					var containers = folderPath.Split (new [] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

					// Folder name was mentioned
					if (containers.Length > 1) {
						FileName = containers.Last ();
						Folders = new List<string> (containers);
						Folders.RemoveAt (Folders.Count - 1);

						if (Folders.Any (folder => !(syntaxFactsService.IsValidIdentifier (folder) || syntaxFactsService.IsVerbatimIdentifier (folder)))) {
							areFoldersValidIdentifiers = false;
						}
					} else if (containers.Length == 1) {
						// File goes at the root of the Directory
						FileName = containers [0];
						Folders = null;
					} else {
						MessageService.ShowError (GettextCatalog.GetString ("Illegal characters in path."));
						return false;
					}
				} else {
					// The new file will be outside the root of the project and folders will be null
					Folders = null;

					var lastIndexOfSeparator = this.FullFilePath.LastIndexOf ('\\');
					if (lastIndexOfSeparator == -1) {
						MessageService.ShowError (GettextCatalog.GetString ("Illegal characters in path."));
						return false;
					}

					FileName = this.FullFilePath.Substring (lastIndexOfSeparator + 1);
				}

				// Check for reserved words in the folder or filenameSystem.IO.Path.DirectorySeparatorChar
				if (FullFilePath.Split (new [] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar  }, StringSplitOptions.RemoveEmptyEntries).Any (s => reservedKeywords.Contains (s, StringComparer.OrdinalIgnoreCase))) {
					MessageService.ShowError (GettextCatalog.GetString ("File path cannot use reserved keywords."));
					return false;
				}

				// We check to see if file path of the new file matches the filepath of any other existing file or if the Folders and FileName matches any of the document then
				// we say that the file already exists.
				if (this.SelectedProject.Documents.Where (n => n != null).Where (n => n.FilePath == FullFilePath).Any () ||
					(this.Folders != null && FileName != null &&
					 this.SelectedProject.Documents.Where (n => n.Name != null && n.Folders.Count > 0 && n.Name == FileName && this.Folders.SequenceEqual (n.Folders)).Any ()) ||
					 System.IO.File.Exists (FullFilePath)) {
					MessageService.ShowError (GettextCatalog.GetString ("File already exists."));
					return false;
				}
			}

			return true;
		}

		bool TryGetImplicitFilePath (string implicitPathContainer, string message, out string implicitPath)
		{
			var indexOfLastSeparator = implicitPathContainer.LastIndexOf (System.IO.Path.DirectorySeparatorChar);
			if (indexOfLastSeparator == -1) {
				MessageService.ShowError (message);
				implicitPath = null;
				return false;
			}

			implicitPath = implicitPathContainer.Substring (0, indexOfLastSeparator);
			return true;
		}
	}
}
