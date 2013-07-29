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
using System.Threading.Tasks;
using MonoDevelop.Ide.ProgressMonitoring;
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
using MonoDevelop.Core.Assemblies;
using System.Runtime.InteropServices;

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectHandler : CommandHandler
	{
		DotNetProject currentProject;

		protected override void Run ()
		{
			ProjectGraph graph = BuildGraph (currentProject);

			if (graph != null) {
				var nativeWindow = IdeApp.Workbench.RootWindow;
				Xwt.WindowFrame parentFrame = Xwt.Toolkit.CurrentEngine.WrapWindow (nativeWindow);

				using (var dialog = new SplitProjectDialog (currentProject, graph)) {
					if (dialog.Run (parentFrame) == Xwt.Command.Ok) {
						//Create class library project
						string classLibraryName = "Testing 123";
						var classLibraryBasePath = currentProject.BaseDirectory.ParentDirectory.Combine (classLibraryName);
						Directory.CreateDirectory (classLibraryBasePath.ToString ());

						ProjectCreateInformation info = new ProjectCreateInformation();
						info.ProjectName = classLibraryName;
						info.ParentFolder = currentProject.ParentFolder;
						info.ProjectBasePath = classLibraryBasePath;
						info.SolutionPath = currentProject.ParentSolution.FileName;
						info.SolutionName = currentProject.ParentSolution.Name;
						info.ActiveConfiguration = IdeApp.Workspace.ActiveConfiguration;

						var newProject = (DotNetProject) IdeApp.ProjectOperations.CreateProject("CSharpEmptyProject", currentProject.ParentFolder, info);
						newProject.FileFormat = currentProject.ParentSolution.FileFormat;

						newProject.TargetFramework = currentProject.TargetFramework;
						newProject.CompileTarget = CompileTarget.Library;
						newProject.References.AddRange (currentProject.References);

						currentProject.ParentFolder.AddItem (newProject);

						currentProject.References.Add (new MonoDevelop.Projects.ProjectReference (newProject));

						var nodesToMove = dialog.SelectedNodes;

						using (var transferFilesMonitor = new MessageDialogProgressMonitor(true, false)) {
							transferFilesMonitor.BeginStepTask (GettextCatalog.GetString ("Moving files"), nodesToMove.Count, 1);

							foreach (var node in nodesToMove) {
								Console.WriteLine ("move = {0}", node.File.FilePath);

								var newPath = newProject.BaseDirectory.Combine (node.File.ProjectVirtualPath.ToRelative (currentProject.BaseDirectory));

								IdeApp.ProjectOperations.TransferFiles (transferFilesMonitor, currentProject, node.File.FilePath,
								                                       newProject, newPath, true, true);

								transferFilesMonitor.Step (1);
							}

							transferFilesMonitor.EndTask ();
						}

						IdeApp.ProjectOperations.Save (newProject);
						IdeApp.ProjectOperations.Save (currentProject);
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
					projectGraph.AddNode (new ProjectGraph.Node (file));
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

					if (file.Subtype != Subtype.Code)
						continue;

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

