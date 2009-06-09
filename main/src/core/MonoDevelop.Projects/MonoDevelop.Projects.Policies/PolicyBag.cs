// 
// Policy.cs
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core.Serialization;


namespace MonoDevelop.Projects.Policies
{
	
	[DataItem ("Policies")]
	public class PolicyBag: ICustomDataItem, IPolicyContainer
	{
		PolicyDictionary policies;
		internal bool ReadOnly { get; set; }
		
		public PolicyBag (SolutionItem owner)
		{
			this.Owner = owner;
		}
		
		internal PolicyBag ()
		{
		}
		
		public SolutionItem Owner { get; internal set; }
		
		public bool IsEmpty {
			get { return policies == null || policies.Count == 0; }
		}
		
		public T Get<T> () where T : class, IEquatable<T>, new ()
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (typeof(T), null, out policy)) {
					if (!PolicyService.IsUndefinedPolicy (policy))
						return (T)policy;
					else
						return PolicyService.GetDefaultPolicy<T> ();
				}
			}
			if (IsRoot)
				return PolicyService.GetDefaultPolicy<T> ();
			else
				return Owner.ParentFolder.Policies.Get<T> ();
		}
		
		public T Get<T> (IEnumerable<string> scopes) where T : class, IEquatable<T>, new ()
		{
			// The search is done vertically, looking first at the parents
			foreach (string scope in scopes) {
				IPolicyContainer currentBag = this;
				while (currentBag != null) {
					if (currentBag.DirectHas<T> (scope)) {
						T pol = currentBag.DirectGet<T> (scope);
						if (!PolicyService.IsUndefinedPolicy (pol))
							return pol;
						// If the bag has the policy (Has<> returns true) but the policy is undefined,
						// then we have to keep looking using the base scopes.
						// We start looking from the original bag, using the new scope.
						break;
					} else
						currentBag = currentBag.ParentPolicies;
				}
			}
			return PolicyService.GetDefaultPolicy<T>(scopes);
		}
		
		// Gets policy directly from the bag without cacading or creating default instances
		T IPolicyContainer.DirectGet<T> ()
		{
			return ((IPolicyContainer)this).DirectGet<T> ((string)null);
		}
		
		T IPolicyContainer.DirectGet<T> (string scope)
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (typeof(T), scope, out policy))
					return (T) policy;
			}
			return null;
		}
		
		public bool IsRoot {
			get { return Owner == null || Owner.ParentFolder == null; }
		}
		
		IPolicyContainer IPolicyContainer.ParentPolicies {
			get {
				if (Owner != null && Owner.ParentFolder != null)
					return Owner.ParentFolder.Policies;
				else
					return null;
			}
		}
		
		public void Set<T> (T value) where T : class, IEquatable<T>, new ()
		{
			Set (value, null);
		}
		
		public void Set<T> (T value, string scope) where T : class, IEquatable<T>, new ()
		{
			CheckReadOnly ();
			PolicyKey key = new PolicyKey (typeof(T), scope);
			System.Diagnostics.Debug.Assert (key.Scope == scope);
			
			if (policies == null) {
				policies = new PolicyDictionary ();
			} else {
				object oldVal = null;
				policies.TryGetValue (key, out oldVal);
				if (oldVal != null && ((IEquatable<T>)oldVal).Equals (value))
					return;
			}
			
			policies[key] = value;
			OnPolicyChanged (key.PolicyType, key.Scope);
		}
		
		public bool Remove<T> () where T : class, IEquatable<T>, new ()
		{
			return Remove<T> (null);
		}
		
		public bool Remove<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			CheckReadOnly ();
			if (policies != null) {
				if (policies.Remove (new PolicyKey (typeof(T), scope))) {
					OnPolicyChanged (typeof(T), scope);
					if (policies.Count == 0)
						policies = null;
					return true;
				}
			}
			return false;
		}
		
		bool IPolicyContainer.DirectHas<T> ()
		{
			return ((IPolicyContainer)this).DirectHas<T> ((string)null);
		}
		
		bool IPolicyContainer.DirectHas<T> (string scope)
		{
			return policies != null && policies.ContainsKey (new PolicyKey (typeof(T), scope));
		}
		
		bool DirectHas (Type type, string scope)
		{
			return policies != null && policies.ContainsKey (new PolicyKey (type, scope));
		}
		
		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			if (policies == null)
				return null;
			
			DataCollection dc = new DataCollection ();
			foreach (KeyValuePair<PolicyKey,object> p in policies)
				dc.Add (PolicyService.DiffSerialize (p.Key.PolicyType, p.Value, p.Key.Scope));
			return dc;
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			if (data.Count == 0)
				return;
			
			policies = new PolicyDictionary ();
			foreach (DataNode node in data) {
				ScopedPolicy val = PolicyService.DiffDeserialize (node);
				policies.Add (val);
			}
		}
		
		internal void PropagatePolicyChangeEvent (PolicyChangedEventArgs args)
		{
			if (PolicyChanged != null)
				PolicyChanged (this, args);
			SolutionFolder solFol = Owner as SolutionFolder;
			if (solFol != null)
				foreach (SolutionItem item in solFol.Items)
					if (!item.Policies.DirectHas (args.PolicyType, args.Scope))
						item.Policies.PropagatePolicyChangeEvent (args);
		}
		
		void CheckReadOnly ()
		{
			if (ReadOnly)
				throw new InvalidOperationException ("This PolicyBag can't be modified");
		}
		
		protected void OnPolicyChanged (Type policyType, string scope)
		{
			PropagatePolicyChangeEvent (new PolicyChangedEventArgs (policyType, scope));
		}
		
		public event EventHandler<PolicyChangedEventArgs> PolicyChanged;
	}
	
	internal struct PolicyKey : IEquatable<PolicyKey>
	{
		public Type PolicyType { get; private set; }
		public string Scope { get; private set; }
		
		public PolicyKey (Type policyType, string scope)
		{
			this.PolicyType = policyType;
			this.Scope = scope;
		}
		
		public override bool Equals (object obj)
		{
			return obj is PolicyKey && Equals ((PolicyKey)obj);
		}
		
		public bool Equals (PolicyKey other)
		{
			return other.PolicyType.AssemblyQualifiedName == PolicyType.AssemblyQualifiedName && other.Scope == Scope;
		}
		
		public override int GetHashCode ()
		{
			int code = PolicyType.AssemblyQualifiedName.GetHashCode ();
			unchecked {
				if (Scope != null)
					code += Scope.GetHashCode ();
			}
			return code;
		}
		
		public override string ToString ()
		{
			if (Scope != null)
				return string.Format("[Policy: Type={0}, scope={1}]", PolicyType, Scope);
			else
				return string.Format("[Policy: Type={0}]", PolicyType);
		}
	}
	
	internal class PolicyDictionary : Dictionary<PolicyKey, object>
	{
		public PolicyDictionary ()
		{
		}
		
		public object this [Type policyType] {
			get { return this [new PolicyKey (policyType, null)]; }
			set { this [new PolicyKey (policyType, null)] = value; }
		}
		
		public object this [Type policyType, string scope] {
			get { return this [new PolicyKey (policyType, scope)]; }
			set { this [new PolicyKey (policyType, scope)] = value; }
		}
		
		public bool TryGetValue (Type policyTypeKey, string scopeKey, out object value)
		{
			return TryGetValue (new PolicyKey (policyTypeKey, scopeKey), out value);
		}
		
		public bool TryGetValue (Type policyTypeKey, out object value)
		{
			return TryGetValue (new PolicyKey (policyTypeKey, null), out value);
		}
		
		public void Add (ScopedPolicy scopedPolicy)
		{
			Add (new PolicyKey (scopedPolicy.PolicyType, scopedPolicy.Scope), scopedPolicy.Policy);
		}
		
		public bool ContainsKey (Type policyType, string scope)
		{
			return ContainsKey (new PolicyKey (policyType, scope));
		}
	}
}
