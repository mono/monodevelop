// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.FormattingStrategy;
using MonoDevelop.Core.Properties;
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
