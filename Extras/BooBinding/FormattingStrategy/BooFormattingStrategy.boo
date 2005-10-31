namespace BooBinding.FormattingStrategy

import System
import MonoDevelop.SourceEditor.Document
import MonoDevelop.SourceEditor.FormattingStrategy


class BooFormattingStrategy (DefaultFormattingStrategy):
	protected override def SmartIndentLine (d as IFormattableDocument, line as int):
		if line > 0:
			prev_text = d.GetLineAsString (line - 1)
			prev_text_trim = prev_text.Trim ()
			curr_text = d.GetLineAsString (line)
			curr_text_trim = curr_text.Trim ()
			
			if prev_text_trim.EndsWith (":"):
				indent = GetIndentation (d, line -1) + d.IndentString
				d.ReplaceLine (line, indent + curr_text_trim)
				return indent.Length

			// XXX: Add support for ending blocks with multiple blank lines
		
		return AutoIndentLine (d, line)
