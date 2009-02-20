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
	
	
	public class CompileErrorTooltipProvider: ITooltipProvider
	{
		
		public CompileErrorTooltipProvider()
		{
		}

		#region ITooltipProvider implementation 
		
		public object GetItem (TextEditor editor, int offset)
		{
			ExtensibleTextEditor ed = (ExtensibleTextEditor) editor;
			return ed.GetErrorInformationAt (offset);
		}
		
		public Gtk.Window CreateTooltipWindow (TextEditor editor, Gdk.ModifierType modifierState, object item)
		{
			LanguageItemWindow result = new LanguageItemWindow (null, modifierState, null, null, (string)item, null);
			if (result.IsEmpty)
				return null;
			return result;
		}
		
		public void GetRequiredPosition (TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			LanguageItemWindow win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		
		public bool IsInteractive (TextEditor editor, Gtk.Window tipWindow)
		{
			return false;
		}

		
		#endregion 
		
	}
}
