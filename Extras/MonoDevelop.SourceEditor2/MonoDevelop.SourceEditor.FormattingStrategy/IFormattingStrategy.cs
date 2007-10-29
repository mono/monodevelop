
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor.FormattingStrategy
{
	/// <summary>
	/// This interface handles the auto and smart indenting and formating
	/// in the document while  you type. Language bindings could overwrite this 
	/// interface and define their own indentation/formating.
	/// </summary>
	public interface IFormattingStrategy {
		
		/// <summary>
		/// This function formats a specific line after <code>ch</code> is pressed.
		/// </summary>
		/// <returns>
		/// the caret delta position the caret will be moved this number
		/// of bytes (e.g. the number of bytes inserted before the caret, or
		/// removed, if this number is negative)
		/// </returns>
		int FormatLine (TextEditor editor, int line, int caretOffset, char charTyped, string indentString, bool autoInsertCurlyBracket);
		
		/// <summary>
		/// This function sets the indentation level in a specific line
		/// </summary>
		/// <returns>
		/// the number of inserted characters.
		/// </returns>
		int IndentLine (TextEditor editor, int line, string indentString);
	}	
}
