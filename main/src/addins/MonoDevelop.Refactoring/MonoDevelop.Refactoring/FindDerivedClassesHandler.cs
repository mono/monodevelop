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
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Ide.FindInFiles;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Semantics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace MonoDevelop.Refactoring
{
	public class FindDerivedClassesHandler : CommandHandler
	{
		public static void FindDerivedClasses (ITypeDefinition cls)
		{
			FindDerivedSymbols (cls, null);
		}

		public static void FindDerivedMembers (IMember member)
		{
			var cls = member.DeclaringTypeDefinition;
			if (cls == null)
				return;
			FindDerivedSymbols (cls, member);
		}

		static void FindDerivedSymbols (ITypeDefinition cls, IMember member)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;

			var sourceProject = TypeSystemService.GetProject (cls);
			if (sourceProject == null)
				return;

			var compilations = ReferenceFinder.GetAllReferencingProjects (solution, sourceProject)
				.Select (TypeSystemService.GetCompilation).Where (c => c != null).ToList ();

			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
				var label = member == null
					? GettextCatalog.GetString ("Searching for derived classes in solution...")
					: GettextCatalog.GetString ("Searching for derived members in solution...");
				monitor.BeginTask (label, compilations.Count);

				Parallel.ForEach (compilations, comp => {
					try {
						SearchCompilation (monitor, comp, cls, member);
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
						monitor.ReportError ("Unhandled error while searching", ex);
					}
					monitor.Step (1);
				});

				monitor.EndTask ();
			};
		}

		static void SearchCompilation (ISearchProgressMonitor monitor, ICompilation comp, ITypeDefinition cls, IMember member)
		{
			var importedType = comp.Import (cls);
			if (importedType == null) {
				return;
			}

			IMember impMember = null;
			if (member != null) {
				impMember = comp.Import (member);
				if (impMember == null) {
					return;
				}
			}

			foreach (var derivedType in comp.MainAssembly.GetAllTypeDefinitions ()) {
				if (!derivedType.IsDerivedFrom (importedType))
					continue;
				IEntity result;
				if (member != null) {
					result = FindDerivedMember (impMember, derivedType);
					if (result == null)
						continue;
				}
				else {
					result = derivedType;
				}
				ReportResult (monitor, result);
			}
		}

		static IMember FindDerivedMember (IMember importedMember, ITypeDefinition derivedType)
		{
			IMember derivedMember;
			if (importedMember.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
				derivedMember = derivedType.GetMembers (null, GetMemberOptions.IgnoreInheritedMembers)
					.FirstOrDefault (m => m.ImplementedInterfaceMembers.Any (im => im.Region == importedMember.Region));
			}
			else {
				derivedMember = InheritanceHelper.GetDerivedMember (importedMember, derivedType);
			}
			return derivedMember;
		}

		static void ReportResult (ISearchProgressMonitor monitor, IEntity result)
		{
			string filename = result.Region.FileName;
			if (string.IsNullOrEmpty (filename))
				return;

			var textFile = TextFileProvider.Instance.GetTextEditorData (filename);
			var start = textFile.LocationToOffset (result.Region.Begin);
			textFile.SearchRequest.SearchPattern = result.Name;
			var sr = textFile.SearchForward (start);
			if (sr != null)
				start = sr.Offset;

			if (textFile.Parent == null)
				textFile.Dispose ();

			monitor.ReportResult (new MemberReference (result, result.Region, start, result.Name.Length));
		}

		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;

			ResolveResult resolveResult;
			var item = CurrentRefactoryOperationsHandler.GetItem (doc, out resolveResult);

			var typeDef = item as ITypeDefinition;
			if (typeDef != null && ((typeDef.Kind == TypeKind.Class && !typeDef.IsSealed) || typeDef.Kind == TypeKind.Interface)) {
				FindDerivedClasses (typeDef);
				return;
			}

			var member = item as IMember;
			var handler = new FindDerivedSymbolsHandler (member);
			if (handler.IsValid) {
				handler.Run ();
				return;
			}
		}
	}
}

