// 
// WorkspaceItem.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Extensions;
using Mono.Addins;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects.MD1;

namespace MonoDevelop.Projects
{
	public abstract class WorkspaceItem : WorkspaceObject, IWorkspaceFileObject
	{
		Workspace parentWorkspace;
		FilePath fileName;
		PropertyBag userProperties;
		FileStatusTracker<WorkspaceItemEventArgs> fileStatusTracker;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		FilePath baseDirectory;

		internal WorkspaceItem ()
		{
			userProperties = new PropertyBag ();
			fileStatusTracker = new FileStatusTracker<WorkspaceItemEventArgs> (this, OnReloadRequired, new WorkspaceItemEventArgs (this));
		}

		public Workspace ParentWorkspace {
			get { return parentWorkspace; }
			internal set {
				parentWorkspace = value; 
				ParentObject = value;
			}
		}
		
		// User properties are only loaded when the project is loaded in the IDE.
		public virtual PropertyBag UserProperties {
			get {
				return userProperties; 
			}
		}
		
		public new string Name {
			get {
				return base.Name;
			}
			set {
				AssertMainThread ();
				if (fileName.IsNullOrEmpty)
					SetLocation (FilePath.Empty, value);
				else {
					FilePath dir = fileName.ParentDirectory;
					string ext = fileName.Extension;
					FileName = dir.Combine (value) + ext;
				}
			}
		}

		[ThreadSafe]
		protected override string OnGetName ()
		{
			var fname = fileName;
			if (fname.IsNullOrEmpty)
				return string.Empty;
			else
				return fname.FileNameWithoutExtension;
		}
		
		public virtual FilePath FileName {
			[ThreadSafe] get {
				return fileName;
			}
			set {
				AssertMainThread ();
				string oldName = Name;
				fileName = value;
				if (oldName != Name)
					OnNameChanged (new WorkspaceItemRenamedEventArgs (this, oldName, Name));
				NotifyModified ();
			}
		}

		public virtual void SetLocation (FilePath baseDirectory, string name)
		{
			// Add a dummy extension to the file name.
			// It will be replaced by the correct one, depending on the format
			FileName = baseDirectory.Combine (name) + ".x";
		}
		
		public new FilePath BaseDirectory {
			get {
				return base.BaseDirectory;
			}
			set {
				AssertMainThread ();
				if (!value.IsNull && !FileName.IsNull && FileName.ParentDirectory.FullPath == value.FullPath)
					baseDirectory = null;
				else if (value.IsNullOrEmpty)
					baseDirectory = null;
				else
					baseDirectory = value.FullPath;
				NotifyModified ();
			}
		}

		[ThreadSafe]
		protected sealed override string OnGetBaseDirectory ()
		{
			var dir = baseDirectory;
			if (dir.IsNull)
				return FileName.ParentDirectory.FullPath;
			else
				return dir;
		}
		
		[ThreadSafe]
		protected sealed override string OnGetItemDirectory ()
		{
			return FileName.ParentDirectory.FullPath;
		}
		
		protected override void OnExtensionChainInitialized ()
		{
			itemExtension = ExtensionChain.GetExtension<WorkspaceItemExtension> ();
			base.OnExtensionChainInitialized ();
		}

		WorkspaceItemExtension itemExtension;

		WorkspaceItemExtension ItemExtension {
			get {
				if (itemExtension == null)
					AssertExtensionChainCreated ();
				return itemExtension;
			}
		}

		protected override IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			foreach (var e in base.CreateDefaultExtensions ())
				yield return e;
			yield return new DefaultWorkspaceItemExtension ();
		}

		[ThreadSafe]
		public IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			return ItemExtension.GetItemFiles (includeReferencedFiles);
		}
		
		[ThreadSafe]
		protected virtual IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			if (!FileName.IsNullOrEmpty)
				yield return FileName;
		}

		[ThreadSafe]
		public virtual bool ContainsItem (WorkspaceObject obj)
		{
			return this == obj;
		}
		
		[ThreadSafe]
		public virtual IEnumerable<Project> GetProjectsContainingFile (FilePath fileName)
		{
			yield break;
		}
		
		[ThreadSafe]
		public virtual ReadOnlyCollection<string> GetConfigurations ()
		{
			return new ReadOnlyCollection<string> (new string [0]);
		}

		internal void SetParentWorkspace (Workspace workspace)
		{
			AssertMainThread ();
			parentWorkspace = workspace;
		}

		protected virtual void OnConfigurationsChanged ()
		{
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, EventArgs.Empty);
			if (ParentWorkspace != null)
				ParentWorkspace.OnConfigurationsChanged ();
		}

		public Task SaveAsync (FilePath fileName, ProgressMonitor monitor)
		{
			AssertMainThread ();
			FileName = fileName;
			return SaveAsync (monitor);
		}

		[ThreadSafe]
		public Task SaveAsync (ProgressMonitor monitor)
		{
			return Runtime.RunInMainThread (async delegate {
				using (await WriteLock ()) {
					foreach (var f in GetItemFiles (false))
						FileService.RequestFileEdit (f);
					try {
						fileStatusTracker.BeginSave ();
						await ItemExtension.Save (monitor);
						await OnSaveUserProperties (); // Call the virtual to avoid the lock
						OnSaved (new WorkspaceItemEventArgs (this));
				
						// Update save times
					} finally {
						fileStatusTracker.EndSave ();
					}
				}
				FileService.NotifyFileChanged (FileName);
			});
		}

		protected internal virtual Task OnSave (ProgressMonitor monitor)
		{
			return Task.FromResult (0);
		}

		public virtual bool NeedsReload {
			get {
				return fileStatusTracker.NeedsReload;
			}
			set {
				fileStatusTracker.NeedsReload = value;
			}
		}
		
		[ThreadSafe]
		public bool ItemFilesChanged {
			get {
				return fileStatusTracker.ItemFilesChanged;
			}
		}

		[ThreadSafe]
		internal protected virtual bool OnGetSupportsExecute ()
		{
			return true;
		}

		[ThreadSafe]
		public IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			yield break;
		}

		protected bool Loading { get; private set; }

		/// <summary>
		/// Called when a load operation for this item has started
		/// </summary>
		internal protected virtual Task OnBeginLoad ()
		{
			Loading = true;
			return LoadUserProperties ();
		}
		
		/// <summary>
		/// Called when a load operation for this item has finished
		/// </summary>
		internal protected virtual Task OnEndLoad ()
		{
			Loading = false;
			fileStatusTracker.ResetLoadTimes ();
			return Task.FromResult (true);
		}

		/// <summary>
		/// Called when an item has been fully created and/or loaded
		/// </summary>
		/// <remarks>>
		/// This method is invoked when all operations required for creating or loading this item have finished.
		/// If the item is being created in memory, this method will be called just after OnExtensionChainInitialized.
		/// If the item is being loaded from a file, it will be called after OnEndLoad.
		/// If the item is being created from a template, it will be called after InitializeNew
		/// </remarks>
		protected virtual void OnItemReady ()
		{
		}

		internal void NotifyItemReady ()
		{
			ItemExtension.ItemReady ();
			OnItemReady ();
		}

		public async Task LoadUserProperties ()
		{
			using (await ReadLock ())
				await OnLoadUserProperties ();
		}

		protected virtual Task OnLoadUserProperties ()
		{
			userProperties.Dispose ();
			userProperties = new PropertyBag ();

			string oldPreferencesFileName = GetLegacyPreferencesFileName ();
			string preferencesFileName = GetPreferencesFileName ();

			return Task.Run (() => {
				MigrateLegacyUserPreferencesFile (oldPreferencesFileName, preferencesFileName);

				if (!File.Exists (preferencesFileName))
					return;

				using (var streamReader = new StreamReader (preferencesFileName)) {
					XmlTextReader reader = new XmlTextReader (streamReader);
					try {
						reader.MoveToContent ();
						if (reader.LocalName != "Properties")
							return;

						XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
						ser.SerializationContext.BaseFile = preferencesFileName;
						userProperties = (PropertyBag)ser.Deserialize (reader, typeof(PropertyBag));
					} catch (Exception e) {
						LoggingService.LogError ("Exception while loading user solution preferences.", e);
						return;
					} finally {
						reader.Close ();
					}
				}
			});
		}
		
		public async Task SaveUserProperties ()
		{
			using (await WriteLock ())
				await OnSaveUserProperties ();
		}

		protected virtual Task OnSaveUserProperties ()
		{
			FilePath file = GetPreferencesFileName ();
			var userProps = userProperties;

			return Task.Run (() => {
				if (userProps == null || userProps.IsEmpty) {
					if (File.Exists (file))
						File.Delete (file);
					return;
				}
			
				XmlTextWriter writer = null;
				try {
					Directory.CreateDirectory (file.ParentDirectory);

					writer = new XmlTextWriter (file, System.Text.Encoding.UTF8);
					writer.Formatting = Formatting.Indented;
					XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
					ser.SerializationContext.BaseFile = file;
					ser.Serialize (writer, userProps, typeof(PropertyBag));
				} catch (Exception e) {
					LoggingService.LogWarning ("Could not save solution preferences: " + file, e);
				} finally {
					if (writer != null)
						writer.Close ();
				}
			});
		}
		
		string GetLegacyPreferencesFileName ()
		{
			return FileName.ChangeExtension (".userprefs");
		}

		static void MigrateLegacyUserPreferencesFile (string legacyPreferencesFileName, FilePath preferencesFileName)
		{
			if (!File.Exists (legacyPreferencesFileName))
				return;

			if (!File.Exists (preferencesFileName)) {
				Directory.CreateDirectory (preferencesFileName.ParentDirectory);

				File.Move (legacyPreferencesFileName, preferencesFileName);
			}
		}

		internal string GetPreferencesFileName ()
		{
			return BaseDirectory.Combine (".vs", Name, "xs", "UserPrefs.xml");
		}

		public virtual StringTagModelDescription GetStringTagModelDescription ()
		{
			StringTagModelDescription model = new StringTagModelDescription ();
			model.Add (GetType ());
			return model;
		}
		
		public virtual StringTagModel GetStringTagModel ()
		{
			StringTagModel source = new StringTagModel ();
			source.Add (this);
			return source;
		}
		
		public FilePath GetAbsoluteChildPath (FilePath relPath)
		{
			return relPath.ToAbsolute (BaseDirectory);
		}

		public FilePath GetRelativeChildPath (FilePath absPath)
		{
			return absPath.ToRelative (BaseDirectory);
		}
		
		protected override void OnDispose ()
		{
			if (userProperties != null)
				userProperties.Dispose ();
			base.OnDispose ();
		}
		
		protected virtual void OnNameChanged (WorkspaceItemRenamedEventArgs e)
		{
			fileStatusTracker.ResetLoadTimes ();
			NotifyModified ();
			if (NameChanged != null)
				NameChanged (this, e);
		}

		protected void NotifyModified ()
		{
			OnModified (new WorkspaceItemEventArgs (this));
		}
		
		protected virtual void OnModified (WorkspaceItemEventArgs args)
		{
			if (Modified != null)
				Modified (this, args);
		}
		
		protected virtual void OnSaved (WorkspaceItemEventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		protected virtual void OnReloadRequired (WorkspaceItemEventArgs args)
		{
			fileStatusTracker.FireReloadRequired (args);
		}
		
		public event EventHandler ConfigurationsChanged;
		public event EventHandler<WorkspaceItemRenamedEventArgs> NameChanged;
		public event EventHandler<WorkspaceItemEventArgs> Modified;
		public event EventHandler<WorkspaceItemEventArgs> Saved;
		
/*		public event EventHandler<WorkspaceItemEventArgs> ReloadRequired {
			add {
				fileStatusTracker.ReloadRequired += value;
			}
			remove {
				fileStatusTracker.ReloadRequired -= value;
			}
		}*/

		internal class DefaultWorkspaceItemExtension: WorkspaceItemExtension
		{
			internal protected override IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles)
			{
				return Item.OnGetItemFiles (includeReferencedFiles);
			}

			internal protected override Task Save (ProgressMonitor monitor)
			{
				return Item.OnSave (monitor);
			}
		}
	}
	
	class FileStatusTracker<TEventArgs> where TEventArgs:EventArgs
	{
		Dictionary<string,DateTime> lastSaveTime;
		Dictionary<string,DateTime> reloadCheckTime;
		bool savingFlag;
//		bool needsReload;
//		List<FileSystemWatcher> watchers;
//		Action<TEventArgs> onReloadRequired;
		TEventArgs eventArgs;
		EventHandler<TEventArgs> reloadRequired;
		
		IWorkspaceFileObject item;
		
		public FileStatusTracker (IWorkspaceFileObject item, Action<TEventArgs> onReloadRequired, TEventArgs eventArgs)
		{
			this.item = item;
			this.eventArgs = eventArgs;
//			this.onReloadRequired = onReloadRequired;
			lastSaveTime = new Dictionary<string,DateTime> ();
			reloadCheckTime = new Dictionary<string,DateTime> ();
			savingFlag = false;
			reloadRequired = null;
		}
		
		public void BeginSave ()
		{
			savingFlag = true;
//			needsReload = false;
			DisposeWatchers ();
		}
		
		public void EndSave ()
		{
			ResetLoadTimes ();
			savingFlag = false;
		}
		
		public void ResetLoadTimes ()
		{
			lastSaveTime.Clear ();
			reloadCheckTime.Clear ();
			foreach (FilePath file in item.GetItemFiles (false))
				lastSaveTime [file] = reloadCheckTime [file] = GetLastWriteTime (file);
//			needsReload = false;
			if (reloadRequired != null)
				InternalNeedsReload ();
		}
		
		public bool NeedsReload {
			get {
				if (savingFlag)
					return false;
				return InternalNeedsReload ();
			}
			set {
//				needsReload = value;
				if (value) {
					reloadCheckTime.Clear ();
					foreach (FilePath file in item.GetItemFiles (false))
						reloadCheckTime [file] = DateTime.MinValue;
				} else {
					ResetLoadTimes ();
				}
			}
		}
		
		public bool ItemFilesChanged {
			get {
				if (savingFlag)
					return false;
				foreach (FilePath file in item.GetItemFiles (false))
					if (GetLastSaveTime (file) != GetLastWriteTime (file))
						return true;
				return false;
			}
		}
		
		bool InternalNeedsReload ()
		{
			foreach (FilePath file in item.GetItemFiles (false)) {
				if (GetLastReloadCheckTime (file) != GetLastWriteTime (file))
					return true;
			}
			return false;
/*			
			if (needsReload)
				return true;
			
			// Watchers already set? if so, then since needsReload==false, no change has
			// happened so far
			if (watchers != null)
				return false;
		
			// Handlers are not set up. Do the check now, and set the handlers.
			watchers = new List<FileSystemWatcher> ();
			foreach (FilePath file in item.GetItemFiles (false)) {
				FileSystemWatcher w = new FileSystemWatcher (file.ParentDirectory, file.FileName);
				w.IncludeSubdirectories = false;
				w.Changed += HandleFileChanged;
				w.EnableRaisingEvents = true;
				watchers.Add (w);
				if (GetLastReloadCheckTime (file) != GetLastWriteTime (file))
					needsReload = true;
			}
			return needsReload;
			*/
		}
		
		void DisposeWatchers ()
		{
/*			if (watchers == null)
				return;
			foreach (FileSystemWatcher w in watchers)
				w.Dispose ();
			watchers = null;
*/		}

/*		void HandleFileChanged (object sender, FileSystemEventArgs e)
		{
			if (!savingFlag && !needsReload) {
				needsReload = true;
				onReloadRequired (eventArgs);
			}
		}
*/
		DateTime GetLastWriteTime (FilePath file)
		{
			try {
				if (!file.IsNullOrEmpty && File.Exists (file))
					return File.GetLastWriteTime (file);
			} catch {
			}
			return GetLastSaveTime (file);
		}

		DateTime GetLastSaveTime (FilePath file)
		{
			DateTime dt;
			if (lastSaveTime.TryGetValue (file, out dt))
				return dt;
			else
				return DateTime.MinValue;
		}

		DateTime GetLastReloadCheckTime (FilePath file)
		{
			DateTime dt;
			if (reloadCheckTime.TryGetValue (file, out dt))
				return dt;
			else
				return DateTime.MinValue;
		}
		
		public event EventHandler<TEventArgs> ReloadRequired {
			add {
				reloadRequired += value;
				if (InternalNeedsReload ())
					value (this, eventArgs);
			}
			remove {
				reloadRequired -= value;
			}
		}
		
		public void FireReloadRequired (TEventArgs args)
		{
			if (reloadRequired != null)
				reloadRequired (this, args);
		}
	}

	[Mono.Addins.Extension]
	class WorkspaceItemTagProvider: IStringTagProvider
	{
		public IEnumerable<StringTagDescription> GetTags (Type type)
		{
			if (typeof(WorkspaceItem).IsAssignableFrom (type) && !typeof(Solution).IsAssignableFrom (type)) {
				// Solutions have its own provider
				yield return new StringTagDescription ("WorkspaceFile", GettextCatalog.GetString ("Workspace File"));
				yield return new StringTagDescription ("WorkspaceName", GettextCatalog.GetString ("Workspace Name"));
				yield return new StringTagDescription ("WorkspaceDir", GettextCatalog.GetString ("Workspace Directory"));
			}
		}
		
		public object GetTagValue (object obj, string tag)
		{
			WorkspaceItem item = (WorkspaceItem) obj;
			switch (tag) {
				case "WORKSPACENAME": return item.Name;
				case "WORKSPACEFILE": return item.FileName;
				case "WORKSPACEDIR": return item.BaseDirectory;
			}
			throw new NotSupportedException ();
		}
	}
}
