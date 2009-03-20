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
	public class PolicyBag: ICustomDataItem
	{
		Dictionary<Type, object> policies;
		
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
				if (policies.TryGetValue (typeof(T), out policy))
					return (T) policy;
			}
			if (IsRoot)
				return PolicyService.GetDefaultPolicy<T> ();
			else
				return Owner.ParentFolder.Policies.Get<T> ();
		}
		
		// Gets policy directly from the bag without cacading or creating default instances
		public T DirectGet<T> () where T : class, IEquatable<T>, new ()
		{
			if (policies != null) {
				object policy;
				if (policies.TryGetValue (typeof(T), out policy))
					return (T) policy;
			}
			return null;
		}
		
		public bool IsRoot {
			get { return Owner.ParentFolder == null; }
		}
		
		public void Set<T> (T value) where T : class, IEquatable<T>, new ()
		{
			if (value == null) {
				Remove<T> ();
				return;
			}
			if (policies == null)
				policies = new Dictionary<Type, object> ();
			
			T oldVal = DirectGet<T> ();
			if (oldVal != null && oldVal.Equals (value))
				return;
			
			policies[typeof(T)] = value;
			OnPolicyChanged (typeof(T), value);
		}
		
		public bool Remove<T> () where T : class, IEquatable<T>, new ()
		{
			if (policies != null) {
				bool ret = policies.Remove (typeof (T));
				if (!ret)
					return ret;
				
				OnPolicyChanged (typeof (T), Get<T> ());
				if (policies.Count == 0)
					policies = null;
				return ret;
			}
			return false;
		}
		
		public bool Has<T> ()
		{
			return Has (typeof (T));
		}
		
		public bool Has (Type type)
		{
			return IsRoot || (policies != null && policies.ContainsKey (type));
		}
		
		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			if (policies == null)
				return null;
			
			DataCollection dc = new DataCollection ();
			foreach (object p in policies.Values)
				dc.Add (PolicyService.DiffSerialize (p));
			return dc;
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			if (data.Count == 0)
				return;
			
			policies = new Dictionary<Type, object> ();
			foreach (DataNode node in data) {
				object pol = PolicyService.DiffDeserialize (node);
				policies.Add (pol.GetType (), pol);
			}
		}
		
		internal void PropagatePolicyChangeEvent (PolicyChangedEventArgs args)
		{
			if (PolicyChanged != null)
				PolicyChanged (this, args);
			SolutionFolder solFol = Owner as SolutionFolder;
			if (solFol != null)
				foreach (SolutionItem item in solFol.Items)
					if (!item.Policies.Has (args.PolicyType))
						item.Policies.PropagatePolicyChangeEvent (args);
		}
		
		protected void OnPolicyChanged (Type policyType, object policy)
		{
			PropagatePolicyChangeEvent (new PolicyChangedEventArgs (policyType, policy));
		}
		
		public event EventHandler<PolicyChangedEventArgs> PolicyChanged;
	}
	
}
