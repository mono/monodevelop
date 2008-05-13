
using System;
using System.Reflection;
using System.Collections;
using Mono.Unix;

namespace Stetic.Editor
{
	public class IconSelectorMenu: Gtk.Menu
	{
		IProject project;
		
		public event IconEventHandler IconSelected;
		
		public IconSelectorMenu (IProject project)
		{
			this.project = project;
			
			// Stock icon selector
			IconSelectorMenuItem selStock = new IconSelectorMenuItem (new StockIconSelectorItem ());
			selStock.IconSelected += OnStockSelected;
			Insert (selStock, -1);
			
			// Project icon selector
			if (project != null && project.IconFactory.Icons.Count > 0) {
				IconSelectorMenuItem selProject = new IconSelectorMenuItem (new ProjectIconSelectorItem (project));
				selProject.IconSelected += OnStockSelected;
				Insert (selProject, -1);
			}
			
			Insert (new Gtk.SeparatorMenuItem (), -1);
			
			Gtk.MenuItem it = new Gtk.MenuItem (Catalog.GetString ("More..."));
			it.Activated += OnSetStockActionType;
			Insert (it, -1);
		}
		
		void OnStockSelected (object s, IconEventArgs args)
		{
			if (IconSelected != null)
				IconSelected (this, args);
		}
		
		void OnSetStockActionType (object ob, EventArgs args)
		{
			Stetic.Editor.SelectIconDialog dialog = new Stetic.Editor.SelectIconDialog (null, project);
			using (dialog)
			{
				if (dialog.Run () != (int) Gtk.ResponseType.Ok)
					return;
				if (IconSelected != null)
					IconSelected (this, new IconEventArgs (dialog.Icon));
			}
		}
	}
}
