//
// ISelectionSurroundingProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System;

namespace Mono.TextEditor
{
	/// <summary>
	/// A selection surrounding provider handles a special handling how the text editor behaves when the user
	/// types a key with a selection. The selection can be surrounded instead of beeing replaced.
	/// </summary>
	public interface ISelectionSurroundingProvider	
	{
		/// <summary>
		/// Gets the selection surroundings for a given unicode key.
		/// </summary>
		/// <returns>
		/// true, if the key is valid for a surrounding action.
		/// </returns>
		/// <param name='unicodeKey'>
		/// The key to handle.
		/// </param>
		/// <param name='start'>
		/// The start of the surrounding
		/// </param>
		/// <param name='end'>
		/// The end of the surrounding
		/// </param>
		bool GetSelectionSurroundings (TextEditorData textEditorData, uint unicodeKey, out string start, out string end);

		void HandleSpecialSelectionKey (TextEditorData textEditorData, uint unicodeKey);
	}

	/// <summary>
	/// Null selection surrounding provider. Basically turns off that feature.
	/// </summary>
	public sealed class NullSelectionSurroundingProvider : ISelectionSurroundingProvider
	{
		public bool GetSelectionSurroundings (TextEditorData textEditorData, uint unicodeKey, out string start, out string end)
		{
			start = end = "";
			return false;
		}

		public void HandleSpecialSelectionKey (TextEditorData textEditorData, uint unicodeKey)
		{
			throw new NotSupportedException ();
		}
	}

	/// <summary>
	/// Default selection surrounding provider.
	/// </summary>
	public class DefaultSelectionSurroundingProvider : ISelectionSurroundingProvider
	{
		public virtual bool GetSelectionSurroundings (TextEditorData textEditorData, uint unicodeKey, out string start, out string end)
		{
			switch ((char)unicodeKey) {
			case '"':
				start = end = "\"";
				return true;
			case '\'':
				start = end = "'";
				return true;
			case '(':
				start = "(";
				end = ")";
				return true;
			case '<':
				start = "<";
				end = ">";
				return true;
			case '[':
				start = "[";
				end = "]";
				return true;
			case '{':
				start = "{";
				end = "}";
				return true;
			default:
				start = end = "";
				return false;
			}
		}
	
		public virtual void HandleSpecialSelectionKey (TextEditorData textEditorData,uint unicodeKey)
		{
			string start, end;
			GetSelectionSurroundings (textEditorData, unicodeKey, out start, out end);
			
			if (textEditorData.MainSelection.SelectionMode == SelectionMode.Block) {
				var selection = textEditorData.MainSelection;
				int startCol = System.Math.Min (selection.Anchor.Column, selection.Lead.Column) - 1;
				int endCol = System.Math.Max (selection.Anchor.Column, selection.Lead.Column);
				for (int lineNumber = selection.MinLine; lineNumber <= selection.MaxLine; lineNumber++) {
					DocumentLine lineSegment = textEditorData.GetLine (lineNumber);
					
					if (lineSegment.Offset + startCol < lineSegment.EndOffset)
						textEditorData.Insert (lineSegment.Offset + startCol, start);
					if (lineSegment.Offset + endCol < lineSegment.EndOffset)
						textEditorData.Insert (lineSegment.Offset + endCol, end);
				}
				
				textEditorData.MainSelection = new Selection (
					new DocumentLocation (selection.Anchor.Line, endCol == selection.Anchor.Column ? endCol + start.Length : startCol + 1 + start.Length),
					new DocumentLocation (selection.Lead.Line, endCol == selection.Anchor.Column ? startCol + 1 + start.Length : endCol + start.Length),
					Mono.TextEditor.SelectionMode.Block);
				textEditorData.Document.CommitMultipleLineUpdate (textEditorData.MainSelection.MinLine, textEditorData.MainSelection.MaxLine);
			} else {
				int anchorOffset = textEditorData.MainSelection.GetAnchorOffset (textEditorData);
				int leadOffset = textEditorData.MainSelection.GetLeadOffset (textEditorData);
				if (leadOffset < anchorOffset) {
					int tmp = anchorOffset;
					anchorOffset = leadOffset;
					leadOffset = tmp;
				}
				textEditorData.Insert (anchorOffset, start);
				textEditorData.Insert (leadOffset >= anchorOffset ? leadOffset + start.Length : leadOffset, end);
				textEditorData.SetSelection (anchorOffset + start.Length, leadOffset + start.Length);
			}
		}
	}
}

