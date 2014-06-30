//
// AbstractEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class TextEditorExtension : ICommandRouter, IDisposable
	{
		public EditContext Document {
			get;
			protected set;
		}

		public TextEditor Editor {
			get;
			protected set;
		}

		internal TextEditorExtension Next {
			get;
			set;
		}

		protected internal void Initialize (EditContext document, TextEditor editor)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (document == null)
				throw new ArgumentNullException ("document");
			if (Document != null)
				throw new InvalidOperationException ("Extension is already initialized.");
			Document = document;
			Editor = editor;
			Initialize ();
		}

		protected virtual void Initialize ()
		{
		}

		public virtual bool IsValidInContext (EditContext document)
		{
			return true;
		}
		
		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public virtual bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			return Next == null || Next.KeyPress (key, keyChar, modifier);
		}

		public virtual void Dispose ()
		{
		}

		void CheckInitialized ()
		{
			if (Document == null)
				throw new InvalidOperationException ("Editor extension not yet initialized");
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return Next;
		}

		protected Ambience GetAmbience ()
		{
			CheckInitialized ();
			return AmbienceService.GetAmbienceForFile (Document.FileName);
		}
	}

	class TextEditorExtensionMarker : TextEditorExtension
	{
		public override bool IsValidInContext (EditContext document)
		{
			return false;
		}
	}
}