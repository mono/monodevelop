namespace BooBinding.FormattingStrategy

import System
import MonoDevelop.Ide.Gui
import MonoDevelop.SourceEditor.FormattingStrategy


class BooFormattingStrategy (DefaultFormattingStrategy):
	protected override def SmartIndentLine (d as TextEditor, line as int, indentString as string):
		if line > 0:
			prev_text = d.GetLineText (line - 1)
			prev_text_trim = prev_text.Trim ()
			curr_text = d.GetLineText (line)
			curr_text_trim = curr_text.Trim ()
			
			if prev_text_trim.EndsWith (":"):
				indent = GetIndentation (d, line -1) + indentString
				d.ReplaceLine (line, indent + curr_text_trim)
				return indent.Length

			// XXX: Add support for ending blocks with multiple blank lines
		
		return AutoIndentLine (d, line, indentString)
