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

namespace MonoDevelop.Projects.Policies
{
	public abstract class PolicyContainer
	{
		internal PolicyDictionary policies;
		
		public bool IsEmpty {
			get { return policies == null || policies.Count == 0; }
		}
		
		// The Get methods return policies taking into account inheritance. If a policy
		// can't be found it may return null, but never an 'undefined' policy.
		public T Get<T> () where T : class, IEquatable<T>, new ()
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (typeof(T), null, out policy)) {
					if (!PolicyService.IsUndefinedPolicy (policy))
						return (T)policy;
					else if (InheritDefaultPolicies)
						return PolicyService.GetDefaultPolicy<T> ();
					else
						return null;
				}
			}
			if (!InheritDefaultPolicies)
				return null;
			else if (IsRoot)
				return PolicyService.GetDefaultPolicy<T> ();
			else
				return ParentPolicies.Get<T> ();
		}
		
		public T Get<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			return Get<T> (new string[] { scope });
		}
		
		public T Get<T> (IEnumerable<string> scopes) where T : class, IEquatable<T>, new ()
		{
			// The search is done vertically, looking first at the parents
			foreach (string scope in scopes) {
				PolicyContainer currentBag = this;
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
			if (InheritDefaultPolicies)
				return PolicyService.GetDefaultPolicy<T>(scopes);
			else
				return null;
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
			foreach (KeyValuePair<PolicyKey,object> pinfo in policies) {
				if (pinfo.Key.PolicyType == t)
					yield return new ScopedPolicy (t, pinfo.Value, pinfo.Key.Scope);
			}
			object pol = Get (t);
			if (pol != null)
				yield return new ScopedPolicy (t, pol, null);
		}
		
		internal object Get (Type type)
		{
			return Get (type, (string) null);
		}
		
		internal object Get (Type type, string scope)
		{
			if (policies == null)
				return null;
			object o;
			if (policies.TryGetValue (type, scope, out o)) {
				if (PolicyService.IsUndefinedPolicy (o))
					return null;
			}
			return o;
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
			return DirectGet<T> (null);
		}
		
		public T DirectGet<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (typeof(T), scope, out policy))
					return (T) policy;
			}
			return null;
		}
		
		public bool DirectHas<T> ()
		{
			return DirectHas<T> ((string)null);
		}
		
		public bool DirectHas<T> (string scope)
		{
			return policies != null && policies.ContainsKey (new PolicyKey (typeof(T), scope));
		}
		
		public bool DirectHas<T> (IEnumerable<string> scopes)
		{
			foreach (string scope in scopes) {
				if (DirectHas<T> (scope))
					return true;
			}
			return false;
		}

		public abstract PolicyContainer ParentPolicies { get; }
		
		#endregion
		
		protected abstract bool InheritDefaultPolicies { get; }
		
		protected virtual void OnPolicyChanged (Type policyType, string scope)
		{
			if (PolicyChanged != null)
				PolicyChanged (this, new PolicyChangedEventArgs (policyType, scope));
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
	}
}
