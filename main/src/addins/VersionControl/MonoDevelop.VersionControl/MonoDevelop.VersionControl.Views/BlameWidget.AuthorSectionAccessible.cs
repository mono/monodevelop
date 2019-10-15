//
// BlameWidget.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.VersionControl.Views
{
	partial class BlameWidget
	{
		class AuthorSectionAccessible : IDisposable
		{
			public AccessibilityElementProxy Accessible { get; private set; }

			public AuthorSectionAccessible (BlameRenderer widget, int line, Annotation ann, double y1, double y2)
			{
				if (widget is null)
					throw new ArgumentNullException (nameof (widget));
				if (ann is null)
					throw new ArgumentNullException (nameof (ann));

				Accessible = AccessibilityElementProxy.ButtonElementProxy ();
				Accessible.GtkParent = widget;

				Accessible.SetRole (AtkCocoa.Roles.AXButton, GettextCatalog.GetString ("Blame annotation"));

				string msg = widget.GetCommitMessage (line, false);

				Accessible.Label = GettextCatalog.GetString ("Author {0} Date {1} Revision {2} Message {3}", ann.Author, ann.Date, widget.TruncRevision (ann.Text), msg);

				int y = (int)y1;
				int h = (int)(y2 - y1);
				Accessible.FrameInGtkParent = new Rectangle (0, y, widget.Allocation.Width, h);
				var cocoaY = widget.Allocation.Height - y - h;
				Accessible.FrameInParent = new Rectangle (0, cocoaY, widget.Allocation.Width, h);
				Console.WriteLine (y1 +"/"+ h +"/"+ widget.Allocation.Width + ":"+ ann.Text);
			}

			public void Dispose ()
			{
				if (Accessible == null)
					return;
				Accessible = null;
			}
		}
	}
}
