//
// MSBuildProjectInstance.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.MSBuild
{
	public sealed class MSBuildProjectInstance: IDisposable
	{
		MSBuildProject msproject;
		List<IMSBuildItemEvaluated> evaluatedItems = new List<IMSBuildItemEvaluated> ();
		List<IMSBuildItemEvaluated> evaluatedItemsIgnoringCondition = new List<IMSBuildItemEvaluated> ();
		MSBuildEvaluatedPropertyCollection evaluatedProperties;
		MSBuildTarget[] targets = new MSBuildTarget[0];
		MSBuildTarget[] targetsIgnoringCondition = new MSBuildTarget[0];
		Dictionary<string,string> globalProperties = new Dictionary<string, string> ();
		ConditionedPropertyCollection conditionedProperties;

		MSBuildProjectInstanceInfo info;

		object projectInstance;
		MSBuildEngine engine;

		public MSBuildProjectInstance (MSBuildProject project)
		{
			msproject = project;
			evaluatedItemsIgnoringCondition = new List<IMSBuildItemEvaluated> ();
			evaluatedProperties = new MSBuildEvaluatedPropertyCollection (msproject);
			globalProperties = new Dictionary<string, string> (project.GlobalProperties);
		}

		public void Dispose ()
		{
			if (projectInstance != null) {
				engine.DisposeProjectInstance (projectInstance);
				projectInstance = null;
				engine = null;
			}
		}

		public void SetGlobalProperty (string property, string value)
		{
			globalProperties [property] = value;
		}

		public void RemoveGlobalProperty (string property)
		{
			globalProperties.Remove (property);
		}

		internal bool OnlyEvaluateProperties { get; set; }

		public Task EvaluateAsync ()
		{
			return Task.Run (() => Evaluate ());
		}

		public void Evaluate ()
		{
			if (projectInstance != null)
				engine.DisposeProjectInstance (projectInstance);

			info = msproject.LoadNativeInstance ();

			engine = info.Engine;
			projectInstance = engine.CreateProjectInstance (info.Project);

			try {
				// Set properties defined by global property providers, and then
				// properties explicitly set to this instance

				foreach (var gpp in MSBuildProjectService.GlobalPropertyProviders) {
					foreach (var prop in gpp.GetGlobalProperties ())
						engine.SetGlobalProperty (projectInstance, prop.Key, prop.Value);
				}
				foreach (var prop in globalProperties)
					engine.SetGlobalProperty (projectInstance, prop.Key, prop.Value);

				engine.Evaluate (projectInstance);

				SyncBuildProject (info.ItemMap, info.Engine, projectInstance);
			} catch (Exception ex) {
				// If the project can't be evaluated don't crash
				LoggingService.LogError ("MSBuild project could not be evaluated", ex);
				throw new ProjectEvaluationException (msproject, ex.Message);
			}
		}

		internal const string NodeIdPropertyName = "__MD_NodeId";

		void SyncBuildProject (Dictionary<string,MSBuildItem> currentItems, MSBuildEngine e, object project)
		{
			evaluatedItemsIgnoringCondition.Clear ();
			evaluatedItems.Clear ();

			if (!OnlyEvaluateProperties) {
				
				var evalItems = new Dictionary<string,MSBuildItemEvaluated> ();
				foreach (var it in e.GetEvaluatedItems (project)) {
					var xit = it as MSBuildItemEvaluated;
					if (xit == null) {
						xit = CreateEvaluatedItem (e, it);
						var itemId = e.GetItemMetadata (it, NodeIdPropertyName);
						var key = itemId + " " + xit.Include;
						if (evalItems.ContainsKey (key))
							continue; // xbuild seems to return duplicate items when using wildcards. This is a workaround to avoid the duplicates.
						MSBuildItem pit;
						if (!string.IsNullOrEmpty (itemId) && currentItems.TryGetValue (itemId, out pit)) {
							xit.AddSourceItem (pit);
							xit.Condition = pit.Condition;
							evalItems [key] = xit;
						}
					}
					evaluatedItems.Add (xit);
				}

				var evalItemsNoCond = new Dictionary<string,MSBuildItemEvaluated> ();
				foreach (var it in e.GetEvaluatedItemsIgnoringCondition (project)) {
					var xit = it as MSBuildItemEvaluated;
					if (xit == null) {
						xit = CreateEvaluatedItem (e, it);
						var itemId = e.GetItemMetadata (it, NodeIdPropertyName);
						MSBuildItemEvaluated evItem;
						var key = itemId + " " + xit.Include;
						if (evalItemsNoCond.ContainsKey (key))
							continue; // xbuild seems to return duplicate items when using wildcards. This is a workaround to avoid the duplicates.
						if (!string.IsNullOrEmpty (itemId) && evalItems.TryGetValue (key, out evItem)) {
							evaluatedItemsIgnoringCondition.Add (evItem);
							evalItemsNoCond [key] = evItem;
							continue;
						}
						MSBuildItem pit;
						if (!string.IsNullOrEmpty (itemId) && currentItems.TryGetValue (itemId, out pit)) {
							xit.AddSourceItem (pit);
							xit.Condition = pit.Condition;
							evalItemsNoCond [key] = xit;
						}
					}
					evaluatedItemsIgnoringCondition.Add (xit);
				}

				// Clear the node id metadata
				foreach (var it in evaluatedItems.Concat (evaluatedItemsIgnoringCondition))
					((MSBuildPropertyGroupEvaluated)it.Metadata).RemoveProperty (NodeIdPropertyName);

				targets = e.GetTargets (project).ToArray ();
				targetsIgnoringCondition = e.GetTargetsIgnoringCondition (project).ToArray ();
			}

			var props = new MSBuildEvaluatedPropertyCollection (msproject);
			evaluatedProperties = props;
			props.SyncCollection (e, project);

			conditionedProperties = engine.GetConditionedProperties (project);
		}

		MSBuildItemEvaluated CreateEvaluatedItem (MSBuildEngine e, object it)
		{
			string name, include, finalItemSpec;
			bool imported;
			e.GetEvaluatedItemInfo (it, out name, out include, out finalItemSpec, out imported);
			var xit = new MSBuildItemEvaluated (msproject, name, include, finalItemSpec);
			xit.IsImported = imported;
			((MSBuildPropertyGroupEvaluated)xit.Metadata).Sync (e, it);
			return xit;
		}

		public IMSBuildEvaluatedPropertyCollection EvaluatedProperties {
			get { return evaluatedProperties; }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItems {
			get { return evaluatedItems; }
		}

		public IEnumerable<IMSBuildItemEvaluated> EvaluatedItemsIgnoringCondition {
			get { return evaluatedItemsIgnoringCondition; }
		}

		public IEnumerable<IMSBuildTargetEvaluated> Targets {
			get {
				return targets;
			}
		}

		public IEnumerable<IMSBuildTargetEvaluated> TargetsIgnoringCondition {
			get {
				return targetsIgnoringCondition;
			}
		}

		internal IPropertySet GetPropertiesLinkedToGroup (MSBuildPropertyGroup group)
		{
			evaluatedProperties.LinkToGroup (group);
			return evaluatedProperties;
		}

		internal ConditionedPropertyCollection GetConditionedProperties ()
		{
			return conditionedProperties;
		}

		public IEnumerable<MSBuildItem> FindGlobItemsIncludingFile (string include)
		{
			return engine?.FindGlobItemsIncludingFile (projectInstance, include);
		}

		internal IEnumerable<MSBuildItem> FindUpdateGlobItemsIncludingFile (string include, MSBuildItem globItem)
		{
			return engine?.FindUpdateGlobItemsIncludingFile (projectInstance, include, globItem);
		}

		/// <summary>
		/// Notifies that a property has been modified in the project, so that the evaluated
		/// value for that property in this instance may be out of date.
		/// </summary>
		internal void SetPropertyValueStale (string name)
		{
			evaluatedProperties.SetPropertyValueStale (name);
		}
	}

	class MSBuildProjectInstanceInfo
	{
		public object Project { get; set; }
		public MSBuildEngine Engine { get; set; }
		public int ProjectStamp { get; set; }
		public Dictionary<string,MSBuildItem> ItemMap { get; set; }
	}
}

