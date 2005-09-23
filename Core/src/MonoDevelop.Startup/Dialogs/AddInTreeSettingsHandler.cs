using System;
using System.Configuration;
using System.Collections;
using System.Xml;

namespace MonoDevelop
{
	public class AddInSettingsHandler : System.Configuration.IConfigurationSectionHandler
	{
		public AddInSettingsHandler()
		{
		}

		public object Create(object parent, object configContext, System.Xml.XmlNode section)
		{
			ArrayList addInDirectories = new ArrayList();
			XmlNode attr = section.Attributes.GetNamedItem("ignoreDefaultPath");
			if (attr != null) {
				try {
					addInDirectories.Add(Convert.ToBoolean( attr.Value ));
				} catch {
					addInDirectories.Add(false);
				}
			} else {
				addInDirectories.Add(false);
			}
			
			XmlNodeList addInDirList = section.SelectNodes("AddInDirectory");
			foreach (XmlNode addInDir in addInDirList) {
				XmlNode path = addInDir.Attributes.GetNamedItem("path");
				if (path != null) {
					addInDirectories.Add(path.Value);
				}
			}
			return addInDirectories;
		}

		public static string[] GetAddInDirectories(out bool ignoreDefaultPath)
		{
			ArrayList addInDirs = System.Configuration.ConfigurationSettings.GetConfig("AddInDirectories") as ArrayList;
			if (addInDirs != null) {
				int i, count = addInDirs.Count;
				if (count <= 1) {
					ignoreDefaultPath = false;
					return null;
				}
				ignoreDefaultPath = (bool) addInDirs[0];
				string [] directories = new string[count-1];
				for (i = 0; i < count-1; i++) {
					directories[i] = addInDirs[i+1] as string;
				}
				return directories;
			}
			ignoreDefaultPath = false;
			return null;
		}
	}
}
