using System;
using System.Collections.Generic;

using MonoDevelop.Core;

namespace MonoDevelop.CodeAnalysis {
	
	public abstract class DictionaryBasedRuleLoader : IRuleLoader {
		private Dictionary<Category, IList<IRule>> categorizedRules;
		private bool rulesLoaded = false;
		
		public DictionaryBasedRuleLoader ()
		{
			categorizedRules = new Dictionary<Category, IList<IRule>> ();	
		}
		
		/// <summary>
		/// Adds a rule to the specified category (one should exist).
		/// </summary>
		protected void AddRule (Category c, IRule r)
		{
			categorizedRules [c].Add (r);
		}
		
		void EnsureLoaded ()
		{
			if (!rulesLoaded)
				LoadAllRules ();
		}
		
		void LoadAllRules ()
		{
			foreach (Category c in categorizedRules.Keys)
				LoadRules (c);
			
			rulesLoaded = true;
		}

		/// <summary>
		/// Loads rules of specified category.
		/// </summary>
		protected abstract void LoadRules (Category c);

		/// <summary>
		/// Adds a category to internal dictionary.
		/// </summary>
		protected void RegisterCategory (string id)
		{
			Category c = new Category (id, id); // TODO: make id and name different?
			categorizedRules.Add (c, new List<IRule> ());
		}
		
		
		public IEnumerable<IRule> GetRules ()
		{
			EnsureLoaded ();
			foreach (List<IRule> ruleList in categorizedRules.Values)
				foreach (IRule rule in ruleList)
					yield return rule;
		}

		public IEnumerable<IRule> GetRules (Category category)
		{
			EnsureLoaded ();
			if (!categorizedRules.ContainsKey (category))
				throw new ArgumentOutOfRangeException ("category",
					category, AddinCatalog.GetString ("Category '{0}' does not exist.", category));
			
			return categorizedRules [category];
		}

		public IEnumerable<Category> GetCategories ()
		{
			EnsureLoaded ();
			return categorizedRules.Keys;
		}
	}
}
