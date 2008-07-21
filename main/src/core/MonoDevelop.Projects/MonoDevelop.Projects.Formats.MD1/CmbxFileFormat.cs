//
// CmbxFileFormat.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Formats.MD1
{
	interface ICombineReader {
		object ReadCombine (XmlReader reader);
	}
	
	class CombineReaderV2: XmlConfigurationReader, ICombineReader
	{
		DataSerializer serializer;
		ArrayList entries = new ArrayList ();
		IProgressMonitor monitor;
		Type objectType;
		string baseFile;
		
		public CombineReaderV2 (DataSerializer serializer, IProgressMonitor monitor, Type objectType)
		{
			this.serializer = serializer;
			this.objectType = objectType;
			baseFile = serializer.SerializationContext.BaseFile;
			this.monitor = monitor;
		}
		
		public object ReadCombine (XmlReader reader)
		{
			DataItem data = (DataItem) Read (reader);
			
			// Both combine and SolutionFolder use the same element name, but the types are different
			if (data.Name == "Combine" && objectType == typeof(SolutionFolder))
				data.Name = "SolutionFolder";
			
			SolutionFolder folder;
			IExtendedDataItem obj = (IExtendedDataItem) serializer.CreateInstance (objectType, data);
			Solution sol = obj as Solution;
			if (sol != null) {
				folder = sol.RootFolder;
				sol.ConvertToFormat (MD1ProjectService.FileFormat, false);
				sol.FileName = serializer.SerializationContext.BaseFile;
				folder.ExtendedProperties ["FileName"] = serializer.SerializationContext.BaseFile;
			}
			else {
				folder = (SolutionFolder) obj;
				obj.ExtendedProperties ["FileName"] = serializer.SerializationContext.BaseFile;
			}
			
			// The folder entries must be added before deserializing the folder
			// since other folder members depend on it
			
			foreach (SolutionItem ce in entries)
				folder.Items.Add (ce);
			
			serializer.Deserialize (obj, data);
			
			if (sol != null) {
				CreateSolutionConfigurations (sol);
				LoadStartupMode (sol);
			}
			
			
			return obj;
		}
		
		protected override DataNode ReadChild (XmlReader reader, DataItem parent)
		{
			if (reader.LocalName == "Entries") {
				if (reader.IsEmptyElement) { reader.Skip(); return null; }
				string basePath = Path.GetDirectoryName (baseFile);
				reader.ReadStartElement ();
				
				ArrayList files = new ArrayList ();
				while (MoveToNextElement (reader)) {
					string nodefile = reader.GetAttribute ("filename");
					nodefile = FileService.RelativeToAbsolutePath (basePath, nodefile);
					files.Add (nodefile);
					reader.Skip ();
				}
				
				monitor.BeginTask (GettextCatalog.GetString ("Loading solution: {0}", baseFile), files.Count);
				try {
					foreach (string nodefile in files) {
						try {
							if (Path.GetExtension (nodefile).ToLower () == ".mds") {
								entries.Add (ReadSolutionFolder (nodefile, monitor));
							}
							else {
								SolutionEntityItem entry = (SolutionEntityItem) Services.ProjectService.ReadSolutionItem (monitor, nodefile);
								entries.Add (entry);
							}
						} catch (Exception ex) {
							UnknownSolutionItem entry = new UnknownSolutionItem ();
							entry.FileName = nodefile;
							entry.LoadError = ex.Message;
							entries.Add (entry);
							monitor.ReportError (GettextCatalog.GetString ("Could not load item: {0}", nodefile), ex);
						}
						monitor.Step (1);
					}
				} finally {
					monitor.EndTask ();
				}
				
				reader.ReadEndElement ();
				return null;
			}
			
			return base.ReadChild (reader, parent);
		}
		
		void CreateSolutionConfigurations (Solution sol)
		{
			CombineConfigurationSet configs = (CombineConfigurationSet) sol.ExtendedProperties ["Configurations"];
			foreach (CombineConfiguration config in configs.Configurations) {
				SolutionConfiguration sconf = config.SolutionConfiguration ?? new SolutionConfiguration (config.Name);
				sol.Configurations.Add (sconf);
			}
			
			foreach (SolutionEntityItem item in sol.GetAllSolutionItems<SolutionEntityItem> ()) {
				
				List<IExtendedDataItem> chain = new List<IExtendedDataItem> ();
				SolutionItem it = item;
				while (it != null) {
					chain.Insert (0, it);
					it = it.ParentFolder;
				}
				chain [0] = sol;
				
				foreach (SolutionConfiguration sconfig in sol.Configurations) {
					SolutionConfigurationEntry se = sconfig.AddItem (item);
					string itemConfig = FindItemConfiguration (chain, sconfig.Id);
					if (itemConfig != null) {
						se.Build = true;
						se.ItemConfiguration = itemConfig;
					}
					else
						se.Build = false;
				}
			}
			
			sol.DefaultConfigurationId = configs.Active;
		}
		
		void LoadStartupMode (Solution sol)
		{
			// Recursively look for startup items. If there is more than one,
			// it means it is a multi startup solution.
			List<SolutionEntityItem> items = new List<SolutionEntityItem> ();
			FindStartupItems (sol, sol.RootFolder, items);
			
			if (items.Count == 1) {
				sol.SingleStartup = true;
				sol.StartupItem = items [0];
			} else if (items.Count > 1) {
				sol.SingleStartup = false;
				sol.MultiStartupItems.AddRange (items);
			}
		}
		
		void FindStartupItems (Solution sol, SolutionFolder folder, List<SolutionEntityItem> items)
		{
			CombineStartupMode startupMode = (CombineStartupMode) folder.ExtendedProperties ["StartMode"];
			if (startupMode != null) {
				if (startupMode.SingleStartup) {
					SolutionItem it = FindItemByName (folder, startupMode.StartEntryName);
					if (it is SolutionFolder)
						FindStartupItems (sol, (SolutionFolder)it, items);
					else if (it is SolutionEntityItem)
						items.Add ((SolutionEntityItem) it);
					return;
				} else {
					foreach (CombineStartupEntry cse in startupMode.Entries) {
						if (cse.Type == "Execute") {
							SolutionItem it = FindItemByName (folder, cse.Entry);
							if (it is SolutionFolder)
								FindStartupItems (sol, (SolutionFolder)it, items);
							else if (it is SolutionEntityItem)
								items.Add ((SolutionEntityItem) it);
						}
					}
				}
			}
		}
		
		SolutionItem FindItemByName (SolutionFolder folder, string name)
		{
			foreach (SolutionItem it in folder.Items)
				if (it.Name == name)
					return it;
			return null;
		}
		
		string FindItemConfiguration (List<IExtendedDataItem> chain, string configId)
		{
			for (int n=0; n < chain.Count - 1; n++) {
				CombineConfigurationSet configs = (CombineConfigurationSet) chain[n].ExtendedProperties ["Configurations"];
				if (configs == null)
					return null;
				SolutionItem item = (SolutionItem) chain [n+1];
				CombineConfiguration combineConfig = configs.GetConfig (configId);
				if (combineConfig == null) {
					monitor.ReportWarning ("Configuration '" + configId + "' not found in solution item '" + item.Name + "'.");
					return null;
				}
				string mappedConfigId = combineConfig.GetMappedConfig (item.Name);
				if (mappedConfigId == null)
					return null;
				if (mappedConfigId != string.Empty)
					configId = mappedConfigId;
			}
			return configId;
		}
	
		object ReadSolutionFolder (string file, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			reader.MoveToContent ();
			
			string version = reader.GetAttribute ("version");
			if (version == null)
				version = reader.GetAttribute ("fileversion");
			
			DataSerializer serializer = new DataSerializer (MD1ProjectService.DataContext, file);
			
			try {
				if (version != "2.0")
					throw new UnknownProjectVersionException (file, version);
				ICombineReader combineReader = new CombineReaderV2 (serializer, monitor, typeof(SolutionFolder));
				return combineReader.ReadCombine (reader);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load solution: {0}", file), ex);
				throw;
			} finally {
				reader.Close ();
			}
		}
	}
	
	class CombineWriterV2: XmlConfigurationWriter
	{
		SolutionFolder folder;
		DataSerializer serializer;
		IProgressMonitor monitor;
		Type objectType;
		
		public CombineWriterV2 (DataSerializer serializer, IProgressMonitor monitor, Type objectType)
		{
			this.serializer = serializer;
			this.monitor = monitor;
			this.objectType = objectType;
		}

		public void WriteCombine (XmlWriter writer, object item)
		{
			if (item is Solution) {
				Solution sol = (Solution) item;
				folder = sol.RootFolder;
				CreateCombineConfigurations (sol.RootFolder);
				CreateStartupModes (sol);
			}
			else
				folder = (SolutionFolder) item;
			
			DataItem data = (DataItem) serializer.Serialize (item, objectType);
			
			// Both combine and SolutionFolder use the same element name, but the types are different
			if (data.Name == "SolutionFolder" && objectType == typeof(SolutionFolder))
				data.Name = "Combine";
			
			Write (writer, data);
		}
		
		void CreateStartupModes (Solution sol)
		{
			// Initialize the startup modes
			ReadOnlyCollection<SolutionFolder> folders = sol.GetAllSolutionItems<SolutionFolder> ();
			foreach (SolutionFolder folder in folders) {
				CombineStartupMode startupMode = new CombineStartupMode ();
				startupMode.SingleStartup = true;
				startupMode.StartEntryName = null;
				foreach (SolutionItem it in folder.Items)
					startupMode.AddEntry (it.Name);
				folder.ExtendedProperties ["StartMode"] = startupMode;
			}
			
			// Get the list of startup items
			IEnumerable<SolutionEntityItem> items;
			if (sol.SingleStartup)
				items = new SolutionEntityItem [] { sol.StartupItem };
			else
				items = sol.MultiStartupItems;

			// Setup the startup modes
			foreach (SolutionEntityItem it in items) {
				if (it == null)
					continue;
				SolutionFolder folder = it.ParentFolder;
				SolutionItem prevItem = it;
				while (folder != null) {
					CombineStartupMode startupMode = (CombineStartupMode) folder.ExtendedProperties ["StartMode"];
					if (startupMode.SingleStartup) {
						// If a start entry is already set, convert to multi startup mode
						if (startupMode.StartEntryName == null)
							startupMode.StartEntryName = prevItem.Name;
						else if (startupMode.StartEntryName != prevItem.Name) {
							startupMode.SingleStartup = false;
							startupMode.MakeExecutable (startupMode.StartEntryName);
							startupMode.MakeExecutable (prevItem.Name);
						}
					} else {
						// Already multi startup, just add the new item
						startupMode.MakeExecutable (prevItem.Name);
					}
					prevItem = folder;
					folder = folder.ParentFolder;
				}
			}
			
			// Set the startup item for folders which don't have one
			foreach (SolutionFolder folder in folders) {
				CombineStartupMode startupMode = (CombineStartupMode) folder.ExtendedProperties ["StartMode"];
				if (startupMode.SingleStartup && startupMode.StartEntryName == null) {
					if (folder.Items.Count > 0)
						startupMode.StartEntryName = folder.Items [0].Name;
				}
			}
		}
		
		void CreateCombineConfigurations (SolutionFolder folder)
		{
			IDictionary extendedProperties = folder.ExtendedProperties;
			
			CombineConfigurationSet configs = CreateCombineConfigurationSet (folder.ParentSolution, folder.IsRoot);
			configs.Active = folder.ParentSolution.DefaultConfigurationId;
			
			extendedProperties ["Configurations"] = configs;

			foreach (SolutionItem it in folder.Items) {
				
				if (it is SolutionFolder) {
					foreach (SolutionConfiguration conf in folder.ParentSolution.Configurations) {
						CombineConfiguration cc = configs.GetConfig (conf.Id);
						CombineConfigurationEntry ce = new CombineConfigurationEntry (it.Name, true, conf.Id);
						cc.Entries.Add (ce);
					}
					CreateCombineConfigurations ((SolutionFolder) it);
				}
				else if (it is SolutionEntityItem) {
					SolutionEntityItem sit = (SolutionEntityItem) it;
					foreach (SolutionConfiguration conf in folder.ParentSolution.Configurations) {
						CombineConfiguration cc = configs.GetConfig (conf.Id);
						SolutionConfigurationEntry sce = conf.GetEntryForItem (sit);
						CombineConfigurationEntry ce = new CombineConfigurationEntry (it.Name, sce.Build, sce.ItemConfiguration);
						cc.Entries.Add (ce);
					}
				}
			}
		}
		
		CombineConfigurationSet CreateCombineConfigurationSet (Solution sol, bool isRoot)
		{
			CombineConfigurationSet cset = new CombineConfigurationSet ();
			foreach (SolutionConfiguration conf in sol.Configurations) {
				CombineConfiguration cc = new CombineConfiguration ();
				cc.Name = conf.Id;
				if (isRoot)
					cc.SolutionConfiguration = conf;
				cset.Configurations.Add (cc);
			}
			return cset;
		}
		
		protected override void WriteChildren (XmlWriter writer, DataItem item)
		{
			base.WriteChildren (writer, item);

			writer.WriteStartElement ("Entries");
			foreach (SolutionItem entry in folder.Items) {
				writer.WriteStartElement ("Entry");
				
				string baseDir = Path.GetDirectoryName (serializer.SerializationContext.BaseFile);
				string fname = null;
				
				if (entry is SolutionFolder) {
					SolutionFolder cfolder = (SolutionFolder) entry;
					fname = cfolder.ExtendedProperties ["FileName"] as string;
					if (fname == null) {
						// Guess a good file name for the mds file.
						if (Directory.Exists (Path.Combine (baseDir, cfolder.Name)))
							fname = Path.Combine (Path.Combine (baseDir, cfolder.Name), cfolder.Name + ".mds");
						else
							fname = Path.Combine (baseDir, cfolder.Name + ".mds");
					}
				}
				else if (entry is SolutionEntityItem) {
					fname = ((SolutionEntityItem)entry).FileName;
				}
				if (fname == null)
					throw new InvalidOperationException ("Don't know how to save item of type " + entry.GetType ());
				
				string relfname = FileService.AbsoluteToRelativePath (baseDir, fname);
				writer.WriteAttributeString ("filename", relfname);
				writer.WriteEndElement ();
				if (entry is SolutionEntityItem)
					((SolutionEntityItem)entry).Save (monitor);
				else
					WriteSolutionFolder ((SolutionFolder) entry, fname, monitor);
			}
			writer.WriteEndElement ();
		}
		
		void WriteSolutionFolder (SolutionFolder folder, string file, IProgressMonitor monitor)
		{
			StreamWriter sw = new StreamWriter (file);
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", file), 1);
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				DataSerializer serializer = new DataSerializer (MD1ProjectService.DataContext, file);
				CombineWriterV2 combineWriter = new CombineWriterV2 (serializer, monitor, typeof(SolutionFolder));
				combineWriter.WriteCombine (tw, folder);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save solution: {0}", file), ex);
				throw;
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}
	}
}
