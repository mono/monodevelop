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
using System.Collections.Generic;
using System.IO;
using System.Xml;

using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Policies
{
	public class PolicySet: PolicyContainer
	{
		internal PolicySet (string id, string name)
		{
			this.Id = id;
			this.Name = name;
		}
		
		public override bool IsRoot {
			get { return true; }
		}
		
		public override PolicyContainer ParentPolicies {
			get { return null; }
		}
		
		protected override bool InheritDefaultPolicies {
			get { return false; }
		}
		
		public string Name { get; private set; }
		public string Id { get; private set; }
		
		internal void AddSerializedPolicies (StreamReader reader)
		{
			if (policies == null)
				policies = new PolicyDictionary ();
			foreach (ScopedPolicy policyPair in PolicyService.RawDeserializeXml (reader)) {
				PolicyKey key = new PolicyKey (policyPair.PolicyType, policyPair.Scope);
				if (policies.ContainsKey (key))
					throw new InvalidOperationException ("Cannot add second policy of type '" +  
					                                     key.ToString () + "' to policy set '" + Id + "'");
				policies[key] = policyPair.Policy;
			}
		}
		
		internal void RemoveSerializedPolicies (StreamReader reader)
		{
			if (policies == null)
				return;
			// NOTE: this could be more efficient if it just got the types instead of a 
			// full deserialisation
			foreach (ScopedPolicy policyPair in PolicyService.RawDeserializeXml (reader))
				policies.Remove (new PolicyKey (policyPair.PolicyType, policyPair.Scope));
		}
		
		internal void SaveToFile (StreamWriter writer)
		{
			XmlWriterSettings xws = new XmlWriterSettings ();
			xws.Indent = true;
			XmlConfigurationWriter cw = new XmlConfigurationWriter ();
			cw.StoreAllInElements = true;
			cw.StoreInElementExceptions = new String[] { "scope", "inheritsSet", "inheritsScope" };
			using (XmlWriter xw = XmlTextWriter.Create(writer, xws)) {
				xw.WriteStartElement ("PolicySet");
				if (policies != null) {
					foreach (KeyValuePair<PolicyKey,object> policyPair in policies)
						cw.Write (xw, PolicyService.DiffSerialize (policyPair.Key.PolicyType, policyPair.Value, policyPair.Key.Scope));
				}
				xw.WriteEndElement ();
			}
		}
		
		internal void LoadFromFile (StreamReader reader)
		{
			if (policies == null)
				policies = new PolicyDictionary ();
			else
				policies.Clear ();
			
			//note: can't use AddSerializedPolicies as we want diff serialisation
			foreach (ScopedPolicy policyPair in PolicyService.DiffDeserializeXml (reader)) {
				PolicyKey key = new PolicyKey (policyPair.PolicyType, policyPair.Scope);
				if (policies.ContainsKey (key))
					throw new InvalidOperationException ("Cannot add second policy of type '" +  
					                                     key.ToString () + "' to policy set '" + Id + "'");
				policies[key] = policyPair.Policy;
			}
		}
	}
}
