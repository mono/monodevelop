//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TriggersEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		
		private ISchemaProvider schemaProvider;
		private TableSchema table;
		private TriggerSchemaCollection triggers;
		
		private SchemaActions action;
		
		private ListStore store;
		private ListStore storeTypes;
		private ListStore storeEvents;
		
		private const int colNameIndex = 0;
		private const int colTypeIndex = 1;
		private const int colEventIndex = 2;
		private const int colFireTypeIndex = 3;
		private const int colPositionIndex = 4;
		private const int colActiveIndex = 5;
		private const int colCommentIndex = 6;
		private const int colSourceIndex = 7;
		private const int colObjIndex = 8;
		
		public TriggersEditorWidget (ISchemaProvider schemaProvider, SchemaActions action)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
			
			this.Build();
			
			sqlEditor.Editable = false;
			sqlEditor.TextChanged += new EventHandler (SourceChanged);
			
			store = new ListStore (typeof (string), typeof (string), typeof (string), typeof (bool), typeof (string), typeof (bool), typeof (string), typeof (string), typeof (object));
			storeTypes = new ListStore (typeof (string));
			storeEvents = new ListStore (typeof (string));
			listTriggers.Model = store;
			listTriggers.Selection.Changed += new EventHandler (OnSelectionChanged);
			
			foreach (string name in Enum.GetNames (typeof (TriggerType)))
			         storeTypes.AppendValues (name);
			foreach (string name in Enum.GetNames (typeof (TriggerEvent)))
			         storeEvents.AppendValues (name);
			
			TreeViewColumn colName = new TreeViewColumn ();
			TreeViewColumn colType = new TreeViewColumn ();
			TreeViewColumn colEvent = new TreeViewColumn ();
			TreeViewColumn colFireType = new TreeViewColumn ();
			TreeViewColumn colPosition = new TreeViewColumn ();
			TreeViewColumn colActive = new TreeViewColumn ();
			TreeViewColumn colComment = new TreeViewColumn ();
			
			colName.Title = AddinCatalog.GetString ("Name");
			colType.Title = AddinCatalog.GetString ("Type");
			colEvent.Title = AddinCatalog.GetString ("Event");
			colFireType.Title = AddinCatalog.GetString ("Each Row");
			colPosition.Title = AddinCatalog.GetString ("Position");
			colActive.Title = AddinCatalog.GetString ("Active");
			colComment.Title = AddinCatalog.GetString ("Comment");
			
			colType.MinWidth = 120;
			colEvent.MinWidth = 120;
			
			CellRendererText nameRenderer = new CellRendererText ();
			CellRendererCombo typeRenderer = new CellRendererCombo ();
			CellRendererCombo eventRenderer = new CellRendererCombo ();
			CellRendererToggle fireTypeRenderer = new CellRendererToggle ();
			CellRendererText positionRenderer = new CellRendererText ();
			CellRendererToggle activeRenderer = new CellRendererToggle ();
			CellRendererText commentRenderer = new CellRendererText ();
			
			nameRenderer.Editable = true;
			nameRenderer.Edited += new EditedHandler (NameEdited);
			
			typeRenderer.Model = storeTypes;
			typeRenderer.TextColumn = 0;
			typeRenderer.Editable = true;
			typeRenderer.Edited += new EditedHandler (TypeEdited);
			
			eventRenderer.Model = storeEvents;
			eventRenderer.TextColumn = 0;
			eventRenderer.Editable = true;
			eventRenderer.Edited += new EditedHandler (EventEdited);
			
			fireTypeRenderer.Activatable = true;
			fireTypeRenderer.Toggled += new ToggledHandler (FireTypeToggled);
			
			positionRenderer.Editable = true;
			positionRenderer.Edited += new EditedHandler (PositionEdited);
			
			activeRenderer.Activatable = true;
			activeRenderer.Toggled += new ToggledHandler (ActiveToggled);
			
			commentRenderer.Editable = true;
			commentRenderer.Edited += new EditedHandler (CommentEdited);

			colName.PackStart (nameRenderer, true);
			colType.PackStart (typeRenderer, true);
			colEvent.PackStart (eventRenderer, true);
			colFireType.PackStart (fireTypeRenderer, true);
			colPosition.PackStart (positionRenderer, true);
			colActive.PackStart (activeRenderer, true);
			colComment.PackStart (commentRenderer, true);

			colName.AddAttribute (nameRenderer, "text", colNameIndex);
			colType.AddAttribute (typeRenderer, "text", colTypeIndex);
			colEvent.AddAttribute (eventRenderer, "text", colEventIndex);
			colFireType.AddAttribute (fireTypeRenderer, "active", colFireTypeIndex);
			colPosition.AddAttribute (positionRenderer, "text", colPositionIndex);
			colActive.AddAttribute (activeRenderer, "active", colActiveIndex);
			colComment.AddAttribute (commentRenderer, "text", colCommentIndex);
			
			listTriggers.AppendColumn (colName);
			listTriggers.AppendColumn (colType);
			listTriggers.AppendColumn (colEvent);
			listTriggers.AppendColumn (colFireType);
			listTriggers.AppendColumn (colPosition);
			listTriggers.AppendColumn (colActive);
			listTriggers.AppendColumn (colComment);
			
			ShowAll ();
		}
		
		public void Initialize (TableSchema table, TriggerSchemaCollection triggers)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			if (triggers == null)
				throw new ArgumentNullException ("triggers");

			this.table = table;
			this.triggers = triggers;
			
			foreach (TriggerSchema trigger in triggers)
				AddTrigger (trigger);
		}

		protected virtual void RemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (listTriggers.Selection.GetSelected (out iter)) {
				TriggerSchema trigger = store.GetValue (iter, colObjIndex) as TriggerSchema;
				
				if (MessageService.Confirm (
					AddinCatalog.GetString ("Are you sure you want to remove trigger '{0}'?", trigger.Name),
					AlertButton.Remove
				)) {
					store.Remove (ref iter);
					triggers.Remove (trigger);
					EmitContentChanged ();
				}
			}
		}

		protected virtual void AddClicked (object sender, EventArgs e)
		{
			TriggerSchema trigger = schemaProvider.CreateTriggerSchema ("trigger_" + table.Name);
			trigger.TableName = table.Name;
			int index = 1;
			while (triggers.Contains (trigger.Name))
				trigger.Name = "trigger_" + table.Name + (index++); 
			triggers.Add (trigger);
			AddTrigger (trigger);
			EmitContentChanged ();
		}
		
		private void AddTrigger (TriggerSchema trigger)
		{
			store.AppendValues (trigger.Name, trigger.TriggerType.ToString (),
				trigger.TriggerEvent.ToString (), trigger.TriggerFireType == TriggerFireType.ForEachRow,
				trigger.Position.ToString (), trigger.IsActive, trigger.Comment,
				trigger.Source , trigger);
		}
		
		private void NameEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				if (!string.IsNullOrEmpty (args.NewText)) {
					store.SetValue (iter, colNameIndex, args.NewText);
				} else {
					string oldText = store.GetValue (iter, colNameIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		protected virtual void OnSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (listTriggers.Selection.GetSelected (out iter)) {
				buttonRemove.Sensitive = true;
				sqlEditor.Editable = true;
				
				TriggerSchema trigger = store.GetValue (iter, colObjIndex) as TriggerSchema;
				
				sqlEditor.Text = trigger.Source;
				
			} else {
				buttonRemove.Sensitive = false;
				sqlEditor.Editable = false;
				sqlEditor.Text = String.Empty;
			}
		}
		
		private void SourceChanged (object sender, EventArgs args)
		{
			TreeIter iter;
			if (listTriggers.Selection.GetSelected (out iter)) {
				store.SetValue (iter, colSourceIndex, sqlEditor.Text);
				EmitContentChanged ();
			}
		}
		
		private void TypeEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				foreach (string name in Enum.GetNames (typeof (TriggerType))) {
					if (args.NewText == name) {
						store.SetValue (iter, colTypeIndex, args.NewText);
						EmitContentChanged ();
						return;
					}
				}
				string oldText = store.GetValue (iter, colTypeIndex) as string;
				(sender as CellRendererText).Text = oldText;
			}
		}
		
		private void EventEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				foreach (string name in Enum.GetNames (typeof (TriggerEvent))) {
					if (args.NewText == name) {
						store.SetValue (iter, colEventIndex, args.NewText);
						EmitContentChanged ();
						return;
					}
				}
				string oldText = store.GetValue (iter, colEventIndex) as string;
				(sender as CellRendererText).Text = oldText;
			}
		}
		
		private void PositionEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				int len;
				if (!string.IsNullOrEmpty (args.NewText) && int.TryParse (args.NewText, out len)) {
					store.SetValue (iter, colPositionIndex, args.NewText);
					EmitContentChanged ();
				} else {
					string oldText = store.GetValue (iter, colPositionIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void FireTypeToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, colFireTypeIndex);
	 			store.SetValue (iter, colFireTypeIndex, !val);
				EmitContentChanged ();
	 		}
		}
		
		private void ActiveToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, colActiveIndex);
	 			store.SetValue (iter, colActiveIndex, !val);
				EmitContentChanged ();
	 		}
		}
		
		private void CommentEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				store.SetValue (iter, colCommentIndex, args.NewText);
				EmitContentChanged ();
			}
		}
		
		public virtual bool ValidateSchemaObjects (out string msg)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					string name = store.GetValue (iter, colNameIndex) as string;
					string source = store.GetValue (iter, colSourceIndex) as string;
					//type, event, firetype, position and fireType are always valid
					
					if (String.IsNullOrEmpty (source)) {
						msg = AddinCatalog.GetString ("Trigger '{0}' does not contain a trigger statement.", name);
						return false;
					}
				} while (store.IterNext (ref iter));
			}
			msg = null;
			return true;
		}
		
		public virtual void FillSchemaObjects ()
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					TriggerSchema trigger = store.GetValue (iter, colObjIndex) as TriggerSchema;

					trigger.Name = store.GetValue (iter, colNameIndex) as string;
					
					trigger.TriggerType = (TriggerType)Enum.Parse (typeof (TriggerType), store.GetValue (iter, colTypeIndex) as string);
					trigger.TriggerEvent = (TriggerEvent)Enum.Parse (typeof (TriggerEvent), store.GetValue (iter, colEventIndex) as string);
					trigger.TriggerFireType = (TriggerFireType)Enum.Parse (typeof (TriggerFireType), store.GetValue (iter, colFireTypeIndex) as string);
					
					trigger.Position = int.Parse (store.GetValue (iter, colPositionIndex) as string);
					trigger.IsActive = (bool)store.GetValue (iter, colActiveIndex);
					
					trigger.Comment = store.GetValue (iter, colCommentIndex) as string;
					trigger.Source = store.GetValue (iter, colSourceIndex) as string;
	
					table.Triggers.Add (trigger);
				} while (store.IterNext (ref iter));
			}
		}
		
		protected virtual void EmitContentChanged ()
		{
			if (ContentChanged != null)
				ContentChanged (this, EventArgs.Empty);
		}
	}
}
