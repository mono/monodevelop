// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Xml;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;

using MonoDevelop.Services;

namespace MonoDevelop.Internal.ExternalTool
{
	/// <summary>
	/// This class handles the external tools 
	/// </summary>
	internal class ToolLoader
	{
		static string TOOLFILE        = "MonoDevelop-tools.xml";
		static string TOOLFILEVERSION = "1";
		
		static ArrayList tool         = new ArrayList();
		
		public static  ArrayList Tool {
			get {
				return tool;
			}
			set {
				tool = value;
				Debug.Assert(tool != null, "SharpDevelop.Tool.Data.ToolLoader : set ArrayList Tool (value == null)");
			}
		}
		
		static bool LoadToolsFromStream(string filename)
		{
			XmlDocument doc = new XmlDocument();
			try {
				doc.Load(filename);
				
				if (doc.DocumentElement.Attributes["VERSION"].InnerText != TOOLFILEVERSION)
					return false;
				
				tool = new ArrayList();
				
				XmlNodeList nodes  = doc.DocumentElement.ChildNodes;
				foreach (XmlElement el in nodes)
					tool.Add(new ExternalTool(el));
			} catch (Exception) {
				return false;
			}
			return true;
		}
		
		static void WriteToolsToFile(string fileName)
		{
			XmlDocument doc    = new XmlDocument();
			doc.LoadXml("<TOOLS VERSION = \"" + TOOLFILEVERSION + "\" />");
			
			foreach (ExternalTool et in tool) {
				doc.DocumentElement.AppendChild(et.ToXmlElement(doc));
			}
			
			Runtime.FileUtilityService.ObservedSave(new NamedFileOperationDelegate(doc.Save), fileName, FileErrorPolicy.ProvideAlternative);
		}
		
		/// <summary>
		/// This method loads the external tools from a XML based
		/// configuration file.
		/// </summary>
		static ToolLoader()
		{
			if (!LoadToolsFromStream (Runtime.Properties.ConfigDirectory + TOOLFILE)) {
				//Runtime.LoggingService.Info("Tools: can't load user defaults, reading system defaults");
				if (!LoadToolsFromStream (Runtime.Properties.DataDirectory +
				                         Path.DirectorySeparatorChar + "options" + 
				                         Path.DirectorySeparatorChar + TOOLFILE)) {
					Runtime.MessageService.ShowWarning(GettextCatalog.GetString ("Can't load external tools configuration file"));
				}
			}
		}
		
		/// <summary>
		/// This method saves the external tools to a XML based
		/// configuration file in the current user's own files directory
		/// </summary>
		public static void SaveTools()
		{
			WriteToolsToFile (Runtime.Properties.ConfigDirectory + TOOLFILE);
		}
	}
}
