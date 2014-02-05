using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Gtk;
using ICSharpCode.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PackageSourcesWidget : Gtk.Bin
	{
		RegisteredPackageSourcesViewModel viewModel;
		ListStore packageSourcesStore;
		const int IsEnabledCheckBoxColumn = 0;
		const int PackageSourceDescriptionColumn = 1;
		const int PackageSourceViewModelColumn = 2;
		
		public PackageSourcesWidget (RegisteredPackageSourcesViewModel viewModel)
		{
			this.Build ();
			this.viewModel = viewModel;
			this.InitializeTreeView ();
			this.LoadPackageSources ();
			this.viewModel.PackageSourceViewModels.CollectionChanged += PackageSourceViewModelsCollectionChanged;
			UpdateEnabledButtons ();
		}
		
		void InitializeTreeView ()
		{
			packageSourcesStore = new ListStore (typeof (bool), typeof (string), typeof (PackageSourceViewModel));
			packageSourcesTreeView.Model = packageSourcesStore;
			packageSourcesTreeView.AppendColumn (CreateTreeViewColumn ());
			packageSourcesTreeView.Selection.Changed += PackageSourcesTreeViewSelectionChanged;
		}
		
		TreeViewColumn CreateTreeViewColumn ()
		{
			var column = new TreeViewColumn ();
			
			var checkBoxRenderer = new CellRendererToggle ();
			checkBoxRenderer.Toggled += PackageSourceCheckBoxToggled;
			column.PackStart (checkBoxRenderer, false);
			column.AddAttribute (checkBoxRenderer, "active", IsEnabledCheckBoxColumn);
			
			var textRenderer = new CellRendererText ();
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "markup", PackageSourceDescriptionColumn);
			
			return column;
		}
		
		void LoadPackageSources ()
		{
			AddPackageSourcesToTreeView (viewModel.PackageSourceViewModels);
		}
		
		void AddPackageSourcesToTreeView (IEnumerable<PackageSourceViewModel> packageSourceViewModels)
		{
			foreach (PackageSourceViewModel packageSourceViewModel in packageSourceViewModels) {
				AddPackageSourceToTreeView (packageSourceViewModel);
			}
		}
		
		void AddPackageSourceToTreeView (PackageSourceViewModel packageSourceViewModel)
		{
			packageSourcesStore.AppendValues (packageSourceViewModel.IsEnabled, GetPackageSourceDescriptionMarkup (packageSourceViewModel), packageSourceViewModel);
		}
		
		string GetPackageSourceDescriptionMarkup (PackageSourceViewModel packageSourceViewModel)
		{
			string format = "{0}\n<span foreground='blue' underline='single'>{1}</span>";
			return MarkupString.Format (format, packageSourceViewModel.Name, packageSourceViewModel.SourceUrl);
		}
		
		void PackageSourceCheckBoxToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			packageSourcesStore.GetIterFromString (out iter, args.Path);
			PackageSourceViewModel packageSourceViewModel = GetPackageSourceViewModel (iter);
			packageSourceViewModel.IsEnabled = !packageSourceViewModel.IsEnabled;
			packageSourcesStore.SetValue (iter, IsEnabledCheckBoxColumn, packageSourceViewModel.IsEnabled);
		}
		
		PackageSourceViewModel GetPackageSourceViewModel (TreeIter iter)
		{
			return packageSourcesStore.GetValue (iter, PackageSourceViewModelColumn) as PackageSourceViewModel;
		}
		
		void PackageSourcesTreeViewSelectionChanged (object sender, EventArgs e)
		{
			viewModel.SelectedPackageSourceViewModel = GetSelectedPackageSourceViewModel ();
			UpdateEnabledButtons ();
		}
		
		PackageSourceViewModel GetSelectedPackageSourceViewModel ()
		{
			TreeIter iter;
			if (packageSourcesTreeView.Selection.GetSelected (out iter)) {
				return GetPackageSourceViewModel (iter);
			}
			return null;
		}
		
		void UpdateEnabledButtons ()
		{
			this.addButton.Sensitive = viewModel.CanAddPackageSource;
			this.moveUpButton.Sensitive = viewModel.CanMovePackageSourceUp;
			this.moveDownButton.Sensitive= viewModel.CanMovePackageSourceDown;
			this.removeButton.Sensitive = viewModel.CanRemovePackageSource;
		}
		
		void PackageSourceViewModelsCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					AddNewPackageSources (e);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemovePackageSources (e);
					break;
				case NotifyCollectionChangedAction.Move:
					MovePackageSources (e);
					break;
			}
			
			UpdateEnabledButtons ();
		}
		
		void AddNewPackageSources (NotifyCollectionChangedEventArgs e)
		{
			AddPackageSourcesToTreeView (e.NewItems.OfType<PackageSourceViewModel>());
		}
		
		void RemovePackageSources (NotifyCollectionChangedEventArgs e)
		{
			foreach (PackageSourceViewModel packageSourceViewModel in e.OldItems.OfType<PackageSourceViewModel>()) {
				TreeIter iter = GetTreeIter (packageSourceViewModel);
				packageSourcesStore.Remove (ref iter);
			}
		}
		
		TreeIter GetTreeIter (PackageSourceViewModel packageSourceViewModel)
		{
			TreeIter foundIter = TreeIter.Zero;
			packageSourcesStore.Foreach ((model, path, iter) => {
				var currentViewModel = model.GetValue (iter, PackageSourceViewModelColumn) as PackageSourceViewModel;
				if (currentViewModel == packageSourceViewModel) {
					foundIter = iter;
					return true;
				}
				return false;
			});
			return foundIter;
		}
		
		void MovePackageSources (NotifyCollectionChangedEventArgs e)
		{
			TreeIter sourceIter = GetTreeIter (e.NewItems[0] as PackageSourceViewModel);
			TreeIter destinationIter = GetTreeIter (e.NewStartingIndex);
			packageSourcesStore.Swap (sourceIter, destinationIter);
		}
		
		TreeIter GetTreeIter (int index)
		{
			TreeIter foundIter = TreeIter.Zero;
			int currentIndex = 0;
			packageSourcesStore.Foreach ((model, path, iter) => {
				if (currentIndex == index) {
					foundIter = iter;
					return true;
				}
				currentIndex++;
				return false;
			});
			return foundIter;
		}
		
		void MoveUpButtonClicked (object sender, EventArgs e)
		{
			viewModel.MovePackageSourceUp ();
			UpdateEnabledButtons ();
		}
		
		void MoveDownButtonClicked (object sender, EventArgs e)
		{
			viewModel.MovePackageSourceDown ();
			UpdateEnabledButtons ();
		}
		
		void RemoveButtonClicked (object sender, EventArgs e)
		{
			viewModel.RemovePackageSource ();
			UpdateEnabledButtons ();
		}
		
		void AddButtonClicked (object sender, EventArgs e)
		{
			viewModel.AddPackageSource ();
			UpdateEnabledButtons ();
		}
		
		void BrowseButtonClicked (object sender, EventArgs e)
		{
			viewModel.BrowsePackageFolder ();
			this.packageSourceNameTextBox.Text = viewModel.NewPackageSourceName;
			this.packageSourceTextBox.Text = viewModel.NewPackageSourceUrl;
			UpdateEnabledButtons ();
		}
		
		void PackageSourceNameTextBoxChanged (object sender, EventArgs e)
		{
			viewModel.NewPackageSourceName = this.packageSourceNameTextBox.Text;
			UpdateEnabledButtons ();
		}
		
		void PackageSourceTextBoxChanged (object sender, EventArgs e)
		{
			viewModel.NewPackageSourceUrl = this.packageSourceTextBox.Text;
			UpdateEnabledButtons ();
		}
	}
}

