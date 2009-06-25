// 
// PolicyDictionary.cs
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
	internal struct PolicyKey : IEquatable<PolicyKey>
	{
		public Type PolicyType { get; private set; }
		public string Scope { get; private set; }
		
		public PolicyKey (Type policyType, string scope): this ()
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
