//
// ClassOutlineTextEditorExtensionSortingPanelWidget.cs
//
// Authors:
//  Helmut Duregger <helmutduregger@gmx.at>
//
// Copyright (c) 2010 Helmut Duregger
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
//

using System;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.PropertyGrid;


namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Provides the options panel for the global document class outline sorting preferences.
	/// </summary>
	///
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension"/>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtensionSortingPanelWidget"/>

	public class ClassOutlineTextEditorExtensionSortingPanel: OptionsPanel
	{
		ClassOutlineTextEditorExtensionSortingPanelWidget widget;

		public override Gtk.Widget CreatePanelWidget ()
		{
			return widget = new ClassOutlineTextEditorExtensionSortingPanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}


	/// <summary>
	/// This widget provides a PropertyGrid tree view for the sorting properties panel.
	/// </summary>
	///
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension"/>
	/// <seealso cref="MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtensionSortingPanel"/>

	[System.ComponentModel.ToolboxItem(true)]
	public partial class ClassOutlineTextEditorExtensionSortingPanelWidget : Gtk.Bin
	{
		const string HELP_TEXT = "Here you can configure if and how the entries in the document outline should be sorted. They are first sorted by"
			+ " the sort key. If two entries have the same sort key, they will be sorted alphabetically."
			+ " Entries with lower keys sort higher up in the hierarchy. Valid keys lie in [0, 255].";

		PropertyGrid propertyGrid;
		bool         hasChanged;

		public ClassOutlineTextEditorExtensionSortingPanelWidget ()
		{
			this.Build ();

			ClassOutlineTextEditorExtensionSortingProperties properties = MonoDevelop.Core.PropertyService.Get<ClassOutlineTextEditorExtensionSortingProperties>(
				ClassOutlineTextEditorExtension.SORTING_PROPERTY,
				ClassOutlineTextEditorExtensionSortingProperties.GetDefaultInstance ());

			propertyGrid = new PropertyGrid();

			propertyGrid.ShowHelp = true;
			propertyGrid.ShowAll ();
			propertyGrid.ShowToolbar = false;
			propertyGrid.CurrentObject = properties;
			propertyGrid.SetHelp ("", GettextCatalog.GetString (HELP_TEXT));
			propertyGrid.Changed += HandleChanged;

			/*
			 * Similar to EditTemplateDialog we remove the GUI design placeholder
			 * and add the real widget.
			 */

			vbox2.Remove (scrolledwindow1);
			vbox2.PackEnd (propertyGrid, true, true, 0);

			hasChanged = false;
		}

		/// <summary>
		/// Remembers any property changes for later calls to Store().
		/// </summary>

		public void HandleChanged (object sender, EventArgs e)
		{
			hasChanged = true;
		}

		/// <summary>
		/// Fires the MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtension.EventSortingPropertiesChanged event
		/// if the properties have changed, e.g. the HandleChanged event listener was invoked.
		/// </summary>

		public void Store ()
		{
			if (hasChanged) {

				/*
				 * Notifiy listeners on properties changes, this includes the
				 * ClassOutlineTextEditorExtension
				 */

				ClassOutlineTextEditorExtension.OnSortingPropertiesChanged (this, EventArgs.Empty);
			}

			propertyGrid.Changed -= HandleChanged;
		}
	}
}

