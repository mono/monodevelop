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
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using ICS = ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	[PolicyType ("C# naming")]
	public class NamingPolicy : IEquatable<NamingPolicy>
	{
//		List<NamingRule> rules = new List<NamingRule> ();
//		
//		public IList<NamingRule> Rules {
//			get { return rules; }
//		}


		#region IEquatable implementation
		public bool Equals (NamingPolicy other)
		{
			return other == this;
		}
		#endregion

//		//finds rules that would match on the same items
//		void FindConflicts ()
//		{
//			for (int i = 0; i < rules.Count - 1; i++) {
//				var rule = rules[i];
//				for (int j = i + 1; j < rules.Count; j++) {
//					if (RulesConflict (rule, rules[j]))
//						Console.WriteLine ("Rule conflict: {0}, {1}", i, j);
//				}
//			}
//		}
//		
//		bool RulesConflict (NamingRule r, NamingRule s)
//		{
///*			//they don't match the same kinds at all
//			if ((r.MatchKind & s.MatchKind) == 0)
//				return false;
//			
//			//one requires something that the other does not
//			var matchAllIntersect = r.MatchAllModifiers & s.MatchAllModifiers;
//			if (matchAllIntersect != r.MatchAllModifiers && matchAllIntersect != s.MatchAllModifiers)
//				return false;
//			
//			if (r.MatchAnyModifiers != 0 && s.MatchAnyModifiers != 0 && (r.MatchAnyModifiers & s.MatchAnyModifiers) == 0)
//				return false;
//			
//			//one requires something that the other prohibits
//			if ((r.MatchAllModifiers & s.MatchNoModifiers) != 0 || (s.MatchAllModifiers & r.MatchNoModifiers) != 0)
//				return false;
//			
//			if (s.MatchAnyModifiers != 0 && (r.MatchNoModifiers & s.MatchAnyModifiers) == s.MatchAnyModifiers)
//				return false;
//			
//			if (r.MatchAnyModifiers != 0 && (s.MatchNoModifiers & r.MatchAnyModifiers) == r.MatchAnyModifiers)
//				return false;
//			*/
//			return true;
//		}
	}
}