namespace MonoDevelop.EditorBindings.FormattingStrategy {
	public interface IFormattableDocument {
		string GetLineAsString (int ln);
		
		void BeginAtomicUndo ();
		void EndAtomicUndo ();
		void ReplaceLine (int ln, string txt);
		
		IndentStyle IndentStyle { get; }
		bool AutoInsertCurlyBracket { get; }
		
		string TextContent { get; }
		int TextLength { get; }
		char GetCharAt (int offset);
		void Insert (int offset, string text);
		
		string IndentString { get ; }
		
		int GetClosingBraceForLine (int ln, out int openingLine);
		void GetLineLengthInfo (int ln, out int offset, out int len);
		
	}
}
