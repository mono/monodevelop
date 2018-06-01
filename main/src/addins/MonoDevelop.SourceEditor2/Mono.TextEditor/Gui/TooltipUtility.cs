//
// TooltipUtility.cs
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

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Ide.CodeTemplates;
using Services = MonoDevelop.Projects.Services;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.Ide.TextEditing;
using System.Text;
using Mono.Addins;
using MonoDevelop.Components;
using Mono.TextEditor.Utils;
using MonoDevelop.Core.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor.Wrappers;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;

namespace Mono.TextEditor
{
	static class TooltipUtility
	{
		internal static Document GetDocument (MonoTextEditor editor)
		{
			foreach (var doc in IdeApp.Workbench.Documents) {
				var textEditor = doc.Editor;
				if (textEditor == null)
					continue;
				if (textEditor.FileName == editor.FileName)
					return doc;
			}
			return null;
		}

		internal static void ShowAndPositionTooltip (MonoTextEditor editor, Xwt.WindowFrame tipWindow, int mouseX, int mouseY, int width, double xalign)
		{
			int ox = 0, oy = 0;
			if (editor.GdkWindow != null)
				editor.GdkWindow.GetOrigin (out ox, out oy);

			width += 10;

			int x = mouseX + ox + editor.Allocation.X;
			int y = mouseY + oy + editor.Allocation.Y;
			Gdk.Rectangle geometry = editor.Screen.GetUsableMonitorGeometry (editor.Screen.GetMonitorAtPoint (x, y));

			x -= (int)((double)width * xalign);
			y += 10;

			if (x + width >= geometry.X + geometry.Width)
				x = geometry.X + geometry.Width - width;
			if (x < geometry.Left)
				x = geometry.Left;

			int h = (int)tipWindow.Height;
			if (y + h >= geometry.Y + geometry.Height)
				y = geometry.Y + geometry.Height - h;
			if (y < geometry.Top)
				y = geometry.Top;

			tipWindow.Location = new Xwt.Point (x, y);

			tipWindow.Show ();
		}
	}
}