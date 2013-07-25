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
						token.ThrowIfCancellationRequested ();

						//TODO: Compute strongly connected components

					} catch (AggregateException ex) {
						ex.Handle(innerEx => innerEx is TaskCanceledException);
					}
					Application.Invoke (() => {
						okButton.Sensitive = true;
					});
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
						if (resolveResult is ErrorResolveResult) {
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
						var types = GetTypeDependencies(projectGraph, node, resolveResult);
						node.AddTypeDependencies(types);
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

		IEnumerable<IType> GetTypeDependencies (ProjectGraph projectGraph, ProjectGraph.Node node, ResolveResult resolveResult)
		{
			//We won't worry about generics here
			//because if we have Foo<T>, then T is already handled separately
			//and if we have Foo<T>(T x) called as Foo(exprOfTypeT), then the implicit T dependency is handled by the expression

			if (resolveResult.IsError) {
				throw new ProjectHasErrorsException();
			}

			if (resolveResult is ConstantResolveResult || resolveResult is AwaitResolveResult || resolveResult is MethodGroupResolveResult
			    || resolveResult is LocalResolveResult || resolveResult is ThisResolveResult || resolveResult is NamespaceResolveResult) {

				//We don't care about await expr, because we already solved the dependencies in expr
				//We don't care about MethodGroupResolveResult because we only want to choose the dependency of the specific chosen method
				//LocalResolveResult has been resolved where it was declared, so we also don't care about that

				return Enumerable.Empty<IType>();
			}

			var typeResolveResult = resolveResult as TypeResolveResult;
			if (typeResolveResult != null) {
				return new IType[] { typeResolveResult.Type };
			}

			var memberResolveResult = resolveResult as MemberResolveResult;
			if (memberResolveResult != null) {
				return new IType[] {
					memberResolveResult.Member.DeclaringType,
					memberResolveResult.Type
				};
			}

			var lambdaResult = resolveResult as LambdaResolveResult;
			if (lambdaResult != null) {
				return new IType[] { lambdaResult.Type };
			}

			var operatorResolveResult = resolveResult as OperatorResolveResult;
			if (operatorResolveResult != null) {
				var types = new List<IType> ();
				var userDefinedMethod = operatorResolveResult.UserDefinedOperatorMethod;
				if (userDefinedMethod != null) {
					types.Add (userDefinedMethod.DeclaringType);
				}
				types.Add (operatorResolveResult.Type);
				return types;
			}

			var conversionResolveResult = resolveResult as ConversionResolveResult;
			if (conversionResolveResult != null) {
				//We have to check because of implicit conversions
				return new IType[] { conversionResolveResult.Type };
			}

			throw new NotImplementedException("TODO: SplitProjectDialog.GetTypeDependencies for " + resolveResult.GetType ().FullName);
		}
	}
}

