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
		
		public T Get<T> () where T : new ()
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
		
		public bool IsRoot {
			get { return Owner.ParentFolder == null; }
		}
		
		public void Set<T> (T value)
		{
			if (value == null) {
				Remove<T> ();
				return;
			}
			if (policies == null)
				policies = new Dictionary<Type, object> ();
			policies[typeof(T)] = value;
		}
		
		public bool Remove<T> ()
		{
			if (policies != null) {
				bool ret = policies.Remove (typeof (T));
				if (policies.Count == 0)
					policies = null;
				return ret;
			}
			return false;
		}
		
		public bool Has<T> ()
		{
			return IsRoot || (policies != null && policies.ContainsKey (typeof (T)));
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
	}
}
