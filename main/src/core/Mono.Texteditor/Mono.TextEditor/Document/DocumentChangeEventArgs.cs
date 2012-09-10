// ReplaceEventArgs.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Text;
using ICSharpCode.NRefactory.Editor;

namespace Mono.TextEditor
{
	public class DocumentChangeEventArgs : ICSharpCode.NRefactory.Editor.TextChangeEventArgs
	{
		public int ChangeDelta {
			get {
				return InsertionLength - RemovalLength;
			}
		}

		public AnchorMovementType AnchorMovementType {
			get;
			private set;
		}

		public DocumentChangeEventArgs (int offset, string removedText, string insertedText, AnchorMovementType anchorMovementType = AnchorMovementType.Default) : base (offset, removedText, insertedText)
		{
			AnchorMovementType = anchorMovementType;
		}

		public override string ToString ()
		{
			return string.Format ("[ReplaceEventArgs: Offset={0}, RemovedText={1}, RemovalLength={2}, InsertedText={3}, InsertionLength={4}]", Offset, RemovedText != null ? RemovedText.Text : "null", RemovalLength, InsertedText != null ? InsertedText.Text : "null", InsertionLength);
		}
	}
}
