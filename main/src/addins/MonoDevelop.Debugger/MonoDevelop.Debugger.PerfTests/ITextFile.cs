//
// TextFile.cs
//
// Author:
//       Artem Bukhonov <artem.bukhonov@jetbrains.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#pragma warning disable 1587
namespace Mono.Debugging.PerfTests
{
	public interface ITextFile
	{
//		TODO: Implement in another part of the class

		/// <summary>
		/// Content of the file
		/// </summary>
		string Text { get; }
		/// <summary>
		/// Full path to file
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Returns line and column (1-based) by given offset (0-based)
		/// </summary>
		/// <param name="offset">0-based</param>
		/// <param name="line">1-based</param>
		/// <param name="col">1-based</param>
		void GetLineColumnFromPosition (int offset, out int line, out int col);

		/// <summary>
		/// Returns offset by given line and column (1-based)
		/// </summary>
		/// <param name="line">line (1-based)</param>
		/// <param name="column">column (1-based)</param>
		/// <returns>offset (0-based)</returns>
		int GetPositionFromLineColumn (int line, int column);

		/// <summary>
		/// Returns the text starting from <paramref name="startOffset"/> with length=<paramref name="endOffset"/>
		/// </summary>
		/// <param name="startOffset">0-based starting offset</param>
		/// <param name="endOffset">0-based end offset</param>
		/// <returns></returns>
		string GetText(int startOffset, int endOffset);

		/// <summary>
		/// Returns length of the given line (1-based)
		/// </summary>
		/// <param name="line">1-based line</param>
		/// <returns></returns>
		int GetLineLength (int line);
	}
}