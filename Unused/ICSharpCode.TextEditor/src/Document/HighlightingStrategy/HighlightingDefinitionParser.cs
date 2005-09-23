// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using System.Collections;
using System.Reflection;

namespace MonoDevelop.TextEditor.Document
{
	internal class HighlightingDefinitionParser
	{
		private HighlightingDefinitionParser()
		{
			// This is a pure utility class with no instances.	
		}
		
		static string[] environmentColors = {"VRuler", "Selection", "LineNumbers",
											"InvalidLines", "EOLMarkers", "SpaceMarkers", "TabMarkers",
											"CaretMarker", "FoldLine", "FoldMarker"};
		static ArrayList errors = null;
		
		public static DefaultHighlightingStrategy Parse(SyntaxMode syntaxMode, XmlTextReader xmlTextReader)
		{
			try {
				XmlValidatingReader validatingReader = new XmlValidatingReader(xmlTextReader);
				Stream shemaStream = Assembly.GetCallingAssembly().GetManifestResourceStream("Mode.xsd");
				validatingReader.Schemas.Add("", new XmlTextReader(shemaStream));
				validatingReader.ValidationType = ValidationType.Schema;
			    validatingReader.ValidationEventHandler += new ValidationEventHandler (ValidationHandler);
				
				
				XmlDocument doc = new XmlDocument();
				doc.Load(validatingReader);
				
				DefaultHighlightingStrategy highlighter = new DefaultHighlightingStrategy(doc.DocumentElement.Attributes["name"].InnerText);
				
				if (doc.DocumentElement.Attributes["extensions"]!= null) {
					highlighter.Extensions = doc.DocumentElement.Attributes["extensions"].InnerText.Split(new char[] { ';', '|' });
				}
				/*
				if (doc.DocumentElement.Attributes["indent"]!= null) {
					highlighter.DoIndent = Boolean.Parse(doc.DocumentElement.Attributes["indent"].InnerText);
				}
				*/
				XmlElement environment = doc.DocumentElement["Environment"];
		
				highlighter.SetDefaultColor(new HighlightBackground(environment["Default"]));
				
				foreach (string aColorName in environmentColors) {
					highlighter.SetColorFor(aColorName, new HighlightColor(environment[aColorName]));
				}
				
				// parse properties
				if (doc.DocumentElement["Properties"]!= null) {
					foreach (XmlElement propertyElement in doc.DocumentElement["Properties"].ChildNodes) {
						highlighter.Properties[propertyElement.Attributes["name"].InnerText] =  propertyElement.Attributes["value"].InnerText;
					}
				}
				
				if (doc.DocumentElement["Digits"]!= null) {
					highlighter.SetColorFor("Digits", new HighlightColor(doc.DocumentElement["Digits"]));
				}
				
				XmlNodeList nodes = doc.DocumentElement.GetElementsByTagName("RuleSet");
				foreach (XmlElement element in nodes) {
					highlighter.AddRuleSet(new HighlightRuleSet(element));
				}
				
				xmlTextReader.Close();
				
				if(errors!=null) {
					ReportErrors(syntaxMode.FileName);
					errors = null;
					return null;
				} else {
					return highlighter;		
				}
			} catch (Exception) {
				//MessageBox.Show("Could not load mode definition file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
				return null;
			}
		}	
		
		private static void ValidationHandler(object sender, ValidationEventArgs args)
		{
			if (errors==null) {
				errors=new ArrayList();
			}
			errors.Add(args);
		}

		private static void ReportErrors(string fileName)
		{
			StringBuilder msg = new StringBuilder();
			msg.Append("Could not load mode definition file. Reason:\n\n");
			foreach(ValidationEventArgs args in errors) {
				msg.Append(args.Message);
				msg.Append(Console.Out.NewLine);
			}
			//MessageBox.Show(msg.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
		}

	}
}
