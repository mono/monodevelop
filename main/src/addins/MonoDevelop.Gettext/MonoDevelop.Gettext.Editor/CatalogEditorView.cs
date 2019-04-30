//
// CatalogEditor.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// Copyright (C) 2007 David Makovský
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

using System;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide;

namespace MonoDevelop.Gettext.Editor
{
	[ExportFileDocumentController (FileExtension = ".po", Name = "Gettext Editor", CanUseAsDefault = true, InsertBefore = "DefaultDisplayBinding")]
	class CatalogEditorView : FileDocumentController, IUndoHandler
	{
		Catalog catalog;
		POEditorWidget poEditorWidget;
		FilePath fileName;
		
		protected override Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			TabPageLabel = GettextCatalog.GetString ("Gettext Editor");
			return base.OnInitialize (modelDescriptor, status);
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			TranslationProject project = null;

			if (IdeApp.IsInitialized) {
				foreach (var tp in IdeApp.Workspace.GetAllItems<TranslationProject> ())
					if (tp.BaseDirectory == FilePath.ParentDirectory)
						project = tp;
			}

			catalog = new Catalog (project);
			poEditorWidget = new POEditorWidget (project);
			catalog.DirtyChanged += delegate (object sender, EventArgs args) {
				HasUnsavedChanges = catalog.IsDirty;
			};

			catalog.Load (null, FilePath);

			poEditorWidget.Catalog = catalog;
			poEditorWidget.POFileName = fileName;
			poEditorWidget.UpdateRules (System.IO.Path.GetFileNameWithoutExtension (fileName));
			return poEditorWidget;
		}

		protected override Task OnSave ()
		{
			catalog.Save (FilePath);
			return Task.CompletedTask;
		}
		
		#region IUndoHandler implementation
		void IUndoHandler.Undo ()
		{
			poEditorWidget.Undo ();
		}
		
		void IUndoHandler.Redo ()
		{
			poEditorWidget.Redo ();
		}
		
		IDisposable IUndoHandler.OpenUndoGroup ()
		{
			return poEditorWidget.OpenUndoGroup ();
		}
		
		bool IUndoHandler.EnableUndo {
			get {
				return poEditorWidget.EnableUndo;
			}
		}
		
		bool IUndoHandler.EnableRedo {
			get {
				return poEditorWidget.EnableRedo;
			}
		}
		#endregion
	}
}
