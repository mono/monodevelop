// PythonEditorIndentation.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;

namespace PyBinding.Gui
{
	public class PythonEditorIndentation : TextEditorExtension
	{
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return !String.IsNullOrEmpty (doc.Name) && Path.GetExtension (doc.Name) == ".py";
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Return)
			{
				string lastLine = Editor.GetLineText (Editor.CursorLine);
				string trimmed = lastLine.Trim ();
				bool indent = false;
				
				if ((modifier & Gdk.ModifierType.ControlMask) != 0)
				{
					Editor.InsertText (Editor.CursorPosition, "\n");

					string endTrim = lastLine.TrimEnd ();
					if (!String.IsNullOrEmpty (endTrim))
					{
						int i = 0;
						while (Char.IsWhiteSpace (endTrim[i]))
							i++;

						if (i > 4)
							i -= 4;

						for (int j = 0; j < i; j++)
							Editor.InsertText (Editor.CursorPosition, " ");
						
						return false;
					}
					else if (lastLine.Length > 4)
					{
						// get the last line, remove 4 chars from it if we can
						Editor.InsertText (Editor.CursorPosition, lastLine.Substring (0, lastLine.Length - 4));
						return false;
					}
				}

				if (trimmed.EndsWith (":"))
				{
					indent = true;
				}
				else if (trimmed.StartsWith ("if ") ||
				         trimmed.StartsWith ("def "))
				{
					int openCount = lastLine.Split ('(').Length;
					int closeCount = lastLine.Split (')').Length;

					if (openCount > closeCount)
						indent = true;
				}
				else if (String.IsNullOrEmpty (trimmed))
				{
					// duplicate the last line
					base.KeyPress (key, keyChar, modifier);
					Editor.InsertText (Editor.CursorPosition, lastLine);
					return false;
				}

				if (indent)
				{
					base.KeyPress (key, keyChar, modifier);
					Editor.InsertText (Editor.CursorPosition, "    ");
					return false;
				}
			}
			
			return base.KeyPress (key, keyChar, modifier);
		}
	}
}
