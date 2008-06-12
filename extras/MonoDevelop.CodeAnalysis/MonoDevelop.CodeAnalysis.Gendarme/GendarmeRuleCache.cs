using System;
using System.Collections.Generic;

using CA = MonoDevelop.CodeAnalysis;
using GF = Gendarme.Framework;
using Mono.Cecil;

namespace MonoDevelop.CodeAnalysis.Gendarme {

	static class GendarmeRuleCache {
		
		private static Dictionary<Type, GF.IRule> cachedRules;
		private static Dictionary<GF.IRule, CA.IRule> boundProxies;
		
		static GendarmeRuleCache ()
		{
			cachedRules = new Dictionary<Type, GF.IRule> ();
			boundProxies = new Dictionary<GF.IRule, CA.IRule> ();
		}
		
		public static CA.IRule CreateOrGetProxy (Type ruleType)
		{
			if (!Utilities.IsGendarmeRule (ruleType))
				throw new ArgumentException (AddinCatalog.GetString ("{0} is not a rule type because it does not implement IRule interface.",
								ruleType), "ruleType");

			if (!cachedRules.ContainsKey (ruleType)) {
				// create `real' rule and cache it
				GF.IRule rule = (GF.IRule) Activator.CreateInstance (ruleType);
				cachedRules.Add (ruleType, rule);
				// create a proxy and cache it
				GendarmeRule proxy = new GendarmeRule (rule);
				boundProxies.Add (rule, proxy);
				return proxy;
			} else {
				// return from cache
				return boundProxies [cachedRules [ruleType]];
			}
		}
		
		public static CA.IRule GetProxy (GF.IRule rule)
		{
			if (!boundProxies.ContainsKey (rule))
				throw new ArgumentException (AddinCatalog.GetString ("{0} rule has not been cached but it should.", rule), "rule");
			
			return boundProxies [rule];
		}
	}
}