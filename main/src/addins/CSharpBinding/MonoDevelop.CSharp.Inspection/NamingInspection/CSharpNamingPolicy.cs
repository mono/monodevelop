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
using System.Text;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using ICS = ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp.Inspection
{
	[PolicyType ("C# naming")]
	public class CSharpNamingPolicy // : IEquatable<CSharpNamingPolicy>
	{
		List<NamingRule> rules = new List<NamingRule> ();
		
		public IList<NamingRule> Rules {
			get { return rules; }
		}
		
		public CSharpNamingPolicy ()
		{
			AddFdgRules ();
			
			// NOTE: the first rule in the list that matches the kind and modifiers will be used
			// however, these rules' modifiers have been more precisely defined so that their 
			// order in the list should not matter
			
			//private constants should be SCREAMING_CAPS
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.LocalVariable,
				MatchAllModifiers = ICS.Modifiers.Const,
				NamingStyle = NamingStyle.AllUpper,
			});
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Field,
				MatchAllModifiers = ICS.Modifiers.Const | ICS.Modifiers.Private,
				NamingStyle = NamingStyle.AllUpper,
			});
			//local variables should be camelCase
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.LocalVariable,
				MatchNoModifiers = ICS.Modifiers.Const,
				NamingStyle = NamingStyle.CamelCase,
			});
			//private fields should be camelCase
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Field,
				MatchAllModifiers = ICS.Modifiers.Private,
				MatchNoModifiers = ICS.Modifiers.Const,
				NamingStyle = NamingStyle.CamelCase,
			});
			//other private members should be PascalCase
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Type | ( DeclarationKinds.Member & ~DeclarationKinds.Field),
				MatchAllModifiers = ICS.Modifiers.Private,
				MatchNoModifiers = ICS.Modifiers.Extern,
				NamingStyle = NamingStyle.PascalCase,
			});
		}
		
		//rules from the framework design guidelines
		void AddFdgRules ()
		{
			//parameters should be camelCase
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Parameter,
				NamingStyle = NamingStyle.CamelCase,
			});
			//type parameters should be PascalCase and prefixed with a T
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.TypeParameter,
				NamingStyle = NamingStyle.PascalCase,
				RequiredPrefixes = new [] { "T" },
			});
			//interfaces should be PascalCase and prefixed with an I
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Interface,
				NamingStyle = NamingStyle.PascalCase,
				RequiredPrefixes = new [] { "I" },
			});
			//all other nonprivate identifiers should be PascalCase
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Type | DeclarationKinds.Member,
				MatchNoModifiers = ICS.Modifiers.Const | ICS.Modifiers.Private,
				NamingStyle = NamingStyle.PascalCase,
			});
			//namespaces should be PascalCase
			rules.Add (new NamingRule () {
				MatchKind = DeclarationKinds.Namespace,
				NamingStyle = NamingStyle.PascalCase,
			});
		}
		
		//finds rules that would match on the same items
		void FindConflicts ()
		{
			for (int i = 0; i < rules.Count - 1; i++) {
				var rule = rules[i];
				for (int j = i + 1; j < rules.Count; j++) {
					if (RulesConflict (rule, rules[j]))
						Console.WriteLine ("Rule conflict: {0}, {1}", i, j);
				}
			}
		}
		
		bool RulesConflict (NamingRule r, NamingRule s)
		{
			//they don't match the same kinds at all
			if ((r.MatchKind & s.MatchKind) == 0)
				return false;
			
			//one requires something that the other does not
			var matchAllIntersect = r.MatchAllModifiers & s.MatchAllModifiers;
			if (matchAllIntersect != r.MatchAllModifiers && matchAllIntersect != s.MatchAllModifiers)
				return false;
			
			if (r.MatchAnyModifiers != 0 && s.MatchAnyModifiers != 0 && (r.MatchAnyModifiers & s.MatchAnyModifiers) == 0)
				return false;
			
			//one requires something that the other prohibits
			if ((r.MatchAllModifiers & s.MatchNoModifiers) != 0 || (s.MatchAllModifiers & r.MatchNoModifiers) != 0)
				return false;
			
			if (s.MatchAnyModifiers != 0 && (r.MatchNoModifiers & s.MatchAnyModifiers) == s.MatchAnyModifiers)
				return false;
			
			if (r.MatchAnyModifiers != 0 && (s.MatchNoModifiers & r.MatchAnyModifiers) == r.MatchAnyModifiers)
				return false;
			
			return true;
		}
	}
}