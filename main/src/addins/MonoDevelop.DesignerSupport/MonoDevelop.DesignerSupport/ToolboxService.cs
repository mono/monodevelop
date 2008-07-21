//
// ToolboxService.cs: Loads, stores and manipulates toolbox items.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2006 Michael Hutchinson
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

using MonoDevelop.DesignerSupport.Toolbox;

namespace MonoDevelop.DesignerSupport
{
	public class ToolboxService
	{
		readonly static string toolboxLoaderPath = "/MonoDevelop/DesignerSupport/ToolboxLoaders";
		readonly static string toolboxProviderPath = "/MonoDevelop/DesignerSupport/ToolboxProviders";

		ToolboxConfiguration config;
		List<IToolboxLoader> loaders = new List<IToolboxLoader> ();
		List<IToolboxDynamicProvider> dynamicProviders  = new List<IToolboxDynamicProvider> ();
		
		IToolboxConsumer currentConsumer;
		ItemToolboxNode selectedItem;
		
		internal ToolboxService ()
		{
			// Null check here because the service may be loaded in an external process
			if (IdeApp.Workbench != null)
				IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (onActiveDocChanged);
			
			AddinManager.AddExtensionNodeHandler (toolboxLoaderPath, OnLoaderExtensionChanged);
			AddinManager.AddExtensionNodeHandler (toolboxProviderPath, OnProviderExtensionChanged);
			
			if (IdeApp.Workbench != null)
				onActiveDocChanged (null, null);
		}
		
		static string ToolboxConfigFile {
			get { return System.IO.Path.Combine (PropertyService.ConfigPath, "Toolbox.xml"); }
		}
		
		#region Extension loading
		
		void OnLoaderExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				loaders.Add ((IToolboxLoader) args.ExtensionObject);
			else if (args.Change == ExtensionChange.Remove)
				loaders.Remove ((IToolboxLoader) args.ExtensionObject);
		}
		
		void OnProviderExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				IToolboxDynamicProvider dyProv = args.ExtensionObject as IToolboxDynamicProvider;
				if (dyProv != null) {
					dynamicProviders.Add ((IToolboxDynamicProvider) args.ExtensionObject);
					dyProv.ItemsChanged += OnProviderItemsChanged;
				}
				
				IToolboxDefaultProvider defProv = args.ExtensionObject as IToolboxDefaultProvider;
				if (defProv!= null) {
					RegisterDefaultToolboxProvider (defProv);
				}	
			}
			else if (args.Change == ExtensionChange.Remove) {
				IToolboxDynamicProvider dyProv = args.ExtensionObject as IToolboxDynamicProvider;
				if (dyProv != null) {
					dyProv.ItemsChanged -= OnProviderItemsChanged;
					dynamicProviders.Remove (dyProv);
				}
			}
			
			OnToolboxContentsChanged ();
		}
		
		void OnProviderItemsChanged (object s, EventArgs args)
		{
			OnToolboxContentsChanged ();
		}
		
		public void AddUserItems ()
		{
			ComponentSelectorDialog dlg = new ComponentSelectorDialog (currentConsumer);
			try {
				dlg.Fill ();
				dlg.Run ();
			}
			finally {
				dlg.Destroy ();
			}
		}
		
		void AddUserItems (string fileName)
		{			
			foreach (IToolboxLoader loader in loaders) {
				//check whether the loader can handle this file extension
				bool match = false;
				foreach (string ext in loader.FileTypes)
					if (fileName.EndsWith (ext))
						match = true;
				if (!match)
					continue;
				
				System.Collections.Generic.IList<ItemToolboxNode> loadedItems = loader.Load (fileName);
				
				//prevent user from loading the same items again
				foreach (ItemToolboxNode node in loadedItems) {
					bool found = false;
					foreach (ItemToolboxNode n in Configuration.ItemList) {
						if (node.Equals (n))
							found = true;
					}
					if (!found)
						Configuration.ItemList.Add (node);
				}
			}
		}
		
		public void RemoveUserItem (ItemToolboxNode node)
		{
			Configuration.ItemList.Remove (node);
			SaveConfiguration ();
			OnToolboxContentsChanged ();
		}
		
		public void RegisterDefaultToolboxProvider (IToolboxDefaultProvider provider)
		{
			string pname = provider.GetType().FullName;
			
			if (!Configuration.LoadedDefaultProviders.Contains (pname)) {
				Configuration.LoadedDefaultProviders.Add (pname);
				
				IEnumerable<ItemToolboxNode> newItems = provider.GetDefaultItems ();
				if (newItems != null)
					Configuration.ItemList.AddRange (newItems);
				
				IEnumerable<string> files = provider.GetDefaultFiles ();
				if (files != null) {
					foreach (string f in files)
						AddUserItems (f);
				}
				SaveConfiguration ();
				OnToolboxContentsChanged ();
			}
		}
		
		public void ResetToolboxContents ()
		{
			config = new ToolboxConfiguration ();
			foreach (object ob in AddinManager.GetExtensionObjects (toolboxProviderPath)) {
				IToolboxDefaultProvider provider = ob as IToolboxDefaultProvider;
				if (provider != null)
					RegisterDefaultToolboxProvider (provider);
			}
		}
		
		internal IList<ItemToolboxNode> GetFileItems (string fileName)
		{
			// Gets the list of items provided by a file.
			// It checks every toolbox loader, and loads the file in an external
			// process if required.
			
			List<ItemToolboxNode> items = new List<ItemToolboxNode> ();
			bool isolatedLoaded = false;
			foreach (IToolboxLoader loader in loaders) {
				if (!fileName.EndsWith (loader.FileTypes[0]))
					continue;
				
				try {
					if (loader.ShouldIsolate) {
						// Load the widgets in an external process. ExternalFileLoader.Load() class will
						// load the file using all loaders which require isolation.
						if (isolatedLoaded)
							continue;
						isolatedLoaded = true;
						ExternalFileLoader eloader = (ExternalFileLoader) Runtime.ProcessService.CreateExternalProcessObject (typeof(ExternalFileLoader),true);
						string s = eloader.LoadFile (fileName);
						XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
						ToolboxList list = (ToolboxList) ser.Deserialize (new StringReader (s), typeof(ToolboxList));
						items.AddRange (list);
					}
					else {
						IList<ItemToolboxNode> loadedItems = loader.Load (fileName);
						items.AddRange (loadedItems);
					}
				}
				catch (Exception ex) {
					// Ignore
					LoggingService.LogError (ex.ToString ());
				}
			}
			return items;
		}
		
		internal IList<ItemToolboxNode> InternalGetFileItems (string fileName)
		{
			// Method called by from the external process to get the items of
			// a file. Only loaders requiring isolation are used here, since
			// the other loaders are handled in the main process.
			
			List<ItemToolboxNode> items = new List<ItemToolboxNode> ();
			foreach (IToolboxLoader loader in loaders) {
				if (!fileName.EndsWith (loader.FileTypes[0]))
					continue;
				
				try {
					if (loader.ShouldIsolate) {
						IList<ItemToolboxNode> loadedItems = loader.Load (fileName);
						items.AddRange (loadedItems);
					}
				}
				catch {
					// Ignore
				}
			}
			return items;
		}
		
		~ToolboxService ()
		{
			AddinManager.RemoveExtensionNodeHandler (toolboxLoaderPath, OnLoaderExtensionChanged);
			AddinManager.RemoveExtensionNodeHandler (toolboxProviderPath, OnLoaderExtensionChanged);
		}
		
		#endregion
		
		#region Configuration
		
		ToolboxConfiguration Configuration {
			get {
				if (config == null) {
					try {
						if (File.Exists (ToolboxConfigFile))
							config = ToolboxConfiguration.LoadFromFile (ToolboxConfigFile);
						else
							config = new ToolboxConfiguration ();
					} catch (Exception ex) {
						// Ignore, just provide a default configuration
						LoggingService.LogError (ex.ToString ());
						config = new ToolboxConfiguration ();
					}
				}
				return config;
			}
		}
		
		void SaveConfiguration ()
		{
			if (config != null) {
				try {
					config.SaveContents (ToolboxConfigFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Error saving toolbox configuration.", ex);
				}
			}
		}
		
		#endregion
		
		#region Actions and properties
		
		public IList<ItemToolboxNode> GetCurrentToolboxItems ()
		{
			return GetToolboxItems (CurrentConsumer);
		}
		
		public Gtk.TargetEntry[] GetCurrentDragTargetTable ()
		{
			if (CurrentConsumer != null)
				return CurrentConsumer.DragTargets;
			else
				return new Gtk.TargetEntry [0];
		}
		
		
		public IList<ItemToolboxNode> GetToolboxItems (IToolboxConsumer consumer)
		{
			List<ItemToolboxNode> arr = new List<ItemToolboxNode> ();
			Hashtable nodes = new Hashtable ();
			
			if (consumer != null) {
				//get the user items
				foreach (ItemToolboxNode node in Configuration.ItemList)
					//hide unknown nodes -- they're only there because deserialisation has failed, so they won't actually be usable
					if ( !(node is UnknownToolboxNode))
						if (!nodes.Contains (node) && IsSupported (node, consumer)) {
							arr.Add (node);
							nodes.Add (node, node);
						}
				
				//merge the list of dynamic items from each provider
				foreach (IToolboxDynamicProvider prov in dynamicProviders) {
					IEnumerable<ItemToolboxNode> dynItems = prov.GetDynamicItems (consumer);
					if (dynItems != null) {
						foreach (ItemToolboxNode node in dynItems) {
							if (!nodes.Contains (node)) {
								arr.Add (node);
								nodes.Add (node, node);
							}
						}
					}
				}
			}
			//arr.Sort ();
			
			return arr;
		}
		
		internal void UpdateUserItems (IEnumerable<ItemToolboxNode> newItems)
		{
			Configuration.ItemList.Clear ();
			Configuration.ItemList.AddRange (newItems);
			SaveConfiguration ();
			OnToolboxContentsChanged ();
		}
		
		internal IEnumerable<ItemToolboxNode> UserItems {
			get { return Configuration.ItemList; }
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
		
		public void DragSelectedItem (Gtk.Widget source, Gdk.DragContext ctx)
		{
			if ((CurrentConsumer == null) || (selectedItem == null))
				return;
			
			try {
				CurrentConsumer.DragItem (selectedItem, source, ctx);
				OnToolboxUsed (CurrentConsumer, selectedItem);
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Error dragging toolbox item.", ex);
				//run this dialog on a timeout so it doesn't block the drag completing
				GLib.Timeout.Add (100, delegate {
					MonoDevelop.Core.Gui.MessageService.ShowException (ex, "Error dragging toolbox item.");
					return false;
				});
			}
		}
		
		#endregion
		
		#region Change notification
		
		Document oldActiveDoc =  null;
		Project oldProject = null;
		bool configChanged;
		IToolboxDynamicProvider viewProvider = null;
		
		void onActiveDocChanged (object o, EventArgs e)
		{
			if (oldActiveDoc != null)
				oldActiveDoc.ViewChanged -= OnViewChanged;
			if (oldProject != null)
				oldProject.Modified -= onProjectConfigChanged;
			
			oldActiveDoc = IdeApp.Workbench.ActiveDocument;
			oldProject = oldActiveDoc != null?
				oldActiveDoc.Project : null;
			
			if (oldActiveDoc != null)
				oldActiveDoc.ViewChanged += OnViewChanged;					
			if (oldProject != null)
				oldProject.Modified += onProjectConfigChanged;
			
			OnViewChanged (null, null);
		}
		
		void OnViewChanged (object sender, EventArgs args)
		{
			if (viewProvider != null) {
				this.dynamicProviders.Remove (viewProvider);
				viewProvider.ItemsChanged -= OnProviderItemsChanged;
			}
			
			//only treat active ViewContent as a Toolbox consumer if it implements IToolboxConsumer
			if (IdeApp.Workbench.ActiveDocument != null) {
				CurrentConsumer = IdeApp.Workbench.ActiveDocument.ActiveView as IToolboxConsumer;
				viewProvider    = IdeApp.Workbench.ActiveDocument.ActiveView as IToolboxDynamicProvider;
				if (viewProvider != null)  {
					this.dynamicProviders.Add (viewProvider);
					viewProvider.ItemsChanged += OnProviderItemsChanged;
					OnToolboxContentsChanged ();
				}
			} else {
				CurrentConsumer = null;
				viewProvider = null;
			}
		}
		
		//changing project settings could cause the toolbox contents to change
		void onProjectConfigChanged (object sender, EventArgs args)
		{
			if (configChanged)
				return;
			
			//project usually broadcasts many changes at once, so try to group them
			configChanged = true;
			GLib.Timeout.Add (500, delegate {
				configChanged = false;
				OnToolboxContentsChanged ();
				return false;
			});
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
		
		public bool IsSupported (ItemToolboxNode node, IToolboxConsumer consumer)
		{
			if (consumer is ICustomFilteringToolboxConsumer)
				return ((ICustomFilteringToolboxConsumer)consumer).SupportsItem (node);
			
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
		private bool FilterPermitted (ItemToolboxNode node, ToolboxItemFilterAttribute desFa, 
		    ICollection<ToolboxItemFilterAttribute> filterAgainst, IToolboxConsumer consumer)
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
		
		internal ComponentIndex GetComponentIndex (IProgressMonitor monitor)
		{
			// Returns an index of all components that can be added to the toolbox.
			
			ComponentIndex index = ComponentIndex.Load ();
			
			// Get the list of assemblies that need to be updated
			
			Hashtable files = new Hashtable ();
			List<ComponentIndexFile> toupdate = new List<ComponentIndexFile> ();
			List<ComponentIndexFile> todelete = new List<ComponentIndexFile> ();
			foreach (ComponentIndexFile ia in index.Files) {
				files [ia.FileName] = ia.FileName;
				if (!File.Exists (ia.FileName))
					todelete.Add (ia);
				if (ia.NeedsUpdate)
					toupdate.Add (ia);
			}
			
			// Look for new assemblies
			
			foreach (string filePath in Runtime.SystemAssemblyService.GetAssemblyPaths (ClrVersion.Default)) {
				if (!files.Contains (filePath)) {
					ComponentIndexFile c = new ComponentIndexFile (filePath);
					index.Files.Add (c);
					toupdate.Add (c);
				}
			}
			
			foreach (ComponentIndexFile ia in todelete) {
				index.Files.Remove (ia);
			}
			
			if (toupdate.Count > 0) {
				monitor.BeginTask (GettextCatalog.GetString ("Looking for components..."), toupdate.Count);
				foreach (ComponentIndexFile ia in toupdate) {
					ia.Update ();
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
			
			if (toupdate.Count > 0 || todelete.Count > 0)
				index.Save ();
			
			return index;
		}
		
		#endregion
	}
	
	[Serializable]
	internal class ComponentIndex
	{
		List<ComponentIndexFile> files = new List<ComponentIndexFile> ();
		
		static string ToolboxIndexFile {
			get { return Path.Combine (PropertyService.ConfigPath, "ToolboxIndex.xml"); }
		}
		
		internal static ComponentIndex Load ()
		{
			if (!File.Exists (ToolboxIndexFile))
				return new ComponentIndex ();
			
			XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
			try {
				using (StreamReader sr = new StreamReader (ToolboxIndexFile)) {
					return (ComponentIndex) ser.Deserialize (sr, typeof(ComponentIndex));
				}
			}
			catch (Exception ex) {
				// Ignore exceptions
				LoggingService.LogError (ex.ToString ());
				return new ComponentIndex ();
			}
		}
		
		public void Save ()
		{
			XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
			try {
				using (StreamWriter sw = new StreamWriter (ToolboxIndexFile)) {
					ser.Serialize (sw, this, typeof(ComponentIndex));
				}
			}
			catch (Exception ex) {
				// Ignore exceptions
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		public ComponentIndexFile AddFile (string file)
		{
			ComponentIndexFile cf = new ComponentIndexFile (file);
			cf.Update ();
			if (cf.Components.Count == 0)
				return null;
			else {
				files.Add (cf);
				return cf;
			}
		}
		
		[ItemProperty]
		public List<ComponentIndexFile> Files {
			get { return files; }
		}
	}
	
	[Serializable]
	internal class ComponentIndexFile
	{
		[ItemProperty]
		string fileName;
		
		[ItemProperty]
		string location;
		
		[ItemProperty]
		string timestamp;
		
		[ItemProperty ("Components")]
		List<ItemToolboxNode> entries = new List<ItemToolboxNode> ();
		
		public ComponentIndexFile ()
		{
		}
		
		public ComponentIndexFile (string fileName)
		{
			this.fileName = fileName;
		}
		
		public bool NeedsUpdate {
			get {
				if (File.Exists (fileName))
					return GetFileTimestamp (fileName) != timestamp;
				else
					return false;
			}
		}
		
		public string Location {
			get {
				return location != null ? location : fileName;
			}
		}

		public string FileName {
			get {
				return fileName;
			}
		}

		public string Name {
			get {
				return Path.GetFileNameWithoutExtension (fileName);
			}
		}

		public List<ItemToolboxNode> Components {
			get {
				return entries;
			}
		}

		public void Update ()
		{
			IList<ItemToolboxNode> items = DesignerSupport.Service.ToolboxService.GetFileItems (fileName);
			
			location = null;
			this.timestamp = GetFileTimestamp (fileName);
			entries = new List<ItemToolboxNode> (items);
			
			// Set the location field only if this is a widget library
			if (entries.Count > 0 && Runtime.SystemAssemblyService.GetPackageFromPath (fileName) != null)
				location = Runtime.SystemAssemblyService.GetAssemblyFullName (fileName);
		}
		
		string GetFileTimestamp (string file)
		{
			DateTime tim = File.GetLastWriteTime (file);
			return System.Xml.XmlConvert.ToString (tim, System.Xml.XmlDateTimeSerializationMode.Utc);
		}
	}
	
	[MonoDevelop.Core.Execution.AddinDependency ("MonoDevelop.DesignerSupport")] 
	class ExternalFileLoader: RemoteProcessObject
	{
		public string LoadFile (string file)
		{
			Gtk.Application.Init ();
			ToolboxService service = new ToolboxService ();
			XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
			ToolboxList tl = new ToolboxList ();
			IList<ItemToolboxNode> list = service.InternalGetFileItems (file);
			tl.AddRange (list);
			StringWriter sw = new StringWriter ();
			ser.Serialize (sw, tl);
			return sw.ToString ();
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
