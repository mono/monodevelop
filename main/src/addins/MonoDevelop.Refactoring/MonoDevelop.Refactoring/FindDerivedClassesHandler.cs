// 
// FindDerivedClassesHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Ide.FindInFiles;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Linq;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Semantics;
using System.Threading.Tasks;

namespace MonoDevelop.Refactoring
{
	public class FindDerivedClassesHandler : CommandHandler
	{
		public static void FindDerivedClasses (ITypeDefinition cls)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;
			
			var sourceProject = TypeSystemService.GetProject (cls);
			if (sourceProject == null)
				return;
			
			var projects = ReferenceFinder.GetAllReferencingProjects (solution, sourceProject);
			ThreadPool.QueueUserWorkItem (delegate {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
					var cache = new Dictionary<string, TextEditorData> ();
					monitor.BeginTask (GettextCatalog.GetString ("Searching for derived classes in solution..."), projects.Count);

					Parallel.ForEach (projects, p => {
						var comp = TypeSystemService.GetCompilation (p);
						if (comp == null)
							return;
						var importedType = comp.Import (cls);
						if (importedType == null) {
							return;
						}
						foreach (var type in comp.MainAssembly.GetAllTypeDefinitions ()) {
							if (!type.IsDerivedFrom (importedType)) 
								continue;
							TextEditorData textFile;
							if (!cache.TryGetValue (type.Region.FileName, out textFile)) {
								cache [type.Region.FileName] = textFile = TextFileProvider.Instance.GetTextEditorData (type.Region.FileName);
							}
							int position = textFile.LocationToOffset (type.Region.Begin);
							string keyword;
							switch (type.Kind) {
							case TypeKind.Interface:
								keyword = "interface";
								break;
							case TypeKind.Struct:
								keyword = "struct";
								break;
							case TypeKind.Delegate:
								keyword = "delegate";
								break;
							case TypeKind.Enum:
								keyword = "enum";
								break;
							default:
								keyword = "class";
								break;
							}
							while (position < textFile.Length - keyword.Length) {
								if (textFile.GetTextAt (position, keyword.Length) == keyword) {
									position += keyword.Length;
									while (position < textFile.Length && textFile.GetCharAt (position) == ' ' || textFile.GetCharAt (position) == '\t')
										position++;
									break;
								}
								position++;
							}
							monitor.ReportResult (new MonoDevelop.Ide.FindInFiles.SearchResult (new FileProvider (type.Region.FileName, p), position, 0));
						}
						monitor.Step (1);
					});
					foreach (var tf in cache.Values) {
						if (tf.Parent == null)
							tf.Dispose ();
					}
					monitor.EndTask ();
				}
			});
		}
		
		protected override void Run (object data)
		{
//			var doc = IdeApp.Workbench.ActiveDocument;
//			if (doc == null || doc.FileName == FilePath.Null)
//				return;
//			ResolveResult resolveResult;
//			var item = CurrentRefactoryOperationsHandler.GetItem (doc, out resolveResult);
//			
//			IMember eitem = resolveResult != null ? (resolveResult.CallingMember ?? resolveResult.CallingType) : null;
//			string itemName = null;
//			if (item is IMember)
//				itemName = ((IMember)item).FullName;
//			if (item != null && eitem != null && (eitem.Equals (item) || (eitem.FullName == itemName && !(eitem is IProperty) && !(eitem is IMethod)))) {
//				item = eitem;
//				eitem = null;
//			}
//			ITypeDefinition eclass = null;
//			if (item is ITypeDefinition) {
//				if (((ITypeDefinition)item).Kind == TypeKind.Interface)
//					eclass = CurrentRefactoryOperationsHandler.FindEnclosingClass (ctx, editor.Name, line, column); else
//					eclass = (IType)item;
//				if (eitem is IMethod && ((IMethod)eitem).IsConstructor && eitem.DeclaringType.Equals (item)) {
//					item = eitem;
//					eitem = null;
//				}
//			}
		}
	}
}

