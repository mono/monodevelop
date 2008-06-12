using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

using CA = MonoDevelop.CodeAnalysis;

namespace MonoDevelop.CodeAnalysis.Smokey {
	
	public class SmokeyRuleLoader : CA.DictionaryBasedRuleLoader {

		public SmokeyRuleLoader ()
		{
			foreach (string resource in SmokeyRunner.Smokey.GetManifestResourceNames ()) {				
				if (!resource.EndsWith (".xml"))
					continue;				
				
				if (resource == "Schema.xml")
					continue;
								
				string categoryName = resource.Substring (0, resource.Length - 4); // remove ".xml"
				RegisterCategory (categoryName);
			}
		}
		
		protected override void LoadRules (Category c)
		{			
			// load xml file from smokey resources
			using (Stream ruleInfo = SmokeyRunner.Smokey.GetManifestResourceStream (c.Id + ".xml")) {
				using (XmlTextReader reader = new XmlTextReader (ruleInfo)) {
					reader.WhitespaceHandling = WhitespaceHandling.None;
			
					// we need to know all rule ids, names and descriptions
					while (reader.Read ()) {
						reader.ReadToFollowing ("Violation");	
						string ruleId = reader.GetAttribute ("checkID"); // id
						do {
							if (reader.EOF)
								break;
					
							reader.ReadToFollowing ("Translation");
						} while (reader.GetAttribute ("lang") != "en");
				
						if (reader.EOF)
								break;
				
						string ruleName = reader.GetAttribute ("typeName"); // name
						reader.ReadToFollowing ("Description");
						string ruleDescription = reader.ReadElementContentAsString (); // description
				
						SmokeyRule rule = new SmokeyRule (ruleId, ruleName, ruleDescription);
						SmokeyRuleCache.Add (rule);
						base.AddRule (c, rule);
					}
				}
			}
		}
	}
}
