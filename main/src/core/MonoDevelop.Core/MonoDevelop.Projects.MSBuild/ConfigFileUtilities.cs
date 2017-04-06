using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.Projects.MSBuild
{
	class ConfigFileUtilities
	{
		internal static void SetSubelementAttribute (XElement element, string subelementName, string attributeName, string attributeValue)
		{
			var appContextSwitchOverrides = element.Elements ().FirstOrDefault (e => e.Name.LocalName == subelementName);
			if (appContextSwitchOverrides != null) {
				var existingValue = appContextSwitchOverrides.Attribute (attributeName);
				if (existingValue != null) {
					if (!existingValue.Value.Contains (attributeValue)) {
						existingValue.Value = existingValue.Value + ";" + attributeValue;
					}
				}
				else {
					appContextSwitchOverrides.SetAttributeValue (attributeName, attributeValue);
				}
			}
			else {
				appContextSwitchOverrides = new XElement (subelementName);
				appContextSwitchOverrides.SetAttributeValue (attributeName, attributeValue);
				element.AddFirst (appContextSwitchOverrides);
			}
		}
	}
}
