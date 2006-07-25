
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
		
		ToolboxList allItems = new ToolboxList ();
		List<IToolboxLoader> loaders = new List<IToolboxLoader> ();
		
		IToolboxConsumer currentConsumer;
		ItemToolboxNode selectedItem;
		
		internal ToolboxService ()
		{			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (onActiveDocChanged);
			Runtime.AddInService.RegisterExtensionItemListener (toolboxLoaderPath, OnLoaderExtensionChanged);
		}
		
		#region Extension loading
		
		void OnLoaderExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add)
				loaders.Add ((IToolboxLoader) item);
		}
		
		public void UserAddItems ()
		{
			Gtk.FileChooserDialog fcd = new Gtk.FileChooserDialog ("Add items to toolbox", (Gtk.Window) IdeApp.Services.MessageService.RootWindow, Gtk.FileChooserAction.Open);
			fcd.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Open, Gtk.ResponseType.Ok);
			fcd.DefaultResponse = Gtk.ResponseType.Ok;
			fcd.Filter = new Gtk.FileFilter ();
			fcd.Filter.AddPattern ("*.dll");
			fcd.SelectMultiple = false;

			Gtk.ResponseType response = (Gtk.ResponseType) fcd.Run( );
			fcd.Hide ();
			
			if (response == Gtk.ResponseType.Ok && fcd.Filename != null)
					AddItems (fcd.Filename);
			
			//fcd.Destroy();
		}
		
		public void AddItems (string fileName)
		{			
			foreach (IToolboxLoader loader in loaders) {
				if (!fileName.EndsWith (loader.FileTypes[0]))
					continue;
				
				System.Collections.Generic.IList<ItemToolboxNode> loadedItems = loader.Load (fileName);
				
				foreach (ItemToolboxNode node in loadedItems) {
					bool found = false;
					foreach (ItemToolboxNode n in allItems)
						if (node == n)
							found = true;
					if (!found)
						allItems.Add (node);
				}
			}
			
			OnToolboxChanged ();
		}
		
		~ToolboxService ()
		{
			Runtime.AddInService.UnregisterExtensionItemListener (toolboxLoaderPath, OnLoaderExtensionChanged);
		}
		
		#endregion
		
		#region Serializing/deserializing
		
		public void SaveContents (string fileName)
		{
			using (StreamWriter writer = new StreamWriter (fileName)) {
				XmlDataSerializer serializer = new XmlDataSerializer (new DataContext ());
				serializer.Serialize (writer, allItems as IList);
			}
		}
		
		public void LoadContents (string fileName)
		{
			object o;
			
			using (StreamReader reader = new StreamReader (fileName))
			{
				XmlDataSerializer serializer = new XmlDataSerializer (new DataContext ());
				o = serializer.Deserialize (reader, typeof (ToolboxList));	
			}
			
			allItems = (ToolboxList) o;
			OnToolboxChanged ();
		}
		
		#endregion
		
		#region Actions and properties
		
		public IList GetCurrentToolboxItems ()
		{
			return GetToolboxItems (currentConsumer);
		}
		
		public IList GetToolboxItems (IToolboxConsumer consumer)
		{	
			return allItems;
			
			//FIXME: enable filtering
			ArrayList arr = new ArrayList ();
			
			foreach (ItemToolboxNode node in allItems)
				if (IsSupported (node, consumer))
					arr.Add (node);
			
			return arr;
		}
		
		public IToolboxConsumer CurrentConsumer {
			get { return currentConsumer; }
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
			if ((currentConsumer == null) || (selectedItem == null))
				return;
			
			currentConsumer.Use (selectedItem);
			OnToolboxUsed (currentConsumer, selectedItem);
		}
		
		#endregion
		
		#region Change notification
		
		Document oldActiveDoc =  null;
		void onActiveDocChanged (object o, EventArgs e)
		{
			if (oldActiveDoc != null)
				oldActiveDoc.ViewChanged -= OnViewChanged;
			IdeApp.Workbench.ActiveDocument.ViewChanged += OnViewChanged;
			oldActiveDoc = IdeApp.Workbench.ActiveDocument;
			
			OnViewChanged (null, null);
		}
		
		void OnViewChanged (object sender, EventArgs args)
		{
			//only treat active ViewContent as a Toolbox consumer if it implements IToolboxConsumer
			currentConsumer = IdeApp.Workbench.ActiveDocument.ActiveView as IToolboxConsumer;			
			OnToolboxConsumerChanged (currentConsumer);
		}
		
		protected virtual void OnToolboxChanged ()
		{
			if (ToolboxChanged != null)
				ToolboxChanged (this, new EventArgs ()); 	
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
		
		public event EventHandler ToolboxChanged;
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
		
		private class ToolboxList : List<ItemToolboxNode>
		{
			
		}
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
