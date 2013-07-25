//
// SplitProjectDialog.cs
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xwt;
using Mono.TextEditor;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.CSharp.SplitProject
{
	public class SplitProjectDialog : Dialog
	{
		DotNetProject project;

		CancellationTokenSource cancellationTokenSource;

		DialogButton okButton;
		DialogButton cancelButton;

		public SplitProjectDialog (DotNetProject project)
		{
			this.project = project;

			cancellationTokenSource = new CancellationTokenSource ();

			var task = RunGraphGeneration (cancellationTokenSource.Token);

			Title = "Split Project";

			Buttons.Add (okButton = new Xwt.DialogButton (Command.Ok) { Sensitive = false });
			Buttons.Add (cancelButton = new Xwt.DialogButton (Command.Cancel));

			cancelButton.Clicked += (object sender, EventArgs e) => {
				cancellationTokenSource.Cancel();
				try {
					task.Wait ();
				} catch (AggregateException) {
					// Ensure the dialog is closed
					Dispose ();

					throw;
				}
			};
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			cancellationTokenSource.Dispose ();
		}

		Task RunGraphGeneration (CancellationToken token) {
			 return BuildGraph(token).ContinueWith ((Task<ProjectGraph> task) => {
				try {
					try {
						if (task.Exception != null) {
							throw task.Exception;
						}
						ProjectGraph graph = task.Result;

						var nodeStack = new List<ProjectGraph.Node>();
						foreach (var node in graph.Nodes) {
							if (!nodeStack.Contains (node)) {
								Visit(node, nodeStack, token);
							}
						}

						graph.ResetVisitedNodes();

						//The algorithm requires that we reverse the direction of all graph arcs
						//so we'll use VisitReversed instead of Visit

						var stronglyConnectedComponents = new List<List<ProjectGraph.Node>>();

						while (nodeStack.Any ()) {
							ProjectGraph.Node topVertex = nodeStack[nodeStack.Count - 1];

							var stronglyConnectedComponent = new List<ProjectGraph.Node>();

							VisitReversed(topVertex, stronglyConnectedComponent, token);

							nodeStack.RemoveAll(stronglyConnectedComponent.Contains);

							stronglyConnectedComponents.Add (stronglyConnectedComponent);
						}

						Application.Invoke (() => {
							foreach (var stronglyConnectedComponent in stronglyConnectedComponents) {
								Console.WriteLine("Component:");

								foreach (var node in stronglyConnectedComponent) {
									Console.WriteLine (node);
								}

								Console.WriteLine("---");
							}

							okButton.Sensitive = true;
						});

					} catch (AggregateException ex) {
						ex.Handle(innerEx => innerEx is TaskCanceledException);
					}
				} catch (AggregateException ex) {
					if (ex.InnerExceptions.All (innerEx => innerEx is ProjectHasErrorsException)) {
						Content = new Label(GettextCatalog.GetString("Error: Please fix all errors before splitting project."));
					}
					else {
						throw;
					}
				}
			});;
		}

		void Visit (ProjectGraph.Node node, List<ProjectGraph.Node> nodeStack, CancellationToken token)
		{
			token.ThrowIfCancellationRequested ();

			node.Visited = true;

			foreach (var destinationNode in node.DestinationNodes) {
				if (!destinationNode.Visited) {
					Visit (destinationNode, nodeStack, token);
				}
			}

			nodeStack.Add (node);
		}

		void VisitReversed (ProjectGraph.Node node, List<ProjectGraph.Node> nodeStack, CancellationToken token)
		{
			token.ThrowIfCancellationRequested ();

			node.Visited = true;

			foreach (var destinationNode in node.SourceNodes) {
				if (!destinationNode.Visited) {
					VisitReversed (destinationNode, nodeStack, token);
				}
			}

			nodeStack.Add (node);
		}

		Task<ProjectGraph> BuildGraph (CancellationToken token) {
			return Task.Factory.StartNew(() => {
				var projectGraph = new ProjectGraph();

				//Get nodes
				foreach (var file in project.Files) {
					token.ThrowIfCancellationRequested();
					projectGraph.AddNode(new ProjectGraph.Node(file));
				}

				//Find out which types each file depends on

				Dictionary<IType, List<ProjectGraph.Node>> typeDefinitions = new Dictionary<IType, List<ProjectGraph.Node>>();

				foreach (var node in projectGraph.Nodes) {
					token.ThrowIfCancellationRequested();

					var file = node.File;

					if (file.BuildAction != "Compile") continue;

					if (!file.Name.EndsWith (".cs", StringComparison.InvariantCultureIgnoreCase)) {
						continue;
					}

					bool isOpen;
					System.Text.Encoding encoding;
					bool hadBom;

					var data = TextFileProvider.Instance.GetTextEditorData (file.Name, out hadBom, out encoding, out isOpen);

					ParsedDocument parsedDocument;
					using (var reader = new StreamReader (data.OpenStream ()))
						parsedDocument = new MonoDevelop.CSharp.Parser.TypeSystemParser ().Parse (true, file.Name, reader, project);

					token.ThrowIfCancellationRequested();

					var resolver = new CSharpAstResolver (TypeSystemService.GetCompilation (project), (SyntaxTree) parsedDocument.Ast, parsedDocument.ParsedFile as CSharpUnresolvedFile);

					var ctx = new MDRefactoringContext (project as DotNetProject, data, parsedDocument, resolver, resolver.RootNode.StartLocation, token);

					//Step 1. Find all type declarations and identify which are partial
					var typeDeclarations = ctx.RootNode.Descendants.OfType<TypeDeclaration>();
					foreach (var typeDeclaration in typeDeclarations) {
						var resolveResult = ctx.Resolve(typeDeclaration);
						if (resolveResult.IsError) {
							throw new ProjectHasErrorsException();
						}
						var typeResolveResult = resolveResult as TypeResolveResult;
						var type = typeResolveResult.Type;
						if (!typeDefinitions.ContainsKey(type)) {
							typeDefinitions[type] = new List<ProjectGraph.Node>();
						}
						typeDefinitions[type].Add (node);
						node.AddTypeDependency(type);
					}

					foreach (var expression in ctx.RootNode.Descendants.OfType<Expression>()) {
						token.ThrowIfCancellationRequested();

						var resolveResult = ctx.Resolve(expression);
						var type = GetTypeDependency(projectGraph, node, resolveResult);
						node.AddTypeDependency(type);
					}

					foreach (var type in ctx.RootNode.Descendants.OfType<AstType>()) {
						token.ThrowIfCancellationRequested();

						var resolvedType = ctx.ResolveType (type);
						node.AddTypeDependency (resolvedType);
					}
				}

				//Turn the type dependencies into node dependencies
				foreach (var node in projectGraph.Nodes) {
					token.ThrowIfCancellationRequested();

					foreach (var dependedType in node.TypeDependencies) {
						List<ProjectGraph.Node> dependedNodes;
						if (typeDefinitions.TryGetValue(dependedType, out dependedNodes)) {
							foreach (var dependedNode in dependedNodes) {
								node.AddEdgeTo(dependedNode);
							}
						}
					}
				}

				return projectGraph;
			}, TaskCreationOptions.LongRunning);
		}

		IType GetTypeDependency (ProjectGraph projectGraph, ProjectGraph.Node node, ResolveResult resolveResult)
		{
			if (resolveResult.IsError) {
				//FIXME
				return SpecialType.NullType;
				//throw new ProjectHasErrorsException();
			}

			return resolveResult.Type;
		}
	}
}

