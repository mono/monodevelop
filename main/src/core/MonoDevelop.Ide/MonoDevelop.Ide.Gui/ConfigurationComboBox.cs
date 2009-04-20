//
// ConfigurationComboBox.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	internal class ConfigurationComboBox: CustomItem
	{
		Gtk.Alignment align;
		DropDownBox combo;
		DropDownBox.ComboItemSet configs = new DropDownBox.ComboItemSet ();
		DropDownBox.ComboItemSet runtimes = new DropDownBox.ComboItemSet ();
		object defaultRuntime = new object ();
		bool updating;
		
		public ConfigurationComboBox () 
		{
			align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 0f);
			Add (align);
			
			align.LeftPadding = 3;
			align.RightPadding = 3;
			
			combo = new DropDownBox ();
			combo.AddItemSet (configs);
			combo.AddItemSet (runtimes);
			
			align.Add (combo);
			ShowAll ();
			
			Combo.Changed += OnChanged;
			IdeApp.Workspace.ConfigurationsChanged += OnConfigurationsChanged;
			IdeApp.Workspace.RuntimesChanged += OnConfigurationsChanged;
			IdeApp.Workspace.ActiveRuntimeChanged += OnActiveRuntimeChanged;
			IdeApp.Workspace.ActiveConfigurationChanged += OnActiveConfigurationChanged;
			Reset ();
		}
		
		protected DropDownBox Combo {
			get { return combo; }
		}
		
		public override void SetToolbarStyle (Gtk.Toolbar toolbar)
		{
/*			if (Style != null) {
				if (toolbar.IconSize == Gtk.IconSize.Menu || toolbar.IconSize == Gtk.IconSize.SmallToolbar) {
					Pango.FontDescription fd = Style.FontDescription.Copy ();
					fd.Size = (int) (fd.Size * Pango.Scale.Small);
					ctx.FontDesc = fd;
				} else {
					ctx.FontDesc = Style.FontDescription;
				}
			}
*/		}
		
		void Reset ()
		{
			configs.Clear ();
			runtimes.Clear ();
			UpdateLabel ();
		}
		
		void RefreshCombo ()
		{
			configs.Clear ();
			runtimes.Clear ();
			
			foreach (string conf in IdeApp.Workspace.GetConfigurations ()) {
				DropDownBox.ComboItem item = new DropDownBox.ComboItem (conf, conf);
				configs.Add (item);
				if (conf == IdeApp.Workspace.ActiveConfiguration)
					configs.CurrentItem = item.Item;
			}
			
			foreach (TargetRuntime tr in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
				DropDownBox.ComboItem item = new DropDownBox.ComboItem (tr.DisplayName, tr);
				runtimes.Add (item);
				if (tr == IdeApp.Workspace.ActiveRuntime)
					runtimes.CurrentItem = tr;
			}
			
			// If there is only one runtime, there is no need to show it
			if (runtimes.Count == 1)
				runtimes.Clear ();
			else {
				DropDownBox.ComboItem item = new DropDownBox.ComboItem (GettextCatalog.GetString ("Default Runtime"), defaultRuntime);
				runtimes.Insert (0, item);
				if (IdeApp.Workspace.UseDefaultRuntime)
					runtimes.CurrentItem = defaultRuntime;
			}
			
			UpdateLabel ();
			Combo.ShowAll ();
			
			if (configs.CurrentItem == null && configs.Count > 0)
				IdeApp.Workspace.ActiveConfiguration = (string) configs [0].Item;
			
			if (runtimes.CurrentItem == null && runtimes.Count > 0)
				IdeApp.Workspace.ActiveRuntime = Runtime.SystemAssemblyService.CurrentRuntime;
		}
		
		void UpdateLabel ()
		{
			if ((configs.Count == 0 && runtimes.Count == 0) || !IdeApp.Workspace.IsOpen) {
				combo.Sensitive = false;
				combo.ActiveText = "";
				return;
			}
			combo.Sensitive = true;
			string label = "";
			if (configs.Count > 0) {
				label = (string) configs.CurrentItem;
				if (runtimes.Count > 0) {
					if (runtimes.CurrentItem is TargetRuntime)
						label += " (" + ((TargetRuntime)runtimes.CurrentItem).DisplayName + ")";
				}
			} else if (runtimes.Count > 0) {
				if (runtimes.CurrentItem is TargetRuntime)
					label = ((TargetRuntime)runtimes.CurrentItem).DisplayName;
				else
					label = GettextCatalog.GetString ("Default Runtime");
			}
			combo.ActiveText = label;
		}
		
		void OnConfigurationsChanged (object sender, EventArgs e)
		{
			RefreshCombo ();
		}
		
		void OnActiveConfigurationChanged (object sender, EventArgs e)
		{
			if (updating)
				return;

			foreach (DropDownBox.ComboItem item in configs) {
				if (item.Label == IdeApp.Workspace.ActiveConfiguration) {
					configs.CurrentItem = IdeApp.Workspace.ActiveConfiguration;
					UpdateLabel ();
					return;
				}
			}

			// Configuration not found. Set one that is known.
			if (configs.Count > 0)
				IdeApp.Workspace.ActiveConfiguration = configs [0].Label;
		}
		
		void OnActiveRuntimeChanged (object sender, EventArgs e)
		{
			if (updating)
				return;
			
			foreach (DropDownBox.ComboItem item in runtimes) {
				if ((IdeApp.Workspace.UseDefaultRuntime && item.Item == defaultRuntime) ||
				    (item.Label == IdeApp.Workspace.ActiveRuntime.DisplayName)) {
					runtimes.CurrentItem = item.Item;
					UpdateLabel ();
					return;
				}
			}
		}
		
		protected void OnChanged (object sender, DropDownBox.ChangedEventArgs args)
		{
			if (updating)
				return;
			if (IdeApp.Workspace.IsOpen) {
				updating = true;
				if (args.ItemSet == configs) {
					IdeApp.Workspace.ActiveConfiguration = (string) args.Item;
				} else if (args.ItemSet == runtimes) {
					if (args.Item is TargetRuntime)
						IdeApp.Workspace.ActiveRuntime = (TargetRuntime) args.Item;
					else
						IdeApp.Workspace.UseDefaultRuntime = true;
				}
				UpdateLabel ();
				updating = false;
			}
		}
	}
	
	class DropDownBox : Gtk.Button
	{
		Gtk.Label label;
		List<ComboItemSet> items = new List<ComboItemSet> ();
		
		public event EventHandler<ChangedEventArgs> Changed;
		
		public class ComboItem
		{
			public ComboItem (string label, object item) {
				Label = label;
				Item = item;
			}
			public string Label { get; set; }
			public object Item { get; set; }
		}
		
		public class ComboItemSet: List<ComboItem>
		{
			public object CurrentItem { get; set; }
			
			public new void Clear ()
			{
				base.Clear ();
				CurrentItem = null;
			}
		}
		
		public class ChangedEventArgs: EventArgs
		{
			public ComboItemSet ItemSet { get; set; }
			public object Item { get; set; }
		}
		
		public DropDownBox ()
		{
			Gtk.HBox hbox = new Gtk.HBox ();
			
			label = new Gtk.Label ();
			label.Xalign = 0;
			
			hbox.PackStart (label, true, true, 3);
			
			hbox.PackEnd (new Gtk.Arrow (Gtk.ArrowType.Down, Gtk.ShadowType.None), false, false, 1);
			hbox.PackEnd (new Gtk.VSeparator (), false, false, 1);
			Child = hbox;
		}
		
		public string ActiveText {
			get {
				return label.Text;
			}
			set {
				label.Text = value;
			}
		}
		
		public void Clear ()
		{
			items.Clear ();
		}
		
		public void AddItemSet (ComboItemSet iset)
		{
			items.Add (iset);
		}
		
		void SelectItem (ComboItemSet iset, ComboItem item)
		{
			iset.CurrentItem = item.Item;
			if (Changed != null) {
				ChangedEventArgs args = new ChangedEventArgs ();
				args.ItemSet = iset;
				args.Item = item.Item;
				Changed (this, args);
			}
		}

		protected override void OnPressed ()
		{
			base.OnPressed ();
			Gtk.Menu menu = new Gtk.Menu ();
			foreach (ComboItemSet iset in items) {
				if (iset.Count == 0)
					continue;
				
				if (menu.Children.Length > 0) {
					Gtk.SeparatorMenuItem sep = new Gtk.SeparatorMenuItem ();
					sep.Show ();
					menu.Insert (sep, -1);
				}
				
				Gtk.RadioMenuItem grp = new Gtk.RadioMenuItem ("");
				foreach (ComboItem ci in iset) {
					Gtk.RadioMenuItem mi = new Gtk.RadioMenuItem (grp, ci.Label);
					if (ci.Item == iset.CurrentItem || ci.Item.Equals (iset.CurrentItem))
						mi.Active = true;
					
					ComboItemSet isetLocal = iset;
					ComboItem ciLocal = ci;
					mi.Activated += delegate {
						SelectItem (isetLocal, ciLocal);
					};
					mi.ShowAll ();
					menu.Insert (mi, -1);
				}
			}
			menu.Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
		}

		void PositionFunc (Gtk.Menu mn, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle rect = this.Allocation;
			x += rect.X;
			y += rect.Y + rect.Height;
			
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > this.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			if (mn.SizeRequest ().Width < rect.Width)
				mn.WidthRequest = rect.Width;
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}
	}
	
}
