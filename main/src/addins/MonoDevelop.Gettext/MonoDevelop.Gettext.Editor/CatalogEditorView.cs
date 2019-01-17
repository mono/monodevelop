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
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System.Threading.Tasks;

namespace MonoDevelop.Gettext.Editor
{
	class CatalogEditorView : ViewContent, IUndoHandler
	{
		Catalog catalog;
		POEditorWidget poEditorWidget;
		
		public CatalogEditorView (TranslationProject project, string poFile)
		{
			catalog = new Catalog (project);
			poEditorWidget = new POEditorWidget (project);
			catalog.DirtyChanged += delegate (object sender, EventArgs args) {
				IsDirty = catalog.IsDirty;
			};
		}
		
		public override Task Load (FileOpenInformation fileOpenInformation)
		{
			var fileName = fileOpenInformation.FileName;
//			using (IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
			catalog.Load (null, fileName);
//			}
			poEditorWidget.Catalog = catalog;
			poEditorWidget.POFileName = fileName;
			poEditorWidget.UpdateRules (System.IO.Path.GetFileNameWithoutExtension (fileName));
			
			this.ContentName = fileName;
			this.IsDirty = false;
			return Task.FromResult (true);
		}
		
		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			catalog.Save (fileSaveInformation.FileName);
			ContentName = fileSaveInformation.FileName;
			IsDirty = false;
			return Task.FromResult (true);
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
	
		public override Control Control
		{
			get { return poEditorWidget; }
		}
				
		public override bool IsReadOnly
		{
			get { return false; }
		}
		
		public override string TabPageLabel 
		{
			get { return GettextCatalog.GetString ("Gettext Editor"); }
		}
	}
}
