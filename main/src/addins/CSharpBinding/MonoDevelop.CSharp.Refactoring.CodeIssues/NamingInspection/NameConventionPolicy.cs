// 
// NamingConventions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Serialization;
using ICS = ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	[PolicyType ("Naming Conventions Policy")]
	class NameConventionPolicy : IEquatable<NameConventionPolicy>
	{
		NameConventionRule[] rules = new NameConventionRule[0];

		[ItemProperty]
		public NameConventionRule[] Rules {
			get { return rules; }
			set { rules = value; }
		}

		public NameConventionPolicy Clone ()
		{
			var result = new NameConventionPolicy ();
			result.rules = new List<NameConventionRule> (rules.Select (r => r.Clone ())).ToArray ();
			return result;
		}

		public NameConventionPolicy ()
		{
			rules = new List<NameConventionRule> (DefaultRules.GetFdgRules ().Select (r => new NameConventionRule (r))).ToArray ();
		}

		class NamingConventionService : ICSharpCode.NRefactory.CSharp.Refactoring.NamingConventionService
		{
			NameConventionPolicy policy;
			ICSharpCode.NRefactory.CSharp.Refactoring.NamingRule[] rules = null;
			public override IEnumerable<ICSharpCode.NRefactory.CSharp.Refactoring.NamingRule> Rules {
				get {
					if (rules == null) {
						this.rules = policy.Rules.Select (r => r.GetNRefactoryRule ()).ToArray ();
					}
					return rules;
				}
			}

			public NamingConventionService (MonoDevelop.CSharp.Refactoring.CodeIssues.NameConventionPolicy policy)
			{
				this.policy = policy;
			}
			
		}

		public ICSharpCode.NRefactory.CSharp.Refactoring.NamingConventionService CreateNRefactoryService ()
		{
			return new NamingConventionService (this);
		}

		#region IEquatable implementation
		public bool Equals (NameConventionPolicy other)
		{
			if (Rules.Length != other.Rules.Length) 
				return false;
			for (int i = 0; i < rules.Length; i++) {
				if (!rules [i].Equals (other.Rules[i]))
					return false;
			}
			return true;
		}
		#endregion
	}
}