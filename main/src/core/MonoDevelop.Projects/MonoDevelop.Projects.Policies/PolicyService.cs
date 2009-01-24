// 
// PolicyService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Policies
{
	
	
	public static class PolicyService
	{
		const string TYPE_EXT_POINT = "/MonoDevelop/ProjectModel/PolicyTypes";
		const string SET_EXT_POINT  = "/MonoDevelop/ProjectModel/PolicySets";
		
		static List<PolicySet> sets = new List<PolicySet> ();
		static DataSerializer serializer;
		static Dictionary<string, Type> policyNames = new Dictionary<string, Type> ();
		static Dictionary<Type, string> policyTypes = new Dictionary<Type, string> ();
		
		static PolicyService ()
		{
			AddinManager.AddExtensionNodeHandler (TYPE_EXT_POINT, HandlePolicyTypeUpdated);
			AddinManager.AddExtensionNodeHandler (SET_EXT_POINT, HandlePolicySetUpdated);
		}
		
		static void HandlePolicySetUpdated (object sender, ExtensionNodeEventArgs args)
		{
			switch (args.Change) {
			case ExtensionChange.Add:
				sets.Add (((PolicySetNode) args.ExtensionNode).Set);
				break;
			case ExtensionChange.Remove:
				sets.Remove (((PolicySetNode) args.ExtensionNode).Set);
				break;
			}
		}
		
		static void HandlePolicyTypeUpdated (object sender, ExtensionNodeEventArgs args)
		{
			Type t = ((DataTypeCodon)args.ExtensionNode).Class;
			if (t == null) {
				throw new UserException ("Type '" + ((DataTypeCodon)args.ExtensionNode).TypeName
				                         + "' not found. It could not be registered as a serializable type.");
			}
			
			string name = null;
			object[] obs = t.GetCustomAttributes (typeof (DataItemAttribute), true);
			if (obs != null && obs.Length > 0)
				name = ((DataItemAttribute)obs[0]).Name;
			
			if (String.IsNullOrEmpty (name))
				name = t.Name;
			
			switch (args.Change) {
			case ExtensionChange.Add:
				if (policyTypes.ContainsKey (t))
					throw new UserException ("The Policy type '" + t.FullName + "' may only be registered once.");
				if (policyNames.ContainsKey (name))
					throw new UserException ("Only one Policy type may have the ID '" + name + "'");
				policyTypes.Add (t, name);
				policyNames.Add (name, t);
				
				break;
			case ExtensionChange.Remove:
				policyTypes.Remove (t);
				policyNames.Remove (name);
				break;
			}
		}
		
		static DataSerializer Serializer {
			get {
				if (serializer == null)
					serializer = new DataSerializer (new DataContext ());
				return serializer;
			}
		}		
		
		internal static System.Collections.IEnumerable RawDeserializeXml (System.IO.StreamReader reader)
		{
			var xr = System.Xml.XmlReader.Create (reader);
			XmlConfigurationReader configReader = XmlConfigurationReader.DefaultReader;
			while (!xr.EOF)
				yield return RawDeserialize (configReader.Read (xr));
		}
		
		internal static object RawDeserialize (DataNode data)
		{
			Type t = GetRegisteredType (data.Name);
			return Serializer.Deserialize (t, data);
		}
		
		static Type GetRegisteredType (string name)
		{
			Type t;
			if (!policyNames.TryGetValue (name, out t))
				throw new InvalidOperationException ("Cannot deserialise unregistered policy name '" + name + "'");
			return t;
		}
		
		internal static DataNode RawSerialize (object policy)
		{
			string name;
			if (!policyTypes.TryGetValue (policy.GetType (), out name))
				throw new InvalidOperationException ("Cannot serialise unregistered policy type '" + policy.GetType () + "'");
			DataNode node = Serializer.Serialize (policy);
			node.Name = name;
			return node;
		}
		
		internal static object DiffDeserialize (DataNode data)
		{
			DataItem item = (DataItem) data;
			DataValue inheritVal = item.ItemData.Extract ("inheritsSet") as DataValue;
			if (inheritVal == null) {
				return RawDeserialize (data);
			}
			PolicySet set = GetSet (inheritVal.Value);
			if (set == null)
				throw new InvalidOperationException ("No policy set found for id '" + inheritVal.Value + "'");
			Type t = GetRegisteredType (data.Name);
			object baseItem = set.Get (t);
			if (baseItem == null)
				throw new InvalidOperationException ("Policy set '" + set.Id + "' does not contain a policy for '"
				                                     + data.Name + "'");
			DataNode baseline = RawSerialize (baseItem);
			return RawDeserialize (ApplyOverlay (baseline, data));
		}
		
		static PolicySet GetSet (string id)
		{
			foreach (PolicySet s in sets)
				if (s.Id == id)
					return s;
			return null;
		}
		
		internal static DataNode DiffSerialize (object policy)
		{
			PolicySet minSet = null;
			int min = Int32.MaxValue;
			DataNode node = null;
			
			DataNode raw = RawSerialize (policy);
			Type t = policy.GetType ();
			
			//find the policy with the fewest differences
			foreach (PolicySet set in GetPolicySets (t)) {
				DataNode baseline = RawSerialize (set.Get (t));
				int size = 0;
				DataNode tempNode = ExtractOverlay (baseline, raw, ref size);
				if (size < min) {
					minSet = set;
					min = size;
					node = tempNode;
				}	
			}
			
			if (node != null) {
				((DataItem)node).ItemData.Add (new DataValue ("inheritsSet", minSet.Id));
				return node;
			} else {
				return raw;
			}
		}
		
		//DESTRUCTIVE: this alters the nodes in baseline
		static DataNode ApplyOverlay (DataNode baseline, DataNode diffNode)
		{
			if (baseline.Name != diffNode.Name)
				throw new InvalidOperationException ("Node names do not match");
			
			if (diffNode is DataValue)
				return diffNode;
			
			DataItem item = (DataItem) baseline;
			DataItem diffItem = (DataItem) diffNode;
			if (diffItem.HasItemData)
				ApplyOverlay (item, diffItem);
			return item;
		}
		
		static void ApplyOverlay (DataItem baseline, DataItem diffNode)
		{
			foreach (DataNode node in diffNode.ItemData)
			{
				DataNode baselineNode = baseline[node.Name];
				if (baselineNode == null)
					throw new InvalidOperationException ("Diff node ' " + node.Name + " does not exist on " +
					                                     "the baseline node. It is likely that the serialised " +
					                                     "objects have default values, which cannot safely " +
					                                     "be diff-serialised.");
				
				DataValue val = baselineNode as DataValue;
				if (val != null) {
					baseline.ItemData.Remove (baselineNode);
					baseline.ItemData.Add (node);
				} else {
					ApplyOverlay ((DataItem)baselineNode, (DataValue)node);
				}
			}
		}
		
		static DataNode ExtractOverlay (DataNode baseline, DataNode diffNode, ref int size)
		{
			if (baseline.Name != diffNode.Name)
				throw new InvalidOperationException ("Node names do not match");
	
			DataValue val = diffNode as DataValue;
			if (val != null) {
				size += val.Name.Length;
				if (val.Value != null)
					size += val.Value.Length;
				return diffNode;
			} else {
				return ExtractOverlay ((DataItem)baseline, (DataItem)diffNode, ref size);
			}
		}
		
		static DataItem ExtractOverlay (DataItem baseline, DataItem diffNode, ref int size)
		{
			DataItem newItem = new DataItem ();
			newItem.Name = baseline.Name;
			
			foreach (DataNode node in baseline.ItemData)
			{
				DataNode overlayNode = diffNode.ItemData[node.Name];
				if (overlayNode == null)
					throw new InvalidOperationException ("Baseline node ' " + node.Name + " does not exist on " +
					                                     "the diff node. It is likely that the serialised " +
					                                     "objects have default values, which cannot safely " +
					                                     "be diff-serialised.");
				
				DataValue val = overlayNode as DataValue;
				if (val != null) {
					if (val.Value != ((DataValue)node).Value) {
						size += val.Name.Length;
						if (val.Value != null)
							size += val.Value.Length;
						newItem.ItemData.Add (val);
					}
				} else {
					DataItem childItem = ExtractOverlay ((DataItem) node, (DataItem) overlayNode, ref size);
					if (childItem != null && childItem.HasItemData) {
						size += childItem.Name.Length + childItem.Name.Length;
						newItem.ItemData.Add (childItem);
					}
				}
			}
			
			return newItem;
		}
		
		public static IEnumerable<PolicySet> GetPolicySets (Type t)
		{
			foreach (PolicySet s in sets)
				if (s.Has (t))
					yield return s;
		}
		
		public static IEnumerable<PolicySet> GetPolicySets<T> ()
		{
			foreach (PolicySet s in sets)
				if (s.Has<T>())
					yield return s;
		}
		
		public static T GetDefaultPolicy<T> () where T : new ()
		{
			return new T ();
		}
		
		public static IEnumerable<PolicySet> GetMatchingSets<T> (T policy) where T : class, IEquatable<T>
		{
			foreach (PolicySet ps in sets) {
				IEquatable<T> match = ps.Get<T> () as IEquatable<T>;
				if (match != null && match.Equals (policy))
					yield return ps;
			}
		}
		
		
	}
}
