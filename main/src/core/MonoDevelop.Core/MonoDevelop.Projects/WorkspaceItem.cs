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

namespace MonoDevelop.Projects
{
	public abstract class WorkspaceItem : WorkspaceObject, IWorkspaceFileObject, ILoadController
	{
		Workspace parentWorkspace;
		FileFormat format;
		internal bool FormatSet;
		FilePath fileName;
		int loading;
		PropertyBag userProperties;
		FileStatusTracker<WorkspaceItemEventArgs> fileStatusTracker;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		FilePath baseDirectory;

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
				if (userProperties == null)
					userProperties = new PropertyBag ();
				return userProperties; 
			}
		}
		
		public new string Name {
			get {
				return base.Name;
			}
			set {
				if (fileName.IsNullOrEmpty)
					SetLocation (FilePath.Empty, value);
				else {
					FilePath dir = fileName.ParentDirectory;
					string ext = fileName.Extension;
					FileName = dir.Combine (value) + ext;
				}
			}
		}

		protected override string OnGetName ()
		{
			if (fileName.IsNullOrEmpty)
				return string.Empty;
			else
				return fileName.FileNameWithoutExtension;
		}
		
		public virtual FilePath FileName {
			get {
				return fileName;
			}
			set {
				string oldName = Name;
				fileName = value;
				if (FileFormat != null)
					fileName = FileFormat.GetValidFileName (this, fileName);
				if (oldName != Name)
					OnNameChanged (new WorkspaceItemRenamedEventArgs (this, oldName, Name));
				NotifyModified ();

				// Load the user properties after the file name has been set.
				if (Loading)
					LoadUserProperties ();
			}
		}

		public void SetLocation (FilePath baseDirectory, string name)
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
				if (!value.IsNull && !FileName.IsNull && FileName.ParentDirectory.FullPath == value.FullPath)
					baseDirectory = null;
				else if (value.IsNullOrEmpty)
					baseDirectory = null;
				else
					baseDirectory = value.FullPath;
				NotifyModified ();
			}
		}

		protected override string OnGetBaseDirectory ()
		{
			if (baseDirectory.IsNull)
				return FileName.ParentDirectory.FullPath;
			else
				return baseDirectory;
		}
		
		protected override string OnGetItemDirectory ()
		{
			return FileName.ParentDirectory.FullPath;
		}
		
		protected bool Loading {
			get { return loading > 0; }
		}
		
		protected WorkspaceItem ()
		{
			ProjectExtensionUtil.LoadControl (this);
			fileStatusTracker = new FileStatusTracker<WorkspaceItemEventArgs> (this, OnReloadRequired, new WorkspaceItemEventArgs (this));
			Initialize (this);
		}

		WorkspaceItemExtension itemExtension;

		WorkspaceItemExtension ItemExtension {
			get {
				if (itemExtension == null)
					itemExtension = ExtensionChain.GetExtension<WorkspaceItemExtension> ();
				return itemExtension;
			}
		}

		protected override IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			yield return new DefaultWorkspaceItemExtension ();
		}

		public IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			return ItemExtension.GetItemFiles (includeReferencedFiles);
		}
		
		protected virtual IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = FileFormat.Format.GetItemFiles (this);
			if (!string.IsNullOrEmpty (FileName) && !col.Contains (FileName))
				col.Add (FileName);
			return col;
		}

		public virtual bool ContainsItem (WorkspaceObject obj)
		{
			return this == obj;
		}
		
		[Obsolete("Use GetProjectsContainingFile() (plural) instead")]
		public virtual Project GetProjectContainingFile (FilePath fileName)
		{
			return null;
		}

		public virtual IEnumerable<Project> GetProjectsContainingFile (FilePath fileName)
		{
			yield break;
		}
		
		public virtual ReadOnlyCollection<string> GetConfigurations ()
		{
			return new ReadOnlyCollection<string> (new string [0]);
		}

		internal void SetParentWorkspace (Workspace workspace)
		{
			parentWorkspace = workspace;
		}

		public virtual FileFormat FileFormat {
			get {
				if (format == null) {
					format = Services.ProjectService.GetDefaultFormat (this);
				}
				return format;
			}
		}
		
		public virtual bool SupportsFormat (FileFormat format)
		{
			return true;
		}
		
		public virtual Task ConvertToFormat (FileFormat format, bool convertChildren)
		{
			FormatSet = true;
			this.format = format;
			if (!string.IsNullOrEmpty (FileName))
				FileName = format.GetValidFileName (this, FileName);
			return Task.FromResult (0);
		}
		
		protected virtual void OnConfigurationsChanged ()
		{
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, EventArgs.Empty);
			if (ParentWorkspace != null)
				ParentWorkspace.OnConfigurationsChanged ();
		}

		public void Save (FilePath fileName, ProgressMonitor monitor)
		{
			SaveAsync (fileName, monitor).Wait ();
		}
		
		public Task SaveAsync (FilePath fileName, ProgressMonitor monitor)
		{
			FileName = fileName;
			return SaveAsync (monitor);
		}

		public void Save (ProgressMonitor monitor)
		{
			SaveAsync (monitor).Wait ();
		}

		public async Task SaveAsync (ProgressMonitor monitor)
		{
			try {
				fileStatusTracker.BeginSave ();
				await ItemExtension.Save (monitor);
				SaveUserProperties ();
				OnSaved (new WorkspaceItemEventArgs (this));
				
				// Update save times
			} finally {
				fileStatusTracker.EndSave ();
			}
			FileService.NotifyFileChanged (FileName);
		}

		protected internal virtual Task OnSave (ProgressMonitor monitor)
		{
			return Services.ProjectService.InternalWriteWorkspaceItem (monitor, FileName, this);
		}

		public virtual bool NeedsReload {
			get {
				return fileStatusTracker.NeedsReload;
			}
			set {
				fileStatusTracker.NeedsReload = value;
			}
		}
		
		public virtual bool ItemFilesChanged {
			get {
				return fileStatusTracker.ItemFilesChanged;
			}
		}

		internal protected virtual bool OnGetSupportsExecute ()
		{
			return true;
		}

		void ILoadController.BeginLoad ()
		{
			loading++;
			OnBeginLoad ();
		}
		
		void ILoadController.EndLoad ()
		{
			loading--;
			fileStatusTracker.ResetLoadTimes ();
			OnEndLoad ();
		}
		
		protected virtual void OnBeginLoad ()
		{
		}
		
		protected virtual void OnEndLoad ()
		{
		}
		
		public virtual void LoadUserProperties ()
		{
			if (userProperties != null)
				userProperties.Dispose ();
			userProperties = null;
			
			string preferencesFileName = GetPreferencesFileName ();
			if (!File.Exists (preferencesFileName))
				return;
			
			XmlTextReader reader = new XmlTextReader (preferencesFileName);
			try {
				reader.MoveToContent ();
				if (reader.LocalName != "Properties")
					return;

				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				ser.SerializationContext.BaseFile = preferencesFileName;
				userProperties = (PropertyBag) ser.Deserialize (reader, typeof(PropertyBag));
			} catch (Exception e) {
				LoggingService.LogError ("Exception while loading user solution preferences.", e);
				return;
			} finally {
				reader.Close ();
			}
		}
		
		public virtual void SaveUserProperties ()
		{
			string file = GetPreferencesFileName ();
			
			if (userProperties == null || userProperties.IsEmpty) {
				if (File.Exists (file))
					File.Delete (file);
				return;
			}
			
			XmlTextWriter writer = null;
			try {
				writer = new XmlTextWriter (file, System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				ser.SerializationContext.BaseFile = file;
				ser.Serialize (writer, userProperties, typeof(PropertyBag));
			} catch (Exception e) {
				LoggingService.LogWarning ("Could not save solution preferences: " + GetPreferencesFileName (), e);
			} finally {
				if (writer != null)
					writer.Close ();
			}
		}
		
		string GetPreferencesFileName ()
		{
			return FileName.ChangeExtension (".userprefs");
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
		
		public override void Dispose()
		{
			base.Dispose ();
			if (userProperties != null)
				userProperties.Dispose ();
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
