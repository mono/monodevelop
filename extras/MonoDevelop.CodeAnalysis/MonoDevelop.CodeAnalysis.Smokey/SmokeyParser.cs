using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Smokey {
	
	internal static class SmokeyParser {
		private const string RuleErrorMessage = "A rule has failed to run.";
		
		public static IEnumerable<IViolation> ParseOutput (StreamReader sr, IEnumerable<CA.IRule> ruleSet)
		{
			List<IViolation> found = new List<IViolation> ();
			
			// FIXME: instead of checking each defect if its rule is in "set"
			// we should think on making use of Smokey "ignore" feature

			// we should only return violations with rules id in this list
			List<string> ruleIds = new List<string> ();
			foreach (CA.IRule rule in ruleSet)
				ruleIds.Add (rule.Id);
			
			// if assembly is big, Gendarme outputs progress bar using dots
			// before actual xml, so we might want to need to move forward
			while (sr.Peek () != '<')
				sr.Read ();
			
			// go!
			using (XmlTextReader reader = new XmlTextReader (sr)) {
				reader.WhitespaceHandling = WhitespaceHandling.None;
				string file = string.Empty;
				int line = 0;
				
				while (reader.Read ()) {
					if (reader.NodeType != XmlNodeType.Element)
						continue;
					
					if("Location" == reader.Name) {
						file = reader.GetAttribute("file");
						if(null == file){ file = string.Empty; }
						if(!int.TryParse(reader.GetAttribute("line"), out line)){ line = 0; }
					}
					
					if (reader.Name != "Violation")
						continue;
					
					
					// get rule id
					string ruleId = reader.GetAttribute ("checkID");
					// if we don't need to check for this rule, let it go
					if (!ruleIds.Contains (ruleId))
						continue;
					
					// parse severity (or try to)
					CA.Severity severity = ParseSeverity (reader.GetAttribute ("severity"));
					
					// parse solution and problem
					string problem = null;
					string solution = null;
					while (reader.Read ()) {
						if (reader.NodeType != XmlNodeType.Element)
							continue;
						
						if (reader.Name == "Cause")
							problem = reader.ReadString ();
						else if (reader.Name == "Fix")
							solution = reader.ReadString ();
						else if (problem != null && solution != null)
							break;
					}

					// sometimes Smokey rules throw an exception
					// we shouldn't return "dead" violations
					if (IsErrorMessage (problem))
						continue;
					
					// go!
					found.Add (new SmokeyViolation (ruleId, problem, solution, severity, file, line));
				}
			}
			return found;
		}
		
		private static CA.Severity ParseSeverity (string value)
		{
			if (value == "Error")
				return CA.Severity.High; // FIXME: or Critical?
			else if (value == "Warning")
				return CA.Severity.Medium;
			else if (value == "Nitpick")
				return CA.Severity.Low;
			else
				return CA.Severity.Medium; // by default?
		}
		
		private static bool IsErrorMessage (string cause)
		{
			return cause == RuleErrorMessage;
		}
	}
}
