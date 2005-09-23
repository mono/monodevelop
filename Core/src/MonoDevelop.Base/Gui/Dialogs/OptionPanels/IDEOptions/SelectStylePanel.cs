// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Components;
using MonoDevelop.Services;
using MonoDevelop.Core.AddIns;

using Gtk;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	internal class SelectStylePanel : AbstractOptionPanel
	{
		SelectStylePanelWidget widget;

		public override void LoadPanelContents()
		{
			Add (widget = new SelectStylePanelWidget ());
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ();
			return true;
		}

		class SelectStylePanelWidget : GladeWidgetExtract 
		{
			[Glade.Widget] public Gtk.CheckButton extensionButton;
			[Glade.Widget] public Gtk.CheckButton hiddenButton;
			
					
			public SelectStylePanelWidget () : base ("Base.glade", "SelectStylePanel")
			{
				extensionButton.Active  = Runtime.Properties.GetProperty("MonoDevelop.Gui.ProjectBrowser.ShowExtensions", true);
				hiddenButton.Active  = Runtime.Properties.GetProperty("MonoDevelop.Gui.FileScout.ShowHidden", false);

			}
			
			public void Store()
			{
				Runtime.Properties.SetProperty("MonoDevelop.Gui.ProjectBrowser.ShowExtensions", extensionButton.Active);
				Runtime.Properties.SetProperty("MonoDevelop.Gui.FileScout.ShowHidden", hiddenButton.Active);
			}
		}
	}
}
