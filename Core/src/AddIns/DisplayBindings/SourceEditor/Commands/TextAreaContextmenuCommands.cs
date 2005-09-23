// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Text;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Gui.Components;
using MonoDevelop.Gui;
using MonoDevelop.TextEditor;
using MonoDevelop.Core.Services;

namespace MonoDevelop.DefaultEditor.Commands
{
	public class ShowBufferOptions : AbstractMenuCommand
	{
		public override void Run()
		{
			Console.WriteLine ("Not ported to the new editor yet");
			/*
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window == null || !(window.ViewContent is ITextEditorControlProvider)) {
				return;
			}
			TextEditorControl textarea = ((ITextEditorControlProvider)window.ViewContent).TextEditorControl;
			PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
			*/
			/*TabbedOptions o = new TabbedOptions(resourceService.GetString("Dialog.Options.BufferOptions"),
			                                    ((IProperties)propertyService.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties())),
			                                    AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/ViewContent/DefaultTextEditor/OptionsDialog"));*/
			//o.Width  = 450;
			//o.Height = 425;
			//o.FormBorderStyle = FormBorderStyle.FixedDialog;
			//o.ShowDialog();
			//o.Dispose();
			//textarea.OptionsChanged();
		}
	}
	
	public class HighlightingTypeBuilder : ISubmenuBuilder
	{
		//TextEditorControl  control      = null;
		//Gtk.MenuItem[] menuCommands = null;
			
		public Gtk.MenuItem[] BuildSubmenu (ConditionCollection conditionCollection, object owner)
		{
			/*
			control = (TextEditorControl)owner;
			
			ArrayList menuItems = new ArrayList();
			
			foreach (DictionaryEntry entry in HighlightingManager.Manager.HighlightingDefinitions) {
				SdMenuCheckBox item = new SdMenuCheckBox(null, null, entry.Key.ToString());
				item.Active = control.Document.HighlightingStrategy.Name == entry.Key.ToString();
				item.Toggled    += new EventHandler(ChangeSyntax);
				menuItems.Add(item);
			}
			menuCommands = (Gtk.MenuItem[])menuItems.ToArray(typeof(Gtk.MenuItem));
			return menuCommands;
			*/
			return null;
		}
		
		void ChangeSyntax (object sender, EventArgs e)
		{
			/*
			if (control != null) {
				SdMenuCheckBox item = (SdMenuCheckBox)sender;
				foreach (SdMenuCheckBox i in menuCommands) {
					i.Active = false;
				}
				item.Active = true;
				IHighlightingStrategy strat = HighlightingStrategyFactory.CreateHighlightingStrategy(((Gtk.Label)item.Child).Text);
				if (strat == null) {
					throw new Exception("Strategy can't be null");
				}
				control.Document.HighlightingStrategy = strat;
				control.Refresh();
			}*/
		}
	}	
}
