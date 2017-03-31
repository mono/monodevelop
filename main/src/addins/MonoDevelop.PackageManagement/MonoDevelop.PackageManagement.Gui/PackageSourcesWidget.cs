using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Gtk;
using MonoDevelop.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AutoTest;
using System.ComponentModel;

namespace MonoDevelop.PackageManagement
{
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class PackageSourcesWidget : Gtk.Bin
	{
		RegisteredPackageSourcesViewModel viewModel;
		ListStore packageSourcesStore;
		const int IsEnabledCheckBoxColumn = 1;
		const int PackageSourceIconColumn = 2;
		const int PackageSourceViewModelColumn = 3;
		
		public PackageSourcesWidget (RegisteredPackageSourcesViewModel viewModel)
		{
			this.Build ();
			this.viewModel = viewModel;
			this.InitializeTreeView ();
			this.LoadPackageSources ();
			AddEventHandlers ();
			UpdateEnabledButtons ();
		}

		void AddEventHandlers ()
		{
			this.removeButton.Clicked += RemoveButtonClicked;
			this.addButton.Clicked += AddButtonClicked;

			this.viewModel.PackageSourceViewModels.CollectionChanged += PackageSourceViewModelsCollectionChanged;
			this.viewModel.PackageSourceChanged += PackageSourceChanged;
		}

		public override void Dispose ()
		{
			this.viewModel.PackageSourceViewModels.CollectionChanged -= PackageSourceViewModelsCollectionChanged;
			this.viewModel.PackageSourceChanged -= PackageSourceChanged;
			this.viewModel.Dispose ();
			base.Dispose ();
		}
		
		void InitializeTreeView ()
		{
			packageSourcesStore = new ListStore (typeof (object), typeof (bool), typeof (IconId), typeof (PackageSourceViewModel));
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("store__Data", "store__Selected",
				"store__IconId", "store__Model");
			TypeDescriptor.AddAttributes (packageSourcesStore, modelAttr);
			packageSourcesTreeView.Model = packageSourcesStore;
			packageSourcesTreeView.SearchColumn = -1; // disable the interactive search
			packageSourcesTreeView.AppendColumn (CreateTreeViewColumn ());
			packageSourcesTreeView.Selection.Changed += PackageSourcesTreeViewSelectionChanged;
			packageSourcesTreeView.RowActivated += PackageSourcesTreeViewRowActivated;
			packageSourcesTreeView.Reorderable = true;
			packageSourcesTreeView.DragEnd += PackageSourcesTreeViewDragEnded;
		}

		TreeViewColumn CreateTreeViewColumn ()
		{
			var column = new TreeViewColumn ();
			column.Spacing = 0;

			var dummyRenderer = new CellRendererImage ();
			dummyRenderer.Width = 1;
			dummyRenderer.Xpad = 0;
			column.PackStart (dummyRenderer, false);

			var checkBoxRenderer = new CellRendererToggle ();
			checkBoxRenderer.Toggled += PackageSourceCheckBoxToggled;
			checkBoxRenderer.Xpad = 7;
			checkBoxRenderer.Ypad = 12;
			checkBoxRenderer.Xalign = 0;
			checkBoxRenderer.Yalign = 0;
			column.PackStart (checkBoxRenderer, false);
			column.AddAttribute (checkBoxRenderer, "active", IsEnabledCheckBoxColumn);

			var iconRenderer = new CellRendererImage ();
			iconRenderer.StockSize = IconSize.Dnd;
			iconRenderer.Xalign = 0;
			iconRenderer.Xpad = 0;
			column.PackStart (iconRenderer, false);
			column.AddAttribute (iconRenderer, "icon-id", PackageSourceIconColumn);

			var packageSourceRenderer = new PackageSourceCellRenderer ();
			packageSourceRenderer.Mode = CellRendererMode.Activatable;
			column.PackStart (packageSourceRenderer, true);
			column.AddAttribute (packageSourceRenderer, "package-source", PackageSourceViewModelColumn);

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
			packageSourcesStore.AppendValues (
				null,
				packageSourceViewModel.IsEnabled,
				new IconId ("md-nuget-package-source"),
				packageSourceViewModel);
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
		
		void RemoveButtonClicked (object sender, EventArgs e)
		{
			viewModel.RemovePackageSource ();
			UpdateEnabledButtons ();
		}
		
		void AddButtonClicked (object sender, EventArgs e)
		{
			viewModel.IsEditingSelectedPackageSource = false;
			using (var dialog = new AddPackageSourceDialog (viewModel)) {
				if (ShowDialogWithParent (dialog) == Xwt.Command.Ok) {
					UpdateSelectedPackageSource ();
				}
				UpdateEnabledButtons ();
			}
		}

		Xwt.Command ShowDialogWithParent (AddPackageSourceDialog dialog)
		{
			Xwt.WindowFrame parent = Xwt.Toolkit.CurrentEngine.WrapWindow (Toplevel);
			return dialog.Run (parent);
		}

		void UpdateSelectedPackageSource ()
		{
			TreeIter iter = GetTreeIter (viewModel.SelectedPackageSourceViewModel);
			if (iter.Equals (TreeIter.Zero)) {
				packageSourcesTreeView.Selection.UnselectAll ();
			} else {
				packageSourcesTreeView.Selection.SelectIter (iter);
			}
		}

		void PackageSourcesTreeViewRowActivated (object o, RowActivatedArgs args)
		{
			viewModel.IsEditingSelectedPackageSource = true;
			using (var dialog = new AddPackageSourceDialog (viewModel)) {
				ShowDialogWithParent (dialog);
				UpdateEnabledButtons ();
			}
		}

		void PackageSourcesTreeViewDragEnded (object o, DragEndArgs args)
		{
			HasPackageSourcesOrderChanged = true;
		}

		public bool HasPackageSourcesOrderChanged { get; private set; }

		public IEnumerable<PackageSourceViewModel> GetOrderedPackageSources ()
		{
			var packageSourceViewModels = new List<PackageSourceViewModel> ();
			packageSourcesStore.Foreach ((model, path, iter) => {
				var currentViewModel = model.GetValue (iter, PackageSourceViewModelColumn) as PackageSourceViewModel;
				packageSourceViewModels.Add (currentViewModel);
				return false;
			});

			return packageSourceViewModels;
		}

		void PackageSourceChanged (object sender, PackageSourceViewModelChangedEventArgs e)
		{
			TreeIter iter = GetTreeIter (e.PackageSource);
			packageSourcesStore.SetValue (
				iter,
				PackageSourceViewModelColumn,
				e.PackageSource);
		}
	}
}

