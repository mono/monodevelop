// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Resources;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	public class LanguageService : AbstractService
	{
		string languagePath;
		
//		PixbufList languageImageList = null;
		ArrayList languages         = null;
		
/*		public PixbufList LanguageImageList {
			get {
				if (languageImageList == null) {
					LoadLanguageDefinitions();
				}
				return languageImageList;
			}
		}
*/
		public ArrayList Languages {
			get {
				if (languages == null) {
					LoadLanguageDefinitions();
				}
				return languages;
			}
		}
		void LoadLanguageDefinitions()
		{
//			languageImageList = new PixbufList();
			languages         = new ArrayList();
			
			XmlDocument doc = new XmlDocument();
			doc.Load(languagePath + "LanguageDefinition.xml");
			
			XmlNodeList nodes = doc.DocumentElement.ChildNodes;
			
			foreach (XmlNode node in nodes) {
				XmlElement el = node as XmlElement;
				if (el != null) {
					languages.Add(new Language(el.Attributes["name"].InnerText,
					                           el.Attributes["code"].InnerText,
//					                           LanguageImageList.Count));
					                           0));
//					LanguageImageList.Add(new Gdk.Pixbuf(languagePath + el.Attributes["icon"].InnerText));
				}
			}
		}
		
		public LanguageService()
		{
			PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
			languagePath =  propertyService.DataDirectory +
			                Path.DirectorySeparatorChar + "resources" +
		                    Path.DirectorySeparatorChar + "languages" +
		                    Path.DirectorySeparatorChar;
		}
	}
}
