using System;
using System.Collections.Generic;

using GF = Gendarme.Framework;
using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Gendarme {
	
	public class GendarmeRule : CA.IRule {
		
		private readonly GF.IRule rule;
		private readonly string description;
		
		internal GendarmeRule (GF.IRule rule)
		{
			this.rule = rule;
			
			object [] attrs = rule.GetType ().GetCustomAttributes (typeof (GF.ProblemAttribute), false);
			if (attrs == null || attrs.Length == 0)
				return;
			
			GF.ProblemAttribute problem = (GF.ProblemAttribute) attrs [0];
			description = problem.Problem;
		}
		
		public string Id {
			get { return rule.FullName; }
		}

		public string Name {
			get { return rule.Name; }
		}

		public string Description {
			get { return description; } 
		}
		
		internal GF.IRule InternalRule {
			get { return rule; }
		}
	}
}
