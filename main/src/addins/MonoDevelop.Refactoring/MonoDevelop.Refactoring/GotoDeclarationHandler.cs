// 
// GotoDeclarationHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;
using ICSharpCode.NRefactory.Analysis;
using MonoDevelop.Projects;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;

namespace MonoDevelop.Refactoring
{
	public class GotoDeclarationHandler : CommandHandler
	{
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;

			ResolveResult resolveResoult;
			object item = CurrentRefactoryOperationsHandler.GetItem (doc, out resolveResoult);
			var entity = item as INamedElement;
			if (entity != null) {
				Task.Factory.StartNew (() => {
					entity = CheckIfDefinedInOtherOpenSolution (entity);
					Xwt.Application.Invoke (() => IdeApp.ProjectOperations.JumpToDeclaration (entity));
				});
			} else {
				var v = item as IVariable;
				if (v != null)
					IdeApp.ProjectOperations.JumpToDeclaration (v);
			}
		}

		/// <summary>
		/// Gets a set of all the assemblies that are built from the given solution
		/// </summary>
		static HashSet<IAssembly> GetAllAssemblies (Solution solution)
		{
			var assemblies = new HashSet<IAssembly> ();

			foreach (var project in solution.GetAllProjects ()) {
				var comp = TypeSystemService.GetCompilation (project);
				if (comp == null)
					continue;
				assemblies.Add (comp.MainAssembly);
			}

			return assemblies;
		}

		/// <summary>
		/// Checks if the INamedElement is actually defined in another open solution and returns a INamedElement from that solution instead.
		/// Provides support for going to the source code definition of an element instead of the Assembly Browser.
		/// Returns the original entity if no suitable open solution was found
		/// </summary>
		static INamedElement CheckIfDefinedInOtherOpenSolution (INamedElement entity)
		{
			var ex = entity as IEntity;

			if (ex != null && ex.Region.IsEmpty) {
				// the entity was not found as a file in the current solution (no region)
				// let's see if we can find in another open solution

				if (IdeApp.Workspace != null) {
					Solution[] solutions;

					// do we have workspace? check the workspace for solution items
					var workspace = IdeApp.Workspace.Items.OfType<Workspace> ().FirstOrDefault ();
					if (workspace != null) 
						solutions = workspace.Items.OfType<Solution> ().ToArray ();
					else 
						solutions = IdeApp.Workspace.Items.OfType<Solution> ().ToArray ();

					foreach (var solution in solutions) {
						// skip the current solution
						if (IdeApp.ProjectOperations.CurrentSelectedSolution == solution)
							continue;

						var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
						using (monitor) {
							monitor.BeginTask (GettextCatalog.GetString ("Building type graph in solution ..."), 1); 

							var assemblies = GetAllAssemblies (solution);
							var tg = new TypeGraph (assemblies);
							var node = tg.GetNode (ex.DeclaringTypeDefinition); 
							if (node != null) {
								// look for the member that we're after
								var foundMember = node.TypeDefinition.Members.FirstOrDefault (x => x.FullName == entity.FullName);

								if (foundMember != null)
									return foundMember;

								// fall back to just the type, at least we're part way there
								return node.TypeDefinition;
							}

							monitor.EndTask ();
						}

					}
				}
			}

			return entity;
		}
	}
}
