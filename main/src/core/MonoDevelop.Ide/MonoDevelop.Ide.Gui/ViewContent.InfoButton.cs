//
// ViewContent.InfoBar.cs
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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	public abstract partial class ViewContent
	{
		public sealed class InfoButton
		{
			public string Icon { get; }
			public string Text { get; }
			public System.Action ClickAction { get; }
			public bool AutoHideInfoArea { get; set; } = true;

			public InfoButton (string icon, string text, System.Action clickAction)
			{
				Icon = icon;
				Text = text;
				ClickAction = clickAction;
			}

			public InfoButton (string text, System.Action clickAction)
			{
				Text = text;
				ClickAction = clickAction;
			}
		}
	}
}
