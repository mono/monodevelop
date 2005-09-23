// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Security;
using System.Security.Permissions;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	/// This class is used to convert xml files using xslt
	/// </summary>
	public class ConvertXml
	{
		/// <remarks>
		/// The main module loads the three required input vars
		/// and performs the transform
		/// </remarks>
		/// <param name="args">
		/// arg1 - the input file (preferably VS.NET .csproj)
		/// arg2 - path to XSL transform file
		/// arg3 - path to output file (preferably SD .prjx)
		/// </param>
		public static void Convert(string inputFile, string xslPath, string outputFile)
		{
			Convert(inputFile, xslPath, outputFile, null);
		}
		public static void Convert(string inputFile, string xslPath, string outputFile, XsltArgumentList xsltArgList)
		{
			// Transform the file
			XmlReader reader = GetXML(inputFile);
			XmlReader oTransformed = TransformXmlToXml(reader, xslPath, xsltArgList);
			reader.Close();
			
			// Output results to file path
			XmlDocument myDoc = new XmlDocument();
			myDoc.Load(oTransformed);
			myDoc.Save(outputFile);
		}
		
		public static void Convert(string inputFile, XmlReader xslReader, string outputFile, XsltArgumentList xsltArgList)
		{
			// Transform the file
			XmlReader reader = GetXML(inputFile);
			XmlReader oTransformed = TransformXmlToXml(reader, xslReader, xsltArgList);
			reader.Close();
			
			// Output results to file path
			XmlDocument myDoc = new XmlDocument();
			myDoc.Load(oTransformed);
			myDoc.Save(outputFile);
		}

		public static string ConvertToString(string inputFile, string xslPath)
		{
			return ConvertToString(inputFile, xslPath, null);
		}
		
		public static string ConvertToString(string inputFile, string xslPath, XsltArgumentList xsltArgList)
		{
			// Transform the file
			XmlReader reader = GetXML(inputFile);
			XmlReader oTransformed = TransformXmlToXml(reader, xslPath, xsltArgList);
			reader.Close();
			
			// Output results to string
			XmlDocument myDoc = new XmlDocument();
			myDoc.Load(oTransformed);
			StringWriter sw = new StringWriter();
			myDoc.Save(sw);
			return sw.ToString();
		}
		
		public static string ConvertData(string inputXml, string xslPath, XsltArgumentList xsltArgList)
		{
			XmlReader reader = new XmlTextReader(new StringReader(inputXml));
			XmlReader oTransformed = TransformXmlToXml(reader, xslPath, xsltArgList);
			reader.Close();
			
			// Output results to string
			XmlDocument myDoc = new XmlDocument();
			myDoc.Load(oTransformed);
			StringWriter sw = new StringWriter();
			myDoc.Save(sw);
			return sw.ToString();
		}
		
		public static string ConvertData(string inputXml, XmlReader xslReader, XsltArgumentList xsltArgList)
		{
			XmlReader reader = new XmlTextReader(new StringReader(inputXml));
			XmlReader oTransformed = TransformXmlToXml(reader, xslReader, xsltArgList);
			reader.Close();
			
			// Output results to string
			XmlDocument myDoc = new XmlDocument();
			myDoc.Load(oTransformed);
			StringWriter sw = new StringWriter();
			myDoc.Save(sw);
			return sw.ToString();
		}
		
		public static XmlReader TransformXmlToXml(XmlReader oXML, string XSLPath, XsltArgumentList xsltArgList)
		{
			XslTransform xslt = new XslTransform();
			xslt.Load(XSLPath);
			
			XPathDocument inputData = new XPathDocument(oXML);
			
			return xslt.Transform(inputData, xsltArgList, new XmlSecureResolver (new XmlUrlResolver (), new PermissionSet (PermissionState.Unrestricted)));
		}
		
		public static XmlReader TransformXmlToXml(XmlReader oXML, XmlReader XSLReader, XsltArgumentList xsltArgList)
		{
			XslTransform xslt = new XslTransform();
			xslt.Load(XSLReader, new XmlSecureResolver (new XmlUrlResolver (), new PermissionSet (PermissionState.Unrestricted)), null);
			
			XPathDocument inputData = new XPathDocument(oXML);
			
			return xslt.Transform(inputData, xsltArgList, new XmlSecureResolver (new XmlUrlResolver (), new PermissionSet (PermissionState.Unrestricted)));
		}
		
		/// <summary>
		/// GetXML returns an XmlReader dependent on the contents
		/// of the passed input param.
		/// GetXML checks for the following conditions:
		/// blank string returns an empty XmlReader
		/// less-than at start assumes an XML file
		/// back-slash at start assumes UNC path
		/// otherwise, URL is assumed
		/// </summary>
		/// <param name="strInput"></param>
		/// <returns></returns>
		public static XmlReader GetXML(string strInput)
		{
			// Check if string is blank
			if (strInput.Length == 0) {
				// Return the empty xml reader
				return new XmlTextReader("");
			} else {
					// Check if string starts with "<"
					// If it does, it is an XML file
					if (strInput.Substring(0,1) == "<")
					{
						//String could be an xml file - load
						return new XmlTextReader(new StringReader(strInput));
					}
					else
						{
							// Assume this is a file path - return loaded XML
							return new XmlTextReader(strInput);
						}
				}
		}
	}
}
