// CompileErrorTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.SourceEditor
{
	class CompileErrorTooltipProvider: TooltipInformationTooltipProvider
	{
		internal static ExtensibleTextEditor GetExtensibleTextEditor (TextEditor editor)
		{
			var view = editor.GetContent<SourceEditorView> ();
			if (view == null)
				return null;
			return view.TextEditor;
		}

		#region ITooltipProvider implementation 
		public override Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default(CancellationToken))
		{
			var ed = GetExtensibleTextEditor (editor);
			if (ed == null)
				return Task.FromResult<TooltipItem> (null);

			var errorInformation = GetErrorInformationAt (ed, offset);
			if (string.IsNullOrEmpty (errorInformation.info))
				return Task.FromResult<TooltipItem> (null);
			var sb = StringBuilderCache.Allocate ();
			FontService.AppendSmallSansFontMarkup (sb);
			sb.Append (errorInformation.info);
			sb.Append ("</span>");

			return Task.FromResult (new TooltipItem (StringBuilderCache.ReturnAndFree (sb), editor.GetLineByOffset (offset)));
		}
		#endregion 

		internal static (string info, int start, int end) GetErrorInformationAt (ExtensibleTextEditor editor,  int offset)
		{
			var location = editor.OffsetToLocation (offset);
			var line = editor.Document.GetLine (location.Line);
			if (line == null)
				return (null, -1, -1);

			StringBuilder sb = null;
			int start = -1;
			int end = int.MaxValue;
			foreach (var e in editor.Document.GetTextSegmentMarkersAt (offset)) {
				var error = e as ErrorMarker;
				if (error == null)
					continue;
				if (sb != null)
					sb.AppendLine ();
				else
					sb = StringBuilderCache.Allocate ();
				start = Math.Max (start, error.Offset);
				end = Math.Min (end, error.EndOffset);

				sb.Append ("<b>");
				if (error.Error.ErrorType == MonoDevelop.Ide.TypeSystem.ErrorType.Warning)
					sb.Append (GettextCatalog.GetString ("Warning"));
				else
					sb.Append (GettextCatalog.GetString ("Error"));
				sb.Append ("</b>: ");
				sb.Append (GLib.Markup.EscapeText (error.Error.Message));
			}

			return (sb != null ? StringBuilderCache.ReturnAndFree (sb) : null, start, end - start);
		}

	}
}
