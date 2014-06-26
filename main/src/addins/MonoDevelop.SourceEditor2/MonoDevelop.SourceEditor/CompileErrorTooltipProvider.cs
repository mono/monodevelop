// CompileErrorTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	class CompileErrorTooltipProvider: TooltipProvider
	{
		internal static ExtensibleTextEditor GetExtensibleTextEditor (TextEditor editor)
		{
			var view = editor.TextEditorImpl as SourceEditorView;
			if (view == null)
				return null;
			return view.TextEditor;
		}

		#region ITooltipProvider implementation 
		public override TooltipItem GetItem (TextEditor editor, int offset)
		{
			var ed = GetExtensibleTextEditor (editor);
			if (ed == null)
				return null;

			string errorInformation = ed.GetErrorInformationAt (offset);
			if (string.IsNullOrEmpty (errorInformation))
				return null;

			return new TooltipItem (errorInformation, editor.GetLineByOffset (offset));
		}
		
		public override Gtk.Window CreateTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			var result = new LanguageItemWindow (GetExtensibleTextEditor (editor), modifierState, null, (string)item.Item, null);
			if (result.IsEmpty)
				return null;
			return result;
		}
		
		public override void GetRequiredPosition (TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			var win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		#endregion 
	}
}
