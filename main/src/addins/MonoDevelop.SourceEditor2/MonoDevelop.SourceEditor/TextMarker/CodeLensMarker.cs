//
// CodeLensMarker.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Linq;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.SourceEditor.Wrappers;
using MonoDevelop.Components;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Editor;
using Xwt.Drawing;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.SourceEditor
{
	class CodeLensMarker : TextLineMarker, IExtendingTextLineMarker, ICodeLensMarker
	{
		public bool IsSpaceAbove => true;

		public IDocumentLine Line => LineSegment;

		public void Draw(MonoTextEditor editor, Cairo.Context g, int lineNr, Cairo.Rectangle lineArea)
		{
			double x = lineArea.X;
			double y = lineArea.Y - editor.LineHeight;
			var line = editor.GetLine (lineNr);
			if (line == null)
				return;
			var lineLayout = editor.TextViewMargin.GetLayout (line);
			if (lineLayout == null)
				return;
			var indent = line.GetIndentation (editor.Document);
			lineLayout.GetCursorPos (indent.Length, out Pango.Rectangle strong_pos, out Pango.Rectangle weak_pos);
			x += strong_pos.X / Pango.Scale.PangoScale;
			using (var layout = new Pango.Layout (editor.PangoContext)) {
				foreach (var codeLens in codeLenses) {
					var param = new GtkCodeLansDrawingParameters (Ide.IdeApp.Workbench.ActiveDocument.Editor, lineNr, new Xwt.Rectangle (lineArea.X, lineArea.Y, lineArea.Width, lineArea.Height), x, y, layout, g);
					codeLens.Draw (param);
					x += codeLens.Size.Width + 4;
				}
			}
			if (lineLayout.IsUncached)
				lineLayout.Dispose ();
		}

		public double GetLineHeight(MonoTextEditor editor) => editor.LineHeight * 2;

		#region ICodeLensMarker implementation
		readonly List<CodeLens> codeLenses = new List<CodeLens>();

		public int CodeLensCount => codeLenses.Count;

		public void AddLens(CodeLens lens)
		{
			if (lens == null)
				throw new ArgumentNullException(nameof(lens));
			codeLenses.Add(lens);
		}

		public void RemoveLens(CodeLens lens)
		{
			if (lens == null)
				throw new ArgumentNullException(nameof(lens));
			codeLenses.Remove(lens);
		}
		#endregion
	}
}
