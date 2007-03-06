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
using System.Windows.Forms.Design;

using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors;

namespace MonoDevelop.DesignerSupport.PropertyGrid
{
	public class PropertyGrid: Gtk.VBox
	{
		object currentObject;
		
		private bool showHelp = true;

		private PropertyGridTree tree;
		private VPaned vpaned;
		
		private Tooltips tips;

		private Toolbar toolbar;
		private RadioToolButton catButton;
		private RadioToolButton alphButton;

		private ScrolledWindow textScroll;
		private VBox desc;
		private Label descTitle;
		private TextView descText;
		private Frame descFrame;
		
		EditorManager editorManager;
		
		private System.Windows.Forms.PropertySort propertySort = System.Windows.Forms.PropertySort.Categorized;

		
		public PropertyGrid (): this (new EditorManager ())
		{
		}
		
		internal PropertyGrid (EditorManager editorManager)
		{
			this.editorManager = editorManager;
			tips = new Tooltips ();
			
			#region Toolbar
			toolbar = new Toolbar ();
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.IconSize = IconSize.SmallToolbar;
			base.PackStart (toolbar, false, false, 0);
			
			catButton = new RadioToolButton (new GLib.SList (IntPtr.Zero));
			catButton.IconWidget = new Gtk.Image (new Gdk.Pixbuf (typeof (PropertyGrid).Assembly, "MonoDevelop.DesignerSupport.PropertyGrid.SortByCat.png"));
			catButton.SetTooltip (tips, GettextCatalog.GetString ("Sort in categories"), null);
			catButton.Toggled += new EventHandler (toolbarClick);
			toolbar.Insert (catButton, 0);
			
			alphButton = new RadioToolButton (catButton, Stock.SortAscending);
			alphButton.SetTooltip (tips, GettextCatalog.GetString ("Sort alphabetically"), null);
			alphButton.Clicked += new EventHandler (toolbarClick);
			toolbar.Insert (alphButton, 1);
			
			catButton.Active = true;
			
			SeparatorToolItem sep = new SeparatorToolItem();
			toolbar.Insert (sep, 2);
			
			#endregion

			vpaned = new VPaned ();

			descFrame = new Frame ();
			descFrame.Shadow = ShadowType.In;

			desc = new VBox (false, 0);
			descFrame.Add (desc);

			descTitle = new Label ();
			descTitle.SetAlignment(0, 0);
			descTitle.SetPadding (5, 5);
			descTitle.UseMarkup = true;
			desc.PackStart (descTitle, false, false, 0);

			textScroll = new ScrolledWindow ();
			textScroll.HscrollbarPolicy = PolicyType.Never;
			textScroll.VscrollbarPolicy = PolicyType.Automatic;

			desc.PackEnd (textScroll, true, true, 0);

			//TODO: Use label, but wrapping seems dodgy.
			descText = new TextView ();
			descText.WrapMode = WrapMode.Word;
			descText.WidthRequest = 1;
			descText.HeightRequest = 70;
			descText.Editable = false;
			descText.LeftMargin = 5;
			descText.RightMargin = 5;
			textScroll.Add (descText);

			tree = new PropertyGridTree (editorManager, this);

			vpaned.Pack1 (tree, true, true);
			vpaned.Pack2 (descFrame, false, true);
			
			AddPropertyTab (new DefaultPropertyTab ());
			AddPropertyTab (new EventPropertyTab ());

			base.PackEnd (vpaned);
			Populate ();
		}
			
		internal EditorManager EditorManager {
			get { return editorManager; }
		}
		
		#region Toolbar state and handlers
		
		private const int FirstTabIndex = 3;
		
		void toolbarClick (object sender, EventArgs e)
		{
			if (sender == alphButton)
				PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
			else if (sender == catButton)
				PropertySort = System.Windows.Forms.PropertySort.Categorized;
			else {
				TabRadioToolButton button = (TabRadioToolButton) sender;
				if (selectedTab == button.Tab) return;
				selectedTab = button.Tab;
				Populate ();
			}
		}
		
		public System.Windows.Forms.PropertySort PropertySort {
			get { return propertySort; }
			set {
				if (value != propertySort) {
					propertySort = value;
					Populate ();
				}
			}
		}
		
		private ArrayList propertyTabs = new ArrayList ();
		private PropertyTab selectedTab;
		
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
			}
			else
				rtb = new TabRadioToolButton ((RadioToolButton) toolbar.GetNthItem (propertyTabs.Count + FirstTabIndex - 1));
			
			//load image from PropertyTab's bitmap
			if (tab.Bitmap != null)
				rtb.IconWidget = new Gtk.Image (ImageToPixbuf (tab.Bitmap));
			else
				rtb.IconWidget = new Gtk.Image (Stock.MissingImage, IconSize.SmallToolbar);
			
			rtb.Tab = tab;
			rtb.SetTooltip (tips, tab.TabName, null);
			rtb.Toggled += new EventHandler (toolbarClick);	
			
			toolbar.Insert (rtb, propertyTabs.Count + FirstTabIndex);
			
			propertyTabs.Add(tab);
		}
			
		#endregion
		
		public object CurrentObject {
			get { return currentObject; }
			set {
				if (this.currentObject == value)
					return;
				this.currentObject = value;
				UpdateTabs ();
				Populate();
			}
		}
		
		void UpdateTabs ()
		{
			foreach (Gtk.Widget w in toolbar.Children) {
				TabRadioToolButton but = w as TabRadioToolButton;
				if (but != null)
					but.Visible = currentObject != null && but.Tab.CanExtend (currentObject);
			}
		}
	
		//TODO: add more intelligence for editing state etc. Maybe need to know which property has changed, then just update that
		public void Refresh ()
		{
			QueueDraw ();
		}
		
		void Populate ()
		{
			PropertyDescriptorCollection properties;
			
			if (currentObject != null)
				properties = selectedTab.GetProperties (currentObject);
			else
				properties = new PropertyDescriptorCollection (new PropertyDescriptor[0] {});
			tree.SaveStatus ();
			tree.Clear ();
			tree.PropertySort = propertySort;
			tree.Populate (properties, currentObject);
			tree.RestoreStatus ();
		}
		
		public bool ShowHelp
		{
			get { return showHelp; }
			set {
				if (value != showHelp)
					if (value)
						vpaned.Pack2 (descFrame, false, true);
					else
						vpaned.Remove (descFrame);
			}
		}
		
		public void SetHelp (string title, string description)
		{
			descText.Buffer.Clear ();
			descText.Buffer.InsertAtCursor (description);
			descTitle.Markup = "<b>" + title + "</b>";
		}

		public void ClearHelp()
		{
			descTitle.Text = "";
			descText.Buffer.Clear ();
		}
		
		//for PropertyTab images
		private Gdk.Pixbuf ImageToPixbuf(System.Drawing.Image image)
		{
			using (MemoryStream stream = new MemoryStream ()) {
				image.Save (stream, System.Drawing.Imaging.ImageFormat.Png);
				stream.Position = 0;
				return new Gdk.Pixbuf (stream);
			}
		}
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
}
