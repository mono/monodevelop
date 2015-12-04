//
// ISelectorModel.cs
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

namespace MonoDevelop.Components.MainToolbar
{
	public interface IConfigurationModel
	{
		/// <summary>
		/// Gets the original identifier for the configuration.
		/// </summary>
		/// <value>The original identifier.</value>
		string OriginalId { get; }

		/// <summary>
		/// Gest the display string to be used inside a context menu.
		/// </summary>
		/// <value>The display string.</value>
		string DisplayString { get; }
	}

	public interface IRuntimeModel
	{
		/// <summary>
		/// Gets the items to display as submenu items.
		/// </summary>
		IEnumerable<IRuntimeModel> Children { get; }

		/// <summary>
		/// Gets whether the menu item is a separator.
		/// </summary>
		/// <value><c>true</c> if this instance is separator; otherwise, <c>false</c>.</value>
		bool IsSeparator { get; }

		/// <summary>
		/// Gets whether the menu item is indented.
		/// </summary>
		/// <value><c>true</c> if this instance is indented; otherwise, <c>false</c>.</value>
		bool IsIndented { get; }

		/// <summary>
		/// Gets whether the menu item is notable (bold text).
		/// </summary>
		/// <value><c>true</c> if notable; otherwise, <c>false</c>.</value>
		bool Notable { get; }

		/// <summary>
		/// Gets a value indicating whether this instance has a parent.
		/// </summary>
		/// <remarks>
		/// If a menu item has a parent, it means it is in this list just for easier traversal and it should not be displayed.
		/// </remarks>
		/// <value><c>true</c> if this instance has a parent; otherwise, <c>false</c>.</value>
		bool HasParent { get; }

		/// <summary>
		/// Gets the runtime combo item model.
		/// </summary>
		/// <value>The runtime combo item.</value>
		IRuntimeMutableModel GetMutableModel();
	}

	public interface IRuntimeMutableModel : IDisposable
	{
		/// <summary>
		/// Gets the display string to be used inside a context menu.
		/// </summary>
		/// <value>The display string.</value>
		string DisplayString { get; }

		/// <summary>
		/// Gets the display string to be for selected items.
		/// </summary>
		/// <value>The full display string.</value>
		string FullDisplayString { get; }

		/// <summary>
		/// Gets whether the menu item is visible.
		/// </summary>
		/// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		bool Visible { get; }

		/// <summary>
		/// Gets whether the menu item is enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		bool Enabled { get; }
	}

	public interface ISearchMenuModel
	{
		/// <summary>
		/// Notifies that the item has been activated.
		/// </summary>
		void NotifyActivated ();

		/// <summary>
		/// Gets the display string to be used in the menu.
		/// </summary>
		/// <value>The display string.</value>
		string DisplayString { get; }
	}
}

