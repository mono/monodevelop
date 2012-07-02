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
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	
	
	public class CompileErrorTooltipProvider: TooltipProvider
	{
		
		public CompileErrorTooltipProvider()
		{
		}

		#region ITooltipProvider implementation 
		
		public override TooltipItem GetItem (Mono.TextEditor.TextEditor editor, int offset)
		{
			var ed = editor as ExtensibleTextEditor;
			if (ed == null)
				return null;

			string errorInformation = ed.GetErrorInformationAt (offset);
			if (string.IsNullOrEmpty (errorInformation))
				return null;

			return new TooltipItem (errorInformation, editor.Document.GetLineByOffset (offset));
		}
		
		protected override Gtk.Window CreateTooltipWindow (Mono.TextEditor.TextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			LanguageItemWindow result = new LanguageItemWindow ((ExtensibleTextEditor) editor, modifierState, null, (string)item.Item, null);
			if (result.IsEmpty)
				return null;
			return result;
		}
		
		protected override void GetRequiredPosition (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			LanguageItemWindow win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}

		#endregion 
		
	}
}
