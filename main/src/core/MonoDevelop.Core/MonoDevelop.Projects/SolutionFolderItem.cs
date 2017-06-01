// SolutionItem.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Projects.Policies;
using System.Collections.Generic;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public abstract class SolutionFolderItem: WorkspaceObject, IPolicyProvider
	{
		SolutionFolder parentFolder;
		Solution parentSolution;
		SolutionFolder internalChildren;
		string typeGuid;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		string baseDirectory;
		
		[ItemProperty ("Policies", IsExternal = true, SkipEmpty = true)]
		PolicyBag policies;

		[ItemProperty ("UseMSBuildEngine", DefaultValue = null)]
		public bool? UseMSBuildEngine { get; set; }
		
		PropertyBag userProperties;

		internal List<string> UnresolvedProjectDependencies { get; set; }

		public new string Name {
			get {
				return base.Name;
			}
			set {
				AssertMainThread ();
				if (value != Name) {
					var oldName = Name;
					OnSetName (value);
					OnNameChanged (new SolutionItemRenamedEventArgs (this, oldName, Name));
				}
			}
		}

		public string TypeGuid {
			get {
				return this.typeGuid;
			}
			set {
				AssertMainThread ();
				typeGuid = value;
			}
		}

		protected abstract void OnSetName (string value);

		/// <summary>
		/// Gets the solution to which this item belongs
		/// </summary>
		public Solution ParentSolution {
			get {
				if (parentFolder != null)
					return parentFolder.ParentSolution;
				return parentSolution; 
			}
			internal set {
				if (parentSolution != null && parentSolution != value)
					NotifyUnboundFromSolution (true);
				parentSolution = value;
				NotifyBoundToSolution (true);
			}
		}

		/// <summary>
		/// Gets or sets the base directory of this solution item
		/// </summary>
		/// <value>
		/// The base directory.
		/// </value>
		/// <remarks>
		/// The base directory is the directory where files belonging to this project
		/// are placed. Notice that this directory may be different than the directory
		/// where the project file is placed.
		/// </remarks>
		public new FilePath BaseDirectory {
			get {
				if (baseDirectory == null) {
					FilePath dir = GetDefaultBaseDirectory ();
					if (dir.IsNullOrEmpty)
						dir = ".";
					return dir.FullPath;
				}
				else
					return baseDirectory;
			}
			set {
				AssertMainThread ();

				string newValue;
				FilePath def = GetDefaultBaseDirectory ();
				if (value != FilePath.Null && def != FilePath.Null && value.FullPath == def.FullPath)
					newValue = null;
				else if (string.IsNullOrEmpty (value))
					newValue = null;
				else
					newValue = value.FullPath;

				if (newValue == baseDirectory)
					return;

				baseDirectory = newValue;
				NotifyModified ("BaseDirectory");
			}
		}

		protected override string OnGetBaseDirectory ()
		{
			return BaseDirectory;
		}
		
		protected sealed override string OnGetItemDirectory ()
		{
			FilePath dir = GetDefaultBaseDirectory ();
			if (string.IsNullOrEmpty (dir))
				dir = ".";
			return dir.FullPath;
		}
		
		internal bool HasCustomBaseDirectory {
			get { return baseDirectory != null; }
		}
		
		/// <summary>
		/// Gets the default base directory.
		/// </summary>
		/// <remarks>
		/// The base directory is the directory where files belonging to this project
		/// are placed. Notice that this directory may be different than the directory
		/// where the project file is placed.
		/// </remarks>
		protected virtual FilePath GetDefaultBaseDirectory ( )
		{
			return ParentSolution.BaseDirectory;
		}

		/// <summary>
		/// Gets the identifier of this solution item
		/// </summary>
		/// <remarks>
		/// The identifier is unique inside the solution
		/// </remarks>
		public string ItemId {
			get {
				if (itemId == null)
					itemId = "{" + Guid.NewGuid ().ToString ().ToUpper () + "}";
				return itemId;
			}
			set {
				AssertMainThread ();
				itemId = value;
			}
		}

		string itemId;

		/// <summary>
		/// Gets policies.
		/// </summary>
		/// <remarks>
		/// Returns a policy container which can be used to query policies specific for this
		/// solution item. If a policy is not defined for this item, the inherited value will be returned.
		/// </remarks>
		public PolicyBag Policies {
			get {
				//newly created (i.e. not deserialised) SolutionItems may have a null PolicyBag
				if (policies == null)
					policies = new MonoDevelop.Projects.Policies.PolicyBag ();
				//this is the easiest reliable place to associate a deserialised Policybag with its owner
				policies.Owner = this;
				return policies;
			}
			//setter so that a solution can deserialise the PropertyBag on its RootFolder
			internal set {
				policies = value;
			}
		}
		
		PolicyContainer IPolicyProvider.Policies {
			get {
				return Policies;
			}
		}
		
		/// <summary>
		/// Gets solution item properties specific to the current user
		/// </summary>
		/// <remarks>
		/// These properties are not stored in the project file, but in a separate file which is not to be shared
		/// with other users.
		/// User properties are only loaded when the project is loaded inside the IDE.
		/// </remarks>
		public PropertyBag UserProperties {
			get {
				if (userProperties == null)
					userProperties = new PropertyBag ();
				return userProperties; 
			}
		}
		
		/// <summary>
		/// Initializes the user properties of the item
		/// </summary>
		/// <param name='properties'>
		/// Properties to be set
		/// </param>
		/// <exception cref='InvalidOperationException'>
		/// The user properties have already been set
		/// </exception>
		/// <remarks>
		/// This method is used by the IDE to initialize the user properties when a project is loaded.
		/// </remarks>
		public void LoadUserProperties (PropertyBag properties)
		{
			if (userProperties != null)
				throw new InvalidOperationException ("User properties already loaded.");
			userProperties = properties;
		}
		
		/// <summary>
		/// Gets the parent solution folder.
		/// </summary>
		public SolutionFolder ParentFolder {
			get {
				return parentFolder;
			}
			internal set {
				AssertMainThread ();
				if (parentFolder != null && parentFolder.ParentSolution != null && (value == null || value.ParentSolution != parentFolder.ParentSolution))
					NotifyUnboundFromSolution (false);

				parentFolder = value;
				if (internalChildren != null) {
					internalChildren.ParentFolder = value;
				}
				if (value != null && value.ParentSolution != null) {
					NotifyBoundToSolution (false);
				}
			}
		}

		// Normally, the ParentFolder setter fires OnBoundToSolution. However, when deserializing, child
		// ParentFolder hierarchies can become connected before the ParentSolution becomes set. This method
		// enables us to recursively fire the OnBoundToSolution call in those cases.
		void NotifyBoundToSolution (bool includeInternalChildren)
		{
			var folder = this as SolutionFolder;
			if (folder != null) {
				var items = folder.GetItemsWithoutCreating ();
				if (items != null) {
					foreach (var item in items) {
						item.NotifyBoundToSolution (true);
					}
				}
			}
			if (includeInternalChildren && internalChildren != null) {
				internalChildren.NotifyBoundToSolution (true);
			}
			OnBoundToSolution ();
		}

		void NotifyUnboundFromSolution (bool includeInternalChildren)
		{
			var folder = this as SolutionFolder;
			if (folder != null) {
				var items = folder.GetItemsWithoutCreating ();
				if (items != null) {
					foreach (var item in items) {
						item.NotifyUnboundFromSolution (true);
					}
				}
			}
			if (includeInternalChildren && internalChildren != null) {
				internalChildren.NotifyUnboundFromSolution (true);
			}
			OnUnboundFromSolution ();
		}

		protected override void OnDispose ()
		{
			base.OnDispose ();

			if (userProperties != null) {
				((IDisposable)userProperties).Dispose ();
				userProperties = null;
			}
			
			// parentFolder = null;
			// parentSolution = null;
			// internalChildren = null;
			// policies = null;
		}

		/// <summary>
		/// Gets the time of the last build
		/// </summary>
		/// <returns>
		/// The last build time.
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to get the last build time.
		/// </param>
		[Obsolete("Use MSBuild")]
		public DateTime GetLastBuildTime (ConfigurationSelector configuration)
		{
			return OnGetLastBuildTime (configuration);
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Projects.SolutionItem"/> needs to be reload due to changes in project or solution file
		/// </summary>
		/// <value>
		/// <c>true</c> if needs reload; otherwise, <c>false</c>.
		/// </value>
		public virtual bool NeedsReload {
			get {
				if (ParentSolution != null)
					return ParentSolution.NeedsReload;
				else
					return false;
			}
			set {
			}
		}
		
		/// <summary>
		/// Registers an internal child item.
		/// </summary>
		/// <param name='item'>
		/// An item
		/// </param>
		/// <remarks>
		/// Some kind of projects may be composed of several child projects.
		/// By registering those child projects using this method, the child
		/// projects will be plugged into the parent solution infrastructure
		/// (so for example, the ParentSolution property for those projects
		/// will return the correct value)
		/// </remarks>
		protected void RegisterInternalChild (SolutionFolderItem item)
		{
			AssertMainThread ();
			if (internalChildren == null) {
				internalChildren = new SolutionFolder ();
				internalChildren.ParentFolder = parentFolder;
			}
			internalChildren.Items.Add (item);
		}
		
		/// <summary>
		/// Unregisters an internal child item.
		/// </summary>
		/// <param name='item'>
		/// The item
		/// </param>
		protected void UnregisterInternalChild (SolutionFolderItem item)
		{
			AssertMainThread ();
			if (internalChildren != null)
				internalChildren.Items.Remove (item);
		}
		
		protected override StringTagModelDescription OnGetStringTagModelDescription (ConfigurationSelector conf)
		{
			var model = base.OnGetStringTagModelDescription (conf);
			model.Add (GetType ());
			if (ParentSolution != null)
				model.Add (typeof(Solution));
			return model;
		}

		protected override StringTagModel OnGetStringTagModel (ConfigurationSelector conf)
		{
			StringTagModel source = base.OnGetStringTagModel (conf);
			if (ParentSolution != null)
				source.Add (ParentSolution.GetStringTagModel ());
			return source;
		}

		/// <summary>
		/// Gets the author information for this solution item, inherited from the solution and global settings.
		/// </summary>
		public AuthorInformation AuthorInformation {
			get {
				if (ParentSolution != null)
					return ParentSolution.AuthorInformation;
				else
					return AuthorInformation.Default;
			}
		}

		internal MSBuildFileFormat SolutionFormat { get; private set; }

		/// <summary>
		/// Notifies that this solution item has been modified
		/// </summary>
		/// <param name='hint'>
		/// Hint about which part of the solution item has been modified. This will typically be the property name.
		/// </param>
		public void NotifyModified (string hint)
		{
			OnModified (new SolutionItemModifiedEventArgs (this, hint));
		}
		
		/// <summary>
		/// Raises the modified event.
		/// </summary>
		/// <param name='args'>
		/// Arguments.
		/// </param>
		protected virtual void OnModified (SolutionItemModifiedEventArgs args)
		{
			AssertMainThread ();
			if (Modified != null && !Disposed)
				Modified (this, args);
		}
		
		/// <summary>
		/// Raises the name changed event.
		/// </summary>
		/// <param name='e'>
		/// Arguments.
		/// </param>
		protected virtual void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			NotifyModified ("Name");
			if (NameChanged != null && !Disposed)
				NameChanged (this, e);
		}
		
		/// <summary>
		/// Initializes the item handler.
		/// </summary>
		/// <remarks>
		/// This method is called the first time an item handler is requested.
		/// Subclasses should override this method use SetItemHandler to
		/// assign a handler to this item.
		/// </remarks>
		protected virtual void InitializeItemHandler ()
		{
		}

		/// <summary>
		/// Gets the time of the last build
		/// </summary>
		/// <returns>
		/// The last build time.
		/// </returns>
		/// <param name='configuration'>
		/// Configuration for which to get the last build time.
		/// </param>
		[Obsolete]
		internal protected virtual DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			return DateTime.MinValue;
		}
		
		/// <summary>
		/// Called just after this item is bound to a solution
		/// </summary>
		protected virtual void OnBoundToSolution ()
		{
		}

		/// <summary>
		/// Called just before this item is removed from a solution (ParentSolution is still valid when this method is called)
		/// </summary>
		protected virtual void OnUnboundFromSolution ()
		{
		}

		/// <summary>
		/// Occurs when the name of the item changes
		/// </summary>
		public event SolutionItemRenamedEventHandler NameChanged;
		
		/// <summary>
		/// Occurs when the item is modified.
		/// </summary>
		public event SolutionItemModifiedEventHandler Modified;
	}

}
