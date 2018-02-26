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
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;
using System.Text;
using System.Xml;
using System.Globalization;

namespace MonoDevelop.Projects.Policies
{
	public static class PolicyService
	{
		const string TYPE_EXT_POINT = "/MonoDevelop/ProjectModel/PolicyTypes";
		const string SET_EXT_POINT  = "/MonoDevelop/ProjectModel/PolicySets";
		
		static List<PolicySet> sets = new List<PolicySet> ();
		static List<PolicySet> userSets = new List<PolicySet> ();
		static DataSerializer serializer;
		static Dictionary<string, Type> policyNames = new Dictionary<string, Type> ();
		static Dictionary<Type, string> policyTypes = new Dictionary<Type, string> ();
		static List<string> deletedUserSets = new List<string> ();
		
		static PolicySet systemDefaultPolicies; // Policy set that has the IDE defined default value for all types of policies.
		static PolicySet userDefaultPolicies; // Policy set that has user defined values.

		static PolicyBag systemDefaultPolicyBag = new SystemDefaultPolicyBag { ReadOnly = true };
		static PolicyBag defaultPolicyBag = new PolicyBag { ReadOnly = true };

		static InvariantPolicyBag invariantPolicies = new InvariantPolicyBag ();
		
		static PolicyService ()
		{
			AddinManager.AddExtensionNodeHandler (TYPE_EXT_POINT, HandlePolicyTypeUpdated);
			AddinManager.AddExtensionNodeHandler (SET_EXT_POINT, HandlePolicySetUpdated);
			LoadPolicies ();

			PolicySet pset = GetPolicySetById ("Invariant");
			pset.PolicyChanged += HandleInvariantPolicySetChanged;
			foreach (var pol in pset.Policies)
				invariantPolicies.InternalSet (pol.Key.PolicyType, pol.Key.Scope, pol.Value);
			invariantPolicies.ReadOnly = true;
		}

		static void HandleInvariantPolicySetChanged (object sender, PolicyChangedEventArgs e)
		{
			PolicySet pset = GetPolicySetById ("Invariant");
			object p = pset.Get (e.PolicyType, e.Scope);
			if (p != null)
				invariantPolicies.InternalSet (e.PolicyType, e.Scope, p);
			else
				invariantPolicies.InternalSet (e.PolicyType, e.Scope, Activator.CreateInstance (e.PolicyType));
		}
		
		static void HandlePolicySetUpdated (object sender, ExtensionNodeEventArgs args)
		{
			switch (args.Change) {
			case ExtensionChange.Add:
				PolicySet set = ((PolicySetNode) args.ExtensionNode).Set;
				set.ReadOnly = true;
				sets.Add (set);
				break;
			case ExtensionChange.Remove:
				sets.Remove (((PolicySetNode) args.ExtensionNode).Set);
				break;
			}
		}
		
		static void HandlePolicyTypeUpdated (object sender, ExtensionNodeEventArgs args)
		{
			Type t = ((TypeExtensionNode)args.ExtensionNode).Type;
			
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
				if (invariantPolicies.Get (t) == null)
					invariantPolicies.InternalSet (t, null, Activator.CreateInstance (t));
				break;
			case ExtensionChange.Remove:
				foreach (var s in sets)
					s.RemoveAll (t);
				policyTypes.Remove (t);
				policyNames.Remove (name);
				invariantPolicies.InternalRemove (t, null);
				break;
			}
		}
		
		public static string GetPolicyTypeDescription (Type t)
		{
			foreach (TypeExtensionNode<PolicyTypeAttribute> node in AddinManager.GetExtensionNodes (TYPE_EXT_POINT))
				if (node.Type == t)
					return node.Data.Description;
			return t.Name;
		}
		
		static DataSerializer Serializer {
			get {
				if (serializer == null) {
					serializer = new DataSerializer (new DataContext ());
					serializer.SerializationContext.IncludeDefaultValues = true;
				}
				return serializer;
			}
		}		
		
		internal static IEnumerable<ScopedPolicy> RawDeserializeXml (System.IO.StreamReader reader)
		{
			var xr = new System.Xml.XmlTextReader (reader);
			XmlConfigurationReader configReader = XmlConfigurationReader.DefaultReader;
			while (!xr.EOF && xr.MoveToContent () != System.Xml.XmlNodeType.None) {
				DataNode node = configReader.Read (xr);
				if (node.Name == "PolicySet" && node is DataItem) {
					foreach (DataNode child in ((DataItem)node).ItemData)
						yield return RawDeserialize (child);
				} else {
					yield return RawDeserialize (node);
				}
			}
		}

		internal static IEnumerable<ScopedPolicy> DiffDeserializeXml (System.IO.StreamReader reader)
		{
			var xr = System.Xml.XmlReader.Create (reader);
			return DiffDeserializeXml (xr);
		}
		
		internal static IEnumerable<ScopedPolicy> DiffDeserializeXml (System.Xml.XmlReader xr)
		{
			XmlConfigurationReader configReader = XmlConfigurationReader.DefaultReader;
			while (!xr.EOF && xr.MoveToContent () == System.Xml.XmlNodeType.Element) {
				DataNode node = configReader.Read (xr);
				if (node.Name == "PolicySet" && node is DataItem) {
					foreach (DataNode child in ((DataItem)node).ItemData) {
						if (child is DataItem)
							yield return DiffDeserialize ((DataItem)child);
					}
					yield break;
				} else {
					yield return DiffDeserialize ((DataItem)node);
				}
			}
		}
		
		internal static ScopedPolicy RawDeserialize (DataNode data)
		{
			string scope = null;
			Type t = GetRegisteredType (data.Name);
			if (t == null) {
				UnknownPolicy up = new UnknownPolicy (data);
				return new ScopedPolicy (typeof(UnknownPolicy), up, up.Scope);
			}
			bool allowDiff = false;
			DataItem di = data as DataItem;
			if (di != null) {
				DataValue allowDiffData = di ["allowDiffSerialize"] as DataValue;
				allowDiff = allowDiffData != null && allowDiffData.Value == "True";
				DataValue val = di ["scope"] as DataValue;
				if (val != null)
					scope = val.Value;
				DataValue inh = di ["inheritsSet"] as DataValue;
				if (inh != null && inh.Value == "null")
					return new ScopedPolicy (t, null, scope, allowDiff);
			}
			object o = Serializer.Deserialize (t, data);
			return new ScopedPolicy (t, o, scope, allowDiff);
		}
		
		static Type GetRegisteredType (string name)
		{
			Type t;
			if (!policyNames.TryGetValue (name, out t)) {
				LoggingService.LogWarning ("Cannot deserialise unregistered policy name '" + name + "'");
				return null;
			}
			return t;
		}
		
		internal static DataNode RawSerialize (Type policyType, object policy)
		{
			if (policy is UnknownPolicy)
				return ((UnknownPolicy)policy).Data;
			
			string name;
			if (!policyTypes.TryGetValue (policyType, out name))
				throw new InvalidOperationException ("Cannot serialise unregistered policy type '" + policyType + "'");
			
			DataNode node;
			if (policy != null)
				node = Serializer.Serialize (policy);
			else
				node = new DataItem ();
			node.Name = name;
			return node;
		}
		
		internal static ScopedPolicy DiffDeserialize (DataItem item)
		{
			DataValue inheritVal = item.ItemData ["inheritsSet"] as DataValue;
			if (inheritVal == null || inheritVal.Value == "null")
				return RawDeserialize (item);
			
			Type t = GetRegisteredType (item.Name);
			if (t == null) {
				UnknownPolicy up = new UnknownPolicy (item);
				return new ScopedPolicy (typeof(UnknownPolicy), up, up.Scope);
			}
			
			item.ItemData.Remove (inheritVal);
			
			PolicySet set = GetPolicySetById (inheritVal.Value);
			if (set == null)
				throw new InvalidOperationException ("No policy set found for id '" + inheritVal.Value + "'");
			
			DataValue inheritScope = item.ItemData.Extract ("inheritsScope") as DataValue;
			
			object baseItem = set.Get (t, inheritScope != null ? inheritScope.Value : null);

			// If the policy was not found for the specified scope, try using the default policy.
			// Imprecise values are better than just crashing.
			if (baseItem == null && inheritScope != null)
				baseItem = set.Get (t);
			
			if (baseItem == null) {
				string msg = "Policy set '" + set.Id + "' does not contain a policy for '" + item.Name + "'";
				if (inheritScope != null)
					msg += ", scope '" + inheritScope.Value + "'";
				msg += ". This policy is likely provided by an addin that is not currently installed.";
				throw new InvalidOperationException (msg);
			}
			
			DataValue scopeVal = item.ItemData.Extract ("scope") as DataValue;
			DataNode baseline = RawSerialize (t, baseItem);
			ScopedPolicy p = RawDeserialize (ApplyOverlay (baseline, item));
			return new ScopedPolicy (t, p.Policy, scopeVal != null ? scopeVal.Value : null);
		}
		
		static PolicySet GetPolicySetById (string id)
		{
			foreach (PolicySet s in sets)
				if (s.Id == id)
					return s;
			return null;
		}
		
		internal static DataNode DiffSerialize (Type policyType, object policy, string scope, bool keepDeletedNodes = false, PolicySet diffBasePolicySet = null)
		{
			DataNode node = null;
			DataItem baseNode = null;

			if (policy is UnknownPolicy)
				return ((UnknownPolicy)policy).Data;
			
			DataNode raw = RawSerialize (policyType, policy);
			
			if (policy != null) {
				
				// By default, diff-serialize against the default instance of the policy. Much safer than
				// diffing against sets, which can change or not be present

				bool usedBaseSet = false, usedBaseScope = false;
				object diffBasePolicy = null;

				if (diffBasePolicySet != null) {
					// A set was given for diff serialization. Find a policy for this scope.
					diffBasePolicy = diffBasePolicySet.Get (policyType, scope);
					if (diffBasePolicy != null) {
						usedBaseSet = true;
						usedBaseScope = scope != null;
					} else {
						diffBasePolicy = diffBasePolicySet.Get (policyType);
						if (diffBasePolicy != null)
							usedBaseSet = true;
					}
				}
				if (diffBasePolicy == null)
					diffBasePolicy = Activator.CreateInstance (policyType);

				baseNode = RawSerialize (policyType, diffBasePolicy) as DataItem;
				int size = 0;
				node = ExtractOverlay (baseNode, raw, ref size);

				// Store the set and scope that was used for the diff serialization, if necessary
				if (usedBaseSet)
					((DataItem)node).ItemData.Add (new DataValue ("inheritsSet", diffBasePolicySet.Id));
				else if (keepDeletedNodes)
					((DataItem)node).ItemData.Add (new DataDeletedNode ("inheritsSet"));
				if (usedBaseScope)
					((DataItem)node).ItemData.Add (new DataValue ("inheritsScope", scope));
				else if (keepDeletedNodes)
					((DataItem)node).ItemData.Add (new DataDeletedNode ("inheritsScope"));
				
			} else {
				node = raw;
				((DataItem)node).ItemData.Add (new DataValue ("inheritsSet", "null"));
			}
			
			if (node != null) {
				if (keepDeletedNodes && baseNode != null) {
					foreach (var item in baseNode.ItemData) {
						item.IsDefaultValue = true;
					}
					foreach (var item in ((DataItem)node).ItemData) {
						var baseItem = baseNode.ItemData[item.Name];
						if (baseItem != null)
							baseNode.ItemData.Remove(baseItem);

						baseNode.ItemData.Add (item);
					}
					node = baseNode;
				}
				raw = node;
			}
			if (scope != null)
				((DataItem)raw).ItemData.Add (new DataValue ("scope", scope));
			return raw;
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
			DataValue rem = diffNode.Extract ("__removed") as DataValue;
			if (rem != null) {
				// Remove items marked as removed in the diff
				List<DataNode> toRemove = new List<DataNode> ();
				foreach (string removed in rem.Value.Split (' ')) {
					if (removed [0] == '@') {
						string aname = removed.Substring (1);
						DataNode n = baseline.ItemData [aname];
						if (n != null)
							toRemove.Add (n);
					} else {
						int n = int.Parse (removed, CultureInfo.InvariantCulture);
						if (n < baseline.ItemData.Count )
							toRemove.Add (baseline.ItemData [n]);
					}
				}
				foreach (var nod in toRemove)
					baseline.ItemData.Remove (nod);
			}
			
			List<DataNode> toAdd = new List<DataNode> ();
			HashSet<DataNode> applied = new HashSet<DataNode> ();
			
			foreach (DataNode node in diffNode.ItemData)
			{
				DataNode baselineNode = null;
				if (node is DataItem) {
					DataItem item = (DataItem) node;
					DataValue added = item.ItemData.Extract ("__added") as DataValue;
					if (added != null) {
						DataNode newNode = node;
						DataValue val = item.ItemData.Extract ("__value") as DataValue;
						if (val != null)
							newNode = new DataValue (node.Name, val.Value);
						int pos = int.Parse (added.Value, CultureInfo.InvariantCulture);
						if (pos > baseline.ItemData.Count)
							pos = baseline.ItemData.Count;
						baseline.ItemData.Insert (pos, newNode);
						continue;
					}
					DataValue index = item.ItemData.Extract ("__index") as DataValue;
					if (index != null) {
						int n = int.Parse (index.Value, CultureInfo.InvariantCulture);
						baselineNode = baseline.ItemData [n];
						DataValue val = item.ItemData.Extract ("__value") as DataValue;
						if (val != null) {
							baseline.ItemData [n] = new DataValue (node.Name, val.Value);
							continue;
						}
					}
				}
				
				if (baselineNode == null)
					baselineNode = baseline[node.Name];
				
				if (baselineNode != null && !applied.Add (baselineNode))
					baselineNode = null;
				
				if (baselineNode == null) {
					// New node
					toAdd.Add (node);
					continue;
				}
				
				if (baselineNode is DataValue) {
					int i = baseline.ItemData.IndexOf (baselineNode);
					baseline.ItemData [i] = node;
				} else {
					ApplyOverlay ((DataItem)baselineNode, (DataItem)node);
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
				if (val.Value == null)
					throw new InvalidOperationException ("Data node '" + val.Name + "' has null value, which cannot safely " +
					                                     "be diff-serialised.");
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
			HashSet<DataNode> extracted = new HashSet<DataNode> ();
			
			for (int n=0; n<diffNode.ItemData.Count; n++) {
				DataNode node = diffNode.ItemData [n];
				
				int index;
				DataNode overlayNode = GetBestOverlayNode (baseline, node, out index);
				
				if (overlayNode != null && !extracted.Add (overlayNode))
					overlayNode = null;
				
				if (overlayNode == null) {
					// The node is new.
					if (node is DataItem) {
						((DataItem)node).ItemData.Add (new DataValue ("__added", n.ToString (CultureInfo.InvariantCulture)) {StoreAsAttribute = true});
						newItem.ItemData.Add (node);
					}
					else {
						DataItem nval = new DataItem ();
						nval.Name = node.Name;
						nval.ItemData.Add (new DataValue ("__added", n.ToString (CultureInfo.InvariantCulture)) {StoreAsAttribute = true});
						nval.ItemData.Add (new DataValue ("__value", ((DataValue)node).Value) {StoreAsAttribute = true});
						newItem.ItemData.Add (nval);
					}
					continue;
				}
				
				DataValue val = overlayNode as DataValue;
				if (val != null) {
					if (val.Value != ((DataValue)node).Value) {
						size += val.Name.Length;
						if (val.Value == null)
							throw new InvalidOperationException ("Data node '" + val.Name + "' has null value, which cannot safely " +
							                                     "be diff-serialised.");
						size += val.Value.Length;
						if (index == -1)
							newItem.ItemData.Add (node);
						else {
							DataItem nval = new DataItem ();
							nval.Name = node.Name;
							nval.ItemData.Add (new DataValue ("__index", index.ToString (CultureInfo.InvariantCulture)) {StoreAsAttribute = true});
							nval.ItemData.Add (new DataValue ("__value", ((DataValue)node).Value) {StoreAsAttribute = true});
							newItem.ItemData.Add (nval);
						}
					}
				} else {
					DataItem childItem = ExtractOverlay ((DataItem) overlayNode, (DataItem) node, ref size);
					if (childItem != null && childItem.HasItemData) {
						size += childItem.Name.Length + childItem.Name.Length;
						newItem.ItemData.Add (childItem);
						if (index != -1)
							childItem.ItemData.Add (new DataValue ("__index", index.ToString (CultureInfo.InvariantCulture)) {StoreAsAttribute = true});
					}
				}
			}
			
			StringBuilder removed = StringBuilderCache.Allocate ();
			for (int n=0; n<baseline.ItemData.Count; n++) {
				DataNode node = baseline.ItemData [n];
				if (!extracted.Contains (node)) {
					if (removed.Length > 0)
						removed.Append (' ');
					if (baseline.UniqueNames && node is DataValue)
						removed.Append ("@").Append (node.Name);
					else
						removed.Append (n.ToString (CultureInfo.InvariantCulture));
				}
			}
			
			if (removed.Length > 0)
				newItem.ItemData.Add (new DataValue ("__removed", removed.ToString ()) {StoreAsAttribute = true});
			StringBuilderCache.Free (removed);
			return newItem;
		}
		
		static DataNode GetBestOverlayNode (DataItem baseline, DataNode node, out int index)
		{
			int bestIndex = -1;
			int bestSize = -1;
			
			for (int n=0; n<baseline.ItemData.Count; n++) {
				DataNode bnode = baseline.ItemData [n];
				if (bnode.Name != node.Name)
					continue;
				if (bestIndex == -1) {
					bestIndex = n;
					continue;
				}
				if (bestSize == -1)
					bestSize = CalcDiffSize (baseline.ItemData [bestIndex], node);
				
				int size = CalcDiffSize (bnode, node);
				if (size < bestSize) {
					bestSize = size;
					bestIndex = n;
				}
			}
			if (bestIndex == -1) {
				index = -1;
				return null;
			}
			
			if (bestSize != -1)
				index = bestIndex;
			else
				index = -1; // There is only one item
			
			return baseline.ItemData [bestIndex];
		}
		
		static int CalcDiffSize (DataNode node1, DataNode node2)
		{
			if (node1 is DataValue) {
				DataValue d1 = (DataValue)node1;
				DataValue d2 = node2 as DataValue;
				if (d2 == null)
					return int.MaxValue;
				if (d1.Value == d2.Value)
					return 0;
				else
					return d2.Value.Length;
			} else {
				DataItem it1 = (DataItem) node1;
				DataItem it2 = node2 as DataItem;
				if (it2 == null)
					return int.MaxValue;
				int size = 0;
				foreach (DataNode n2 in it2.ItemData) {
					DataNode n1 = it1.ItemData [n2.Name];
					if (n1 != null)
						size += CalcDiffSize (n1, n2);
					else
						size += CalcSize (n2);
				}
				return size;
			}
		}
		
		static int CalcSize (DataNode node)
		{
			DataValue val = node as DataValue;
			if (val != null)
				return node.Name.Length + (val.Value != null ? val.Value.Length : 0);
			else {
				int size = 0;
				foreach (DataNode n in ((DataItem)node).ItemData)
					size += CalcSize (n);
				return size;
			}
		}
		
		/// <summary>
		/// Gets a policy set.
		/// </summary>
		/// <returns>
		/// The policy set.
		/// </returns>
		/// <param name='name'>
		/// Name of the policy set
		/// </param>
		public static PolicySet GetPolicySet (string name)
		{
			foreach (PolicySet s in sets)
				if (s.Name == name)
					return s;
			return null;
		}
		
		/// <summary>
		/// Get all policy sets which define a specific policy
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		/// <typeparam name='T'>
		/// Type of the policy to look for
		/// </typeparam>
		public static IEnumerable<PolicySet> GetPolicySets<T> ()
		{
			return GetPolicySets<T> (false);
		}
		
		/// <summary>
		/// Get all policy sets which define a specific policy
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		public static IEnumerable<PolicySet> GetPolicySets<T> (bool includeHidden)
		{
			// The default policy set is always included, since it returns a default
			// instance for all types of policies.
			foreach (PolicySet s in sets)
				if (s.DirectHas<T> () && (s.Visible || includeHidden) || s.IsDefaultSet)
					yield return s;
		}
		
		/// <summary>
		/// Get all policy sets which define a policy under a specific scope
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		/// <param name='scope'>
		/// Scope under which the policy has to be defined (it can be for example a mime type)
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		public static IEnumerable<PolicySet> GetPolicySets<T> (string scope)
		{
			return GetPolicySets<T> (scope, false);
		}
		
		/// <summary>
		/// Get all policy sets which define a policy under a specific scope
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		/// <param name='scope'>
		/// Scope under which the policy has to be defined (it can be for example a mime type)
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		public static IEnumerable<PolicySet> GetPolicySets<T> (string scope, bool includeHidden)
		{
			// The default policy set is always included, since it returns a default
			// instance for all types of policies.
			foreach (PolicySet s in sets)
				if (s.DirectHas<T> (scope) && (s.Visible || includeHidden) || s.IsDefaultSet)
					yield return s;
		}
		
		/// <summary>
		/// Get all policy sets which define a policy under a specific set of scopes
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		/// <param name='scopes'>
		/// Scopes under which the policy has to be defined (it can be for example a hirearchy of mime types)
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		public static IEnumerable<PolicySet> GetPolicySets<T> (IEnumerable<string> scopes)
		{
			return GetPolicySets<T> (scopes, false);
		}
		
		/// <summary>
		/// Get all policy sets which define a policy under a specific set of scopes
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		/// <param name='scopes'>
		/// Scopes under which the policy has to be defined (it can be for example a hirearchy of mime types)
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		public static IEnumerable<PolicySet> GetPolicySets<T> (IEnumerable<string> scopes, bool includeHidden)
		{
			// The default policy set is always included, since it returns a default
			// instance for all types of policies.
			foreach (PolicySet s in sets)
				if (s.DirectHas<T> (scopes) && (s.Visible || includeHidden) || s.IsDefaultSet)
					yield return s;
		}
		
		/// <summary>
		/// Gets a list of sets which contain a specific policy value
		/// </summary>
		/// <returns>
		/// The matching sets.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		/// <remarks>
		/// This method returns a list of policy sets which define a policy of type T which is identical
		/// to the policy provided as argument.
		/// </remarks>
		public static IEnumerable<PolicySet> GetMatchingSets<T> (T policy) where T : class, IEquatable<T>, new ()
		{
			return GetMatchingSets<T> (policy, false);
		}
		
		/// <summary>
		/// Gets a list of sets which contain a specific policy value
		/// </summary>
		/// <returns>
		/// The matching sets.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for. Only sets containing this policy will be returned
		/// </typeparam>
		/// <remarks>
		/// This method returns a list of policy sets which define a policy of type T which is identical
		/// to the policy provided as argument.
		/// </remarks>
		public static IEnumerable<PolicySet> GetMatchingSets<T> (T policy, bool includeHidden) where T : class, IEquatable<T>, new ()
		{
			foreach (PolicySet ps in sets) {
				IEquatable<T> match = ps.Get<T> () as IEquatable<T>;
				if (match != null && (ps.Visible || includeHidden) && match.Equals (policy))
					yield return ps;
			}
		}
		
		/// <summary>
		/// Gets a policy set which contains a specific policy value
		/// </summary>
		/// <returns>
		/// The matching policy set.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for.
		/// </typeparam>
		/// <remarks>
		/// This method returns a policy set which defines a policy of type T which is identical
		/// to the policy provided as argument. If there are several matching policy sets, it
		/// returns the first it finds
		/// </remarks>
		public static PolicySet GetMatchingSet<T> (T policy) where T : class, IEquatable<T>, new ()
		{
			return GetMatchingSet<T> (policy, false);
		}
		
		/// <summary>
		/// Gets a policy set which contains a specific policy value
		/// </summary>
		/// <returns>
		/// The matching policy set.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for.
		/// </typeparam>
		/// <remarks>
		/// This method returns a policy set which defines a policy of type T which is identical
		/// to the policy provided as argument. If there are several matching policy sets, it
		/// returns the first it finds
		/// </remarks>
		public static PolicySet GetMatchingSet<T> (T policy, bool includeHidden) where T : class, IEquatable<T>, new ()
		{
			return GetMatchingSet (policy, sets, includeHidden);
		}
		
		/// <summary>
		/// Gets a policy set which contains a specific policy value
		/// </summary>
		/// <returns>
		/// The matching policy set.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <param name='candidateSets'>
		/// List of policy sets where to look for the specified policy
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for.
		/// </typeparam>
		/// <remarks>
		/// This method returns a policy set which defines a policy of type T which is identical
		/// to the policy provided as argument. If there are several matching policy sets, it
		/// returns the first it finds
		/// </remarks>
		public static PolicySet GetMatchingSet<T> (T policy, IEnumerable<PolicySet> candidateSets, bool includeHidden) where T : class, IEquatable<T>, new ()
		{
			foreach (PolicySet ps in candidateSets) {
				T match = ps.Get<T> ();
				if (match != null  && (ps.Visible || includeHidden) && match.Equals (policy))
					return ps;
			}
			return null;
		}
		
		/// <summary>
		/// Gets a policy sets which contains a specific policy value
		/// </summary>
		/// <returns>
		/// The policy set.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <param name='scopes'>
		/// Scopes under which the policy has to be defined (it can be for example a hirearchy of mime types)
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for.
		/// </typeparam>
		/// <remarks>
		/// This method returns a policy set which defines a policy of type T which is identical
		/// to the policy provided as argument. This policy has to be defined under one of the
		/// provided scopes. If there are several matching policy sets, it returns the first it finds.
		/// </remarks>
		public static PolicySet GetMatchingSet<T> (T policy, IEnumerable<string> scopes) where T : class, IEquatable<T>, new ()
		{
			return GetMatchingSet<T> (policy, scopes, false);
		}
		
		/// <summary>
		/// Gets a policy sets which contains a specific policy value
		/// </summary>
		/// <returns>
		/// The policy set.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <param name='scopes'>
		/// Scopes under which the policy has to be defined (it can be for example a hirearchy of mime types)
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for.
		/// </typeparam>
		/// <remarks>
		/// This method returns a policy set which defines a policy of type T which is identical
		/// to the policy provided as argument. This policy has to be defined under one of the
		/// provided scopes. If there are several matching policy sets, it returns the first it finds.
		/// </remarks>		
		public static PolicySet GetMatchingSet<T> (T policy, IEnumerable<string> scopes, bool includeHidden) where T : class, IEquatable<T>, new ()
		{
			return GetMatchingSet (policy, sets, scopes, includeHidden);
		}
		
		/// <summary>
		/// Gets a policy set which contains a specific policy value
		/// </summary>
		/// <returns>
		/// The policy set.
		/// </returns>
		/// <param name='policy'>
		/// Policy to be compared
		/// </param>
		/// <param name='candidateSets'>
		/// List of policy sets where to look for the specified policy
		/// </param>
		/// <param name='scopes'>
		/// Scopes under which the policy has to be defined (it can be for example a hirearchy of mime types)
		/// </param>
		/// <param name='includeHidden'>
		/// True if hidden (system) policy sets have to be returned, False otherwise.
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to look for.
		/// </typeparam>
		/// <remarks>
		/// This method returns a policy set which defines a policy of type T which is identical
		/// to the policy provided as argument. This policy has to be defined under one of the
		/// provided scopes. If there are several matching policy sets, it returns the first it finds.
		/// </remarks>		
		public static PolicySet GetMatchingSet<T> (T policy, IEnumerable<PolicySet> candidateSets, IEnumerable<string> scopes, bool includeHidden) where T : class, IEquatable<T>, new ()
		{
			foreach (PolicySet ps in candidateSets) {
				T match = ps.Get<T> (scopes);
				if (match != null && (ps.Visible || includeHidden) && match.Equals (policy))
					return ps;
			}
			return null;
		}
		
		static FilePath PoliciesFolder {
			get {
				return UserProfile.Current.UserDataRoot.Combine ("Policies");
			}
		}
		
		/// <summary>
		/// Gets a default policy.
		/// </summary>
		/// <returns>
		/// The default policy.
		/// </returns>
		/// <typeparam name='T'>
		/// Type of the policy to be returned
		/// </typeparam>
		/// <remarks>
		/// This method returns the default value for the specified policy type. It can be a value defined by
		/// the user using the default policy options panel, or a system default if the user didn't change it.
		/// </remarks>
		public static T GetDefaultPolicy<T> () where T : class, IEquatable<T>, new ()
		{
			return (T) GetDefaultPolicy (typeof (T));
		}

		internal static object GetDefaultPolicy (Type type)
		{
			// If the user has customized the default policy, return that. If not, return the IDE default.
			// (systemDefaultPolicies always returns a default policy, even if not explicitly defined)
			return userDefaultPolicies.Get (type) ?? systemDefaultPolicies.Get (type);
		}

		/// <summary>
		/// Gets a default policy for a specific scope
		/// </summary>
		/// <returns>
		/// The default policy.
		/// </returns>
		/// <param name='scope'>
		/// Scope under which the policy has to be defined
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to be returned
		/// </typeparam>
		/// <remarks>
		/// This method returns the default value for the specified policy type and scope. It can be a value defined by
		/// the user using the default policy options panel, or a system default if the user didn't change it.
		/// </remarks>
		public static T GetDefaultPolicy<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			// If the user has customized the default policy, return that. If not, return the IDE default.
			// (systemDefaultPolicies always returns a default policy, even if not explicitly defined)
			return userDefaultPolicies.Get<T> (scope) ?? systemDefaultPolicies.Get<T> (scope);
		}

		/// <summary>
		/// Gets a default policy for a specific set of scopes
		/// </summary>
		/// <returns>
		/// The default policy.
		/// </returns>
		/// <param name='scopes'>
		/// Scopes under which the policy has to be defined (it can be for example a hirearchy of mime types)
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to be returned
		/// </typeparam>
		/// <remarks>
		/// This method returns the default value of a policy type for a set of scopes. The policy is looked up under
		/// the provided scopes in sequence, and the first value found is the one returned. If no value is found,
		/// a system default is returned.
		/// </remarks>
		public static T GetDefaultPolicy<T> (IEnumerable<string> scopes) where T : class, IEquatable<T>, new ()
		{
			return (T)GetDefaultPolicy (typeof (T), scopes);
		}

		internal static object GetDefaultPolicy (Type policyType, IEnumerable<string> scopes)
		{
			// If the user has customized the default policy, return that. If not, return the IDE default.
			// (systemDefaultPolicies always returns a default policy, even if not explicitly defined)
			return userDefaultPolicies.Get (policyType, scopes) ?? systemDefaultPolicies.Get (policyType, scopes);
		}

		/// <summary>
		/// Gets default user-defined policy set
		/// </summary>
		public static PolicySet GetUserDefaultPolicySet ()
		{
			return userDefaultPolicies;
		}
		
		/// <summary>
		/// Gets default system-defined policy set
		/// </summary>
		public static PolicySet GetSystemDefaultPolicySet ()
		{
			return systemDefaultPolicies;
		}

		/// <summary>
		/// Gets the invariant policy set
		/// </summary>
		/// <remarks>
		/// The invariant policy set is a policy set whose values will not change in future MonoDevelop versions.
		/// </remarks>
		public static PolicyContainer InvariantPolicies {
			get { return invariantPolicies; }
		}
		

		/// <summary>
		/// Gets the system default policies
		/// </summary>
		/// <value>
		/// The default policies.
		/// </value>
		/// <remarks>
		/// The returned PolicyContainer can be used to query the system default value of policies
		/// </remarks>
		public static PolicyContainer SystemDefaultPolicies {
			get { return defaultPolicyBag; }
		}

		/// <summary>
		/// Gets the user default policies
		/// </summary>
		/// <value>
		/// The default policies.
		/// </value>
		/// <remarks>
		/// The returned PolicyContainer can be used to query the user default value of policies
		/// </remarks>
		public static PolicyContainer DefaultPolicies {
			get { return defaultPolicyBag; }
		}
		
		/// <summary>
		/// Determines whether a policy instance is an undefined policy
		/// </summary>
		/// <returns>
		/// <c>true</c> if the policy is undefined; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='policy'>
		/// Policy to check
		/// </param>
		/// <typeparam name='T'>
		/// Type of the policy to check
		/// </typeparam>
		public static bool IsUndefinedPolicy<T> (T policy)
		{
			return policy == null;
		}
		
		/// <summary>
		/// Gets a undefined policy value
		/// </summary>
		/// <returns>
		/// The undefined policy.
		/// </returns>
		/// <typeparam name='T'>
		/// Type of the policy
		/// </typeparam>
		public static T GetUndefinedPolicy<T> () where T : class, IEquatable<T>, new ()
		{
			return null;
		}
		
		/// <summary>
		/// Gets the policy sets defined by the user
		/// </summary>
		/// <returns>
		/// The user policy sets.
		/// </returns>
		public static IEnumerable<PolicySet> GetUserPolicySets ()
		{
			return userSets;
		}
		
		/// <summary>
		/// Adds a new user defined policy set
		/// </summary>
		/// <param name='pset'>
		/// The policy set
		/// </param>
		public static void AddUserPolicySet (PolicySet pset)
		{
			if (pset.Id != null)
				throw new ArgumentException ("User policy cannot have ID");
			if (string.IsNullOrEmpty (pset.Name))
				throw new ArgumentException ("User policy cannot have null or empty name");
			if (sets.Any (ps => ps.Name == pset.Name))
				throw new ArgumentException ("There is already a policy with the name ' " + pset.Name +  "'");

			userSets.Add (pset);
			sets.Add (pset);
		}
		
		/// <summary>
		/// Removes a user defined policy set
		/// </summary>
		/// <param name='pset'>
		/// The policy set
		/// </param>
		public static void RemoveUserPolicySet (PolicySet pset)
		{
			if (userSets.Remove (pset)) {
				deletedUserSets.Add (GetPolicyFile (pset));
				sets.Remove (pset);
			}
			else
				throw new InvalidOperationException ("The provided property set is not a user defined property set");
		}
		
		/// <summary>
		/// Get all defined policy sets
		/// </summary>
		/// <returns>
		/// The policy sets.
		/// </returns>
		public static IEnumerable<PolicySet> GetPolicySets ()
		{
			return sets;
		}
		
		
		/// <summary>
		/// Saves the policies.
		/// </summary>
		public static void SavePolicies ()
		{
			foreach (var file in deletedUserSets) {
				if (File.Exists (file))
					File.Delete (file);
			}
			deletedUserSets.Clear ();

			// User policies are saved using the system default policies as diff base.
			// In this way we ensure that only the smallest set of changes is serialized
			// WRT the system default. If the system defaults change, the new user defaults
			// will have the new values, with the exception of the values that the user
			// explicitly changed.

			SavePolicy (userDefaultPolicies, systemDefaultPolicies);
			foreach (PolicySet ps in userSets)
				SavePolicy (ps);
		}
		
		static void SavePolicy (PolicySet set, PolicySet diffBasePolicySet = null)
		{
			string file = GetPolicyFile (set);
			string friendlyName = string.Format ("policy '{0}'", set.Name);
			ParanoidSave (file, friendlyName, delegate (StreamWriter writer) {
				var xws = new XmlWriterSettings () {
					Indent = true,
				};
				using (var xw = XmlTextWriter.Create (writer, xws)) {
					xw.WriteStartElement ("Policies");
					set.SaveToXml (xw, diffBasePolicySet);
					xw.WriteEndElement ();
				}
			});
		}
		
		static string GetPolicyFile (PolicySet set)
		{
			return PoliciesFolder.Combine ((set.Id ?? set.Name) + ".mdpolicy.xml");
		}
		
		static void LoadPolicies ()
		{
			systemDefaultPolicies = GetPolicySetById ("Default");

			if (userDefaultPolicies != null)
				userDefaultPolicies.PolicyChanged -= DefaultPoliciesPolicyChanged;
			
			userSets.Clear ();
			userDefaultPolicies = null;

			try {
				if (Directory.Exists (PoliciesFolder)) {
					// Remove duplicate generated by a bug in the policy saving code
					foreach (var file in Directory.GetFiles (PoliciesFolder, "*.mdpolicy.mdpolicy.xml"))
						File.Delete (file);
					foreach (var file in Directory.GetFiles (PoliciesFolder, "*.mdpolicy.mdpolicy.xml.previous"))
						File.Delete (file);

					// User defaults are now stored in UserDefault.mdpolicy.xml, so if that file already exists we can
					// delete the old Default.mdpolicy.xml.
					if (File.Exists (PoliciesFolder.Combine ("Default.mdpolicy.xml")) && File.Exists (PoliciesFolder.Combine ("UserDefault.mdpolicy.xml")))
						File.Delete (PoliciesFolder.Combine ("Default.mdpolicy.xml"));

					foreach (var file in Directory.GetFiles (PoliciesFolder, "*.mdpolicy.xml")) {
						try {
							LoadPolicy (file);
						} catch (Exception ex) {
							LoggingService.LogError (
								string.Format ("Failed to load policy file '{0}'", Path.GetFileName (file)),
								ex
							);
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Policy load failed", ex);
			}
			
			if (userDefaultPolicies == null) {
				userDefaultPolicies = new PolicySet ("UserDefault", "User Default");
			}
			userDefaultPolicies.PolicyChanged += DefaultPoliciesPolicyChanged;
		}
		
		static void LoadPolicy (FilePath file)
		{
			string friendlyName = string.Format ("policy file '{0}'", file.FileName);
			ParanoidLoad (file, friendlyName, delegate (StreamReader reader) {
				var xr = XmlReader.Create (reader);
				xr.MoveToContent ();
				if (xr.LocalName == "PolicySet") {
					userDefaultPolicies = new PolicySet ("UserDefault", null);
					userDefaultPolicies.LoadFromXml (xr);
				} else if (xr.LocalName == "Policies" && !xr.IsEmptyElement) {
					xr.ReadStartElement ();
					xr.MoveToContent ();
					while (xr.NodeType != XmlNodeType.EndElement) {
						PolicySet pset = new PolicySet ();
						pset.LoadFromXml (xr);
						if (pset.Id == "Default" || pset.Id == "UserDefault") {
							pset.Id = "UserDefault";
							pset.Name = "User Default";
							userDefaultPolicies = pset;
						} else {
							// if the policy file does not have a name, use the file name as one
							if (string.IsNullOrEmpty (pset.Name)) {
								pset.Name = file.FileNameWithoutExtension;
							}

							AddUserPolicySet (pset);
						}
						xr.MoveToContent ();
					}
				}
			});
		}

		static void DefaultPoliciesPolicyChanged (object sender, PolicyChangedEventArgs e)
		{
			defaultPolicyBag.PropagatePolicyChangeEvent (e);
		}
		
		#region Paranoid load /save
		
		static bool ParanoidLoad (string fileName, string friendlyName, Action<StreamReader> read)
		{
			StreamReader reader = null;
			
			try {
				if (File.Exists (fileName)) {
					reader = new StreamReader (fileName, System.Text.Encoding.UTF8);
					read (reader);
					return true;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading {0} file '{1}':\n{2}", friendlyName, fileName, ex);
			}
			finally {
				if (reader != null) {
					reader.Close ();
					reader = null;
				}
			}
			
			//if it failed and a backup file exists, try that instead
			string backupFile = fileName + ".previous";
			try {
				if (File.Exists (backupFile)) {
					reader = new StreamReader (backupFile, System.Text.Encoding.UTF8);
					read (reader);
					return true;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading {0} backup file '{1}':\n{2}", friendlyName, backupFile, ex);
			}
			finally {
				if (reader != null)
					reader.Close ();
			}
			
			return false;
		}
		
		static bool ParanoidSave (string fileName, string friendlyName, Action<StreamWriter> write)
		{
			string backupFileName = fileName + ".previous";
			string dir = Path.GetDirectoryName (fileName);
			string tempFileName = Path.Combine (dir, ".#" + Path.GetFileName (fileName));
			
			try {
				if (!Directory.Exists (dir)) {
					Directory.CreateDirectory (dir);
				}
			} catch (IOException ex) {
				LoggingService.LogError ("Error creating directory '{0}' for {1} file\n{2}", dir, friendlyName, ex);
				return false;
			}
			
			//make a copy of the current file
			try {
				if (File.Exists (fileName)) {
					File.Copy (fileName, backupFileName, true);
				}
			} catch (IOException ex) {
				LoggingService.LogError ("Error copying {0} file '{1}' to backup\n{2}", friendlyName, fileName, ex);
			}
			
			//write out the new state to a temp file
			StreamWriter writer = null;
			try {
				
				writer = new StreamWriter (tempFileName, false, System.Text.Encoding.UTF8);
				write (writer);
				writer.Close ();
				writer = null;
				
				//write was successful (no exception)
				//so move the file to the real location, overwriting the old file
				//(NOTE: File.Move doesn't overwrite existing files, so using Mono.Unix)
				FileService.SystemRename (tempFileName, fileName);
				return true;
			}
			catch (Exception ex) {
				LoggingService.LogError ("Error writing {0} file '{1}'\n{2}", friendlyName, tempFileName, ex);
			}
			finally {
				if (writer != null)
					writer.Close ();
			}
			return false;
		}
		
		#endregion
	}
	
	class UnknownPolicy: IEquatable<UnknownPolicy>
	{
		static int upCount = 0;
		
		string scope;
		public DataNode Data;
		
		
		public UnknownPolicy (DataNode data)
		{
			Data = data;
			scope = "unknown:" + (++upCount);
		}
		
		public bool Equals (UnknownPolicy other)
		{
			return false;
		}
		
		public string Scope {
			get { return scope; }
		}
	}
	
	class InvariantPolicyBag: PolicyContainer
	{
		public override bool IsRoot {
			get {
				return true;
			}
		}
		
		public override PolicyContainer ParentPolicies {
			get {
				return null;
			}
		}
		
		protected override object GetDefaultPolicy (Type type)
		{
			return Activator.CreateInstance (type);
		}
		
		protected override object GetDefaultPolicy (Type type, IEnumerable<string> scopes)
		{
			return Activator.CreateInstance (type);
		}
	}

	class SystemDefaultPolicyBag: PolicyBag
	{
		protected override object GetDefaultPolicy (Type type)
		{
			return PolicyService.GetSystemDefaultPolicySet ().Get (type) ?? Activator.CreateInstance (type);
		}

		protected override object GetDefaultPolicy (Type type, IEnumerable<string> scopes)
		{
			return PolicyService.GetSystemDefaultPolicySet ().Get (type, scopes) ?? Activator.CreateInstance (type);
		}
	}
}