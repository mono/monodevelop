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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class TextEditorExtension : ICommandRouter, IDisposable
	{
		public DocumentContext DocumentContext {
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

		protected internal void Initialize (TextEditor editor, DocumentContext context)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (context == null)
				throw new ArgumentNullException ("context");
			if (DocumentContext != null)
				throw new InvalidOperationException ("Extension is already initialized.");
			DocumentContext = context;
			Editor = editor;
			Initialize ();
		}

		protected virtual void Initialize ()
		{
		}

		public virtual bool IsValidInContext (DocumentContext context)
		{
			return true;
		}

		/// <summary>
		/// Return true if the key press should be processed by the editor.
		/// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		/// </summary>
		public virtual bool KeyPress (KeyDescriptor descriptor)
		{
			var tt = System.Diagnostics.Stopwatch.StartNew ();
			var r = Next == null || Next.KeyPress (descriptor);
			return r;
		}

		public virtual void Dispose ()
		{
			Editor = null;
			DocumentContext = null;
		}

		void CheckInitialized ()
		{
			if (DocumentContext == null)
				throw new InvalidOperationException ("Editor extension not yet initialized");
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return Next;
		}

		internal protected virtual object OnGetContent (Type type)
		{
			if (type.IsInstanceOfType (this))
				return this;
			else
				return null;
		}

		internal protected virtual IEnumerable<object> OnGetContents (Type type)
		{
			var c = OnGetContent (type);
			if (c != null)
				yield return c;
		}
	}

	class TextEditorExtensionMarker : TextEditorExtension
	{
		public override bool IsValidInContext (DocumentContext context)
		{
			return false;
		}
	}
}