//
// IInfoBarHost.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
namespace MonoDevelop.Ide.Gui.Components
{
	interface IInfoBarHost
	{
		void AddInfoBar (InfoBarOptions options);
	}

	public class InfoBarOptions
	{
		public string Description { get; }
		public InfoBarItem[] Items { get; set; }

		public InfoBarOptions (string description)
		{
			Description = description ?? throw new ArgumentNullException (nameof (description));
		}
	}

	public readonly struct InfoBarItem
	{
		public readonly string Title;
		public readonly InfoBarItemKind Kind;
		public readonly Action Action;
		public readonly bool CloseAfter;

		public bool IsDefault => Title == null;

		public InfoBarItem (string title, InfoBarItemKind kind, Action action, bool closeAfter)
		{
			Title = title ?? throw new ArgumentNullException (nameof (title));
			Kind = kind;
			Action = action;
			CloseAfter = closeAfter;
		}
	}

	public enum InfoBarItemKind
	{
		Button,
		Hyperlink,
		Close
	}
}
