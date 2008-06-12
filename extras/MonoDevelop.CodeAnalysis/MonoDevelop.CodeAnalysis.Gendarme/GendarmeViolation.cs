using System;
using System.IO;

using CA = MonoDevelop.CodeAnalysis;
using GF = Gendarme.Framework;
using Mono.Cecil;

namespace MonoDevelop.CodeAnalysis.Gendarme {
	
	class GendarmeViolation : IViolation {
		private readonly GF.Defect defect;
		private CA.CodeLocation location;
		
		internal GendarmeViolation (GF.Defect defect)
		{
			this.defect = defect;
			this.location = new CA.CodeLocation(string.Empty, 0, 0);
		}
		
		public CA.IRule Rule {
			get {
				return GendarmeRuleCache.GetProxy (defect.Rule);
			}
		}

		public CA.Severity Severity {
			get {
				switch (defect.Severity) {
				case GF.Severity.Critical:
					return CA.Severity.Critical;
				case GF.Severity.High:
					return CA.Severity.High;
				case GF.Severity.Low:
					return CA.Severity.Low;
				case GF.Severity.Medium:
					return CA.Severity.Medium;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public CA.Confidence Confidence {
			get {
			switch (defect.Confidence) {
				case GF.Confidence.High:
					return CA.Confidence.High;
				case GF.Confidence.Low:
					return CA.Confidence.Low;
				case GF.Confidence.Normal:
					return CA.Confidence.Normal;
				case GF.Confidence.Total:
					return CA.Confidence.Total;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public CA.CodeLocation Location {
			get {
				return location;
			}
		}

		public Uri Documentation {
			get {
				return defect.Rule.Uri;
			}
		}

		public string Problem {
			get {
				return defect.Rule.Problem;
			}
		}

		public string Solution {
			get {
				return defect.Rule.Solution;
			}
		}

		
	}
}