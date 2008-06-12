using System;

namespace MonoDevelop.CodeAnalysis {
	
	public class CodeLocation {
		
		public string File {
			get { return file; }
		}
		private string file;
		
		public int Line {
			get { return line; }
		}
		private int line;
		
		public int Column {
			get { return column; }
		}
		private int column;
		
		public CodeLocation ()
		{
			file = string.Empty;
		}
		
		public CodeLocation(string file, int line, int column)
		{
			this.file = file;
			this.line = line;
			this.column = column;
		}
	}
}
