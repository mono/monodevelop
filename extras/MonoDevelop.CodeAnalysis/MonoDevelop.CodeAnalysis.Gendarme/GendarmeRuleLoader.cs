using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using GF = Gendarme.Framework;
using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Gendarme {
	
	public class GendarmeRuleLoader : CA.DictionaryBasedRuleLoader {
		private static readonly string gendarmeDirectory;

		static GendarmeRuleLoader ()
		{
			gendarmeDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
			
			// FIXME: this is a workaround for bug found either in Gendarme or Cecil:
			GF.AssemblyResolver.Resolver.AddSearchDirectory (gendarmeDirectory);			
		}
		
		public GendarmeRuleLoader ()
		{
			foreach (string lib in Directory.GetFiles (gendarmeDirectory, "Gendarme.Rules.*.dll", SearchOption.TopDirectoryOnly)) {
				string fileName = Path.GetFileName (lib);
				string categoryName = fileName.Substring (15, fileName.Length - 19);
				RegisterCategory (categoryName);
			}
		}
		
		protected override void LoadRules (Category c)
		{			
			string rulesFile = GetRulesAssemblyFileName (c.Id);
			if (!File.Exists (rulesFile))
				throw new ArgumentException (AddinCatalog.GetString ("Could not find '{0}' rules assembly in '{1}'.", c.Id, gendarmeDirectory));
			
			Assembly rulesAssembly = Assembly.LoadFile (Path.GetFullPath (rulesFile));
			foreach (Type t in rulesAssembly.GetTypes ()) {
				if (t.IsAbstract || t.IsInterface)
					continue;

				if (Utilities.IsGendarmeRule (t))
					base.AddRule (c, GendarmeRuleCache.CreateOrGetProxy (t));
			}
		}
		
		static string GetRulesAssemblyFileName (string id)
		{
			return Path.Combine (gendarmeDirectory, "Gendarme.Rules." + id + ".dll");
		}
	}
}
