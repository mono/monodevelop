// 
// FoldActions.cs
// 
// Author:
//   Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
// Copyright (C) 2009 Levi Bard
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	/// <summary>
	/// Actions for manipulating folds in a document
	/// </summary>
	class FoldActions
	{
		/// <summary>
		/// Gets the outermost closed fold pertaining to the current caret position
		/// </summary>
		static FoldSegment GetOutermostClosedFold (TextEditorData data)
		{
			FoldSegment currentFold = null;
			int endOffset = -1, startOffset = int.MaxValue;
			IEnumerable<FoldSegment> folds = data.Document.GetFoldingContaining (data.Caret.Line);
			int lineNumber = data.LogicalToVisualLocation (data.Caret.Location).Line;
			if (null != folds) {
				foreach (FoldSegment fold in folds) {
					if (fold.IsCollapsed && data.LogicalToVisualLine (data.OffsetToLineNumber (fold.Offset)) == lineNumber && 
					    fold.Offset <= startOffset && fold.EndOffset >= endOffset) {
						currentFold = fold;
						startOffset = fold.Offset;
						endOffset = fold.EndOffset;
					}
				}
			}
			
			return currentFold;
		}
		
		/// <summary>
		/// Gets the innermost opened fold containing the current caret position
		/// </summary>
		static FoldSegment GetInnermostOpenedFold (TextEditorData data)
		{
			FoldSegment currentFold = null;
			int  endOffset = int.MaxValue,
			     startOffset = -1;
			IEnumerable<FoldSegment> folds = data.Document.GetFoldingContaining (data.Caret.Line);
			
			if (null != folds) {
				foreach (FoldSegment fold in folds) {
					if (!fold.IsCollapsed && 
					    fold.Offset >= startOffset && fold.EndOffset <= endOffset) {
						currentFold = fold;
						startOffset = fold.Offset;
						endOffset = fold.EndOffset;
					}
				}
			}
			
			return currentFold;
		}
		
		static void Commit (TextEditorData data)
		{
			data.Document.RequestUpdate (new UpdateAll ());
			data.Caret.MoveCaretBeforeFoldings ();
			data.Document.CommitDocumentUpdate ();
			data.RaiseUpdateAdjustmentsRequested ();
		}
	
		/// <summary>
		/// Opens the current fold
		/// </summary>
		public static void OpenFold (TextEditorData data)
		{
			FoldSegment currentFold = GetOutermostClosedFold (data);
			
			if (null != currentFold) {
				currentFold.IsCollapsed = false;
                data.Document.InformFoldChanged(new FoldSegmentEventArgs(currentFold));
                Commit(data);
			}
		}
		
		/// <summary>
		/// Closes the current fold
		/// </summary>
		public static void CloseFold (TextEditorData data)
		{
			FoldSegment currentFold = GetInnermostOpenedFold (data);
			
			if (null != currentFold) {
				currentFold.IsCollapsed = true;
                data.Document.InformFoldChanged(new FoldSegmentEventArgs(currentFold));
                Commit(data);
			}
		}
		
		/// <summary>
		/// If the caret is on a closed fold, opens it; 
		/// else closes the current fold.
		/// </summary>
		public static void ToggleFold (TextEditorData data)
		{
			FoldSegment currentFold = GetOutermostClosedFold (data);
			
			if (null == currentFold) { 
				CloseFold (data);
			} else {
				OpenFold (data);
			}
		}
		
		/// <summary>
		/// Opens the current fold and all its children
		/// </summary>
		public static void OpenFoldRecursive (TextEditorData data)
		{
			FoldSegment currentFold = GetOutermostClosedFold (data);
			
			if (null != currentFold) {
				foreach (FoldSegment fold in data.Document.FoldSegments) {
					if (fold.Offset >= currentFold.Offset && 
					    fold.Offset <= currentFold.EndOffset) {
						fold.IsCollapsed = false;
                        data.Document.InformFoldChanged(new FoldSegmentEventArgs(currentFold));
                    }
                }
				Commit (data);
			}
		}
		
		/// <summary>
		/// Closes the current fold and all its parents
		/// </summary>
		public static void CloseFoldRecursive (TextEditorData data)
		{
			IEnumerable<FoldSegment> folds = data.Document.GetFoldingsFromOffset (data.Caret.Offset);
			
			if (null != folds) {
				foreach (FoldSegment fold in folds) {
					fold.IsCollapsed = true;
                    data.Document.InformFoldChanged(new FoldSegmentEventArgs(fold));
                }
                Commit (data);
			}
		}
		
		/// <summary>
		/// If the caret is on a closed fold, opens it and all its children; 
		/// else closes the current fold and all its parents.
		/// </summary>
		public static void ToggleFoldRecursive (TextEditorData data)
		{
			FoldSegment currentFold = GetOutermostClosedFold (data);
			
			if (null == currentFold) { 
				CloseFoldRecursive (data);
			} else {
				OpenFoldRecursive (data);
			}
		}
		
		/// <summary>
		/// If one fold is closed call OpenAllFolds, otherwise CloseAllFolds
		/// </summary>
		public static void ToggleAllFolds (TextEditorData data)
		{
			if (data.Document.FoldSegments.Any (s => s.IsCollapsed)) {
				OpenAllFolds (data);
			} else {
				CloseAllFolds (data);
			}
			
		}
		
		/// <summary>
		/// Opens all folds in the current document.
		/// </summary>
		public static void OpenAllFolds (TextEditorData data)
		{
			IEnumerable<FoldSegment> folds = data.Document.FoldSegments;
			if (null != folds) {
				foreach (FoldSegment fold in folds) {
					fold.IsCollapsed = false;
                    data.Document.InformFoldChanged(new FoldSegmentEventArgs(fold));
                }
                Commit (data);
			}
		}
		
		/// <summary>
		/// Closes all folds in the current document.
		/// </summary>
		public static void CloseAllFolds (TextEditorData data)
		{
			IEnumerable<FoldSegment> folds = data.Document.FoldSegments;
			if (null != folds) {
				foreach (FoldSegment fold in folds) {
					fold.IsCollapsed = true;
                    data.Document.InformFoldChanged(new FoldSegmentEventArgs(fold));
                }
				Commit (data);
			}
		}
	}
}
