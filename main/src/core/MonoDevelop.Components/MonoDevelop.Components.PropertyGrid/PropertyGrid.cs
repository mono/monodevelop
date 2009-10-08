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

namespace MonoDevelop.Components.PropertyGrid
{
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class PropertyGrid: Gtk.VBox
	{
		object currentObject;
		object[] propertyProviders;

		PropertyGridTree tree;
		HSeparator helpSeparator;
		VPaned vpaned;
		
		Toolbar toolbar;
		RadioToolButton catButton;
		RadioToolButton alphButton;
		ToggleToolButton helpButton;

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
			toolbar = new Toolbar ();
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.IconSize = IconSize.Menu;
			base.PackStart (toolbar, false, false, 0);
			
			catButton = new RadioToolButton (new GLib.SList (IntPtr.Zero));
			catButton.IconWidget = new Gtk.Image (new Gdk.Pixbuf (typeof (PropertyGrid).Assembly,
				"MonoDevelop.Components.PropertyGrid.SortByCat.png"));
			catButton.TooltipText = GettextCatalog.GetString ("Sort in categories");
			catButton.Toggled += new EventHandler (toolbarClick);
			toolbar.Insert (catButton, 0);
			
			alphButton = new RadioToolButton (catButton, Stock.SortAscending);
			alphButton.TooltipText = GettextCatalog.GetString ("Sort alphabetically");
			alphButton.Clicked += new EventHandler (toolbarClick);
			toolbar.Insert (alphButton, 1);
			
			catButton.Active = true;
			
			toolbar.Insert (new SeparatorToolItem (), 2);
			helpButton = new ToggleToolButton (Gtk.Stock.Help);
			helpButton.TooltipText = GettextCatalog.GetString ("Show help panel");
			helpButton.Clicked += delegate {
				ShowHelp = helpButton.Active;
				MonoDevelop.Core.PropertyService.Set (PROP_HELP_KEY, helpButton.Active);
			};
			toolbar.Insert (helpButton, 3);
			
			#endregion

			vpaned = new VPaned ();

			tree = new PropertyGridTree (editorManager, this);
			tree.Changed += delegate {
				Update ();
			};

			VBox tbox = new VBox ();
			helpSeparator = new HSeparator ();
			helpSeparator.Visible = true;
			tbox.PackStart (helpSeparator, false, false, 0);
			tbox.PackStart (tree, true, true, 0);
			helpSeparator = new HSeparator ();
			tbox.PackStart (helpSeparator, false, false, 0);
			helpSeparator.NoShowAll = true;
			vpaned.Pack1 (tbox, true, true);
			
			AddPropertyTab (new DefaultPropertyTab ());
			AddPropertyTab (new EventPropertyTab ());

			base.PackEnd (vpaned);
			base.FocusChain = new Gtk.Widget [] { vpaned };
			
			helpButton.Active = ShowHelp = MonoDevelop.Core.PropertyService.Get<bool> (PROP_HELP_KEY, true);
			
			Populate ();
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
				Populate ();
			}
		}
		
		public PropertySort PropertySort {
			get { return propertySort; }
			set {
				if (value != propertySort) {
					propertySort = value;
					Populate ();
				}
			}
		}
		
		ArrayList propertyTabs = new ArrayList ();
		PropertyTab selectedTab;
		
		public PropertyTab SelectedTab {
			get { return selectedTab; }
		}
		
		private void AddPropertyTab (PropertyTab tab)
		{
			TabRadioToolButton rtb;
			if (propertyTabs.Count == 0) {
				selectedTab = tab;
				rtb = new TabRadioToolButton (new GLib.SList (IntPtr.Zero), Stock.MissingImage);
				rtb.Active = true;
				toolbar.Insert (new SeparatorToolItem (), FirstTabIndex - 1);
			}
			else
				rtb = new TabRadioToolButton (
					(RadioToolButton) toolbar.GetNthItem (propertyTabs.Count + FirstTabIndex - 1));
			
			//load image from PropertyTab's bitmap
			var icon = tab.GetIcon (); 
			if (icon != null)
				rtb.IconWidget = new Gtk.Image (icon);
			else
				rtb.IconWidget = new Gtk.Image (Stock.MissingImage, IconSize.SmallToolbar);
			
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
			this.currentObject = obj;
			this.propertyProviders = propertyProviders;
			UpdateTabs ();
			Populate();
		}
		
		public void CommitPendingChanges ()
		{
			tree.CommitChanges ();
		}
		
		void UpdateTabs ()
		{
			foreach (Gtk.Widget w in toolbar.Children) {
				TabRadioToolButton but = w as TabRadioToolButton;
				if (but != null)
					but.Visible = currentObject != null && but.Tab.CanExtend (currentObject);
			}
		}
	
		//TODO: add more intelligence for editing state etc. Maybe need to know which property has changed, then 
		//just update that
		public void Refresh ()
		{
			QueueDraw ();
		}
		
		void Populate ()
		{
			PropertyDescriptorCollection properties;
			
			tree.SaveStatus ();
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
			set { toolbar.Visible = value; }
		}
		
		#region Hel Pane
		
		public bool ShowHelp
		{
			get { return descFrame != null; }
			set {
				if (value == ShowHelp)
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
				}
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
			
			Pango.FontDescription font = Style.FontDescription.Copy ();
			font.Size = (font.Size * 8) / 10;
			descTextView.ModifyFont (font);
			
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
		
		#endregion
	}
	
	class TabRadioToolButton: RadioToolButton
	{
		public TabRadioToolButton (RadioToolButton group): base (group)
		{
		}
		
		public TabRadioToolButton (GLib.SList group, string icon): base (group, icon)
		{
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
