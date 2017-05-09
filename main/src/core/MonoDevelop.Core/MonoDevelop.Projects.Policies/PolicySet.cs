// 
// PolicySet.cs
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
using System.IO;
using System.Xml;

using MonoDevelop.Core.Serialization;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Policies
{
	/// <summary>
	/// A named set of policies.
	/// </summary>
	public sealed class PolicySet: PolicyContainer
	{
		HashSet<PolicyKey> externalPolicies = new HashSet<PolicyKey> ();
		
		internal PolicySet (string id, string name)
		{
			this.Id = id;
			this.Name = name;
			Visible = true;
		}
		
		public PolicySet ()
		{
			Visible = true;
		}
		
		public override bool IsRoot {
			get { return true; }
		}

		internal bool IsDefaultSet {
			get { return Id == "Default"; }
		}

		/// <summary>
		/// When set to false, this policy set is not visible to the user. This flag can be used
		/// to deprecate existing policy sets (since registered policy sets can't be modified/removed).
		/// </summary>
		public bool Visible { get; set; }
		
		/// <summary>
		/// When set to true, this policy can be used as a base for a differential serialization. It's false by default
		/// </summary>
		public bool AllowDiffSerialize { get; internal set; }
		
		public override PolicyContainer ParentPolicies {
			get { return null; }
		}
		
		protected override object GetDefaultPolicy (Type type)
		{
			// The default policy set always resturns a value for any type of policy.
			if (IsDefaultSet)
				return Activator.CreateInstance (type);
			return null;
		}
		
		protected override object GetDefaultPolicy (Type type, IEnumerable<string> scopes)
		{
			if (IsDefaultSet)
				return Activator.CreateInstance (type);
			return null;
		}
		
		public string Name { get; set; }
		public string Id { get; internal set; }
		
		internal PolicyKey[] AddSerializedPolicies (StreamReader reader)
		{
			if (policies == null)
				policies = new PolicyDictionary ();
			var keys = new List<PolicyKey> ();
			foreach (ScopedPolicy policyPair in PolicyService.RawDeserializeXml (reader)) {
				PolicyKey key = new PolicyKey (policyPair.PolicyType, policyPair.Scope);
				if (policies.ContainsKey (key))
					throw new InvalidOperationException ("Cannot add second policy of type '" +  
					                                     key.ToString () + "' to policy set '" + Id + "'");
				keys.Add (key);
				policies[key] = policyPair.Policy;
				if (!policyPair.SupportsDiffSerialize)
					externalPolicies.Add (key);
			}
			return keys.ToArray ();
		}
		
		internal bool SupportsDiffSerialize (ScopedPolicy pol)
		{
			if (!AllowDiffSerialize)
				return false;
			PolicyKey pk = new PolicyKey (pol.PolicyType, pol.Scope);
			return !externalPolicies.Contains (pk);
		}
		
		public void SaveToFile (FilePath file)
		{
			using (StreamWriter sw = new StreamWriter (file))
				SaveToFile (sw);
		}
		
		internal void SaveToFile (StreamWriter writer)
		{
			XmlWriterSettings xws = new XmlWriterSettings ();
			xws.Indent = true;
			XmlWriter xw = XmlTextWriter.Create(writer, xws);
			using (xw) {
				SaveToXml (xw);
			}
		}
		
		internal void SaveToXml (XmlWriter xw, PolicySet diffBasePolicySet = null)
		{
			XmlConfigurationWriter cw = new XmlConfigurationWriter ();
			cw.StoreAllInElements = true;
			cw.StoreInElementExceptions = new String[] { "scope", "inheritsSet", "inheritsScope" };
			xw.WriteStartElement ("PolicySet");
			if (!string.IsNullOrEmpty (Name))
				xw.WriteAttributeString ("name", Name);
			if (!string.IsNullOrEmpty (Id))
				xw.WriteAttributeString ("id", Id);
			if (policies != null) {
				foreach (KeyValuePair<PolicyKey, object> policyPair in policies)
					cw.Write (xw, PolicyService.DiffSerialize (policyPair.Key.PolicyType, policyPair.Value, policyPair.Key.Scope, diffBasePolicySet: diffBasePolicySet));
			}
			xw.WriteEndElement ();
		}
		
		public void LoadFromFile (FilePath file)
		{
			using (StreamReader sr = new StreamReader (file))
				LoadFromFile (sr);
		}
		
		internal void LoadFromFile (StreamReader reader)
		{
			var xr = XmlReader.Create (reader);
			LoadFromXml (xr);
		}
		
		internal void LoadFromXml (XmlReader reader)
		{
			if (policies == null)
				policies = new PolicyDictionary ();
			else
				policies.Clear ();
			
			reader.MoveToContent ();
			string str = reader.GetAttribute ("name");
			if (!string.IsNullOrEmpty (str))
				Name = str;
			str = reader.GetAttribute ("id");
			if (!string.IsNullOrEmpty (str))
				Id = str;
			reader.MoveToElement ();
			
			//note: can't use AddSerializedPolicies as we want diff serialisation
			foreach (ScopedPolicy policyPair in PolicyService.DiffDeserializeXml (reader)) {
				PolicyKey key = new PolicyKey (policyPair.PolicyType, policyPair.Scope);
				if (policies.ContainsKey (key))
					throw new InvalidOperationException (
						"Cannot add second policy of type '" + key + "' to policy set '" + Id + "'"
					);
				policies[key] = policyPair.Policy;
			}
		}
		
		public PolicySet Clone ()
		{
			PolicySet p = new PolicySet ();
			p.CopyFrom (this);
			p.Name = Name;
			return p;
		}
	}
}
