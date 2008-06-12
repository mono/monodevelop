using System;

using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Smokey {
	
	public class SmokeyViolation : CA.IViolation {
		private string ruleId, problem, solution;
		private CA.Severity severity;
		private CA.CodeLocation location;
		
		public CA.IRule Rule {
			get { return SmokeyRuleCache.Get (ruleId); }
		}

		public CA.Severity Severity {
			get { return severity; }
		}

		public CA.Confidence Confidence {
			get { return CA.Confidence.Normal; }
		}

		public CA.CodeLocation Location {
			get {
				return location;
			}
		}

		public Uri Documentation {
			get {
				throw new NotImplementedException();
			}
		}

		public string Problem {
			get { return problem; }
		}

		public string Solution {
			get { return solution; }
		}

		
		public SmokeyViolation (string ruleId, string problem, string solution, CA.Severity severity, string file, int line)
		{
			this.ruleId = ruleId;
			this.problem = problem;
			this.solution = solution;
			this.severity = severity;
			this.location = new CodeLocation(file, line, 0);
		}
	}
}
