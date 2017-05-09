// 
// PolicyContainer.cs
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Projects.Policies
{
	/// <summary>
	/// A set of policies. Policies are identified by type.
	/// </summary>
	public abstract class PolicyContainer: IPolicyProvider
	{
		internal PolicyDictionary policies;
		
		/// <summary>
		/// Returns true if there isn't any policy defined.
		/// </summary>
		public bool IsEmpty {
			get { return policies == null || policies.Count == 0; }
		}
		
		/// <summary>
		/// The Get methods return policies taking into account inheritance. If a policy
		/// can't be found it may return null, but never an 'undefined' policy.
		/// </summary>
		/// <returns>
		/// The policy of the given type, or null if not found.
		/// </returns>
		public T Get<T> () where T : class, IEquatable<T>, new ()
		{
			return (T)Get (typeof (T));
		}

		public T Get<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			return (T)Get (typeof (T), new string [] { scope });
		}
		
		public T Get<T> (IEnumerable<string> scopes) where T : class, IEquatable<T>, new ()
		{
			return (T)Get (typeof (T), scopes);
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
			} else if (value != null) {
				object oldVal = null;
				policies.TryGetValue (key, out oldVal);
				if (oldVal != null && ((IEquatable<T>)oldVal).Equals (value))
					return;
			}
			
			policies[key] = value;
			OnPolicyChanged (key.PolicyType, key.Scope);
		}
		
		internal void InternalSet (Type t, string scope, object ob)
		{
			PolicyKey key = new PolicyKey (t, scope);
			if (policies == null)
				policies = new PolicyDictionary ();
			policies[key] = ob;
			OnPolicyChanged (key.PolicyType, key.Scope);
		}
		
		/// <summary>
		/// Removes all policies defined in this container
		/// </summary>
		public void Clear ()
		{
			PolicyDictionary oldPolicies = policies;
			policies = null;
			foreach (PolicyKey pk in oldPolicies.Keys)
				OnPolicyChanged (pk.PolicyType, pk.Scope);
		}
		
		public bool Remove<T> () where T : class, IEquatable<T>, new ()
		{
			CheckReadOnly ();
			return Remove<T> (null);
		}
		
		public bool Remove<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			CheckReadOnly ();
			return InternalRemove (typeof(T), scope);
		}
			
		internal bool InternalRemove (Type type, string scope)
		{
			if (policies != null) {
				if (policies.Remove (new PolicyKey (type, scope))) {
					OnPolicyChanged (type, scope);
					if (policies.Count == 0)
						policies = null;
					return true;
				}
			}
			return false;
		}
		
		internal void RemoveAll (Type type)
		{
			if (policies != null) {
				foreach (var p in policies.ToArray ()) {
					if (p.Key.PolicyType == type) {
						policies.Remove (p.Key);
						OnPolicyChanged (type, p.Key.Scope);
					}
				}
				if (policies.Count == 0)
					policies = null;
			}
		}

		internal void RemoveAll (PolicyKey[] keys)
		{
			if (policies != null) {
				foreach (var k in keys) {
					policies.Remove (k);
					OnPolicyChanged (k.PolicyType, k.Scope);
				}
				if (policies.Count == 0)
					policies = null;
			}
		}

		/// <summary>
		/// Copies the policies defined in another container
		/// </summary>
		/// <param name='other'>
		/// A policy container from which to copy the policies
		/// </param>
		/// <remarks>
		/// Policies of this container are removed or replaced by policies defined in the
		/// provided container.
		/// </remarks>
		public void CopyFrom (PolicyContainer other)
		{
			if (other.policies == null && policies == null)
				return;

			// Add and update policies
			
			if (other.policies != null) {
				foreach (KeyValuePair<PolicyKey, object> p in other.policies) {
					object oldVal;
					if (policies == null || !policies.TryGetValue (p.Key, out oldVal) || oldVal == null || !oldVal.Equals (p.Value)) {
						if (policies == null)
							policies = new PolicyDictionary ();
						policies [p.Key] = p.Value;
						OnPolicyChanged (p.Key.PolicyType, p.Key.Scope);
					}
				}
			}
			
			// Remove policies
			
			if (policies != null) {
				foreach (PolicyKey k in policies.Keys.ToArray ()) {
					if (other.policies == null || !other.policies.ContainsKey (k)) {
						policies.Remove (k);
						OnPolicyChanged (k.PolicyType, k.Scope);
					}
				}
			}
		}

		/// <summary>
		/// Import the policies defined by another policy container
		/// </summary>
		/// <param name='source'>
		/// The policy container to be imported
		/// </param>
		/// <param name='includeParentPolicies'>
		/// If <c>true</c>, policies defined by all ancestors of polContainer will also
		/// be imported
		/// </param>
		/// <remarks>
		/// This method adds or replaces policies defined in the source container into
		/// this container. Policies in this container which are not defined in the source container
		/// are not modified or removed.
		/// </remarks>
		public void Import (PolicyContainer source, bool includeParentPolicies)
		{
			if (includeParentPolicies && source.ParentPolicies != null)
				Import (source.ParentPolicies, true);
			
			if (source.policies == null && policies == null)
				return;

			// Add and update policies
			
			if (source.policies != null) {
				foreach (KeyValuePair<PolicyKey, object> p in source.policies) {
					object oldVal;
					if (policies == null || !policies.TryGetValue (p.Key, out oldVal) || oldVal == null || !oldVal.Equals (p.Value)) {
						if (policies == null)
							policies = new PolicyDictionary ();
						policies [p.Key] = p.Value;
						OnPolicyChanged (p.Key.PolicyType, p.Key.Scope);
					}
				}
			}
		}
		
		public IEnumerable<string> GetScopes<T> ()
		{
			foreach (PolicyKey pk in policies.Keys) {
				if (pk.PolicyType == typeof(T))
					yield return pk.Scope;
			}
		}
		
		public IEnumerable<ScopedPolicy<T>> GetScoped<T> () where T : class, IEquatable<T>, new ()
		{
			if (policies == null)
				yield break;
			
			foreach (KeyValuePair<PolicyKey,object> pinfo in policies) {
				if (pinfo.Key.PolicyType == typeof(T))
					yield return new ScopedPolicy<T> ((T)pinfo.Value, pinfo.Key.Scope);
			}
			T pol = Get<T> ();
			if (pol != null && !PolicyService.IsUndefinedPolicy (pol))
				yield return new ScopedPolicy<T> (pol, null);
		}
		
		internal IEnumerable<ScopedPolicy> GetScoped (Type t)
		{
			if (policies != null) {
				foreach (KeyValuePair<PolicyKey,object> pinfo in policies) {
					if (pinfo.Key.PolicyType == t)
						yield return new ScopedPolicy (t, pinfo.Value, pinfo.Key.Scope);
				}
			}
			object pol = Get (t);
			if (pol != null)
				yield return new ScopedPolicy (t, pol, null);
		}
		
		internal object Get (Type type)
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (type, null, out policy)) {
					if (!PolicyService.IsUndefinedPolicy (policy))
						return policy;
					return GetDefaultPolicy (type);
				}
			}
			if (IsRoot)
				return GetDefaultPolicy (type);
			else
				return ParentPolicies.Get (type);
		}
		
		internal object Get (Type type, string scope)
		{
			return Get (type, new [] { scope });
		}

		internal object Get (Type type, IEnumerable<string> scopes)
		{
			// The search is done vertically, looking first at the parents
			foreach (string scope in scopes) {
				PolicyContainer currentBag = this;
				while (currentBag != null) {
					if (currentBag.DirectHas (type, scope)) {
						object pol = currentBag.DirectGet (type, scope);
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
			return GetDefaultPolicy (type, scopes);
		}

		/// <summary>
		/// Gets a list of all policies defined in this container (not inherited)
		/// </summary>
		public IEnumerable<ScopedPolicy> DirectGetAll ()
		{
			if (policies == null)
				return new ScopedPolicy [0];
			return policies.Select (pk => new ScopedPolicy (pk.Key.PolicyType, pk.Value, pk.Key.Scope));
		}
		
		public abstract bool IsRoot { get; }
		
		public event EventHandler<PolicyChangedEventArgs> PolicyChanged;
		
		#region Methods to be used for fine grained management of policies
		
		// The DirectGet set of methods returns policies directly stored in the container,
		// ignoring inherited policies. Those methods can return undefined policies,
		// so return values have to be checked with PolicyService.IsUndefinedPolicy
		
		internal PolicyDictionary Policies { get { return policies; } } 
		
		public T DirectGet<T> () where T : class, IEquatable<T>, new ()
		{
			return (T) DirectGet (typeof(T), null);
		}
		
		public T DirectGet<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			return (T)DirectGet (typeof (T), scope);
		}

		internal object DirectGet (Type type)
		{
			return DirectGet (type, null);
		}

		internal object DirectGet (Type type, string scope)
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (type, scope, out policy))
					return policy;
			}
			return null;
		}

		public bool DirectHas<T> ()
		{
			return DirectHas (typeof (T), (string)null);
		}
		
		public bool DirectHas<T> (string scope)
		{
			return DirectHas (typeof (T), scope);
		}
		
		public bool DirectHas<T> (IEnumerable<string> scopes)
		{
			return DirectHas (typeof (T), scopes);
		}

		internal bool DirectHas (Type type)
		{
			return DirectHas (type, (string)null);
		}

		internal bool DirectHas (Type type, string scope)
		{
			return policies != null && policies.ContainsKey (new PolicyKey (type, scope));
		}

		internal bool DirectHas (Type type, IEnumerable<string> scopes)
		{
			foreach (string scope in scopes) {
				if (DirectHas (type, scope))
					return true;
			}
			return false;
		}

		/// <summary>
		/// The set of policies from which inherit policies when not found in this container
		/// </summary>
		public abstract PolicyContainer ParentPolicies { get; }
		
		#endregion
		
		protected virtual void OnPolicyChanged (Type policyType, string scope)
		{
			if (PolicyChanged != null)
				PolicyChanged (this, new PolicyChangedEventArgs (policyType, scope));
		}
		
		protected virtual object GetDefaultPolicy (Type type)
		{
			return PolicyService.GetDefaultPolicy (type);
		}
		
		protected virtual object GetDefaultPolicy (Type type, IEnumerable<string> scopes)
		{
			return PolicyService.GetDefaultPolicy (type, scopes);
		}
		
		public virtual bool ReadOnly {
			get;
			internal set;
		}
		
		void CheckReadOnly ()
		{
			if (ReadOnly)
				throw new InvalidOperationException ("This PolicyContainer can't be modified");
		}
		
		PolicyContainer IPolicyProvider.Policies {
			get {
				return this;
			}
		}
	}
}
