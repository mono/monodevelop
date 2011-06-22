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
using MonoDevelop.TypeSystem;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Ide.FindInFiles;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Linq;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring
{
	public class FindDerivedClassesHandler : CommandHandler
	{
		
		public static void FindDerivedClasses (ITypeDefinition cls)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;
			var sourceCtx     = cls.GetProjectContent ();
			var sourceProject = sourceCtx.Annotation<Project> ();
			var projects = new List<Tuple<Project, IProjectContent>> ();
			projects.Add (Tuple.Create (sourceProject, sourceCtx));
			foreach (var project in solution.GetAllProjects ()) {
				if (project.GetReferencedItems (ConfigurationSelector.Default).Any (prj => prj == sourceProject))
					projects.Add (Tuple.Create (project, TypeSystemService.GetProjectContext (project)));
			}
			
			ThreadPool.QueueUserWorkItem (delegate {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
					var cache = new Dictionary<string, TextEditorData> ();
					foreach (var p in projects) {
						var combinedContent = sourceProject != p.Item2 ?  (ITypeResolveContext)new CompositeTypeResolveContext (new ITypeResolveContext[] { sourceCtx, p.Item2 }) : sourceCtx;
						foreach (var type in p.Item2.GetAllTypes ()) {
							if (!type.IsDerivedFrom (cls, combinedContent)) 
								continue;
							TextEditorData textFile;
							if (!cache.TryGetValue (type.Region.FileName, out textFile)) {
								cache[type.Region.FileName] = textFile = TextFileProvider.Instance.GetTextEditorData (type.Region.FileName);
							}
							int position = textFile.LocationToOffset (type.Region.BeginLine, type.Region.BeginColumn);
							monitor.ReportResult (new MonoDevelop.Ide.FindInFiles.SearchResult (new FileProvider (type.Region.FileName, p.Item1), position, 0));
						}
					}
					foreach (var tf in cache.Values) {
						if (tf.Parent == null)
							tf.Dispose ();
					}
				}
			});
		}
		
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var ctx = doc.TypeResolveContext;
			ResolveResult resolveResult;
			var item = CurrentRefactoryOperationsHandler.GetItem (ctx, doc, out resolveResult);
			
			if (item is ITypeDefinition) {
				FindDerivedClasses ((ITypeDefinition)item);
				return;
			}
			
//			
//			IMember eitem = resolveResult != null ? (resolveResult.CallingMember ?? resolveResult.CallingType) : null;
//			string itemName = null;
//			if (item is IMember)
//				itemName = ((IMember)item).Name;
//			if (item != null && eitem != null && (eitem.Equals (item) || (eitem.Name == itemName && !(eitem is IProperty) && !(eitem is IMethod)))) {
//				item = eitem;
//				eitem = null;
//			}
//			IType eclass = null;
//			if (item is IType) {
//				if (((IType)item).ClassType == ClassType.Interface)
//					eclass = CurrentRefactoryOperationsHandler.FindEnclosingClass (ctx, editor.Name, line, column); else
//					eclass = (IType)item;
//				if (eitem is IMethod && ((IMethod)eitem).IsConstructor && eitem.DeclaringType.Equals (item)) {
//					item = eitem;
//					eitem = null;
//				}
//			}
//			Refactorer refactorer = new Refactorer (ctx, doc.CompilationUnit, eclass, item, null);
//			refactorer.FindDerivedClasses ();
		}
	}
}

