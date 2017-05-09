//
// IButtonBarButton.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Components.MainToolbar
{
	public class ButtonBarGroup
	{
		// The title of the button group. Used for accessibility
		public string Title { get; private set; }

		// The buttons in this group
		public List<IButtonBarButton> Buttons { get; }

		public ButtonBarGroup (string title)
		{
			Title = title;
			Buttons = new List<IButtonBarButton> ();
		}
	}

	public interface IButtonBarButton
	{
		/// <summary>
		/// Gets the icon that should be used for the button.
		/// </summary>
		/// <value>The icon id.</value>
		IconId Image { get; }

		/// <summary>
		/// Gets whether the button should be enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		bool Enabled { get; }

		/// <summary>
		/// Gets whether the button should be visible.
		/// </summary>
		/// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		bool Visible { get; }

		/// <summary>
		/// Gets whether the button is a separator.
		/// </summary>
		/// <value><c>true</c> if this instance is separator; otherwise, <c>false</c>.</value>
		bool IsSeparator { get; }

		/// <summary>
		/// Gets the button tooltip.
		/// </summary>
		/// <value>The tooltip.</value>
		string Tooltip { get; }

		/// <summary>
		/// Gets the button title
		/// </summary>
		/// <value>The title.</value>
		string Title { get; }

		/// <summary>
		/// Use this when the button is clicked.
		/// </summary>
		void NotifyPushed ();

		/// <summary>
		/// Occurs when Enabled is changed.
		/// </summary>
		event EventHandler EnabledChanged;

		/// <summary>
		/// Occurs when the image is changed.
		/// </summary>
		event EventHandler ImageChanged;

		/// <summary>
		/// Occurs when Visible is changed.
		/// </summary>
		event EventHandler VisibleChanged;

		/// <summary>
		/// Occurs when the tooltip changed.
		/// </summary>
		event EventHandler TooltipChanged;

		/// Occurs when the title is changed
		event EventHandler TitleChanged;
	}
}

