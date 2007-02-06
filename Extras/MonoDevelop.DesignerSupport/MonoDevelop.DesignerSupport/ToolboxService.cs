//
// ToolboxService.cs: Loads, stores and manipulates toolbox items.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Collections.Generic;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects.Serialization;

using MonoDevelop.DesignerSupport.Toolbox;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class ToolboxService
	{
		readonly static string toolboxLoaderPath = "/MonoDevelop/DesignerSupport/ToolboxLoaders";
		readonly static string toolboxProviderPath = "/MonoDevelop/DesignerSupport/ToolboxProviders";
		
		ToolboxList userItems = new ToolboxList ();
		ToolboxList defaultItems = new ToolboxList ();
		List<IToolboxLoader> loaders = new List<IToolboxLoader> ();
		List<IToolboxDynamicProvider> dynamicProviders  = new List<IToolboxDynamicProvider> ();
		
		IToolboxConsumer currentConsumer;
		ItemToolboxNode selectedItem;
		
		internal ToolboxService ()
		{			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (onActiveDocChanged);
			Runtime.AddInService.RegisterExtensionItemListener (toolboxLoaderPath, OnLoaderExtensionChanged);
			Runtime.AddInService.RegisterExtensionItemListener (toolboxProviderPath, OnProviderExtensionChanged);
		}
		
		#region Extension loading
		
		void OnLoaderExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add)
				loaders.Add ((IToolboxLoader) item);
			else if (action == ExtensionAction.Remove)
				loaders.Remove ((IToolboxLoader) item);
		}
		
		void OnProviderExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				IToolboxDynamicProvider dyProv = item as IToolboxDynamicProvider;
				if (dyProv != null)
					dynamicProviders.Add ((IToolboxDynamicProvider) item);
				
				IToolboxDefaultProvider defProv = item as IToolboxDefaultProvider;
				if (defProv!= null) {
					IList<ItemToolboxNode> newItems = defProv.GetItems ();
					if (newItems != null)
						defaultItems.AddRange (newItems);
				}	
			}
			else if (action == ExtensionAction.Remove)
				dynamicProviders.Remove ((IToolboxDynamicProvider) item);
			
			OnToolboxContentsChanged ();
		}
		
		public void AddUserItems ()
		{
			Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog (GettextCatalog.GetString ("Add items to toolbox"), (Gtk.Window) IdeApp.Services.MessageService.RootWindow, Gtk.FileChooserAction.Open);
			fcd.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Open, Gtk.ResponseType.Ok);
			fcd.DefaultResponse = Gtk.ResponseType.Ok;
			fcd.Filter = new Gtk.FileFilter ();
			fcd.Filter.AddPattern ("*.dll");
			fcd.SelectMultiple = false;

			Gtk.ResponseType response = (Gtk.ResponseType) fcd.Run( );
			fcd.Hide ();
			
			if (response == Gtk.ResponseType.Ok && fcd.Filename != null)
					AddUserItems (fcd.Filename);
			
			//fcd.Destroy();
		}
		
		public void AddUserItems (string fileName)
		{			
			foreach (IToolboxLoader loader in loaders) {
				if (!fileName.EndsWith (loader.FileTypes[0]))
					continue;
				
				System.Collections.Generic.IList<ItemToolboxNode> loadedItems = loader.Load (fileName);
				
				//prevent user from loading the same items again
				foreach (ItemToolboxNode node in loadedItems) {
					bool found = false;
					foreach (ItemToolboxNode n in defaultItems) {
						if (node.Equals (n))
							found = true;
					}
					foreach (ItemToolboxNode n in userItems) {
						if (node.Equals (n))
							found = true;
					}
					if (!found)
						userItems.Add (node);
				}
			}
			
			OnToolboxContentsChanged ();
		}
		
		~ToolboxService ()
		{
			Runtime.AddInService.UnregisterExtensionItemListener (toolboxLoaderPath, OnLoaderExtensionChanged);
			Runtime.AddInService.UnregisterExtensionItemListener (toolboxProviderPath, OnLoaderExtensionChanged);
		}
		
		#endregion
		
		#region Serializing/deserializing
		
		public void SaveUserToolbox (string fileName)
		{
			userItems.SaveContents (fileName);
		}
		
		public void LoadUserToolbox (string fileName)
		{
			userItems = ToolboxList.LoadFromFile (fileName);
			OnToolboxContentsChanged ();
		}
		
		#endregion
		
		#region Actions and properties
		
		public IList GetCurrentToolboxItems ()
		{
			return GetToolboxItems (CurrentConsumer);
		}
		
		public IList GetToolboxItems (IToolboxConsumer consumer)
		{
			List<ItemToolboxNode> arr = new List<ItemToolboxNode> ();
			
			if (consumer != null) {
				//get the user items
				foreach (ItemToolboxNode node in userItems)
					//hide unknown nodes -- they're only there because deserialisation has failed, so they won't actually be usable
					if ( !(node is UnknownToolboxNode))
						if (IsSupported (node, consumer))
							arr.Add (node);
				
				//merge the default items
				foreach (ItemToolboxNode node in defaultItems)
					if (IsSupported (node, consumer))
						arr.Add (node);
				
				//merge the list of dynamic items from each provider
				foreach (IToolboxDynamicProvider prov in dynamicProviders) {
					IList<ItemToolboxNode> dynItems = prov.GetDynamicItems (consumer);
					if (dynItems != null)
						arr.AddRange (dynItems);
				}
			}
			
			return arr;
		}
		
		public IToolboxConsumer CurrentConsumer {
			get { return currentConsumer; }
			private set {
				currentConsumer = value;				
				OnToolboxConsumerChanged (currentConsumer);
			}
		}
		
		public ItemToolboxNode SelectedItem {
			get { return selectedItem; }
		}
		
		public void SelectItem (ItemToolboxNode item)
		{
			selectedItem = item;
			OnToolboxSelectionChanged (item);
		}
		
		public void UseSelectedItem ()
		{
			if ((CurrentConsumer == null) || (selectedItem == null))
				return;
			
			CurrentConsumer.ConsumeItem (selectedItem);
			OnToolboxUsed (CurrentConsumer, selectedItem);
		}
		
		#endregion
		
		#region Change notification
		
		Document oldActiveDoc =  null;
		void onActiveDocChanged (object o, EventArgs e)
		{
			if (oldActiveDoc != null)
				oldActiveDoc.ViewChanged -= OnViewChanged;
			if (IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.ViewChanged += OnViewChanged;
			oldActiveDoc = IdeApp.Workbench.ActiveDocument;
			
			OnViewChanged (null, null);
		}
		
		void OnViewChanged (object sender, EventArgs args)
		{
			//only treat active ViewContent as a Toolbox consumer if it implements IToolboxConsumer
			if (IdeApp.Workbench.ActiveDocument != null)
				CurrentConsumer = IdeApp.Workbench.ActiveDocument.ActiveView as IToolboxConsumer;
			else
				CurrentConsumer = null;
		}
		
		protected virtual void OnToolboxContentsChanged ()
		{
			if (ToolboxContentsChanged != null)
				ToolboxContentsChanged (this, new EventArgs ()); 	
		}
		
		protected virtual void OnToolboxConsumerChanged (IToolboxConsumer consumer)
		{
			if (ToolboxConsumerChanged != null)
				ToolboxConsumerChanged (this, new ToolboxConsumerChangedEventArgs (consumer)); 	
		}
		
		protected virtual void OnToolboxSelectionChanged (ItemToolboxNode item)
		{
			if (ToolboxSelectionChanged != null)
				ToolboxSelectionChanged (this, new ToolboxSelectionChangedEventArgs (item)); 	
		}
		
		protected virtual void OnToolboxUsed (IToolboxConsumer consumer, ItemToolboxNode item)
		{
			if (ToolboxUsed != null)
				ToolboxUsed (this, new ToolboxUsedEventArgs (consumer, item)); 	
		}
		
		public event EventHandler ToolboxContentsChanged;
		public event ToolboxConsumerChangedHandler ToolboxConsumerChanged;
		public event ToolboxSelectionChangedHandler ToolboxSelectionChanged;
		public event ToolboxUsedHandler ToolboxUsed;
		
		#endregion
		
		#region Filtering
		
		public virtual bool IsSupported (ItemToolboxNode node, IToolboxConsumer consumer)
		{
			//if something has no filters it is more useful and efficient
			//to show it for everything than to show it for nothing
			if (node.ItemFilters == null || node.ItemFilters.Count == 0)
				return true;
			
			//check all of host's filters
			foreach (ToolboxItemFilterAttribute desFa in consumer.ToolboxFilterAttributes)
			{
				if (!FilterPermitted (node, desFa, node.ItemFilters, consumer))
					return false;
			}
			
			//check all of item's filters
			foreach (ToolboxItemFilterAttribute itemFa in node.ItemFilters)
			{
				if (!FilterPermitted (node, itemFa, consumer.ToolboxFilterAttributes, consumer))
					return false;
			}
			
			//we assume permitted, so only return false when blocked by a filter
			return true;
		}
		
		//evaluate a filter attribute against a list, and check whether permitted
		private bool FilterPermitted (ItemToolboxNode node, ToolboxItemFilterAttribute desFa, ICollection filterAgainst, IToolboxConsumer consumer)
		{
			switch (desFa.FilterType) {
				case ToolboxItemFilterType.Allow:
					//this is really for matching some other filter string against
					return true;
				
				case ToolboxItemFilterType.Custom:
					return consumer.CustomFilterSupports (node);
					
				case ToolboxItemFilterType.Prevent:
					//if host and toolboxitem have same filterstring, then not permitted
					foreach (ToolboxItemFilterAttribute itemFa in filterAgainst)
						if (desFa.Match (itemFa))
							return false;
					return true;
				
				case ToolboxItemFilterType.Require:
					//if host and toolboxitem have same filterstring, then permitted, unless one is prevented
					foreach (ToolboxItemFilterAttribute itemFa in filterAgainst)
						if (desFa.Match (itemFa) && (desFa.FilterType != ToolboxItemFilterType.Prevent))
							return true;
					return false;
			}
			throw new InvalidOperationException ("Unexpected ToolboxItemFilterType value.");
		}
		
		#endregion
	}
	
	public delegate void ToolboxConsumerChangedHandler (object sender, ToolboxConsumerChangedEventArgs e);
	
	public class ToolboxConsumerChangedEventArgs
	{
		IToolboxConsumer consumer;
		
		public ToolboxConsumerChangedEventArgs (IToolboxConsumer consumer)
		{
			this.consumer = consumer;
		}
		
		public IToolboxConsumer Consumer {
			get { return consumer; }
		}
	}
	
	public delegate void ToolboxSelectionChangedHandler (object sender, ToolboxSelectionChangedEventArgs e);
	
	public class ToolboxSelectionChangedEventArgs
	{
		ItemToolboxNode item;
		
		public ToolboxSelectionChangedEventArgs (ItemToolboxNode item)
		{
			this.item = item;
		}
		
		public ItemToolboxNode Item {
			get { return item; }
		}
	}
	
	public delegate void ToolboxUsedHandler (object sender, ToolboxUsedEventArgs e);
	
	public class ToolboxUsedEventArgs
	{
		ItemToolboxNode item;
		IToolboxConsumer consumer;
		
		public ToolboxUsedEventArgs (IToolboxConsumer consumer, ItemToolboxNode item)
		{
			this.item = item;
			this.consumer = consumer;
		}
		
		public ItemToolboxNode Item {
			get { return item; }
		}
		
		public IToolboxConsumer Consumer {
			get { return consumer; }
		}
	}
}
