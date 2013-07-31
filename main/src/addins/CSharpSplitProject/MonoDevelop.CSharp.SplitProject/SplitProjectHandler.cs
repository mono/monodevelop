//
// SplitProjectHandler.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using System.Threading;
using MonoDevelop.Core;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.TypeSystem;
using System.IO;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.ProgressMonitoring;

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectHandler : CommandHandler
	{
		DotNetProject currentProject;

		protected override void Run ()
		{
			IdeApp.Workbench.SaveAll ();

			ProjectGraph graph = BuildGraph (currentProject);

			if (graph != null) {
				using (var dialog = new SplitProjectDialog (graph)) {
					if (MessageService.RunCustomXwtDialog(dialog) == Xwt.Command.Ok) {
						//Create class library project
						string classLibraryName = dialog.NewProjectName;
						var classLibraryBasePath = currentProject.BaseDirectory.ParentDirectory.Combine (classLibraryName);
						Directory.CreateDirectory (classLibraryBasePath.ToString ());

						ProjectCreateInformation info = new ProjectCreateInformation();
						info.ProjectName = classLibraryName;
						info.ParentFolder = currentProject.ParentFolder;
						info.ProjectBasePath = classLibraryBasePath;
						info.SolutionPath = currentProject.ParentSolution.FileName;
						info.SolutionName = currentProject.ParentSolution.Name;
						info.ActiveConfiguration = IdeApp.Workspace.ActiveConfiguration;

						var template = ProjectTemplate.ProjectTemplates.Single (t => t.Id == "CSharpEmptyProject");
						var newProject = (DotNetProject) template.CreateProject (currentProject, info);
						newProject.FileFormat = currentProject.ParentSolution.FileFormat;

						newProject.TargetFramework = currentProject.TargetFramework;
						newProject.CompileTarget = CompileTarget.Library;
						newProject.References.AddRange (currentProject.References);

						//Copy settings and configurations from old project
						newProject.Description = currentProject.Description;
						newProject.TargetFramework = currentProject.TargetFramework;
						newProject.Version = currentProject.Version;
						newProject.SyncVersionWithSolution = currentProject.SyncVersionWithSolution;
						while (newProject.Configurations.Count > 0) {
							newProject.Configurations.RemoveAt (newProject.Configurations.Count - 1);
						}
						foreach (var configuration in currentProject.Configurations) {
							var newConfiguration = (DotNetProjectConfiguration)configuration.Clone ();
							newConfiguration.OutputAssembly = newProject.Name;
							newConfiguration.OutputDirectory = newProject.BaseDirectory.Combine ("bin", newConfiguration.Name);

							newProject.Configurations.Add (newConfiguration);
						}

						newProject.Policies.CopyFrom (currentProject.Policies);

						//Add project
						currentProject.ParentFolder.AddItem (newProject);

						var nodesToMove = dialog.SelectedNodes;

						using (var transferFilesMonitor = new MessageDialogProgressMonitor(true, false)) {
							transferFilesMonitor.BeginStepTask (GettextCatalog.GetString ("Moving files"), nodesToMove.Count, 1);

							foreach (var node in nodesToMove) {
								var parentNode = graph.Nodes.FirstOrDefault (potentialDependency => potentialDependency.File.DependentChildren.Contains(node.File));
								if (parentNode != null && !nodesToMove.Contains(parentNode)) {
									//Dependent file will be moved, but parent won't
									node.File.DependsOn = "";
								}

								Console.WriteLine ("move = {0}", node.File.FilePath);

								var newPath = newProject.BaseDirectory.Combine (node.File.ProjectVirtualPath.ToRelative (currentProject.BaseDirectory));

								IdeApp.ProjectOperations.TransferFiles (transferFilesMonitor, currentProject, node.File.FilePath,
								                                       newProject, newPath, true, true);

								transferFilesMonitor.Step (1);
							}

							transferFilesMonitor.EndTask ();
						}

						ISet<DotNetProject> projectsToSave = new HashSet<DotNetProject> ();
						projectsToSave.Add (newProject);
						projectsToSave.Add (currentProject);

						currentProject.References.Add (new MonoDevelop.Projects.ProjectReference (newProject));

						foreach (var project in currentProject.ParentSolution.GetAllProjects().OfType<DotNetProject>()) {
							if (project.References.Any (reference => reference.ReferenceType == ReferenceType.Project && reference.Reference == currentProject.Name))
							{
								//This project references the old project, so just to be safe we make it reference the new library as well
								project.References.Add (new MonoDevelop.Projects.ProjectReference(newProject));

								projectsToSave.Add (project);
							}
						}

						IdeApp.ProjectOperations.Save (projectsToSave);
						IdeApp.ProjectOperations.Save (currentProject.ParentSolution);
					}
				}
			}
		}

		protected override void Update (CommandInfo info)
		{
			var solutionPad = IdeApp.Workbench.GetPad<SolutionPad> ().Content as SolutionPad;

			currentProject = solutionPad.TreeView.GetSelectedNode ().DataItem as DotNetProject;

			if (currentProject == null || currentProject.LanguageName != "C#") {
				info.Visible = false;
				return;
			}

			info.Visible = true;
		}

		ProjectGraph BuildGraph (DotNetProject project) {
			using (var progress = new MessageDialogProgressMonitor (true, true)) {
				CancellationTokenSource tokenSource = new CancellationTokenSource ();
				CancellationToken token = tokenSource.Token;
				progress.CancelRequested += (IProgressMonitor monitor) => tokenSource.Cancel ();

				progress.BeginTask (GettextCatalog.GetString ("Preparing to split project"), project.Files.Count);

				var projectGraph = new ProjectGraph ();

				//Get nodes
				foreach (var file in project.Files) {
					if (progress.IsCancelRequested) {
						return null;
					}

					if (file.Subtype == Subtype.Code) {
						projectGraph.AddNode (new ProjectGraph.Node (file));
					}
				}

				progress.EndTask ();

				progress.BeginStepTask (GettextCatalog.GetString ("Analyzing files"), project.Files.Count, 1);

				//Find out which types each file depends on

				Dictionary<IType, List<ProjectGraph.Node>> typeDefinitions = new Dictionary<IType, List<ProjectGraph.Node>> ();

				foreach (var node in projectGraph.Nodes) {
					if (progress.IsCancelRequested) {
						return null;
					}

					progress.BeginStepTask (GettextCatalog.GetString ("Analyzing {0}", node), 4, 1);

					var file = node.File;

					if (file.BuildAction != "Compile")
						continue;

					if (!project.LanguageBinding.IsSourceCodeFile(file.FilePath)) {
						continue;
					}

					bool isOpen;
					System.Text.Encoding encoding;
					bool hadBom;

					var data = TextFileProvider.Instance.GetTextEditorData (file.Name, out hadBom, out encoding, out isOpen);

					progress.Step (1);

					ParsedDocument parsedDocument;
					using (var reader = new StreamReader (data.OpenStream ()))
						parsedDocument = new MonoDevelop.CSharp.Parser.TypeSystemParser ().Parse (true, file.Name, reader, project);

					if (progress.IsCancelRequested) {
						return null;
					}

					var resolver = new CSharpAstResolver (TypeSystemService.GetCompilation (project), (SyntaxTree)parsedDocument.Ast, parsedDocument.ParsedFile as CSharpUnresolvedFile);

					var ctx = new MDRefactoringContext (project as DotNetProject, data, parsedDocument, resolver, resolver.RootNode.StartLocation, token);

					progress.Step (1);

					if (ctx.IsInvalid) {
						throw new ProjectHasErrorsException ();
					}

					//Step 1. Find all type declarations and identify which are partial
					var typeDeclarations = ctx.RootNode.Descendants.OfType<TypeDeclaration> ();
					foreach (var typeDeclaration in typeDeclarations) {
						if (progress.IsCancelRequested) {
							return null;
						}

						var resolveResult = ctx.Resolve (typeDeclaration);
						if (resolveResult.IsError) {
							progress.ReportError ("Project has errors. Could not resolve type declaration for type " + typeDeclaration.Name, new ProjectHasErrorsException ());
						}
						var typeResolveResult = resolveResult as TypeResolveResult;
						var type = typeResolveResult.Type;
						if (!typeDefinitions.ContainsKey (type)) {
							typeDefinitions [type] = new List<ProjectGraph.Node> ();
						}

						typeDefinitions [type].Add (node);
						node.AddTypeDependency (type);
					}

					progress.Step (1);

					foreach (var ident in ctx.RootNode.Descendants.OfType<IdentifierExpression>()) {
						var typeResolveResult = ctx.Resolve (ident) as TypeResolveResult;
						if (typeResolveResult != null) {
							node.AddTypeDependency (typeResolveResult.Type);
						}
					}

					foreach (var type in ctx.RootNode.Descendants.OfType<AstType>()) {
						if (progress.IsCancelRequested) {
							return null;
						}

						var resolvedType = ctx.ResolveType (type);
						node.AddTypeDependency (resolvedType);
					}

					progress.Step (1);

					progress.EndTask ();
				}

				progress.EndTask ();

				progress.BeginStepTask (GettextCatalog.GetString ("Analyzing dependencies between different files"), project.Files.Count, 1);

				//Turn the type dependencies into node dependencies
				foreach (var node in projectGraph.Nodes) {
					if (progress.IsCancelRequested) {
						return null;
					}

					foreach (var dependedType in node.TypeDependencies) {
						List<ProjectGraph.Node> dependedNodes;
						if (typeDefinitions.TryGetValue (dependedType, out dependedNodes)) {
							foreach (var dependedNode in dependedNodes) {
								node.AddEdgeTo (dependedNode);
							}
						}
					}

					progress.Step (1);
				}

				progress.EndTask ();

				return progress.IsCancelRequested ? null : projectGraph;
			}
		}
	}
}

