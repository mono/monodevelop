using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jurassic.Compiler
{
	internal class MultiLineComment : Comment
	{
		public int StartLine { get; set; }

		public int StartColumn { get; set; }

		public int EndLine { get; set; }

		public int EndColumn { get; set; }

		public MultiLineComment (string commentData, int startLine, int startColumn, int endLine, int endColumn) : base (commentData)
		{
			StartLine = startLine;
			StartColumn = startColumn;
			EndLine = endLine;
			EndColumn = endColumn;
		}
	}
}
