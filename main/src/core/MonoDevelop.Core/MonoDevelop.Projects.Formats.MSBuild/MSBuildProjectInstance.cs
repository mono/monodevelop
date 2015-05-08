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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProjectInstance
	{
		MSBuildProject msproject;
		List<IMSBuildItemEvaluated> evaluatedItems = new List<IMSBuildItemEvaluated> ();
		List<IMSBuildItemEvaluated> evaluatedItemsIgnoringCondition = new List<IMSBuildItemEvaluated> ();
		MSBuildEvaluatedPropertyCollection evaluatedProperties;
		MSBuildTarget[] targets = new MSBuildTarget[0];
		Dictionary<string,string> globalProperties = new Dictionary<string, string> ();

		public MSBuildProjectInstance (MSBuildProject project)
		{
			msproject = project;
			evaluatedItemsIgnoringCondition = new List<IMSBuildItemEvaluated> ();
			evaluatedProperties = new MSBuildEvaluatedPropertyCollection (msproject);
		}

		public void SetGlobalProperty (string property, string value)
		{
			globalProperties [property] = value;
		}

		public void Evaluate ()
		{
			// Use a private metadata property to assign an id to each item. This id is used to match
			// evaluated items with the items that generated them.
			int id = 0;
			List<XmlElement> idElems = new List<XmlElement> ();

			try {
				var currentItems = new Dictionary<string,MSBuildItem> ();
				foreach (var it in msproject.GetAllItems ()) {
					var c = msproject.Document.CreateElement (NodeIdPropertyName, MSBuildProject.Schema);
					string nid = (id++).ToString ();
					c.InnerXml = nid;
					it.Element.AppendChild (c);
					currentItems [nid] = it;
					idElems.Add (c);
					it.EvaluatedItemCount = 0;
				}

				var supportsMSBuild = msproject.UseMSBuildEngine && msproject.GetGlobalPropertyGroup ().GetValue ("UseMSBuildEngine", true);

				MSBuildEngine e = MSBuildEngine.Create (supportsMSBuild);

				OnEvaluationStarting ();

				var p = e.LoadProjectFromXml (msproject, msproject.FileName, msproject.Document.OuterXml);

				foreach (var prop in globalProperties)
					e.SetGlobalProperty (p, prop.Key, prop.Value);

				e.Evaluate (p);

				SyncBuildProject (currentItems, e, p);
			}
			catch (Exception ex) {
				// If the project can't be evaluated don't crash
				LoggingService.LogError ("MSBuild project could not be evaluated", ex);
				throw new ProjectEvaluationException (msproject, ex.Message);
			}
			finally {
				// Now remove the item id property
				foreach (var el in idElems)
					el.ParentNode.RemoveChild (el);

				OnEvaluationFinished ();
			}
		}

		internal const string NodeIdPropertyName = "__MD_NodeId";

		void SyncBuildProject (Dictionary<string,MSBuildItem> currentItems, MSBuildEngine e, object project)
		{
			evaluatedItemsIgnoringCondition.Clear ();
			evaluatedItems.Clear ();

			foreach (var it in e.GetAllItems (project, false)) {
				string name, include, finalItemSpec;
				bool imported;
				e.GetItemInfo (it, out name, out include, out finalItemSpec, out imported);
				var iid = e.GetItemMetadata (it, NodeIdPropertyName);
				MSBuildItem xit;
				if (currentItems.TryGetValue (iid, out xit)) {
					xit.SetEvalResult (finalItemSpec);
					((MSBuildPropertyGroupEvaluated)xit.EvaluatedMetadata).Sync (e, it);
				}
			}

			var xmlImports = msproject.Imports.ToArray ();
			var buildImports = e.GetImports (project).ToArray ();
			for (int n = 0; n < xmlImports.Length && n < buildImports.Length; n++)
				xmlImports [n].SetEvalResult (e.GetImportEvaluatedProjectPath (project, buildImports [n]));

			var evalItems = new Dictionary<string,MSBuildItemEvaluated> ();
			foreach (var it in e.GetEvaluatedItems (project)) {
				var xit = CreateEvaluatedItem (e, it);
				var itemId = e.GetItemMetadata (it, NodeIdPropertyName);
				var key = itemId + " " + xit.Include;
				if (evalItems.ContainsKey (key))
					continue; // xbuild seems to return duplicate items when using wildcards. This is a workaround to avoid the duplicates.
				MSBuildItem pit;
				if (!string.IsNullOrEmpty (itemId) && currentItems.TryGetValue (itemId, out pit)) {
					xit.SourceItem = pit;
					xit.Condition = pit.Condition;
					pit.EvaluatedItemCount++;
					evalItems [key] = xit;
				}
				evaluatedItems.Add (xit);
			}

			var evalItemsNoCond = new Dictionary<string,MSBuildItemEvaluated> ();
			foreach (var it in e.GetEvaluatedItemsIgnoringCondition (project)) {
				var itemId = e.GetItemMetadata (it, NodeIdPropertyName);
				MSBuildItemEvaluated evItem;
				var xit = CreateEvaluatedItem (e, it);
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
					xit.SourceItem = pit;
					xit.Condition = pit.Condition;
					pit.EvaluatedItemCount++;
					evalItemsNoCond [key] = xit;
				}
				evaluatedItemsIgnoringCondition.Add (xit);
			}

			var props = new MSBuildEvaluatedPropertyCollection (msproject);
			evaluatedProperties = props;
			props.SyncCollection (e, project);

			targets = e.GetTargets (project).ToArray ();
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

		void OnEvaluationStarting ()
		{
			foreach (var g in msproject.PropertyGroups)
				g.OnEvaluationStarting ();
			foreach (var g in msproject.ItemGroups)
				g.OnEvaluationStarting ();
			foreach (var i in msproject.Imports)
				i.OnEvaluationStarting ();
		}

		void OnEvaluationFinished ()
		{
			foreach (var g in msproject.PropertyGroups)
				g.OnEvaluationFinished ();
			foreach (var g in msproject.ItemGroups)
				g.OnEvaluationFinished ();
			foreach (var i in msproject.Imports)
				i.OnEvaluationFinished ();
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

		public IEnumerable<MSBuildTarget> Targets {
			get {
				return targets;
			}
		}
	}
}

