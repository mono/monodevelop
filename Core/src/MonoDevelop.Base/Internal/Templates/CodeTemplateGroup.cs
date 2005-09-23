// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;
using System.Diagnostics;

namespace MonoDevelop.Internal.Templates
{
	/// <summary>
	/// This class reperesents a single Code Template
	/// </summary>
	public class CodeTemplateGroup
	{
		ArrayList extensions = new ArrayList();
		ArrayList templates  = new ArrayList();
		
		public ArrayList Extensions {
			get {
				return extensions;
			}
		}
		
		public ArrayList Templates {
			get {
				return templates;
			}
		}
		
		public string[] ExtensionStrings {
			get {
				string[] extensionStrings = new string[extensions.Count];
				extensions.CopyTo(extensionStrings, 0);
				return extensionStrings;
			}
			set {
				extensions.Clear();
				foreach (string str in value) {
					extensions.Add(str);
				}
			}
		}
		
		public CodeTemplateGroup(string extensions)
		{
			ExtensionStrings = extensions.Split(';');
		}
		
		public CodeTemplateGroup(XmlElement el)
		{
			if (el == null) {
				throw new ArgumentNullException("el");
			}
			string[] exts = el.GetAttribute("extensions").Split(';');
			foreach (string ext in exts) {
				extensions.Add(ext);
			}
			foreach (XmlElement childElement in el.ChildNodes) {
				templates.Add(new CodeTemplate(childElement));
			}
		}
		
		public XmlElement ToXmlElement(XmlDocument doc)
		{
			if (doc == null) {
				throw new ArgumentNullException("doc");
			}
			XmlElement newElement = doc.CreateElement("CodeTemplateGroup");
			
			newElement.SetAttribute("extensions", String.Join(";", ExtensionStrings));
			
			foreach (CodeTemplate codeTemplate in templates) {
				newElement.AppendChild(codeTemplate.ToXmlElement(doc));
			}
			
			return newElement;
		}
		
	}
}
