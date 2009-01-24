// 
// PolicySetNode.cs
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

using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Projects.Extensions
{
	
	[ExtensionNode (Description="A named set of defined policies")]
	[ExtensionNodeChild (typeof (PolicyResourceNode), "Policies")]
	class PolicySetNode : ExtensionNode
	{
		PolicySet polSet;
		
		[NodeAttribute ("_name", Required=true)]
		string name;
		
		protected override void OnChildNodeAdded (ExtensionNode node)
		{
			PolicyResourceNode res = (PolicyResourceNode) node;
			using (System.IO.StreamReader reader = res.GetStream ())
				polSet.AddSerializedPolicies (reader);
			base.OnChildNodeAdded (node);
		}
		
		protected override void OnChildNodeRemoved (ExtensionNode node)
		{
			PolicyResourceNode res = (PolicyResourceNode) node;
			using (System.IO.StreamReader reader = res.GetStream ())
				polSet.RemoveSerializedPolicies (reader);
			base.OnChildNodeRemoved (node);
		}
		
		public PolicySet Set {
			get {
				if (polSet == null) {
					polSet = new PolicySet (Id, MonoDevelop.Core.GettextCatalog.GetString (name));
					foreach (PolicyResourceNode res in ChildNodes) {
						using (System.IO.StreamReader reader = res.GetStream ())
							polSet.AddSerializedPolicies (reader);
					}
				}
				return polSet;
			}
		}
	}
}
