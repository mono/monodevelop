//
// GridOptionPreviewControl.cs
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
using Xwt;
using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using MonoDevelop.Refactoring.Options;
using MonoDevelop.Ide.TypeSystem;
using System.Composition.Hosting;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Refactoring.Options
{
	class AbstractOptionPageControl : VBox
	{
		internal readonly IOptionService OptionService;

		public AbstractOptionPageControl (IServiceProvider serviceProvider)
		{
			this.OptionService = TypeSystemService.Workspace.Services.GetService<IOptionService> ();
		}
	}

	class GridOptionPreviewControl : AbstractOptionPageControl
	{
		readonly AbstractOptionPreviewViewModel viewModel;

		DataField<AbstractCodeStyleOptionViewModel> itemField = new DataField<AbstractCodeStyleOptionViewModel> ();
		DataField<string> descriptionField  = new DataField<string> ();
		DataField<string> propertyNameField = new DataField<string> ();
		DataField<ItemCollection> propertiesField = new DataField<ItemCollection> ();

		DataField<string> notificationPreferenceField     = new DataField<string> ();
		DataField<ItemCollection> notificationPreferencesField = new DataField<ItemCollection> ();

		TreeStore store;
		Xwt.TreeView treeView;

		public GridOptionPreviewControl (IServiceProvider serviceProvider, Func<OptionSet, IServiceProvider, AbstractOptionPreviewViewModel> createViewModel) : base (serviceProvider)
		{
			viewModel = createViewModel (TypeSystemService.Workspace.Options, serviceProvider);
			CreateView ();
			var firstItem = this.viewModel.CodeStyleItems.OfType<AbstractCodeStyleOptionViewModel> ().First ();
			this.viewModel.SetOptionAndUpdatePreview ((firstItem.SelectedPreference ?? firstItem.Preferences[0]).IsChecked, firstItem.Option, firstItem.GetPreview ());
		}

		void CreateView ()
		{
			store = new TreeStore (itemField, descriptionField, propertyNameField, propertiesField, notificationPreferenceField, notificationPreferencesField);

			treeView = new TreeView (store);
			treeView.SelectionChanged += (object sender, EventArgs e) => {
				if (treeView.SelectedRow != null) {
					var item = store.GetNavigatorAt (treeView.SelectedRow)?.GetValue (itemField) as AbstractCodeStyleOptionViewModel;
					if (item != null) {
						this.viewModel.SetOptionAndUpdatePreview (item.SelectedPreference.IsChecked, item.Option, item.GetPreview ());
						return;
					}
				}
				return;

			};
			var col = treeView.Columns.Add (GettextCatalog.GetString ("Description"), descriptionField);
			col.Expands = true;

			var propertyCellView = new ComboBoxCellView (propertyNameField);
			propertyCellView.Editable = true;
			propertyCellView.ItemsField = propertiesField;
			propertyCellView.SelectionChanged += delegate (object sender, WidgetEventArgs e) {
				var treeNavigator = store.GetNavigatorAt (treeView.CurrentEventRow);
				if (treeNavigator == null)
					return;
				var item = treeNavigator.GetValue (itemField);
				if (item == null)
					return;
				var text2 = treeNavigator.GetValue (propertyNameField);

				GLib.Timeout.Add (10, delegate {
					var text = treeNavigator.GetValue (propertyNameField);
					foreach (var pref in item.Preferences) {
						if (pref.Name == text) {
							item.SelectedPreference = pref;
							this.viewModel.SetOptionAndUpdatePreview (pref.IsChecked, item.Option, item.GetPreview ());
						}
					}
					return false;
				});
			};
			col = new ListViewColumn (GettextCatalog.GetString ("Property"), propertyCellView);
			col.Expands = true;
			treeView.Columns.Add (col);

			var severityCellView = new ComboBoxCellView (notificationPreferenceField);
			severityCellView.Editable = true;
			severityCellView.ItemsField = notificationPreferencesField;
			col = new ListViewColumn (GettextCatalog.GetString ("Severity"), severityCellView);
			treeView.Columns.Add (col);

			this.PackStart (treeView, true, true);
			var wrappedEditor = Xwt.Toolkit.CurrentEngine.WrapWidget ((Gtk.Widget)viewModel.TextViewHost, Xwt.NativeWidgetSizing.DefaultPreferredSize);
			this.PackEnd (wrappedEditor, true, true);
			FillTreeStore ();
		}

		void FillTreeStore ()
		{
			var groupNodes = new Dictionary<string, TreeNavigator> ();
			foreach (var item in viewModel.CodeStyleItems) {
				if (!groupNodes.TryGetValue (item.GroupName, out TreeNavigator groupNode)) {
					groupNode = store.AddNode ();
					groupNode.SetValue (descriptionField, item.GroupName);
					groupNode.SetValue (itemField, null);
					groupNode.SetValue (propertyNameField, "");
					groupNode.SetValue (propertiesField, new ItemCollection ());
					groupNode.SetValue (notificationPreferenceField, "");
					groupNode.SetValue (notificationPreferencesField, new ItemCollection ());

					groupNodes [item.GroupName] = groupNode;
				}
				var childNode = groupNode.AddChild ();
				childNode.SetValue (itemField, item);
				childNode.SetValue (descriptionField, item.Description);
				childNode.SetValue (propertyNameField, item.SelectedPreference.Name);
				var itemCollection = new ItemCollection ();
				foreach (var pref in item.Preferences)
					itemCollection.Add (pref.Name);
				childNode.SetValue (propertiesField, itemCollection);

				childNode.SetValue (notificationPreferenceField, item.SelectedNotificationPreference.Name);
				itemCollection = new ItemCollection ();
				foreach (var pref in item.NotificationPreferences)
					itemCollection.Add (pref.Name);
				childNode.SetValue (notificationPreferencesField, itemCollection);
				groupNode.MoveToParent ();
			}
			treeView.ExpandAll (); 
		}

		internal void SaveSettings ()
		{
			var optionSet = this.OptionService.GetOptions ();
			var changedOptions = viewModel.ApplyChangedOptions (optionSet);

			this.OptionService.SetOptions (changedOptions);
		}
	}
}
