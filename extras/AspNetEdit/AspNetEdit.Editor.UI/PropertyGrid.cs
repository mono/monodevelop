 /* 
 * PropertyGrid.cs - PropertyGrid wrapper. Hooks into ISelectionService.
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel.Design;
using Gtk;
using System.ComponentModel;
using System.Collections;
using MonoDevelop.Components.PropertyGrid;

namespace AspNetEdit.Editor.UI
{
	public class PropertyGrid : Gtk.VBox
	{
		private ServiceContainer parentServices = null;
		private ISelectionService selectionService = null;
		private IExtenderListService extenderListService = null;
		private IComponentChangeService changeService = null;
		private ITypeDescriptorFilterService typeDescriptorFilterService = null;
		private MonoDevelop.Components.PropertyGrid.PropertyGrid grid;
		private ListStore components;
		private ComboBox combo;
		
		private bool suppressChange = false;

		public PropertyGrid (ServiceContainer parentServices)
		{
			this.parentServices = parentServices;
			
			grid = new MonoDevelop.Components.PropertyGrid.PropertyGrid ();
			this.PackEnd (grid, true, true, 0);


			components = new ListStore (typeof (string), typeof (IComponent));
			combo = new ComboBox (components);

			CellRenderer rdr = new CellRendererText ();
			combo.PackStart (rdr, true);
			combo.AddAttribute (rdr, "text", 0);

			this.PackStart (combo, false, false, 3);
			
			//for selecting nothing, i.e. deselect all
			components.AppendValues (new object[] { "", null} );

			combo.Changed += new EventHandler (combo_Changed);

			InitialiseServices();
		}

		void combo_Changed (object sender, EventArgs e)
		{
			if (suppressChange) return;
			TreeIter t;
			combo.GetActiveIter(out t);
			IComponent comp = (IComponent) components.GetValue(t, 1);

			//Tell everybody about the new selection. We'll hear about this too.
			selectionService.SetSelectedComponents ((comp == null)? null : new IComponent[] { comp });
		}
		
		// We need these services to be present, but we cache references for efficiency
		// Whenever new designer host loaded etc, must reinitialise the services
		public void InitialiseServices ()
		{
			//unregister old event handlers
			if (selectionService != null)
				selectionService.SelectionChanged -= new EventHandler(selectionService_SelectionChanged);

			//update references
			extenderListService = parentServices.GetService (typeof (IExtenderListService)) as IExtenderListService;
			selectionService = parentServices.GetService (typeof (ISelectionService)) as ISelectionService;
			changeService = parentServices.GetService (typeof (IComponentChangeService)) as IComponentChangeService;
			typeDescriptorFilterService = parentServices.GetService (typeof (ITypeDescriptorFilterService)) as ITypeDescriptorFilterService;

			//register event handlers
			if (selectionService != null)
				selectionService.SelectionChanged += new EventHandler (selectionService_SelectionChanged);
			if (changeService != null) {
				changeService.ComponentAdded += new ComponentEventHandler (changeService_ComponentAdded);
				changeService.ComponentRemoved += new ComponentEventHandler (changeService_ComponentRemoved);
				changeService.ComponentRename += new ComponentRenameEventHandler (changeService_ComponentRename);
				changeService.ComponentChanged += new ComponentChangedEventHandler (changeService_updateValues);
				/*TODO: should we also monitor these?
				changeService.ComponentAdding
				changeService.ComponentChanging
				changeService.ComponentRemoving
				*/
			}

			//get existing components for combo list
			IDesignerHost host = parentServices.GetService (typeof (IDesignerHost)) as IDesignerHost;
			if (host != null)
				foreach (IComponent comp in host.Container.Components)
					changeService_ComponentAdded(host.Container, new ComponentEventArgs (comp));
		}
		
		void changeService_updateValues (object sender, ComponentChangedEventArgs e)
		{
			grid.Refresh ();
		}

		
		void changeService_ComponentRename (object sender, ComponentRenameEventArgs e)
		{
			//We just need to rename the right component in the combobox list
			TreeIter t; 
			components.GetIterFirst (out t);

			do {
				if ((IComponent)components.GetValue (t, 1) == e.Component && (string) components.GetValue(t, 0) == e.OldName) {
					components.SetValue (t, 0, e.NewName);
					return;
				}
			} while (components.IterNext (ref t));
		}

		void changeService_ComponentRemoved (object sender, ComponentEventArgs e)
		{
			//remove component from combobox list
			//need a variable external to foreach so we can pass by ref
			TreeIter iter;
			components.GetIterFirst (out iter);

			do
			{
				if ((IComponent) components.GetValue (iter, 1) == e.Component)
				{
					components.Remove (ref iter);
					break;
				}
			}
			while (components.IterNext (ref iter));
		}

		void changeService_ComponentAdded (object sender, ComponentEventArgs e)
		{
			//simply add to the combobox list
			components.AppendValues (new object[] { e.Component.Site.Name, e.Component} );
		}

		private void selectionService_SelectionChanged (object sender, EventArgs e)
		{
			//stop combo change event from changing selection again!
			suppressChange = true;
			grid.CurrentObject = selectionService.PrimarySelection;
			
			TreeIter iter;
			components.GetIterFirst (out iter);
			
			do
			{
				if ((IComponent) components.GetValue (iter, 1) == selectionService.PrimarySelection)
				{
					combo.SetActiveIter (iter);
					break;
				}
			}
			while (components.IterNext (ref iter));		
			suppressChange = false;	
		}
	}
}
