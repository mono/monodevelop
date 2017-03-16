//
// ShowTextMateScopesHandler.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Components.Commands;
using System.Threading;
using MonoDevelop.Components;
using System.Text;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.Editor.TextMate
{
	class ShowTextMateScopesHandler : CommandHandler
	{
		protected override void Run ()
		{
			var editor = IdeApp.Workbench.ActiveDocument?.Editor;
			if (editor == null)
				return;


			var scopeStack = editor.GetScopeStackAsync (editor.CaretOffset, CancellationToken.None).WaitAndGetResult (CancellationToken.None);

			var sb = new StringBuilder ();
			sb.AppendLine ();
			foreach (var scope in scopeStack.Reverse ()) {
				sb.AppendLine (Ambience.EscapeText (scope));
			}


			var window = new TooltipPopoverWindow ();
			window.Markup = "<span size=\"larger\" font='" + FontService.MonospaceFontName + "'>" + sb.ToString () + "</span>";
			editor.ShowTooltipWindow (window);
		}
	}
}
