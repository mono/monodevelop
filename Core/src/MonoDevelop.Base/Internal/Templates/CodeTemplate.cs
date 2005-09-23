// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Diagnostics;

namespace MonoDevelop.Internal.Templates
{
	/// <summary>
	/// This class reperesents a single Code Template
	/// </summary>
	public class CodeTemplate
	{
		string shortcut     = String.Empty;
		string description  = String.Empty;
		string text         = String.Empty;
		
		public string Shortcut {
			get {
				return shortcut;
			}
			set {
				shortcut = value;
				Debug.Assert(shortcut != null, "MonoDevelop.Internal.Template : string Shortcut == null");
			}
		}
		
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
				Debug.Assert(description != null, "MonoDevelop.Internal.Template : string Description == null");
			}
		}
		
		public string Text {
			get {
				return text;
			}
			set {
				text = value;
				Debug.Assert(text != null, "MonoDevelop.Internal.Template : string Text == null");
			}
		}
		
		public CodeTemplate()
		{
		}
		
		public CodeTemplate(string shortcut, string description, string text)
		{
			this.shortcut    = shortcut;
			this.description = description;
			this.text        = text;
		}
		
		public CodeTemplate(XmlElement el)
		{
			if (el == null) {
				throw new ArgumentNullException("el");
			}
			
			if (el.Attributes["template"] == null || el.Attributes["description"] == null) {
				throw new Exception("CodeTemplate(XmlElement el) : template and description attributes must exist (check the CodeTemplate XML)");
			}
			
			Shortcut    = el.GetAttribute("template");
			Description = el.GetAttribute("description");
			Text        = el.InnerText;
		}
		
		public XmlElement ToXmlElement(XmlDocument doc)
		{
			if (doc == null) {
				throw new ArgumentNullException("doc");
			}
			
			XmlElement newElement = doc.CreateElement("CodeTemplate");
			newElement.SetAttribute("template",    Shortcut);
			newElement.SetAttribute("description", Description);
			newElement.InnerText = Text;
			return newElement;
		}
	}
}
