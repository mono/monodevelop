using MDTextFile = MonoDevelop.Projects.Text.TextFile;

namespace Mono.Debugging.Tests
{
	public class TextFile : ITextFile
	{
		readonly MDTextFile file;

		public TextFile (MDTextFile file)
		{
			this.file = file;
		}

		/// <summary>
		/// Content of the file
		/// </summary>
		public string Text
		{
			get{
				return file.Text;
			}
		}

		/// <summary>
		/// Full path to file
		/// </summary>
		public string Name
		{
			get{
				return file.Name;
			}
		}

		/// <summary>
		/// Returns line and column (1-based) by given offset (0-based)
		/// </summary>
		/// <param name="offset">0-based</param>
		/// <param name="line">1-based</param>
		/// <param name="col">1-based</param>
		public void GetLineColumnFromPosition (int offset, out int line, out int col)
		{
			file.GetLineColumnFromPosition (offset, out line, out col);
		}

		/// <summary>
		/// Returns offset by given line and column (1-based)
		/// </summary>
		/// <param name="line">line (1-based)</param>
		/// <param name="column">column (1-based)</param>
		/// <returns>offset (0-based)</returns>
		public int GetPositionFromLineColumn (int line, int column)
		{
			return file.GetPositionFromLineColumn (line, column);
		}

		/// <summary>
		/// Returns the text starting from <paramref name="offset"/> with length=<paramref name="length"/>
		/// </summary>
		/// <param name="offset">0-based starting offset</param>
		/// <param name="length">length of text</param>
		/// <returns></returns>
		public string GetText (int offset, int length)
		{
			return file.GetText (offset, length);
		}

		/// <summary>
		/// Returns length of the given line (1-based)
		/// </summary>
		/// <param name="line">1-based line</param>
		/// <returns></returns>
		public int GetLineLength (int line)
		{
			return file.GetLineLength (line);
		}
	}
}