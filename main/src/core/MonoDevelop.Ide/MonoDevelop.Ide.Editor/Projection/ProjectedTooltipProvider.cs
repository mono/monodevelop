//
// ProjectedTooltipProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Editor.Projection
{
	sealed class ProjectedTooltipProvider  : TooltipProvider
	{
		readonly Projection projection;
		readonly TooltipProvider projectedTooltipProvider;

		public ProjectedTooltipProvider (Projection projection, TooltipProvider projectedTooltipProvider)
		{
			if (projection == null)
				throw new ArgumentNullException ("projection");
			if (projectedTooltipProvider == null)
				throw new ArgumentNullException ("projectedTooltipProvider");
			this.projectedTooltipProvider = projectedTooltipProvider;
			this.projection = projection;
		}

		public override async Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default(CancellationToken))
		{
			foreach (var pseg in projection.ProjectedSegments) {
				if (pseg.ContainsOriginal (offset)) {
					var result = await projectedTooltipProvider.GetItem (projection.ProjectedEditor, projection.ProjectedContext, pseg.FromOriginalToProjected (offset));
					if (result == null)
						return null;
					result.Offset = pseg.FromProjectedToOriginal (result.Offset);
					return result;
				}
			}
			return null;
		}

		public override bool IsInteractive (TextEditor editor, Window tipWindow)
		{
			return projectedTooltipProvider.IsInteractive (editor, tipWindow);
		}

		public override void ShowTooltipWindow (TextEditor editor, Window tipWindow, TooltipItem item, Xwt.ModifierKeys modifierState, int mouseX, int mouseY)
		{
			projectedTooltipProvider.ShowTooltipWindow (editor, tipWindow, item, modifierState, mouseX, mouseY);
		}

		public override void GetRequiredPosition (TextEditor editor, Window tipWindow, out int requiredWidth, out double xalign)
		{
			projectedTooltipProvider.GetRequiredPosition (editor, tipWindow, out requiredWidth, out xalign);
		}

		public override Window CreateTooltipWindow (TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
		{
			foreach (var pseg in projection.ProjectedSegments) {
				if (pseg.ContainsOriginal (offset)) {
					return projectedTooltipProvider.CreateTooltipWindow (projection.ProjectedEditor, projection.ProjectedContext, item, pseg.FromOriginalToProjected (offset), modifierState);
				}
			}
			return null;
		}

		public override void Dispose ()
		{
			projectedTooltipProvider.Dispose ();
			base.Dispose ();
		}
	}
}

