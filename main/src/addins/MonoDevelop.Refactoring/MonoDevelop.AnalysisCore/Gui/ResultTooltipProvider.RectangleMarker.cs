//
// ResultTooltipProvider.RectangleMarker.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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

#if WIN32
// Mono.Cairo.dll on Windows doesn't have Context.SetSourceColor() so we have to emulate it
// using an extension method so that MonoDevelop can build on Windows.
using MonoDevelop.Components;
#endif

using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.AnalysisCore.Gui
{
	partial class ResultTooltipProvider
	{
		class RectangleMarker : Gtk.DrawingArea
		{
			public RectangleMarker ()
			{
				WidthRequest = 16;
			}

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					const int triangleWidth = 8;
					const int triangleHeight = 4;

					cr.SetSourceColor (SyntaxHighlightingService.GetColor (DefaultSourceEditorOptions.Instance.GetEditorTheme (), EditorThemeColors.LineNumbers));
					var topPosition = Allocation.Height / 2 - triangleHeight / 2;

					cr.MoveTo (Allocation.Width / 2 + triangleWidth / 2, topPosition);
					cr.LineTo (Allocation.Width / 2 - triangleWidth / 2, topPosition);
					cr.LineTo (Allocation.Width / 2, topPosition + triangleHeight);
					cr.LineTo (Allocation.Width / 2 + triangleWidth / 2, topPosition);
					cr.Fill ();
				}
				return true;
			}
		}
	}
}