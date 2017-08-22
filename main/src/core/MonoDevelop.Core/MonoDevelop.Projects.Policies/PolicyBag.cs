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
using MonoDevelop.Core;


namespace MonoDevelop.Projects.Policies
{
	
	[DataItem ("Policies")]
	public class PolicyBag: PolicyContainer, ICustomDataItem
	{
		public PolicyBag (SolutionFolderItem owner)
		{
			this.Owner = owner;
		}
		
		internal PolicyBag ()
		{
		}
		
		public SolutionFolderItem Owner { get; internal set; }
		
		public override bool IsRoot {
			get { return Owner == null || Owner.ParentFolder == null; }
		}
		
		public override PolicyContainer ParentPolicies {
			get {
				if (Owner != null && Owner.ParentFolder != null)
					return Owner.ParentFolder.Policies;
				else
					return null;
			}
		}
		
		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			if (policies == null)
				return null;
			
			DataCollection dc = new DataCollection ();
			foreach (KeyValuePair<PolicyKey,object> p in policies)
				dc.Add (PolicyService.DiffSerialize (p.Key.PolicyType, p.Value, p.Key.Scope, keepDeletedNodes: true));
			return dc;
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			if (data.Count == 0)
				return;
			
			policies = new PolicyDictionary ();
			foreach (DataNode node in data) {
				try {
					if (!(node is DataItem))
						continue;
					ScopedPolicy val = PolicyService.DiffDeserialize ((DataItem)node);
					policies.Add (val);
				} catch (Exception ex) {
					if (handler.SerializationContext.ProgressMonitor != null)
						handler.SerializationContext.ProgressMonitor.ReportError (ex.Message, ex);
					else
						LoggingService.LogError (ex.Message, ex);
				}
			}
		}
		
		internal void PropagatePolicyChangeEvent (PolicyChangedEventArgs args)
		{
			SolutionFolder solFol = Owner as SolutionFolder;
			if (solFol != null)
				foreach (SolutionFolderItem item in solFol.Items)
					if (!item.Policies.DirectHas (args.PolicyType, args.Scope))
						item.Policies.OnPolicyChanged (args.PolicyType, args.Scope);
		}
		
		protected override void OnPolicyChanged (Type policyType, string scope)
		{
			base.OnPolicyChanged (policyType, scope);
			PropagatePolicyChangeEvent (new PolicyChangedEventArgs (policyType, scope));
		}
	}
}
