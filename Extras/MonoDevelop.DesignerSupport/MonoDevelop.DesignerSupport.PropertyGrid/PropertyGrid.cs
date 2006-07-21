/* 
 * PropertyGrid.cs - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 *
 * Authors:
 *  Michael Hutchinson <m.j.hutchinson@gmail.comk>
 * 	Eric Butler <eric@extremeboredom.net>
 *
 * Copyright (C) 2005 Michael Hutchinson
 * Copyright (C) 2005 Eric Butler
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
using System.Reflection;
using System.IO;
using Gtk;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace AspNetEdit.UI
{

public class PropertyGrid : Gtk.VBox
{
	private object currentObject = null;
	private EditorManager editorManager;
	private PropertyDescriptorCollection properties;
	public ArrayList Rows = new ArrayList ();
	private GridRow selectedRow;
	private string defaultPropertyName;
	private string defaultEventName;
	private bool showHelp = true;

	private Table table;
	private VBox expanderBox;
	private ScrolledWindow scrolledWindow;
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

	public PropertyGrid()
		: this(new EditorManager ())
	{
	}

	internal PropertyGrid (EditorManager editorManager)
		: base (false, 0)
	{
		this.editorManager = editorManager;
		
		tips = new Tooltips ();
		
		#region Toolbar
		toolbar = new Toolbar ();
		toolbar.ToolbarStyle = ToolbarStyle.Icons;
		toolbar.IconSize = IconSize.SmallToolbar;
		base.PackStart (toolbar, false, false, 0);
		
		catButton = new RadioToolButton (new GLib.SList (IntPtr.Zero));
		catButton.IconWidget = new Image (new Gdk.Pixbuf (null, "AspNetEdit.UI.PropertyGrid.SortByCat.png"));
		catButton.SetTooltip (tips, "Sort in categories", null);
		catButton.Toggled += new EventHandler (toolbarClick);
		toolbar.Insert (catButton, 0);
		
		alphButton = new RadioToolButton (catButton, Stock.SortAscending);
		alphButton.SetTooltip (tips, "Sort alphabetically", null);
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
		descText.HeightRequest = 100;
		descText.Editable = false;
		descText.LeftMargin = 5;
		descText.RightMargin = 5;
		textScroll.Add (descText);

		scrolledWindow = new ScrolledWindow ();
		scrolledWindow.HscrollbarPolicy = PolicyType.Automatic;
		scrolledWindow.VscrollbarPolicy = PolicyType.Automatic;

		vpaned.Pack1 (scrolledWindow, true, true);
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
			int index = toolbar.GetItemIndex ((RadioToolButton) sender) - FirstTabIndex;
			
			PropertyTab tab = (PropertyTab) propertyTabs[index];
			if (selectedTab == tab) return;
			
			selectedTab = tab;
			Populate (); 
		}
	}
	
	private System.Windows.Forms.PropertySort propertySort = System.Windows.Forms.PropertySort.Categorized;
	
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
		RadioToolButton rtb;
		if (propertyTabs.Count == 0) {
			selectedTab = tab;
			rtb = new RadioToolButton (new GLib.SList (IntPtr.Zero), Stock.MissingImage);
			rtb.Active = true;
		}
		else
			rtb = new RadioToolButton ((RadioToolButton) toolbar.GetNthItem (propertyTabs.Count + FirstTabIndex - 1));
		
		//load image from PropertyTab's bitmap
		if (tab.Bitmap != null)
			rtb.IconWidget = new Gtk.Image (ImageToPixbuf (tab.Bitmap));
		else
			rtb.IconWidget = new Gtk.Image (Stock.MissingImage, IconSize.SmallToolbar);
		
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

			//want to drop reference to this or will cause
			//problems when stopping editing
			SelectedRow = null;

			this.currentObject = value;

			//reset these, but only calculate on demand
			defaultPropertyName = null;
			defaultEventName = null;

			Populate();
		}
	}

	private void Populate()
	{
		bool categorised = PropertySort == System.Windows.Forms.PropertySort.Categorized;
		
		if (currentObject != null)
			properties = selectedTab.GetProperties (currentObject);
		else
			properties = new PropertyDescriptorCollection (new PropertyDescriptor[0] {});

		//kill existing table
		if (expanderBox != null) {
			expanderBox.Destroy ();
			expanderBox = null;
		}
		Rows.Clear();

		//transcribe browsable properties
		ArrayList sorted = new ArrayList();

		foreach (PropertyDescriptor descriptor in properties)
			if (descriptor.IsBrowsable)
				sorted.Add (descriptor);

		//expands to fill empty space
		EventBox bottomWidget = new EventBox ();
		bottomWidget.ModifyBg (StateType.Normal, new Gdk.Color (0xff, 0xff, 0xff));
		bottomWidget.CanFocus = true;
		bottomWidget.ButtonPressEvent += on_bottomWidget_ButtonPressEvent;
		expanderBox = new VBox ();


		//how are we displaying it?
		if (!categorised) {
			sorted.Sort(new SortByName ());
			table = BuildTable (sorted);
			expanderBox.PackStart (table, false, false, 0);
		}
		else {
			sorted.Sort (new SortByCat ());

			ArrayList tabList = new ArrayList ();
			string oldCat = "";
			int oldIndex = 0;
			Expander catHead = null;

			for (int i = 0; i < sorted.Count; i++) {
				PropertyDescriptor pd = (PropertyDescriptor) sorted[i];

				//Create category header
				if (pd.Category != oldCat) {
					if (catHead != null) {
						Table t = BuildTable (sorted.GetRange(oldIndex, i - oldIndex));
						catHead.Add (t);
					}

					catHead = new Expander ("<b>" + pd.Category + "</b>");
					((Label) catHead.LabelWidget).UseMarkup = true;
					catHead.Expanded = true;
					expanderBox.PackStart (catHead, false, false, 0);

					oldCat = pd.Category;
					oldIndex = i;
				}
			}

			if (catHead != null) {
				Table t = BuildTable (sorted.GetRange (oldIndex, sorted.Count - oldIndex));
				catHead.Add (t);
			}

			//TODO: Find better way of getting all tables same size, maybe resizable
			int maxwidth = 1;
			foreach (GridRow pgr in Rows) {
				int width = pgr.propertyNameLabel.SizeRequest ().Width;
				if (width > maxwidth)
					maxwidth = width;
			}
			foreach (GridRow pgr in Rows)
				pgr.propertyNameLabel.WidthRequest = maxwidth;
		}

		expanderBox.PackStart (bottomWidget, true, true, 1);
		scrolledWindow.AddWithViewport (expanderBox);
		expanderBox.ShowAll ();
	}
	
	//TODO: add more intelligence for editing state etc. Maybe need to know which property has changed, then just update that
	public void Refresh ()
	{
		Populate ();
	}

	private Table BuildTable (ArrayList arr)
	{
		//create new table
		Table table = new Table (Convert.ToUInt32 (arr.Count), 2, false);
		table.ColumnSpacing = 1;
		table.RowSpacing = 1;
		table.BorderWidth = 0;

		UInt32 currentRow = 0;

		for (int i = 0; i < arr.Count; i++) {
			PropertyDescriptor pd = (PropertyDescriptor) arr[i];

			//create item
			//TODO: expand children of expandable objects with no editor. Use ExpandableObjectConverter?
			GridRow newRow = new GridRow (this, pd);	
			
			if (newRow.IsValidProperty) {
				table.Attach (newRow.LabelWidget, 0, 1, currentRow, currentRow + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
				table.Attach (newRow.ValueWidget, 1, 2, currentRow, currentRow + 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
				currentRow += 1;
				Rows.Add (newRow);

				if (newRow.PropertyDescriptor == SelectedTab.GetDefaultProperty (this.CurrentObject))
					this.SelectedRow = newRow;
			}
				
		}
		return table;
	}


	private void on_bottomWidget_ButtonPressEvent (object o, EventArgs e)
	{
		(o as EventBox).GrabFocus ();
		SelectedRow = null;
	}

	public GridRow SelectedRow
	{
		get { return selectedRow; }
		set {
			if (selectedRow == value)
				return;

			if (!Rows.Contains (value) && value != null)
				throw new Exception("You cannot select a GridRow that is not in the PropertyGrid");

			//slightly complicated logic here to allow for selection to take
			//place from GridRow or PropertyGrid, and keep state in synch

			GridRow oldSelectedRow = selectedRow;
			selectedRow = value;

			if (oldSelectedRow != null)
				oldSelectedRow.Selected = false;

			if (selectedRow != null)
				selectedRow.Selected = true;
		}
	}

	public void SetHelp(string title, string description)
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

	public event PropertyValueChangedEventHandler PropertyValueChanged;

	public void OnPropertyValueChanged (GridRow changedItem, object oldValue, object newValue)
	{
		if (PropertyValueChanged != null)
			PropertyValueChanged (this, new PropertyValueChangedEventArgs (changedItem, oldValue, newValue));
	}

	private class SortByCat : IComparer
	{
		public int Compare (object x, object y)
		{
			int catcomp = (x as PropertyDescriptor).Category.CompareTo ((y as PropertyDescriptor).Category);

			if (catcomp == 0)
				return (x as PropertyDescriptor).DisplayName.CompareTo ((y as PropertyDescriptor).DisplayName);
			else
				return catcomp;
		}
	}

	private class SortByName : IComparer
	{
		public int Compare(object x, object y)
		{
			return (x as PropertyDescriptor).DisplayName.CompareTo ((y as PropertyDescriptor).DisplayName);
		}
	}

	public enum ViewState {
		ByCategory,
		Alphabetical
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
}

