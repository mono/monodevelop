using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.Projects.MSBuild
{
	class ConfigFileUtilities
	{
		internal static void SetOrAppendSubelementAttributeValue (XElement element, string subelementName, string attributeName, string attributeValue)
		{
			var subelement = element.Elements ().FirstOrDefault (e => e.Name.LocalName == subelementName);
			if (subelement != null) {
				var existingValue = subelement.Attribute (attributeName);
				if (existingValue != null) {
					if (!existingValue.Value.Contains (attributeValue)) {
						existingValue.Value = existingValue.Value + ";" + attributeValue;
					}
				}
				else {
					subelement.SetAttributeValue (attributeName, attributeValue);
				}
			}
			else {
				subelement = new XElement (subelementName);
				subelement.SetAttributeValue (attributeName, attributeValue);
				element.AddFirst (subelement);
			}
		}
	}
}
