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
using System.Collections.Generic;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.Components.MainToolbar
{
	public enum OperationIcon {
		Run,
		Build,
		Stop
	}

	public class HandledEventArgs : EventArgs
	{
		public bool Handled { get; set; }
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

		/// <summary>
		/// Occurs when run button clicked.
		/// </summary>
		event EventHandler RunButtonClicked;
		#endregion

		#region Configuration/Platform Selector
		/// <summary>
		/// Gets or sets a value indicating whether the configuration/platform selector is interactible.
		/// </summary>
		/// <value><c>true</c> if configuration/platform selector is interactible; otherwise, <c>false</c>.</value>
		bool ConfigurationPlatformSensitivity { get; set; }

		/// <summary>
		/// Sets a value indicating whether the platform selector is interactible.
		/// </summary>
		/// <value><c>true</c> if the platform selector is interactible; otherwise, <c>false</c>.</value>
		bool PlatformSensitivity { set; }

		/// <summary>
		/// Gets the active configuration.
		/// </summary>
		/// <value>The active configuration.</value>
		IConfigurationModel ActiveConfiguration { get; set; }

		/// <summary>
		/// Gets the active runtime.
		/// </summary>
		/// <value>The active runtime.</value>
		IRuntimeModel ActiveRuntime { get; set; }

		IEnumerable<IConfigurationModel> ConfigurationModel { get; set; }
		IEnumerable<IRuntimeModel> RuntimeModel { get; set; }

		/// <summary>
		/// Occurs when the configuration changed.
		/// </summary>
		event EventHandler ConfigurationChanged;

		/// <summary>
		/// Occurs when the runtime changed.
		/// </summary>
		event EventHandler<HandledEventArgs> RuntimeChanged;
		#endregion

		#region SearchEntry
		/// <summary>
		/// Sets a value indicating whether the search entry is interactible.
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

		/// <summary>
		/// Gets or sets the search text.
		/// </summary>
		/// <value>The search text.</value>
		string SearchText { get; set; }

		/// <summary>
		/// Focuses the search entry.
		/// </summary>
		void FocusSearchBar ();

		/// <summary>
		/// Occurs when the search entry contents changed.
		/// </summary>
		event EventHandler SearchEntryChanged;

		/// <summary>
		/// Occurs when the search entry is activated.
		/// </summary>
		event EventHandler SearchEntryActivated;

		/// <summary>
		/// Occurs when a key is pressed in the search entry.
		/// </summary>
		event EventHandler<Xwt.KeyEventArgs> SearchEntryKeyPressed;

		/// <summary>
		/// Occurs when the search entry is resized.
		/// </summary>
		event EventHandler SearchEntryResized;

		/// <summary>
		/// Occurs when the search entry lost focus.
		/// </summary>
		event EventHandler SearchEntryLostFocus;

		/// <summary>
		/// Gets the UI widget for the popup window to use as an anchor.
		/// </summary>
		/// <value>The popup anchor.</value>
		Gtk.Widget PopupAnchor { get; }

		/// <summary>
		/// Sets the search entry placeholder message.
		/// </summary>
		/// <value>The placeholder message.</value>
		string SearchPlaceholderMessage { set; }
		#endregion

		#region StatusBar
		/// <summary>
		/// Gets the native status bar.
		/// </summary>
		/// <value>The native status bar.</value>
		StatusBar StatusBar { get; }
		#endregion

		#region CommandBar
		/// <summary>
		/// Rebuilds the toolbar.
		/// </summary>
		/// <param name="buttons">A list of buttons.</param>
		void RebuildToolbar (IEnumerable<IButtonBarButton> buttons);

		/// <summary>
		/// Sets a value indicating whether the button bar is interactible.
		/// </summary>
		/// <value><c>true</c> if button bar is interactible; otherwise, <c>false</c>.</value>
		bool ButtonBarSensitivity { set; }
		#endregion
	}
}

