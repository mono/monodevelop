// 
// ResultTooltipProvider.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor;
using System.Linq;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.AnalysisCore.Gui
{
	class ResultTooltipProvider : TooltipProvider
	{
		#region ITooltipProvider implementation 
		public override Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default (CancellationToken))
		{
			foreach (var marker in editor.GetTextSegmentMarkersAt (offset)) {
				var result = marker.Tag as Result;
				if (result != null) 
					return Task.FromResult(new TooltipItem (result, marker.Offset, marker.Length));
			}
			return Task.FromResult<TooltipItem> (null);

		}

		public override Control CreateTooltipWindow (TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
		{
			var result = item.Item as Result;

			var window = new LanguageItemWindow (CompileErrorTooltipProvider.GetExtensibleTextEditor (editor), modifierState, null, Ambience.EscapeText (result.Message), null);
			if (window.IsEmpty)
				return null;
			return window;
		}

		public override void GetRequiredPosition (TextEditor editor, Control tipWindow, out int requiredWidth, out double xalign)
		{
			var win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width / 4);
			xalign = 0.5;
		}
		#endregion


	}
}
