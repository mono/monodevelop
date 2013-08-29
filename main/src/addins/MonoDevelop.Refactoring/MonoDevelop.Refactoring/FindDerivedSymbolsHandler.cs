//
// FindDerivedSymbolsHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Analysis;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Refactoring
{
	public class FindDerivedSymbolsHandler 
	{
		//Ide.Gui.Document doc;
		IMember entity;

		public FindDerivedSymbolsHandler (Ide.Gui.Document doc, IMember entity)
		{
			//this.doc = doc;
			this.entity = entity;
		}

		public void Run ()
		{
			var assemblies = new HashSet<IAssembly> ();
			foreach (var project in IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
				var comp = TypeSystemService.GetCompilation (project); 
				if (comp == null)
					continue;
				assemblies.Add (comp.MainAssembly);
			}

			TypeGraph tg = new TypeGraph (assemblies);
			var node = tg.GetNode (entity.DeclaringTypeDefinition); 
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
				Stack<IList<TypeGraphNode>> derivedTypes = new Stack<IList<TypeGraphNode>> ();
				derivedTypes.Push (node.DerivedTypes); 
				HashSet<ITypeDefinition> visitedType = new HashSet<ITypeDefinition> ();
				while (derivedTypes.Count > 0) {
					foreach (var derived in derivedTypes.Pop ()) {
						if (visitedType.Contains (derived.TypeDefinition))
							continue;
						derivedTypes.Push (tg.GetNode (derived.TypeDefinition).DerivedTypes);
						visitedType.Add (derived.TypeDefinition);
						var impMember = derived.TypeDefinition.Compilation.Import (entity);
						if (impMember == null)
							continue;
						IMember derivedMember;
						if (entity.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
							derivedMember = derived.TypeDefinition.GetMembers (null, GetMemberOptions.IgnoreInheritedMembers).FirstOrDefault (
								m => m.ImplementedInterfaceMembers.Any (im => im.Region == entity.Region)
							);
						} else {
							derivedMember = InheritanceHelper.GetDerivedMember (impMember, derived.TypeDefinition);
						}
						if (derivedMember == null)
							continue;
						var tf = TextFileProvider.Instance.GetReadOnlyTextEditorData (derivedMember.Region.FileName);
						var start = tf.LocationToOffset (derivedMember.Region.Begin); 
						tf.SearchRequest.SearchPattern = derivedMember.Name;
						var sr = tf.SearchForward (start); 
						if (sr != null)
							start = sr.Offset;

						monitor.ReportResult (new MemberReference (derivedMember, derivedMember.Region, start, derivedMember.Name.Length));
					}
				}
			}
		}
	}
}

