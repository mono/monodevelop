using System;
using System.Collections.Generic;

using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Smokey {

	static class SmokeyRuleCache {
		
		static Dictionary<string, SmokeyRule> cachedRules;
		
		static SmokeyRuleCache ()
		{
			cachedRules = new Dictionary<string, SmokeyRule> ();
		}
		
		public static void Add (SmokeyRule r)
		{
			if (cachedRules.ContainsKey (r.Id))
				return;
			
			cachedRules [r.Id] = r;
		}
		
		public static SmokeyRule Get (string id)
		{
			if (!cachedRules.ContainsKey (id))
				throw new ArgumentException (string.Format ("Rule with '{0}' id could not be found in cache while it should be.", id),
				                             "id");
			return cachedRules [id];
		}
	}
}