// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.CodeTemplates
{
	internal partial class CodeTemplatePanelWidget : Gtk.Bin 
	{
		// members
		List<CodeTemplateGroup> templateGroups;
		int       currentSelectedGroup = -1;
		
		// Gtk widgets
		ListStore templateListViewStore = new ListStore(typeof(CodeTemplate));
		TextBuffer templateTextBuffer = new TextBuffer(null);
		ListStore groupStore = new ListStore (typeof (string));
		
		public CodeTemplateGroup CurrentTemplateGroup {
			get {
				if (currentSelectedGroup < 0 || currentSelectedGroup >= templateGroups.Count) {
					return null;
				}
				return (CodeTemplateGroup)templateGroups[currentSelectedGroup];
			}
		}
		
		public List<CodeTemplateGroup> TemplateGroups {
			get {
				return this.templateGroups;
			}
		}
		
		public CodeTemplatePanelWidget (List<CodeTemplateGroup> templateGroups)
		{
			Build ();
			
			this.templateGroups = templateGroups;
			
			SetLabels();
			
			// set up the treeview
			templateListView.Reorderable = false;
			templateListView.HeadersVisible = true;
			templateListView.Selection.Mode = SelectionMode.Multiple;
			templateListView.Model = templateListViewStore;
			
			// set up group combobox
			groupCombo.Model = groupStore;
			CellRendererText cr = new CellRendererText ();
			groupCombo.PackStart (cr, true);
			groupCombo.AddAttribute (cr, "text", 0);

			// set up the text view
			templateTextView.Buffer = templateTextBuffer;
			//templateTextView.Font = new System.Drawing.Font("Courier New", 10f);
			
			// wire in the events
			removeButton.Clicked += new System.EventHandler(RemoveEvent);
			addButton.Clicked    += new System.EventHandler(AddEvent);
			editButton.Clicked   += new System.EventHandler(EditEvent);
			
			addGroupButton.Clicked    += new System.EventHandler(AddGroupEvent);
			editGroupButton.Clicked += new System.EventHandler(EditGroupEvent);
			removeGroupButton.Clicked += new System.EventHandler(RemoveGroupEvent);
			templateListView.RowActivated		+= new Gtk.RowActivatedHandler(RowActivatedEvent);
			templateListView.Selection.Changed 	+= new EventHandler(IndexChange);
			templateTextBuffer.Changed += new EventHandler(TextChange);
			
			if (templateGroups.Count > 0) {
				currentSelectedGroup = 0;
			}
			
			FillGroupOptionMenu();
			BuildListView();
			IndexChange(null, null);
			SetEnabledStatus();
		}
		
		void SetLabels()
		{
			CellRendererText textRenderer = new CellRendererText ();
			
			// and listview columns 
			templateListView.AppendColumn (
				GettextCatalog.GetString ("Template"), 
				textRenderer,  
				new TreeCellDataFunc(TemplateListViewCellDataFunc));
			templateListView.AppendColumn (
				GettextCatalog.GetString ("Description"), 
				textRenderer, 
				new TreeCellDataFunc(TemplateListViewCellDataFunc));
		}
		
		// function to render the cell
		void TemplateListViewCellDataFunc(TreeViewColumn column, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			CodeTemplate codeTemplate = ((ListStore)model).GetValue(iter, 0) as CodeTemplate;
			
			if(column.Title == GettextCatalog.GetString ("Template"))
			{
				// first column
				((CellRendererText)renderer).Text = codeTemplate.Shortcut;
			}
			else
			{
				// second column
				((CellRendererText)renderer).Text = codeTemplate.Description;
			}
		}
		
		void SetEnabledStatus()
		{
			bool groupSelected = CurrentTemplateGroup != null;
			bool groupsEmpty   = templateGroups.Count != 0;
			
			SetEnabledStatus(groupSelected, addButton, editButton, removeButton, templateListView, templateTextView);
			SetEnabledStatus(groupsEmpty, groupCombo, extensionLabel);
			if (groupSelected) {
				bool oneItemSelected = templateListView.Selection.CountSelectedRows() == 1;
				bool isItemSelected  = templateListView.Selection.CountSelectedRows() > 0;
				SetEnabledStatus(oneItemSelected, editButton, templateTextView);
				SetEnabledStatus(isItemSelected, removeButton);
			}
		}
		
		// disables or enables (sets sensitivty) a specified array of widgets
		public void SetEnabledStatus(bool enabled, params Widget[] controls)
		{
			foreach (Widget control in controls) {				
				if (control == null) {
					Services.MessageService.ShowError(GettextCatalog.GetString ("Control not found!"));
				} else {
					control.Sensitive = enabled;
				}
			}
		}
		
#region GroupComboBox event handler
		void SetGroupSelection(object sender, EventArgs e)
		{
			currentSelectedGroup = groupCombo.Active;
			BuildListView();
		}		
		
#endregion
		
#region Group Button events
		void AddGroupEvent(object sender, EventArgs e)
		{
			CodeTemplateGroup templateGroup = new CodeTemplateGroup(".???");
			if(ShowEditTemplateGroupDialog(ref templateGroup, GettextCatalog.GetString ("New "))) {
				templateGroups.Add(templateGroup);
				FillGroupOptionMenu();
				groupCombo.Active = (int) templateGroups.Count - 1;
				SetEnabledStatus();
			}
		}
		
		void EditGroupEvent(object sender, EventArgs e)
		{
			
			int index = groupCombo.Active;
			CodeTemplateGroup templateGroup = (CodeTemplateGroup) templateGroups[index];
			if(ShowEditTemplateGroupDialog(ref templateGroup, GettextCatalog.GetString ("Edit "))) {
				templateGroups[index] = templateGroup;
				FillGroupOptionMenu();
				groupCombo.Active = index;
				SetEnabledStatus();
			}
		}
		
		void RemoveGroupEvent(object sender, EventArgs e)
		{
			if (CurrentTemplateGroup != null) {
				templateGroups.RemoveAt(currentSelectedGroup);
				if (templateGroups.Count == 0) {
					currentSelectedGroup = -1;
				} else {
					groupCombo.Active = (int) Math.Min(currentSelectedGroup, templateGroups.Count - 1);
				}
				FillGroupOptionMenu();
				BuildListView();
				SetEnabledStatus();
			}
		}
		
		bool ShowEditTemplateGroupDialog(ref CodeTemplateGroup templateGroup, string title)
		{
			EditTemplateGroupDialog etgd = new EditTemplateGroupDialog(templateGroup, title);
			try {
				return (etgd.Run() == (int) Gtk.ResponseType.Ok);
			} finally {
				etgd.Destroy ();
			}
		}
#endregion
		
#region Template Button events
		void RemoveEvent(object sender, System.EventArgs e)
		{
			// first copy the selected item paths into a temp array
			int maxIndex = -1;				
			int selectedItemCount = templateListView.Selection.CountSelectedRows();
			TreeIter[] selectedIters = new TreeIter[selectedItemCount];
			TreeModel lv;
			TreePath[] pathList = templateListView.Selection.GetSelectedRows(out lv);								
			for(int i = 0; i < selectedItemCount; i++) {
				((ListStore)lv).GetIter(out selectedIters[i], pathList[i]);
				maxIndex = pathList[i].Indices[0];
			}
			
			// now delete each item in that list
			foreach(TreeIter toDelete in selectedIters) {
				TreeIter itr = toDelete;					
				((ListStore)lv).Remove(ref itr);
			}
			
			StoreTemplateGroup();
			
			// try and select the next item after the one removed
			if(maxIndex > -1) {
				templateListView.Selection.UnselectAll();
				TreeIter nextIter;
				maxIndex += 1- selectedItemCount;
				if(templateListViewStore.GetIterFromString(out nextIter, maxIndex.ToString())) {				
					// select the next one
					templateListView.Selection.SelectIter(nextIter);
				} else {
					// select the very last one
					maxIndex = templateListViewStore.IterNChildren() - 1;
					if(templateListViewStore.GetIterFromString(out nextIter, (maxIndex).ToString())) {
						templateListView.Selection.SelectIter(nextIter);
					}
				}
			}
		}
		
		void AddEvent(object sender, System.EventArgs e)
		{
			CodeTemplate newTemplate = new CodeTemplate();
			EditTemplateDialog etd = new EditTemplateDialog(newTemplate);
			try {
				if (etd.Run() == (int) Gtk.ResponseType.Ok) {
					CurrentTemplateGroup.Templates.Add(newTemplate);						
					templateListView.Selection.UnselectAll();
					BuildListView();
					
					// select the newly added last one
					TreeIter nextIter;
					int maxIndex = templateListViewStore.IterNChildren() - 1;
					if(templateListViewStore.GetIterFromString(out nextIter, (maxIndex).ToString())) {
						templateListView.Selection.SelectIter(nextIter);
					}
				}
			} finally {
				etd.Destroy ();
			}
		}
		
		void EditEvent(object sender, System.EventArgs e)
		{
			TreeIter selectedIter;
			TreeModel ls;				
			if(((ListStore)templateListView.Model).GetIter(out selectedIter, (TreePath) templateListView.Selection.GetSelectedRows(out ls)[0])) {
				CodeTemplate template = ls.GetValue(selectedIter, 0) as CodeTemplate;
				
				EditTemplateDialog etd = new EditTemplateDialog(template);
				try {
					if (etd.Run() == (int) Gtk.ResponseType.Ok) {
						ls.SetValue(selectedIter, 0, template);
						StoreTemplateGroup();
					}
				} finally {
					etd.Destroy ();
				}
				
				// select the newly edited item
				templateListView.Selection.SelectIter(selectedIter);
			}				
		}
		
		// raised when a treeview row is double clicked on
		void RowActivatedEvent(object sender, Gtk.RowActivatedArgs ra)
		{
			EditEvent(sender, System.EventArgs.Empty);
		}
#endregion

		void FillGroupOptionMenu()
		{
			groupCombo.Changed -= new EventHandler(SetGroupSelection);
			
			groupStore.Clear();

			foreach (CodeTemplateGroup templateGroup in templateGroups)
				groupStore.AppendValues (String.Join (";", templateGroup.ExtensionStrings));

			if (currentSelectedGroup >= 0)
				groupCombo.Active = (int) currentSelectedGroup;
			
			groupCombo.Changed += new EventHandler(SetGroupSelection);
		}
		
		void IndexChange(object sender, System.EventArgs e)
		{
			if(templateListView.Selection.CountSelectedRows() == 1) {
				TreeIter selectedIter;
				TreeModel listStore;				
				((ListStore)templateListView.Model).GetIter(out selectedIter, (TreePath) templateListView.Selection.GetSelectedRows(out listStore)[0]);
				templateTextBuffer.Text    = ((CodeTemplate)listStore.GetValue(selectedIter, 0)).Text;
			} else {
				templateTextBuffer.Text    = String.Empty;
			}
			SetEnabledStatus();
		}
		
		void TextChange(object sender, EventArgs e)
		{
			if(templateListView.Selection.CountSelectedRows() == 1) {
				TreeIter selectedIter;
				TreeModel listStore;				
				((ListStore)templateListView.Model).GetIter(out selectedIter, (TreePath) templateListView.Selection.GetSelectedRows(out listStore)[0]);
				((CodeTemplate)listStore.GetValue(selectedIter, 0)).Text = templateTextBuffer.Text;
			}
		}
		
		void StoreTemplateGroup()
		{
			if (CurrentTemplateGroup != null) {
				CurrentTemplateGroup.Templates.Clear();
					
				TreeIter first;			
				if(templateListView.Model.GetIterFirst(out first)) {
					TreeIter current = first;
					
					// loop through items in the tree, adding each item to the template group
					do {
						CurrentTemplateGroup.Templates.Add ((CodeTemplate)templateListView.Model.GetValue(current, 0)); 
					} while(templateListView.Model.IterNext(ref current));
				}
			}
		}
		
		void BuildListView()
		{
			ListStore listStore = templateListView.Model as ListStore;
			listStore.Clear();
			if (CurrentTemplateGroup != null) {
				foreach (CodeTemplate template in CurrentTemplateGroup.Templates) {
					listStore.AppendValues(template);					
				}
			}
			IndexChange(this, EventArgs.Empty);
		}
	}

	public class CodeTemplatePane : AbstractOptionPanel
	{
		
		CodeTemplatePanelWidget widget;
		
		public override void LoadPanelContents()
		{
			// create a new CodeTemplatePanelWidget using glade files and add it
			// pass in the template groups that it needs
			this.Add (widget = new CodeTemplatePanelWidget (CodeTemplateService.TemplateGroups));
		}
			
		public override bool StorePanelContents()
		{
			// get template groups from widhet and save them
			CodeTemplateService.TemplateGroups = widget.TemplateGroups;
			CodeTemplateService.SaveTemplates();
			return true;
		}		
	}
}
