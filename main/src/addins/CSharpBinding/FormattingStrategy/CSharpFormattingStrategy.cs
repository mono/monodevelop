//  CSharpFormattingStrategy.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.FormattingStrategy;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace CSharpBinding.FormattingStrategy {
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class CSharpFormattingStrategy : DefaultFormattingStrategy {
		/// <summary>
		/// Define CSharp specific smart indenting for a line
		/// </summary>
		protected override int SmartIndentLine (TextEditor d, int lineNr, string indentString)
		{
			// Smart indenting is handled elsewhere in this CSharp Binding
			return 0;
		}
		
		public override int FormatLine (TextEditor d, int line, int cursorOffset, char ch,
		                                string indentString, bool autoInsertCurlyBracket)
		{
			// no-op, only ever called on Enter key press which we handle elsewhere.
			return 0;
		}
	}
}
