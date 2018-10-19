//
// LazyNotebook.cs
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
using Xwt;

namespace MonoDevelop.ExtensionTools
{
	class LazyNotebook : Notebook
	{
		public void Add (Func<Widget> createWidget, string label)
		{
			var createWidgetChain = OnAdd (createWidget);
			var widget = new LazyWidget (createWidgetChain);
			Add (widget, label);
		}

		protected override void OnCurrentTabChanged (EventArgs e)
		{
			base.OnCurrentTabChanged (e);
			Xwt.Application.TimeoutInvoke (0, () => {
				Toolkit.NativeEngine.Invoke (() => {
					var tab = Tabs [CurrentTabIndex];
					var lazy = (LazyWidget)tab.Child;
					lazy.CreateContent ();
				});
				return false;
			});
		}

		protected virtual Func<Widget> OnAdd (Func<Widget> createWidget) => createWidget;

		class LazyWidget : Widget
		{
			readonly Func<Widget> createWidget;

			public LazyWidget (Func<Widget> createWidget)
			{
				this.createWidget = createWidget;
			}

			public void CreateContent ()
			{
				if (Content == null)
					Content = createWidget ();
			}
		}
	}
}
