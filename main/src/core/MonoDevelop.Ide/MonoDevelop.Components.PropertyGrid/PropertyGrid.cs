/* 
 * PropertyGrid.cs - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 *
 * Authors:
 *  Michael Hutchinson <m.j.hutchinson@gmail.comk>
 * 	Eric Butler <eric@extremeboredom.net>
 *  Lluis Sanchez Gual <lluis@novell.com>
 *
 * Copyright (C) 2005 Michael Hutchinson
 * Copyright (C) 2005 Eric Butler
 * Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;
using System.ComponentModel;

using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Components.PropertyGrid.PropertyEditors;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Components.PropertyGrid
{
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class PropertyGrid: Gtk.VBox, IPropertyGrid
	{
		object currentObject;
		object[] propertyProviders;

		PropertyGridTable tree;
		HSeparator helpSeparator;
		HSeparator toolbarSeparator;
		VPaned vpaned;
		
		IToolbarProvider toolbar;
		RadioButton catButton;
		RadioButton alphButton;

		string descTitle, descText;
		Label descTitleLabel;
		TextView descTextView;
		Gtk.Widget descFrame;
		
		EditorManager editorManager;
		
		PropertySort propertySort = PropertySort.Categorized;
		
		const string PROP_HELP_KEY = "MonoDevelop.PropertyPad.ShowHelp";
		
		public PropertyGrid (): this (new EditorManager ())
		{
		}
		
		internal PropertyGrid (EditorManager editorManager)
		{
			this.editorManager = editorManager;
			
			#region Toolbar
			
			PropertyGridToolbar tb = new PropertyGridToolbar ();
			base.PackStart (tb, false, false, 0);
			toolbar = tb;
			
			catButton = new RadioButton ((Gtk.RadioButton)null);
			catButton.Name = "MonoDevelop.PropertyGridToolbar.GtkRadioButton";
			catButton.DrawIndicator = false;
			catButton.Relief = ReliefStyle.None;
			catButton.Image = new ImageView (MonoDevelop.Ide.Gui.Stock.GroupByCategory, IconSize.Menu);
			catButton.Image.Show ();
			catButton.TooltipText = GettextCatalog.GetString ("Sort in categories");
			catButton.Toggled += new EventHandler (toolbarClick);
			toolbar.Insert (catButton, 0);
			
			alphButton = new RadioButton (catButton);
			alphButton.Name = "MonoDevelop.PropertyGridToolbar.GtkRadioButton";
			alphButton.DrawIndicator = false;
			alphButton.Relief = ReliefStyle.None;
			alphButton.Image = new ImageView (MonoDevelop.Ide.Gui.Stock.SortAlphabetically, IconSize.Menu);
			alphButton.Image.Show ();
			alphButton.TooltipText = GettextCatalog.GetString ("Sort alphabetically");
			alphButton.Clicked += new EventHandler (toolbarClick);
			toolbar.Insert (alphButton, 1);
			
			catButton.Active = true;
			
			#endregion

			vpaned = new VPaned ();

			tree = new PropertyGridTable (editorManager, this);
			tree.Changed += delegate {
				Update ();
			};

			CompactScrolledWindow sw = new CompactScrolledWindow ();
			sw.AddWithViewport (tree);
			((Gtk.Viewport)sw.Child).ShadowType = Gtk.ShadowType.None;
			sw.ShadowType = Gtk.ShadowType.None;
			sw.HscrollbarPolicy = PolicyType.Never;
			sw.VscrollbarPolicy = PolicyType.Automatic;

			VBox tbox = new VBox ();
			toolbarSeparator = new HSeparator ();
			toolbarSeparator.Visible = true;
			tbox.PackStart (toolbarSeparator, false, false, 0);
			tbox.PackStart (sw, true, true, 0);
			helpSeparator = new HSeparator ();
			tbox.PackStart (helpSeparator, false, false, 0);
			helpSeparator.NoShowAll = true;
			vpaned.Pack1 (tbox, true, true);
			
			AddPropertyTab (new DefaultPropertyTab ());
			AddPropertyTab (new EventPropertyTab ());

			base.PackEnd (vpaned);
			base.FocusChain = new Gtk.Widget [] { vpaned };
			
			Populate (saveEditSession: false);
			UpdateTabs ();
		}

		public ISite Site { get; set; }
		
		public void SetToolbarProvider (IToolbarProvider toolbarProvider)
		{
			PropertyGridToolbar t = toolbar as PropertyGridToolbar;
			if (t == null)
				throw new InvalidOperationException ("Custom toolbar provider already set");
			Remove (t);
			foreach (Widget w in t.Children) {
				t.Remove (w);
				toolbarProvider.Insert (w, -1);
			}
			t.Destroy ();
			toolbarSeparator.Hide ();
			toolbar = toolbarProvider;
			UpdateTabs ();
		}
		
		public event EventHandler Changed {
			add { tree.Changed += value; }
			remove { tree.Changed -= value; }
		}
			
		internal EditorManager EditorManager {
			get { return editorManager; }
		}
		
		#region Toolbar state and handlers
		
		private const int FirstTabIndex = 3;
		
		void toolbarClick (object sender, EventArgs e)
		{
			if (sender == alphButton)
				PropertySort = PropertySort.Alphabetical;
			else if (sender == catButton)
				PropertySort = PropertySort.Categorized;
			else {
				TabRadioToolButton button = (TabRadioToolButton) sender;
				if (selectedTab == button.Tab) return;
				selectedTab = button.Tab;
				Populate (saveEditSession: true);
			}
			// If the tree is re-populated while a value is being edited, the focus that the value editor had
			// is not returned back to the tree. We need to explicitly get it.
			tree.GrabFocus ();
		}
		
		public PropertySort PropertySort {
			get { return propertySort; }
			set {
				if (value != propertySort) {
					propertySort = value;
					Populate (saveEditSession: true);
				}
			}
		}
		
		ArrayList propertyTabs = new ArrayList ();
		PropertyTab selectedTab;
		
		public PropertyTab SelectedTab {
			get { return selectedTab; }
		}
		
		TabRadioToolButton firstTab;
		SeparatorToolItem tabSectionSeparator;
		
		private void AddPropertyTab (PropertyTab tab)
		{
			TabRadioToolButton rtb;
			if (propertyTabs.Count == 0) {
				selectedTab = tab;
				rtb = new TabRadioToolButton (null);
				rtb.Active = true;
				firstTab = rtb;
				toolbar.Insert (tabSectionSeparator = new SeparatorToolItem (), FirstTabIndex - 1);
			}
			else
				rtb = new TabRadioToolButton (firstTab);
			
			//load image from PropertyTab's bitmap
			var icon = tab.GetIcon ();
			if (icon != null)
				rtb.Image = new ImageView (icon);
			else
				rtb.Image = new ImageView (MonoDevelop.Ide.ImageService.GetIcon (Stock.MissingImage, IconSize.Menu));
			
			rtb.Tab = tab;
			rtb.TooltipText = tab.TabName;
			rtb.Toggled += new EventHandler (toolbarClick);	
			
			toolbar.Insert (rtb, propertyTabs.Count + FirstTabIndex);
			
			propertyTabs.Add(tab);
		}
			
		#endregion
		
		public object CurrentObject {
			get { return currentObject; }
			set { SetCurrentObject (value, new object[] {value}); }
		}
		
		public void SetCurrentObject (object obj, object[] propertyProviders)
		{
			if (this.currentObject == obj)
				return;
			if (this.propertyProviders != null) {
				foreach (var old in this.propertyProviders.OfType<IDisposable> ())
					old.Dispose ();
			}
			this.currentObject = obj;
			this.propertyProviders = propertyProviders;
			UpdateTabs ();
			Populate(saveEditSession: false);
		}
		
		public void CommitPendingChanges ()
		{
			tree.CommitChanges ();
		}
		
		void UpdateTabs ()
		{
			bool visible = currentObject != null && toolbar.Children.OfType<TabRadioToolButton> ().Count (but => but.Tab.CanExtend (currentObject)) > 1;
			foreach (var w in toolbar.Children.OfType<TabRadioToolButton> ())
				w.Visible = visible;
			if (tabSectionSeparator != null)
				tabSectionSeparator.Visible = visible;
		}
	
		//TODO: add more intelligence for editing state etc. Maybe need to know which property has changed, then 
		//just update that
		public void Refresh ()
		{
			Update (); 
			QueueDraw ();
		}

		internal bool IsEditing {
			get { return tree.IsEditing; } 
		}
		
		internal void Populate (bool saveEditSession)
		{
			PropertyDescriptorCollection properties;
			
			tree.SaveStatus ();
			if (saveEditSession)
				tree.SaveEditSession ();
			tree.Clear ();
			tree.PropertySort = propertySort;
			
			if (currentObject == null) {
				properties = new PropertyDescriptorCollection (new PropertyDescriptor[0] {});
				tree.Populate (properties, currentObject);
			}
			else {
				foreach (object prov in propertyProviders) {
					properties = selectedTab.GetProperties (prov);
					tree.Populate (properties, prov);
				}
			}
			tree.RestoreStatus ();
			tree.RestoreEditSession ();
		}
		
		void Update ()
		{
			PropertyDescriptorCollection properties;
			
			if (currentObject == null) {
				properties = new PropertyDescriptorCollection (new PropertyDescriptor[0] {});
				tree.Update (properties, currentObject);
			}
			else {
				foreach (object prov in propertyProviders) {
					properties = selectedTab.GetProperties (prov);
					tree.Update (properties, prov);
				}
			}
		}
		
		public bool ShowToolbar {
			get { return toolbar.Visible; }
			set { toolbar.Visible = toolbarSeparator.Visible = value; }
		}
		
		public ShadowType ShadowType {
			get { return tree.ShadowType; }
			set { tree.ShadowType = value; }
		}
		
		#region Hel Pane
		
		public bool ShowHelp
		{
			get { return descFrame != null; }
			set {
				// Disable for now. Documentation is shown in tooltips
/*				if (value == ShowHelp)
					return;
				if (value) {
					AddHelpPane ();
					helpSeparator.Show ();
				} else {
					vpaned.Remove (descFrame);
					descFrame.Destroy ();
					descFrame = null;
					descTextView = null;
					descTitleLabel = null;
					helpSeparator.Hide ();
				}*/
			}
		}
		
		void AddHelpPane ()
		{
			VBox desc = new VBox (false, 0);

			descTitleLabel = new Label ();
			descTitleLabel.SetAlignment(0, 0);
			descTitleLabel.SetPadding (5, 2);
			descTitleLabel.UseMarkup = true;
			desc.PackStart (descTitleLabel, false, false, 0);

			ScrolledWindow textScroll = new ScrolledWindow ();
			textScroll.HscrollbarPolicy = PolicyType.Never;
			textScroll.VscrollbarPolicy = PolicyType.Automatic;
			
			desc.PackEnd (textScroll, true, true, 0);
			
			//TODO: Use label, but wrapping seems dodgy.
			descTextView = new TextView ();
			descTextView.WrapMode = WrapMode.Word;
			descTextView.WidthRequest = 1;
			descTextView.HeightRequest = 70;
			descTextView.Editable = false;
			descTextView.LeftMargin = 5;
			descTextView.RightMargin = 5;
			descTextView.ModifyFont (FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11));
			
			textScroll.Add (descTextView);
			
			descFrame = desc;
			vpaned.Pack2 (descFrame, false, true);
			descFrame.ShowAll ();
			UpdateHelp ();
		}
		
		public void SetHelp (string title, string description)
		{
			descTitle = title;
			descText = description;
			UpdateHelp ();
		}
		
		void UpdateHelp ()
		{
			if (!ShowHelp)
				return;
			descTextView.Buffer.Clear ();
			if (descText != null)
				descTextView.Buffer.InsertAtCursor (descText);
			descTitleLabel.Markup = descTitle != null?
				"<b>" + descTitle + "</b>" : string.Empty;
		}

		public void ClearHelp()
		{
			descTitle = descText = null;
			UpdateHelp ();
		}
		
		public interface IToolbarProvider
		{
			void Insert (Widget w, int pos);
			Widget[] Children { get; }
			void ShowAll ();
			bool Visible { get; set; }
		}
		
		class PropertyGridToolbar: HBox, IToolbarProvider
		{
			public PropertyGridToolbar ()
			{
				Spacing = 3;
			}
			
			public void Insert (Widget w, int pos)
			{
				PackStart (w, false, false, 0);
				if (pos != -1) {
					Box.BoxChild bc = (Box.BoxChild) this [w];
					bc.Position = pos;
				}
			}
		}
		
		#endregion
	}
	
	class TabRadioToolButton: RadioButton
	{
		public TabRadioToolButton (RadioButton group): base (group)
		{
			DrawIndicator = false;
			Relief = ReliefStyle.None;
			NoShowAll = true;
			Name = "MonoDevelop.PropertyGridToolbar.GtkRadioButton";
		}
		
		public PropertyTab Tab;
	}
	
	public enum PropertySort
	{
		NoSort = 0,
		Alphabetical,
		Categorized,
		CategorizedAlphabetical
	}
}
