//
// IMainToolbarView.cs
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
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	public enum OperationIcon {
		Run,
		Build,
		Stop
	}

	public class SearchMenuItem
	{
		internal SearchMenuItem (string displayString, string category)
		{
			DisplayString = displayString;
			Category = category;
		}

		/// <summary>
		/// Notifies the activated.
		/// </summary>
		public void NotifyActivated ()
		{
			if (Activated != null)
				Activated (null, null);
		}

		/// <summary>
		/// Gets or sets the display string.
		/// </summary>
		/// <value>The display string.</value>
		public string DisplayString { get; private set; }

		/// <summary>
		/// Gets or sets the category.
		/// </summary>
		/// <value>The category.</value>
		internal string Category { get; set; }

		/// <summary>
		/// Occurs when the menu item is activated.
		/// </summary>
		internal event EventHandler Activated;
	}

	public interface IMainToolbarView
	{
		#region RunButton
		/// <summary>
		/// Gets or sets a value indicating whether the run button is interactible.
		/// </summary>
		/// <value><c>true</c> if run button is interactible; otherwise, <c>false</c>.</value>
		bool RunButtonSensitivity { get; set; }

		/// <summary>
		/// Sets the run button icon type.
		/// </summary>
		/// <value>The run button icon type.</value>
		OperationIcon RunButtonIcon { set; }
		#endregion

		#region Configuration/Platform Selector
		/// <summary>
		/// Gets or sets a value indicating whether the configuration/platform selector is interactible.
		/// </summary>
		/// <value><c>true</c> if configuration/platform selector is interactible; otherwise, <c>false</c>.</value>
		bool ConfigurationPlatformSensitivity { get; set; }

		/// <summary>
		/// Occurs when run button clicked.
		/// </summary>
		event EventHandler RunButtonClicked;
		#endregion

		#region SearchEntry
		/// <summary>
		/// Gets or sets a value indicating whether the search entry is interactible.
		/// </summary>
		/// <value><c>true</c> if search entry is interactible; otherwise, <c>false</c>.</value>
		bool SearchSensivitity { set; }

		/// <summary>
		/// Sets the search menu items.
		/// </summary>
		/// <value>The search menu items.</value>
		SearchMenuItem[] SearchMenuItems { set; }

		/// <summary>
		/// Sets the search category.
		/// </summary>
		/// <value>The search category.</value>
		string SearchCategory { set; }

		void FocusSearchBar ();
		#endregion

		#region StatusBar
		/// <summary>
		/// Gets the native status bar.
		/// </summary>
		/// <value>The native status bar.</value>
		StatusBar StatusBar { get; }
		#endregion

		#region CommandBar
		void ShowCommandBar (string barId);
		void HideCommandBar (string barId);
		#endregion
	}
}

