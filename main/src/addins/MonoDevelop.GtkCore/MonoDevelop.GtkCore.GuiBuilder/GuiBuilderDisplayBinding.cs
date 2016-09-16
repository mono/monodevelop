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


namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderDisplayBinding : IViewDisplayBinding
	{
		bool excludeThis = false;
		
		public string Name {
			get { return MonoDevelop.Core.GettextCatalog.GetString ("Window Designer"); }
		}
		
		public bool CanUseAsDefault {
			get { return true; }
		}
		
		public bool CanHandle (MonoDevelop.Core.FilePath fileName, string mimeType, Project ownerProject)
		{
			if (excludeThis)
				return false;
			
			if (fileName.IsNullOrEmpty)
				return false;
			
			if (GetWindow (fileName, ownerProject) == null)
				return false;
			
			excludeThis = true;
			var db = DisplayBindingService.GetDefaultViewBinding (fileName, mimeType, ownerProject);
			excludeThis = false;
			return db != null;
		}
		
		public ViewContent CreateContent (MonoDevelop.Core.FilePath fileName, string mimeType, Project ownerProject)
		{
			excludeThis = true;
			var db = DisplayBindingService.GetDefaultViewBinding (fileName, mimeType, ownerProject);
			var content = db.CreateContent (fileName, mimeType, ownerProject);
			content.Binding = db;
			var window = GetWindow (fileName, ownerProject);
			if (window == null)
				throw new InvalidOperationException ("GetWindow == null");
			GuiBuilderView view = new GuiBuilderView (content, window);
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
			var docId = TypeSystemService.GetDocumentId (project, file);
			if (docId == null)
				return null;
			var doc = TypeSystemService.GetCodeAnalysisDocument (docId);
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
