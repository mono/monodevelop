//
// AmbienceService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Mono.Addins;
using System.Xml;
using System.Text;

namespace MonoDevelop.Projects.Dom.Output
{
	public static class AmbienceService
	{
		static Ambience defaultAmbience                = new NetAmbience ();
		static Dictionary <string, Ambience> ambiences = new Dictionary <string, Ambience> ();
		
		static AmbienceService ()
		{
			// may not have been initialized in testing environment.
			if (AddinManager.IsInitialized) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/Ambiences", delegate(object sender, ExtensionNodeEventArgs args) {
						Ambience ambience = args.ExtensionObject as Ambience;
						if (ambience == null)
							return;
						string[] mimeTypes = ambience.MimeTypes.Split (';');
							
						switch (args.Change) {
						case ExtensionChange.Add:
							foreach (string mimeType in mimeTypes)
								ambiences[mimeType] = ambience;
							break;
						case ExtensionChange.Remove:
							foreach (string mimeType in mimeTypes) {
								if (ambiences.ContainsKey (mimeType))
									ambiences.Remove (mimeType);
							}
							break;
						}
					});
			}
		}
		
		public static Ambience GetAmbience (IMember member)
		{
			if (member is IType && ((IType)member).CompilationUnit != null)
				return GetAmbienceForFile (((IType)member).CompilationUnit.FileName);
			if (member.DeclaringType != null && member.DeclaringType.CompilationUnit != null)
				return GetAmbienceForFile (member.DeclaringType.CompilationUnit.FileName);
			return defaultAmbience;
		}
		
		public static Ambience GetAmbienceForFile (string fileName)
		{
			foreach (Ambience ambience in ambiences.Values) {
				if (ambience.IsValidFor (fileName))
					return ambience;
			}
			return defaultAmbience;
		}
		
		public static Ambience GetAmbience (string mimeType)
		{
			Ambience result;
			if (!string.IsNullOrEmpty (mimeType) && ambiences.TryGetValue (mimeType, out result))
				return result;
			return defaultAmbience;
		}
		
		public static Ambience GetAmbienceForLanguage (string languageName)
		{
			ILanguageBinding binding = LanguageBindingService.GetBindingPerLanguageName (languageName);
			if (binding != null) {
				return GetAmbienceForFile (binding.GetFileName ("a"));
			} else {
				return defaultAmbience;
			}
		}
		
		public static Ambience DefaultAmbience { get { return defaultAmbience; } }
		#region Documentation
		
		public class DocumentationFormatOptions 
		{
			public static readonly DocumentationFormatOptions Empty = new DocumentationFormatOptions ();
			public string HighlightParameter {
				get;
				set;
			}
			
			public int MaxLineLength {
				get;
				set;
			}
			
			public bool BigHeadings {
				get;
				set;
			}
			
			public bool SmallText {
				get;
				set;
			}
			
			public Ambience Ambience {
				get;
				set;
			}
			
			public string FormatHeading (string heading)
			{
				return BigHeadings ? "<b><big>" + heading + "</big></b>" : "<b>" + heading + "</b>";
			}
			
			public string FormatBody (string body)
			{
				return SmallText ? "<small>" + body.Trim () + Environment.NewLine + "</small>" : body.Trim () + Environment.NewLine;
			}
		}
		
		public static string GetSummaryMarkup (IMember member)
		{
			return GetDocumentationMarkup (GetDocumentationSummary (member));
		}
		
		public static string GetDocumentationSummary (IMember member)
		{
			if (member == null)
				return null;
			if (!string.IsNullOrEmpty (member.Documentation)) {
				int idx1 = member.Documentation.IndexOf ("<summary>");
				int idx2 = member.Documentation.IndexOf ("</summary>");
				string result;
				if (idx2 >= 0 && idx1 >= 0) {
					result = member.Documentation.Substring (idx1 + "<summary>".Length, idx2 - idx1 - "<summary>".Length);
				} else if (idx1 >= 0) {
					result = member.Documentation.Substring (idx1 + "<summary>".Length);
				} else if (idx2 >= 0) {
					result = member.Documentation.Substring (0, idx2 - 1);
				} else {
					result = member.Documentation;
				}
				
				return CleanEmpty (result);
			}
			XmlElement node = (XmlElement)member.GetMonodocDocumentation ();
			if (node != null) {
				string innerXml = (node["summary"].InnerXml ?? "").Trim ();
				StringBuilder sb = new StringBuilder ();
				bool wasWhiteSpace = false;
				for (int i = 0; i < innerXml.Length; i++) {
					char ch = innerXml[i];
					switch (ch) {
					case '\n':
					case '\r':
						break;
					default:
						bool isWhiteSpace = Char.IsWhiteSpace (ch);
						if (isWhiteSpace && wasWhiteSpace)
							continue;
						wasWhiteSpace = isWhiteSpace;
						sb.Append (ch);
						break;
					}
				}
				
			
				return CleanEmpty (sb.ToString ());
			}
			return CleanEmpty (member.Documentation);
		}
		
		static string CleanEmpty (string doc)
		{
			return IsEmptyDocumentation (doc)? null : doc;
		}

		static bool IsEmptyDocumentation (string documentation)
		{
			return string.IsNullOrEmpty (documentation) || documentation.StartsWith ("To be added") || documentation == "we have not entered docs yet";
		}
		
		public static string GetDocumentation (IMember member)
		{
			if (member == null)
				return null;
			if (!string.IsNullOrEmpty (member.Documentation))
				return CleanEmpty (member.Documentation);
			XmlElement node = (XmlElement)member.GetMonodocDocumentation ();
			if (node != null) {
				string result = (node.InnerXml ?? "").Trim ();
				return CleanEmpty (result);
			}
			return null;
		}
		
		static string GetCref (string cref)
		{
			if (cref == null)
				return "";

			if (cref.Length < 2)
				return cref;

			if (cref.Substring (1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}
		
		static bool IsSpecialChar (int charValue)
		{
			return 
				0x01 <= charValue && charValue <= 0x08 ||
				0x0B <= charValue && charValue <= 0x0C ||
				0x0E <= charValue && charValue <= 0x1F ||
				0x7F <= charValue && charValue <= 0x84 ||
				0x86 <= charValue && charValue <= 0x9F;
		}
		
		public static string BreakLines (string text, int maxLineLength)
		{
			if (maxLineLength <= 0)
				return text;
			StringBuilder result = new StringBuilder ();
			int lineLength = 0;
			bool inTag = false;
			bool inAmp = false;
			foreach (char ch in text) {
				switch (ch) {
				case '<':
					inTag = true;
					break;
				case '>':
					inTag = false;
					break;
				case '&':
					inAmp = true;
					break;
				case ';':
					inAmp = false;
					break;
				case '\n':
					lineLength = 0;
					break;
				case '\r':
					lineLength = 0;
					break;
				}
				
				result.Append (ch);
				if (!inTag && !inAmp)
					lineLength++;
				if (!Char.IsLetterOrDigit (ch) && lineLength > maxLineLength) {
					result.AppendLine ();
					lineLength = 0;
				}
			}
			return result.ToString ();
		}
		
		public static string EscapeText (string text)
		{
			StringBuilder result = new StringBuilder ();
			foreach (char ch in text) {
				switch (ch) {
				case '<':
					result.Append ("&lt;");
					break;
				case '>':
					result.Append ("&gt;");
					break;
				case '&':
					result.Append ("&amp;");
					break;
				case '\'':
					result.Append ("&apos;");
					break;
				case '"':
					result.Append ("&quot;");
					break;
				default:
					int charValue = (int)ch;
					if (IsSpecialChar (charValue)) {
						result.AppendFormat ("&#x{0:X};", charValue);
					} else {
						result.Append (ch);
					}
					break;
				}
			}
			return result.ToString ();
		}
		
		public static string GetDocumentationMarkup (string doc)
		{
			return GetDocumentationMarkup (doc, DocumentationFormatOptions.Empty);
		}
		
		static string ParseBody (XmlTextReader xml, string endTagName, DocumentationFormatOptions options)
		{
			StringBuilder result = new StringBuilder (); 
			while (xml.Read ()) {
				switch (xml.NodeType) {
				case XmlNodeType.EndElement:
					if (xml.Name == endTagName) 
						return result.ToString ();
					break;
				case XmlNodeType.Element:
					switch (xml.Name.ToLower ()) {
						case "para":
							result.AppendLine (ParseBody (xml, xml.Name, options));
							break;
						case "see":
							result.Append (' ');
							result.Append ("<i>");
							string name = (GetCref (xml["cref"]) + xml["langword"]).Trim ();
							if (options.Ambience != null)
								name = options.Ambience.ConvertTypeName (name);
							result.Append (EscapeText (name));
							result.Append ("</i>");
							break;
						case "paramref":
							result.Append (' ');
							result.Append ("<i>");
							result.Append (EscapeText (xml["name"].Trim ()));
							result.Append ("</i>");
							break;
					}
					break;
				case XmlNodeType.Text:
					StringBuilder textBuilder = new StringBuilder ();
					bool wasWhiteSpace = true;
					if (IsEmptyDocumentation (xml.Value))
						break;
					foreach (char ch in xml.Value) {
						if (Char.IsWhiteSpace (ch)) {
							if (!wasWhiteSpace)
								textBuilder.Append (' ');
							wasWhiteSpace = true;
							continue;
						}
						wasWhiteSpace = false;
						textBuilder.Append (ch);
					}
					string text = BreakLines (EscapeText (textBuilder.ToString ()), options.MaxLineLength);
					text = text.Trim ();
					if (text.Length > 0 && char.IsLetter (text[0]))
						result.Append (" ");
					result.Append (text.Trim ());
					break;
				}
			}
			return result.ToString ();
		}
		
		public static string GetDocumentationMarkup (string doc, DocumentationFormatOptions options)
		{
			if (string.IsNullOrEmpty (doc))
				return null;
			System.IO.StringReader reader = new System.IO.StringReader ("<docroot>" + doc + "</docroot>");
			XmlTextReader xml = new XmlTextReader (reader);
			StringBuilder ret = new StringBuilder (70);
			StringBuilder parameters = new StringBuilder ();
			StringBuilder exceptions = new StringBuilder ();
			parameters.AppendLine (options.FormatHeading ("Parameters:"));
			
			exceptions.AppendLine (options.FormatHeading ("Exceptions:"));
	//		ret.Append ("<small>");
			int paramCount = 0, exceptionCount = 0, summaryEnd = -1;
			try {
				xml.Read ();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						switch (xml.Name.ToLower ()) {
						case "para":
							ret.Append (options.FormatBody (ParseBody (xml, xml.Name, options)));
							if (summaryEnd < 0)
								summaryEnd = ret.Length;
							break;
						case "summary":
//							ret.AppendLine (GetHeading ("Summary:", options));
							ret.Append (options.FormatBody (ParseBody (xml, xml.Name, options)));
							if (summaryEnd < 0)
								summaryEnd = ret.Length;
							break;
						case "remarks":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.AppendLine (options.FormatHeading ("Remarks:"));
								ret.Append (options.FormatBody (ParseBody (xml, xml.Name, options)));
								if (summaryEnd < 0)
									summaryEnd = ret.Length;
							} else {
								options.FormatBody (ParseBody (xml, xml.Name, options));
							}
							break;
						// skip <example>-nodes
						case "example":
							xml.Skip ();
							xml.Skip ();
							break;
						case "exception":
							exceptionCount++;
							if (options.SmallText)
								exceptions.Append ("<small>");
							exceptions.Append ("<b>");
							exceptions.Append (EscapeText (GetCref (xml["cref"])));
							exceptions.Append (": ");
							exceptions.Append ("</b>");
							if (options.SmallText)
								exceptions.Append ("</small>");
							
							exceptions.AppendLine (options.FormatBody (ParseBody (xml, xml.Name, options)));
							break;
						case "returns":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.AppendLine (options.FormatHeading ("Returns:"));
								ret.Append (options.FormatBody (ParseBody (xml, xml.Name, options)));
							} else {
								options.FormatBody (ParseBody (xml, xml.Name, options));
							}
							break;
						case "param":
							paramCount++;
							string paramName = xml["name"].Trim ();
							parameters.Append ("<i>");
							if (options.HighlightParameter == paramName)
								parameters.Append ("<b>");
							if (options.SmallText)
								parameters.Append ("<small>");
							parameters.Append (EscapeText (paramName));
							if (options.SmallText)
								parameters.Append ("</small>");
							if (options.HighlightParameter == paramName)
								parameters.Append ("</b>");
							parameters.Append (":</i> ");
							parameters.Append (options.FormatBody (ParseBody (xml, xml.Name, options)));
							break;
						case "value":
							ret.AppendLine (options.FormatHeading ("Value:"));
							ret.AppendLine (options.FormatBody (ParseBody (xml, xml.Name, options)));
							break;
						case "seealso":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.Append (options.FormatHeading ("See also:"));
								ret.Append (" " + EscapeText (GetCref (xml["cref"]) + xml["langword"]));
							}
							break;
						}
					}
				} while (xml.Read ());
			
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError (ex.ToString ());
				return EscapeText (doc);
			}
			if (IsEmptyDocumentation (ret.ToString ()))
				return null;
			if (string.IsNullOrEmpty (options.HighlightParameter) && exceptionCount > 0)
				ret.Append (exceptions.ToString ());
			
			string result = ret.ToString ();
			if (summaryEnd < 0)
				summaryEnd = result.Length;
			if (paramCount > 0)
				result = result.Insert (summaryEnd, parameters.ToString ());
		
			result = result.Trim ();
			if (result.EndsWith (Environment.NewLine +"</small>"))
				result = result.Substring (0, result.Length - (Environment.NewLine +"</small>").Length) + "</small>";
			return result;
		}
		#endregion
	}
}
