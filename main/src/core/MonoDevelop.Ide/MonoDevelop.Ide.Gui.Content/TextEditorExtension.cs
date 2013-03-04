// TextEditorExtension.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Gui.Content
{
	public class TextEditorExtension : ITextEditorExtension, ICommandRouter
	{
		internal protected Document document;
		
		public void Initialize (Document document)
		{
			if (this.document != null)
				throw new InvalidOperationException ("Extension is already initialized.");
			this.document = document;
			Initialize ();
		}
		
		public ITextEditorExtension Next {
			get;
			set;
		}
		
		protected Document Document {
			get { return document; }
		}

		protected TextEditorData Editor {
			get { return document != null ? document.Editor : null; }
		}

		protected FilePath FileName {
			get {
				IViewContent view = document.Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}
		
		protected IProjectContent GetParserContext ()
		{
			CheckInitialized ();
			
			IViewContent view = document.Window.ViewContent;
			string file = view.IsUntitled ? view.UntitledName : view.ContentName;
			Project project = view.Project;
			
			if (project != null)
				return TypeSystemService.GetProjectContext (project);
			return TypeSystemService.GetContext (file, Document.Editor.Document.MimeType, Document.Editor.Text);
		}
		
		protected Ambience GetAmbience ()
		{
			CheckInitialized ();
			
			IViewContent view = document.Window.ViewContent;
			string file = view.IsUntitled ? view.UntitledName : view.ContentName;
			return AmbienceService.GetAmbienceForFile (file);
		}
		
		public virtual bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return true;
		}
		
		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public virtual bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			CheckInitialized ();
			
			if (Next == null)
				return true;
			else
				return Next.KeyPress (key, keyChar, modifier);
		}
		
		public virtual void CursorPositionChanged ()
		{
			CheckInitialized ();
			
			if (Next != null)
				Next.CursorPositionChanged ();
		}
		
		public virtual void TextChanged (int startIndex, int endIndex)
		{
			if (Next != null)
				Next.TextChanged (startIndex, endIndex);
		}
		
		public virtual void Initialize ()
		{
			CheckInitialized ();
			
			TextEditorExtension next = Next as TextEditorExtension;
			if (next != null)
				next.Initialize ();
		}
		
		public virtual void Dispose ()
		{
		}
		
		void CheckInitialized ()
		{
			if (document == null)
				throw new InvalidOperationException ("Editor extension not yet initialized");
		}
		
		object ITextEditorExtension.GetExtensionCommandTarget ()
		{
			return this;
		}
		
		object ICommandRouter.GetNextCommandTarget ()
		{
			if (Next != null)
				return Next.GetExtensionCommandTarget ();
			else
				return null;
		}
	}
	
	public interface ITextEditorExtension : IDisposable
	{
		ITextEditorExtension Next {
			get;
		}
		
		bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier);
		void CursorPositionChanged ();
		void TextChanged (int startIndex, int endIndex);
		
		// Return the object that is going to process commands, or null
		// if commands don't need custom processing
		object GetExtensionCommandTarget ();
	}
	
	class TextEditorExtensionMarker: TextEditorExtension
	{
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return false;
		}
	}
}
