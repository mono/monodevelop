// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
//using Reflector.UserInterface;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Pads
{/*
	class IDEContainer : Container
	{
		class IDESite : ISite
		{
			private string name = "";
			private IComponent component;
			private IDEContainer container;

			public IDESite(IComponent sitedComponent, IDEContainer site, string aName)
			{
				component = sitedComponent;
				container = site;
				name = aName;
			}

			public IComponent Component{
				get{ return component;}
			}
			public IContainer Container{
				get{return container;}
			}

			public bool DesignMode{
				get{return false;}
			}

			public string Name {
				get{ return name;}
				set{name=value;}
			}

			public object GetService(Type serviceType)
			{
//				Runtime.LoggingService.Info("REQUEST SERVICE : " + serviceType);
				object o = container.GetService(serviceType);
				
				return o;
			}
		}

		public IDEContainer (IServiceProvider sp)
		{
			serviceProvider = sp;
		}

		protected override object GetService(Type serviceType)
		{
//			Runtime.LoggingService.Info("service {0} requested from IDEContainer", serviceType);
			object service = base.GetService(serviceType);
			if (service == null) {
				service = serviceProvider.GetService(serviceType);
			}
//			Runtime.LoggingService.Info("Returned  : " + service);
			return service;
		}

		public ISite CreateSite(IComponent component)
		{
			return CreateSite(component, "UNKNOWN_SITE");
		}
		
		protected override ISite CreateSite(IComponent component,string name)
		{
			ISite site = base.CreateSite(component,name);
			if (site == null) {
			}
			return new IDESite(component,this,name);
		}
		
		private IServiceProvider serviceProvider;
	}
	
	public class PropertyPad : AbstractPadContent
	{
		static Panel         panel   = null;
		static ComboBox      comboBox = null;
		static PropertyGrid  grid = null;
		static IDesignerHost host = null;
		
		public static PropertyGrid Grid {
			get {
				return grid;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return null; // FIXME
			}
		}
		
		public static event PropertyValueChangedEventHandler PropertyValueChanged;
		public static event EventHandler                     SelectedObjectChanged;
		
		public PropertyPad() : base("${res:MainWindow.Windows.PropertiesScoutLabel}", "Icons.16x16.PropertiesIcon")
		{
			panel = new Panel();
			PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));			
			
			comboBox = new ComboBox();
			comboBox.Dock = DockStyle.Top;
			comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			comboBox.DrawMode = DrawMode.OwnerDrawFixed;
			comboBox.DrawItem += new DrawItemEventHandler(ComboBoxDrawItem);
			comboBox.MeasureItem += new MeasureItemEventHandler(ComboBoxMeasureItem);
			comboBox.SelectedIndexChanged += new EventHandler(ComboBoxSelectedIndexChanged);
			
			grid = new PropertyGrid();
			grid.PropertyValueChanged += new PropertyValueChangedEventHandler(PropertyChanged);
			grid.PropertySort = propertyService.GetProperty("FormsDesigner.DesignerOptions.PropertyGridSortAlphabetical", false) ? PropertySort.Alphabetical : PropertySort.Categorized;
			grid.Dock = DockStyle.Fill;
			panel.Controls.Add(grid);
			panel.Controls.Add(comboBox);
			
			IProjectService projectService = (IProjectService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			projectService.CombineClosed += new CombineEventHandler(CombineClosedEvent);
			
			MenuService menuService = (MenuService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(MenuService));
			//grid.ContextMenu = menuService.CreateContextMenu(this, "/SharpDevelop/Views/PropertyPad/ContextMenu");
		}
		
		void CombineClosedEvent(object sender, CombineEventArgs e)
		{
			SetDesignableObjects(null);
		}
		
		void ComboBoxMeasureItem(object sender, MeasureItemEventArgs mea)
		{
			if (mea.Index < 0 || mea.Index >= comboBox.Items.Count) {
				mea.ItemHeight = comboBox.Font.Height;
				return;
			}
			object item = comboBox.Items[mea.Index];
			SizeF size = mea.Graphics.MeasureString(item.GetType().ToString(), comboBox.Font);
			
			mea.ItemHeight = (int)size.Height;
			mea.ItemWidth  = (int)size.Width;
			
			if (item is IComponent) {
				ISite site = ((IComponent)item).Site;
				if (site != null) {	
					string name = site.Name;
					Font f = new Font(comboBox.Font, FontStyle.Bold);
					mea.ItemWidth += (int)mea.Graphics.MeasureString(name + "-", f).Width;
				}
			}
		}
		
		void ComboBoxDrawItem(object sender, DrawItemEventArgs dea)
		{
			if (dea.Index < 0 || dea.Index >= comboBox.Items.Count) {
				return;
			}
			Graphics g = dea.Graphics;
			Brush stringColor = SystemBrushes.ControlText;
			
			if ((dea.State & DrawItemState.Selected) == DrawItemState.Selected) {
				if ((dea.State & DrawItemState.Focus) == DrawItemState.Focus) {
					g.FillRectangle(SystemBrushes.Highlight, dea.Bounds);
					stringColor = SystemBrushes.HighlightText;
				} else {
					g.FillRectangle(SystemBrushes.Window, dea.Bounds);
				}
			} else {
				g.FillRectangle(SystemBrushes.Window, dea.Bounds);
			}
			
			object item = comboBox.Items[dea.Index];
			int   xPos  = dea.Bounds.X;
			
			if (item is IComponent) {
				ISite site = ((IComponent)item).Site;
				if (site != null) {	
					string name = site.Name;
					Font f = new Font(comboBox.Font, FontStyle.Bold);
					g.DrawString(name, f, stringColor, xPos, dea.Bounds.Y);
					xPos += (int)g.MeasureString(name + "-", f).Width;
				}
			}
			
			string typeString = item.GetType().ToString();
			g.DrawString(typeString, comboBox.Font, stringColor, xPos, dea.Bounds.Y);
		}
		static bool inUpdate = false;
		
		void ComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			if (!inUpdate) {
				ISelectionService selectionService = (ISelectionService)host.GetService(typeof(ISelectionService));
				if (comboBox.SelectedIndex >= 0) {
					selectionService.SetSelectedComponents(new object[] {comboBox.Items[comboBox.SelectedIndex] });
				} else {
					SetDesignableObject(null);
					selectionService.SetSelectedComponents(new object[] { });
				}
				if (SelectedObjectChanged != null) {
					SelectedObjectChanged(this, EventArgs.Empty);
				}
			}
		}
		
		static void SelectedObjectsChanged()
		{
			if (grid.SelectedObjects != null && grid.SelectedObjects.Length == 1) {
				for (int i = 0; i < comboBox.Items.Count; ++i) {
					if (grid.SelectedObject == comboBox.Items[i]) {
						comboBox.SelectedIndex = i;
					}
				}
			} else {
				comboBox.SelectedIndex = -1;
			}
		}
		
		public override void RedrawContent()
		{
			grid.Refresh();
		}
		
		public override void Dispose()
		{
			base.Dispose();
			grid.SelectedObjects = null;
			grid.Dispose();
		}
		
		public static void SetDesignableObject(object obj)
		{
			//grid.SelectedObject  = obj;
			//SelectedObjectsChanged();
		}
		
		public static void SetDesignableObjects(object[] obj)
		{
			grid.SelectedObjects = obj;
			SelectedObjectsChanged();
		}
		
		public static void RemoveHost(IDesignerHost host)
		{
			PropertyPad.host = null;
			grid.Site = null;
			
			ISelectionService selectionService = (ISelectionService)host.GetService(typeof(ISelectionService));
			if (selectionService != null) {
				selectionService.SelectionChanging -= new EventHandler(SelectionChangingHandler);
				selectionService.SelectionChanged  -= new EventHandler(SelectionChangedHandler);
			}
			
			IComponentChangeService componentChangeService = (IComponentChangeService)host.GetService(typeof(IComponentChangeService));
			if (componentChangeService != null) {
				componentChangeService.ComponentAdded   -= new ComponentEventHandler(UpdateSelectedObjects);
				componentChangeService.ComponentRemoved -= new ComponentEventHandler(UpdateSelectedObjects);
				componentChangeService.ComponentRename  -= new ComponentRenameEventHandler(UpdateSelectedObjectsOnRename);
			}
		}
		
		public static void SetDesignerHost(IDesignerHost host)
		{
			PropertyPad.host = host;
			grid.Site = (new IDEContainer(host)).CreateSite(grid);
			grid.PropertyTabs.AddTabType(typeof(System.Windows.Forms.Design.EventsTab), PropertyTabScope.Document);
			
			ISelectionService selectionService = (ISelectionService)host.GetService(typeof(ISelectionService));
			if (selectionService != null) {
				selectionService.SelectionChanging += new EventHandler(SelectionChangingHandler);
				selectionService.SelectionChanged  += new EventHandler(SelectionChangedHandler);
			}
			
			IComponentChangeService componentChangeService = (IComponentChangeService)host.GetService(typeof(IComponentChangeService));
			if (componentChangeService != null) {
				componentChangeService.ComponentAdded   += new ComponentEventHandler(UpdateSelectedObjects);
				componentChangeService.ComponentRemoved += new ComponentEventHandler(UpdateSelectedObjects);
				componentChangeService.ComponentRename  += new ComponentRenameEventHandler(UpdateSelectedObjectsOnRename);
			}
		}
		
		public static void SetSelectableObjects(ICollection coll)
		{
			inUpdate = true;
			try {
				comboBox.Items.Clear();
				if (coll != null) {
					foreach (object obj in coll) {
						comboBox.Items.Add(obj);
					}
				}
				SelectedObjectsChanged();
			} finally {
				inUpdate = false;
			}
		}
		static void UpdateSelectedObjects(object sender, ComponentEventArgs e)
		{
			SetSelectableObjects(host.Container.Components);
		}
		static void UpdateSelectedObjectsOnRename(object sender, ComponentRenameEventArgs e)
		{
			SetSelectableObjects(host.Container.Components);
		}
		
		
		public static void SelectionChangingHandler(object sender, EventArgs args)
		{
		}

		public static void SelectionChangedHandler(object sender, EventArgs args)
		{
			ISelectionService selectionService = sender as ISelectionService;
			if (selectionService != null) {
				ICollection selection = selectionService.GetSelectedComponents();
				object[] selArray = new object[selection.Count];
				selection.CopyTo(selArray, 0);
				grid.SelectedObjects = selArray;
				
				inUpdate = true;
				try {
					SelectedObjectsChanged();
				} finally {
					inUpdate = false;
				}
			}
		}
		
		void PropertyChanged(object sender, PropertyValueChangedEventArgs e)
		{
			if (host != null) {
				DesignerTransaction transaction = host.CreateTransaction("Property Changed");
				transaction.Commit();
			}
			OnPropertyValueChanged(sender, e);
		}
		
		void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
		{
			if(PropertyValueChanged != null) {
				PropertyValueChanged(sender, e);
			}
		}
	}*/
}
