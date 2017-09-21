// 
// ProjectItem.cs
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
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.MSBuild;
using System.Linq;

namespace MonoDevelop.Projects
{
	public class ProjectItem: IExtendedDataItem
	{
		Project project;
		Hashtable extendedProperties;
		ProjectItemMetadata metadata;
		static Dictionary<Type,HashSet<string>> knownMetadataCache = new Dictionary<Type, HashSet<string>> ();

		public ProjectItem ()
		{
			ItemName = MSBuildProjectService.GetNameForProjectItem (GetType());
		}

		public Project Project {
			get {
				return project;
			}
			internal set {
				project = value;
				OnProjectSet ();
			}
		}

		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}

		MSBuildItem backingItem;
		internal MSBuildItem BackingItem {
			get {
				return backingItem;
			}
			set {
				backingItem = value;
				UnevaluatedInclude = backingItem?.Include;
			}
		}

		IMSBuildItemEvaluated backingEvalItem;
		internal IMSBuildItemEvaluated BackingEvalItem {
			get { return backingEvalItem; }
			set {
				backingEvalItem = value;
				var source = backingEvalItem?.SourceItems.FirstOrDefault ();
				IsFromWildcardItem = source != null && source.IsWildcardItem;
			}
		}

		internal bool IsFromWildcardItem { get; private set; }

		internal MSBuildItem WildcardItem {
			get {
				return backingEvalItem?.SourceItems.FirstOrDefault ();
			}
		}

		internal string Condition { get; set; }

		public string ItemName { get; protected set; }

		public virtual string Include { get; protected set; }

		public string UnevaluatedInclude { get; protected set; }

		public ProjectItemFlags Flags { get; set; }

		public ProjectItemMetadata Metadata {
			get {
				if (metadata == null)
					metadata = new ProjectItemMetadata ();
				return metadata;
			}
		}

		public bool IsHidden {
			get { return (Flags & ProjectItemFlags.Hidden) == ProjectItemFlags.Hidden; }
		}

		public bool IsImported {
			get { return backingEvalItem?.IsImported == true; }
		}

		internal protected virtual void Read (Project project, IMSBuildItemEvaluated buildItem)
		{
			ItemName = buildItem.Name;
			Include = buildItem.Include;
			UnevaluatedInclude = buildItem.UnevaluatedInclude;
			Condition = buildItem.Condition;
			metadata = null;

			if (buildItem.SourceItem != null) {
				HashSet<string> knownProps = GetKnownMetadata ();
				foreach (var prop in buildItem.Metadata.GetProperties ()) {
					if (!knownProps.Contains (prop.Name)) {
						if (metadata == null)
							metadata = new ProjectItemMetadata (project.MSBuildProject);
						// Get the evaluated value for the original metadata property
						var p = new ItemMetadataProperty (prop.Name, buildItem.Metadata.GetValue (prop.Name), prop.UnevaluatedValue) { Condition = prop.Condition };
						p.ParentProject = project.MSBuildProject;
						metadata.AddProperty (p);
					}
				}
				if (metadata != null)
					metadata.OnLoaded ();
			}
			buildItem.Metadata.ReadObjectProperties (this, GetType (), true);
		}

		internal protected virtual void Write (Project project, MSBuildItem buildItem)
		{
			buildItem.Condition = Condition;
			buildItem.Metadata.WriteObjectProperties (this, GetType(), true);

			if (metadata != null) {
				metadata.SetProject (buildItem.ParentProject);
				foreach (MSBuildProperty prop in metadata.GetProperties ()) {
					// Use the UnevaluatedValue because if the property has changed, UnevaluatedValue will contain
					// the new value, and if not, it will contain the old unevaluated value
					buildItem.Metadata.SetValue (prop.Name, prop.UnevaluatedValue, condition:prop.Condition);
				}
			}
		}

		/// <summary>
		/// Gets a list of metadata properties which are read and written by this item, so they don't
		/// have to be stored in the generic Metadata dictionary
		/// </summary>
		/// <returns>The known metadata properties.</returns>
		protected virtual IEnumerable<string> GetKnownMetadataProperties ()
		{
			DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
			var props = Services.ProjectService.DataContext.GetProperties (ser.SerializationContext, this);
			foreach (var prop in props)
				if (!prop.IsExternal)
					yield return prop.Name;
		}

		HashSet<string> GetKnownMetadata ()
		{
			HashSet<string> mset;
			lock (knownMetadataCache) {
				if (!knownMetadataCache.TryGetValue (GetType (), out mset))
					knownMetadataCache [GetType()] = mset = new HashSet<string> (GetKnownMetadataProperties ());
			}
			return mset;
		}

		/// <summary>
		/// Invoked when the project to which the item belongs changes.
		/// </summary>
		protected virtual void OnProjectSet ()
		{
		}
	}
	
	public class UnknownProjectItem: ProjectItem
	{
		public UnknownProjectItem (string name, string include)
		{
			this.ItemName = name;
			this.Include = include;
		}
	}

	[Flags]
	public enum ProjectItemFlags
	{
		None = 0,

		/// <summary>
		/// The item is for internal use and will not be shown to the user
		/// </summary>
		Hidden = 1,

		/// <summary>
		/// The item will not be saved to the project file
		/// </summary>
		DontPersist = 2
	}
}
