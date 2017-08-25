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
	/// <summary>
	/// Operation icon corresponding to the Run toolbar button.
	/// </summary>
	public enum OperationIcon {
		Run,
		Build,
		Stop
	}

	/// <summary>
	/// Event arguments which specify if the event succeeded in at least one handler.
	/// </summary>
	public class HandledEventArgs : EventArgs
	{
		/// <summary>
		/// Accumulator for the Handled value.
		/// </summary>
		/// <value><c>true</c> if handled by at least one handler; otherwise, <c>false</c>.</value>
		public bool Handled { get; set; }
	}

	/// <summary>
	/// Interface which specificies the minimum working base of a Run Button.
	/// </summary>
	public interface IRunButtonView
	{
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
	}

	/// <summary>
	/// Interface which specificies the minimum working base of a Configuration/Platform Selector.
	/// </summary>
	public interface ISelectorView
	{
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
		/// Gets the active run configuration.
		/// </summary>
		/// <value>The active run configuration.</value>
		IRunConfigurationModel ActiveRunConfiguration { get; set; }

		/// <summary>
		/// Gets the active runtime.
		/// </summary>
		/// <value>The active runtime.</value>
		IRuntimeModel ActiveRuntime { get; set; }

		/// <summary>
		/// Gets or sets the Configuration model which contains all the usable configurations.
		/// </summary>
		/// <value>The configuration model.</value>
		IEnumerable<IConfigurationModel> ConfigurationModel { get; set; }

		/// <summary>
		/// Gets or sets the Run Configuration model which contains all the usable run configurations.
		/// </summary>
		/// <value>The run configuration model.</value>
		IEnumerable<IRunConfigurationModel> RunConfigurationModel { get; set; }

		/// <summary>
		/// Gets or sets the Runtime model which contains all usable runtimes.
		/// </summary>
		/// <value>The runtime model.</value>
		IEnumerable<IRuntimeModel> RuntimeModel { get; set; }

		/// <summary>
		/// Gets or sets the run configuration selector is visible.
		/// </summary>
		/// <value>The run configuration visible.</value>
		bool RunConfigurationVisible { get; set; }

		/// <summary>
		/// Occurs when the configuration changed.
		/// </summary>
		event EventHandler ConfigurationChanged;

		/// <summary>
		/// Occurs when the run configuration changed.
		/// </summary>
		event EventHandler RunConfigurationChanged;

		/// <summary>
		/// Occurs when the runtime changed.
		/// </summary>
		event EventHandler<HandledEventArgs> RuntimeChanged;
	}

	/// <summary>
	/// Interface which specificies the minimum working base of a Search Bar.
	/// </summary>
	public interface ISearchEntryView
	{
		/// <summary>
		/// Sets a value indicating whether the search entry is interactible.
		/// </summary>
		/// <value><c>true</c> if search entry is interactible; otherwise, <c>false</c>.</value>
		bool SearchSensivitity { set; }

		/// <summary>
		/// Sets the search menu items.
		/// </summary>
		/// <value>The search menu items.</value>
		IEnumerable<ISearchMenuModel> SearchMenuItems { set; }

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
	}

	/// <summary>
	/// Interface which specificies the minimum working base of a Status Bar.
	/// </summary>
	public interface IStatusBarView
	{
		/// <summary>
		/// Gets the native status bar.
		/// </summary>
		/// <value>The native status bar.</value>
		StatusBar StatusBar { get; }
	}

	/// <summary>
	/// Interface which specificies the minimum working base of a Button Bar.
	/// </summary>
	public interface IButtonBarView
	{
		/// <summary>
		/// Rebuilds the toolbar.
		/// </summary>
		/// <param name="groups">A list of ButtonBarGroups.</param>
		void RebuildToolbar (IEnumerable<ButtonBarGroup> groups);

		/// <summary>
		/// Sets a value indicating whether the button bar is interactible.
		/// </summary>
		/// <value><c>true</c> if button bar is interactible; otherwise, <c>false</c>.</value>
		bool ButtonBarSensitivity { set; }
	}

	/// <summary>
	/// Interface which specificies the minimum working base of a MainToolbar.
	/// </summary>
	public interface IMainToolbarView : IRunButtonView, ISelectorView, ISearchEntryView, IStatusBarView, IButtonBarView
	{
	}
}

