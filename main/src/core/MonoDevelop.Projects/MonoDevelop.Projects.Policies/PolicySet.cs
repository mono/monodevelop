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
	public interface IPolicyContainer
	{
		// The Get methods return policies taking into account inheritance. If a policy
		// can't be found it may return null, but never an 'undefined' policy.
		T Get<T> () where T : class, IEquatable<T>, new ();
		T Get<T> (IEnumerable<string> scopes) where T : class, IEquatable<T>, new ();
		
		void Set<T> (T value) where T : class, IEquatable<T>, new ();
		void Set<T> (T value, string scope) where T : class, IEquatable<T>, new ();
		bool Remove<T> ()  where T : class, IEquatable<T>, new ();
		bool Remove<T> (string scope)  where T : class, IEquatable<T>, new ();
		
		bool IsRoot { get; }
		
		event EventHandler<PolicyChangedEventArgs> PolicyChanged;
		
		#region Methods to be used for fine grained management of policies
		
		// The DirectGet set of methods returns policies directly stored in the container,
		// ignoring inherited policies. Those methods can return undefined policies,
		// so return values have to be checked with PolicyService.IsUndefinedPolicy
		T DirectGet<T> () where T : class, IEquatable<T>, new ();
		T DirectGet<T> (string scope) where T : class, IEquatable<T>, new ();
		bool DirectHas<T> () where T : class, IEquatable<T>, new ();
		bool DirectHas<T> (string scope) where T : class, IEquatable<T>, new ();
		IPolicyContainer ParentPolicies { get; }
		
		#endregion
	}
	
	public class PolicySet: IPolicyContainer
	{
		PolicyDictionary policies = new PolicyDictionary ();
		
		internal PolicySet (string id, string name)
		{
			this.Id = id;
			this.Name = name;
		}
		
		internal PolicyDictionary Policies { get { return policies; } } 
		
		bool IPolicyContainer.IsRoot {
			get { return true; }
		}
		
		IPolicyContainer IPolicyContainer.ParentPolicies {
			get { return null; }
		}
		
		public IEnumerable<string> GetScopes<T> ()
		{
			foreach (PolicyKey pk in policies.Keys) {
				if (pk.PolicyType == typeof(T))
					yield return pk.Scope;
			}
		}
		
		public bool Has<T> ()
		{
			return policies.ContainsKey (new PolicyKey (typeof(T), null));
		}
		
		public bool Has<T> (string scope)
		{
			return policies.ContainsKey (new PolicyKey (typeof(T), scope));
		}
		
		public bool Has<T> (IEnumerable<string> scopes)
		{
			foreach (string scope in scopes) {
				if (Has<T> (scope))
					return true;
			}
			return false;
		}

		bool IPolicyContainer.DirectHas<T> ()
		{
			return Has<T> ();
		}
		
		bool IPolicyContainer.DirectHas<T> (string scope)
		{
			return Has<T> (scope);
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
		
		public IEnumerable<ScopedPolicy<T>> GetScoped<T> () where T : class, IEquatable<T>, new ()
		{
			foreach (KeyValuePair<PolicyKey,object> pinfo in policies) {
				if (pinfo.Key.PolicyType == typeof(T))
					yield return new ScopedPolicy<T> ((T)pinfo.Value, pinfo.Key.Scope);
			}
			T pol = Get<T> ();
			if (pol != null && !PolicyService.IsUndefinedPolicy (pol))
				yield return new ScopedPolicy<T> (pol, null);
		}
		
		public T Get<T> () where T : class, IEquatable<T>, new ()
		{
			return Get (typeof (T)) as T;
		}
		
		public T Get<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			T pol = ((IPolicyContainer)this).DirectGet<T> (scope);
			return PolicyService.IsUndefinedPolicy (pol) ? null : pol;
		}
		
		public T Get<T> (IEnumerable<string> scopes) where T : class, IEquatable<T>, new ()
		{
			foreach (string scope in scopes) {
				T pol = Get<T> (scope);
				if (pol != null && !PolicyService.IsUndefinedPolicy (pol))
					return pol;
			}
			return null;
		}
		
		internal object Get (Type type)
		{
			return Get (type, (string) null);
		}
		
		internal object Get (Type type, string scope)
		{
			object o;
			if (policies.TryGetValue (type, scope, out o)) {
				if (PolicyService.IsUndefinedPolicy (o))
					return null;
			}
			return o;
		}
		
		public void Set<T> (T value) where T : class, IEquatable<T>, new ()
		{
			Set (value, null);
		}
		
		T IPolicyContainer.DirectGet<T> ()
		{
			return ((IPolicyContainer)this).DirectGet<T> ((string)null);
		}
		
		T IPolicyContainer.DirectGet<T> (string scope)
		{
			object o;
			policies.TryGetValue (typeof(T), scope, out o);
			return (T) o;
		}
		
		public void Set<T> (T value, string scope) where T : class, IEquatable<T>, new ()
		{
			if (IsReadOnly)
				throw new InvalidOperationException ("Cannot modify fixed policy sets");
			
			PolicyKey key = new PolicyKey (typeof(T), scope);
			IEquatable<T> oldVal = Get<T> (key.Scope);
			if (oldVal != null && oldVal.Equals (value))
				return;
			
			policies[key] = value;
			OnPolicyChanged (key.PolicyType, key.Scope);
		}
		
		public bool Remove<T> () where T : class, IEquatable<T>, new ()
		{
			return Remove<T> (null);
		}

		public bool Remove<T> (string scope) where T : class, IEquatable<T>, new ()
		{
			PolicyKey pk = new PolicyKey (typeof(T), scope);
			if (!policies.Remove (pk))
				return false;
			OnPolicyChanged (pk.PolicyType, scope);
			return true;
		}
		
		public string Name { get; private set; }
		public string Id { get; private set; }
		
		internal void AddSerializedPolicies (StreamReader reader)
		{
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
				foreach (KeyValuePair<PolicyKey,object> policyPair in policies)
					cw.Write (xw, PolicyService.DiffSerialize (policyPair.Key.PolicyType, policyPair.Value, policyPair.Key.Scope));
				xw.WriteEndElement ();
			}
		}
		
		internal void LoadFromFile (StreamReader reader)
		{
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
		
		internal bool IsReadOnly { get; set; }
		
		protected void OnPolicyChanged (Type policyType, string scope)
		{
			if (PolicyChanged != null)
				PolicyChanged (this, new PolicyChangedEventArgs (policyType, scope));
		}
		
		public event EventHandler<PolicyChangedEventArgs> PolicyChanged;
	}
	
	public class ScopedPolicy<T>
	{
		public ScopedPolicy (T policy, string scope)
		{
			Policy = policy;
			Scope = scope;
		}
		
		public string Scope { get; internal set; }
		public T Policy { get; internal set; }
		
		public virtual Type PolicyType {
			get { return typeof(T); }
		}
	}
	
	public class ScopedPolicy: ScopedPolicy<object>
	{
		Type type;
		
		public ScopedPolicy (Type type, object ob, string scope): base (ob, scope)
		{
			this.type = type;
		}
		
		public override Type PolicyType {
			get {
				return type;
			}
		}
	}
}
