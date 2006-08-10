// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ?Â¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Ambience;
using Ambience_ = MonoDevelop.Projects.Ambience.Ambience;

namespace MonoDevelop.Projects.Gui.Completion
{
	class CodeCompletionData : ICompletionDataWithMarkup
	{
		string image;
		string text;
		string description;
		string pango_description;
		string documentation;
		string completionString;

		bool convertedDocumentation = false;
		
		public string CompletionString 
		{
			get 
			{
				return completionString;
			}
		}
		
		
		public int Overloads
		{
			get {
				//return overloads;
				return overload_data.Count;
			}
		}
		
		public string Image
		{
			get {
				return image;
			}
			set {
				image = value;
			}
		}
		
		public string[] Text
		{
			get {
				return new string[] { text };
			}
			set {
				text = value[0];
			}
		}
		public string SimpleDescription
		{
			get {
				return description;
			}
		}
				
		private string GetDescription(string desc)
		{
			// don't give a description string, if no documentation or description is provided
			if (description.Length + documentation.Length == 0) {
				return null;
			}
		
			if (!convertedDocumentation) {
				convertedDocumentation = true;
				try {
					documentation = GetDocumentation(documentation);
				} catch (Exception e) {
					Console.WriteLine(e.ToString());
				}
			}
		
			return (desc + "\n" + documentation).Trim ();
		}		
		
		public string Description
		{
			get {
				return GetDescription(description);		
			}
			set {
				description = value;
			}
		}
		
		public string DescriptionPango
		{
			get {
				return GetDescription(pango_description);				
			}
			set {
				description = value;
			}
		}

		Hashtable overload_data = new Hashtable ();

		public CodeCompletionData[] GetOverloads ()
		{
			return (CodeCompletionData[]) (new ArrayList (overload_data.Values)).ToArray (typeof (CodeCompletionData));
		}

		public void AddOverload (CodeCompletionData overload)
		{
			string desc = overload.SimpleDescription;

			if (desc != description || !overload_data.Contains (desc))
				overload_data[desc] = overload;
		}
		
		public CodeCompletionData (string s, string image)
		{
			description = pango_description = documentation = String.Empty;
			text = s;
			completionString = s;
			this.image = image;
		}
		
		public CodeCompletionData (IClass c, Ambience_ ambience)
		{
			image = Services.Icons.GetIcon(c);
			text = c.Name;
			completionString = c.Name;
			description = ambience.Convert(c);
			pango_description = ambience.Convert(c, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
			documentation = c.Documentation;
		}
		
		public CodeCompletionData (IMethod method, Ambience_ ambience)
		{
			image  = Services.Icons.GetIcon(method);
			text        = method.Name;
			description = ambience.Convert(method);
			pango_description = ambience.Convert(method, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
			completionString = method.Name;
			documentation = method.Documentation;
		}
		
		public CodeCompletionData (IField field, Ambience_ ambience)
		{
			image  = Services.Icons.GetIcon(field);
			text        = field.Name;
			description = ambience.Convert(field);
			pango_description = ambience.Convert(field, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
			completionString = field.Name;
			documentation = field.Documentation;
		}
		
		public CodeCompletionData (IProperty property, Ambience_ ambience)
		{
			image  = Services.Icons.GetIcon(property);
			text        = property.Name;
			description = ambience.Convert(property);
			pango_description  = ambience.Convert(property, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
			completionString = property.Name;
			documentation = property.Documentation;
		}
		
		public CodeCompletionData (IEvent e, Ambience_ ambience)
		{
			image  = Services.Icons.GetIcon(e);
			text        = e.Name;
			description = ambience.Convert(e);
			pango_description  = ambience.Convert(e, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
			completionString = e.Name;
			documentation = e.Documentation;
		}

		public CodeCompletionData (IParameter o, Ambience_ ambience)
		{
			image = MonoDevelop.Core.Gui.Stock.Field;
			text  = o.Name;
			description = "";
			pango_description = "";
			completionString = o.Name;
			documentation = "";
		}
		
		public void InsertAction (ICompletionWidget widget)
		{
			widget.InsertAtCursor (completionString);
		}

		public static string GetDocumentation (string doc)
		{
			System.IO.StringReader reader = new System.IO.StringReader("<docroot>" + doc + "</docroot>");
			XmlTextReader xml   = new XmlTextReader(reader);
			StringBuilder ret   = new StringBuilder();
			Regex whitespace    = new Regex(@"(\s|\n)+", RegexOptions.Singleline);
			
			try {
				xml.Read();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						string elname = xml.Name.ToLower();
						if (elname == "remarks") {
							ret.Append("Remarks:\n");
						} else if (elname == "example") {
							ret.Append("Example:\n");
						} else if (elname == "exception") {
							ret.Append("Exception: " + GetCref(xml["cref"]) + ":\n");
						} else if (elname == "returns") {
							ret.Append("Returns: ");
						} else if (elname == "see") {
							ret.Append(GetCref(xml["cref"]) + xml["langword"]);
						} else if (elname == "seealso") {
							ret.Append("See also: " + GetCref(xml["cref"]) + xml["langword"]);
						} else if (elname == "paramref") {
							ret.Append(xml["name"]);
						} else if (elname == "param") {
							ret.Append(xml["name"].Trim() + ": ");
						} else if (elname == "value") {
							ret.Append("Value: ");
						}
					} else if (xml.NodeType == XmlNodeType.EndElement) {
						string elname = xml.Name.ToLower();
						if (elname == "para" || elname == "param") {
							ret.Append("\n");
						}
					} else if (xml.NodeType == XmlNodeType.Text) {
						ret.Append(whitespace.Replace(xml.Value, " "));
					}
				} while (xml.Read ());
			} catch {
				Console.WriteLine ("DocBoom");
				return doc;
			}
			return ret.ToString ();
		}
		
		static string GetCref (string cref)
		{
			if (cref == null)
				return "";
			
			if (cref.Length < 2)
				return cref;
			
			if (cref.Substring(1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}
	
	}
}
