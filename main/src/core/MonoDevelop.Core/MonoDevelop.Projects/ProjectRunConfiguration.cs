//
// ProjectRunConfiguration.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects.MSBuild;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public class ProjectRunConfiguration: SolutionItemRunConfiguration
	{
		IPropertySet properties;
		MSBuildPropertyGroup mainPropertyGroup;

		public ProjectRunConfiguration (string name): base (name)
		{
		}

		internal protected virtual void Initialize (Project project)
		{
			// There may be run configuration properties defined in the
			// main property group in the project. Those values have to
			// be initially loaded in new run configurations.

			using (var pi = project.MSBuildProject.CreateInstance ()) {
				pi.SetGlobalProperty ("BuildingInsideVisualStudio", "true");
				pi.SetGlobalProperty ("RunConfiguration", "");
				pi.OnlyEvaluateProperties = true;
				pi.Evaluate ();
				var lg = pi.GetPropertiesLinkedToGroup (MainPropertyGroup);
				Read (lg);
				properties = MainPropertyGroup;
				MainPropertyGroup.UnlinkFromProjectInstance ();
			}
		}

		public new Project ParentItem {
			get { return (Project)base.ParentItem; }
		}

		/// <summary>
		/// Copies the data of a run configuration into this configuration
		/// </summary>
		/// <param name="config">Configuration from which to get the data.</param>
		/// <param name="isRename">If true, it means that the copy is being made as a result of a rename or clone operation. In this case,
		/// the overriden method may change the value of some properties that depend on the configuration name.</param>
		public void CopyFrom (ProjectRunConfiguration config, bool isRename = false)
		{
			StoreInUserFile = config.StoreInUserFile;
			OnCopyFrom (config, isRename);
		}

		protected virtual void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
		}

		internal protected virtual void Read (IPropertySet pset)
		{
			properties = pset;
			pset.ReadObjectProperties (this, GetType (), true);
		}

		internal protected virtual void Write (IPropertySet pset)
		{
			pset.WriteObjectProperties (this, GetType (), true);
		}

		internal bool Equals (ProjectRunConfiguration other)
		{
			var dict1 = new Dictionary<string, string> ();
			var dict2 = new Dictionary<string, string> ();

			var thisData = new ProjectItemMetadata ();
			Write (thisData);
			GetProps (MainPropertyGroup, dict1);
			GetProps (thisData, dict1);

			var otherData = new ProjectItemMetadata ();
			other.Write (otherData);
			GetProps (other.MainPropertyGroup, dict2);
			GetProps (otherData, dict2);

			if (dict1.Count != dict2.Count)
				return false;
			foreach (var tp in dict1) {
				string v;
				if (!dict2.TryGetValue (tp.Key, out v) || tp.Value != v)
					return false;
			}
			return true;
		}

		void GetProps (IPropertySet p, Dictionary<string,string> dict)
		{
			foreach (var prop in p.GetProperties ())
				dict [prop.Name] = prop.Value;
		}

		/// <summary>
		/// Property set where the properties for this configuration are defined.
		/// </summary>
		public IPropertySet Properties {
			get {
				return properties ?? MainPropertyGroup;
			}
			internal set {
				properties = value;
			}
		}

		internal MSBuildPropertyGroup MainPropertyGroup {
			get {
				if (mainPropertyGroup == null) {
					if (ParentItem == null)
						mainPropertyGroup = new MSBuildPropertyGroup ();
					else
						mainPropertyGroup = ParentItem.MSBuildProject.CreatePropertyGroup ();
					mainPropertyGroup.IgnoreDefaultValues = true;
				}
				return mainPropertyGroup;
			}
			set {
				mainPropertyGroup = value;
				mainPropertyGroup.IgnoreDefaultValues = true;
			}
		}

		internal MSBuildProjectInstance ProjectInstance { get; set; }

		public bool StoreInUserFile { get; set; } = true;
	}
}

