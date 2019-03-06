//
// GuiBuilderDisplayBinding.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.Gui.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	[ExportDocumentControllerFactory (FileExtension = ".cs", InsertBefore = "DefaultDisplayBinding")]
	public class GuiBuilderDisplayBinding : FileDocumentControllerFactory
	{
		bool excludeThis = false;
		
		public string Name {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("Window Designer"); }
		}
		
		public bool CanUseAsDefault {
			get { return true; }
		}

		protected override async Task<IEnumerable<DocumentControllerDescription>> GetSupportedControllersAsync (FileDescriptor file)
		{
			var list = ImmutableList<DocumentControllerDescription>.Empty;

			if (excludeThis)
				return list;

			if (file.FilePath.IsNullOrEmpty || !(file.Owner is DotNetProject))
				return list;

			if (!IdeApp.Workspace.IsOpen)
				return list;

			if (GetWindow (file.FilePath, (DotNetProject)file.Owner) == null)
				return list;

			excludeThis = true;
			var db = (await IdeServices.DocumentControllerService.GetSupportedControllers (file)).FirstOrDefault (d => d.Role == DocumentControllerRole.Source);
			excludeThis = false;
			if (db != null) {
				list = list.Add (
					new DocumentControllerDescription {
						CanUseAsDefault = true,
						Role = DocumentControllerRole.VisualDesign,
						Name = MonoDevelop.Core.GettextCatalog.GetString ("Window Designer")
					});
			}
			return list;
		}

		public override async Task<DocumentController> CreateController (FileDescriptor file, DocumentControllerDescription controllerDescription)
		{
			excludeThis = true;
			var db = (await IdeServices.DocumentControllerService.GetSupportedControllers (file)).FirstOrDefault (d => d.Role == DocumentControllerRole.Source);
			var content = await db.CreateController (file);
			await content.Initialize (file);
			var window = GetWindow (file.FilePath, (Project)file.Owner);
			if (window == null)
				throw new InvalidOperationException ("GetWindow == null");
			var view = new GuiBuilderView (content, window);
			excludeThis = false;
			return view;
		}
		
		internal static GuiBuilderWindow GetWindow (string file, Project project)
		{
			if (!IdeApp.Workspace.IsOpen)
				return null;
			if (!GtkDesignInfo.HasDesignedObjects (project))
				return null;
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			if (file.StartsWith (info.GtkGuiFolder))
				return null;
			var docId = IdeApp.TypeSystemService.GetDocumentId (project, file);
			if (docId == null)
				return null;
			var doc = IdeApp.TypeSystemService.GetCodeAnalysisDocument (docId);
			if (doc == null)
				return null;
			Microsoft.CodeAnalysis.SemanticModel semanticModel;
			try {
				semanticModel = doc.GetSemanticModelAsync ().Result;
			} catch {
				return null;
			}
			if (semanticModel == null)
				return null;
			var root = semanticModel.SyntaxTree.GetRoot ();
			foreach (var classDeclaration in root.DescendantNodesAndSelf (child => !(child is BaseTypeDeclarationSyntax)).OfType<ClassDeclarationSyntax> ()) {
				var c = semanticModel.GetDeclaredSymbol (classDeclaration);
				GuiBuilderWindow win = info.GuiBuilderProject.GetWindowForClass (c.ToDisplayString (Microsoft.CodeAnalysis.SymbolDisplayFormat.CSharpErrorMessageFormat));
				if (win != null)
					return win;
			}
			return null;
		}
	}
}
