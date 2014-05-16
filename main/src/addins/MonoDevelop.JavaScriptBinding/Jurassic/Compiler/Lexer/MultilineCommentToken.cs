using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jurassic.Compiler
{
	class MultilineCommentToken : LiteralToken
	{
		public int StartLine { get; set; }

		public int StartColumn { get; set; }

		public int EndLine { get; set; }

		public int EndColumn { get; set; }

		public MultilineCommentToken (object value, int startLine, int startColumn, int endLine, int endColumn) : base (value)
		{
			StartLine = startLine;
			StartColumn = startColumn;
			EndLine = endLine;
			EndColumn = endColumn;
		}
	}
}
