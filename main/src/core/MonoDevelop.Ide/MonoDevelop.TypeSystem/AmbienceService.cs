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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.TypeSystem
{
	public static class AmbienceService
	{
		static Dictionary <string, Ambience> ambiences = new Dictionary <string, Ambience> ();
		
		static AmbienceService ()
		{
			// may not have been initialized in testing environment.
			if (AddinManager.IsInitialized) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/Ambiences", delegate(object sender, ExtensionNodeEventArgs args) {
					var ambience = args.ExtensionNode as MonoDevelop.Core.AddIns.MimeTypeExtensionNode;
					switch (args.Change) {
					case ExtensionChange.Add:
						ambiences[ambience.MimeType] = (Ambience) ambience.CreateInstance ();
						break;
					case ExtensionChange.Remove:
						ambiences.Remove (ambience.MimeType);
						break;
					}
				});
			}
		}
		
		public static Ambience GetAmbience (IMember member)
		{
			return GetAmbienceForFile (member.DeclaringTypeDefinition.Region.FileName) ?? new NetAmbience ();
		}
		
		public static Ambience GetAmbienceForFile (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				return DefaultAmbience;
			return GetAmbience (DesktopService.GetMimeTypeForUri (fileName));
		}
		
		public static Ambience GetAmbience (string mimeType)
		{
			Ambience result;
			if (!string.IsNullOrEmpty (mimeType) && ambiences.TryGetValue (mimeType, out result))
				return result;
			return defaultAmbience;
		}
		
		static Ambience defaultAmbience = new NetAmbience ();
		
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
			
			public bool BoldHeadings {
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
			
			
			public DocumentationFormatOptions ()
			{
				BoldHeadings = true;
			}
			
			public string FormatHeading (string heading)
			{
				string result = heading;
				if (BigHeadings)
					result = "<big>" + result + "</big>";
				if (BoldHeadings)
					result = "<b>" + result + "</b>";
				return result;
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
		
		public static string GetDocumentationSummary (IEntity member)
		{
			if (member == null || member.Documentation == null)
				return null;
			string documentation = member.Documentation.Xml.Text;
			
			if (!string.IsNullOrEmpty (documentation)) {
				int idx1 = documentation.IndexOf ("<summary>");
				int idx2 = documentation.IndexOf ("</summary>");
				string result;
				if (idx2 >= 0 && idx1 >= 0) {
					result = documentation.Substring (idx1 + "<summary>".Length, idx2 - idx1 - "<summary>".Length);
				} else if (idx1 >= 0) {
					result = documentation.Substring (idx1 + "<summary>".Length);
				} else if (idx2 >= 0) {
					result = documentation.Substring (0, idx2 - 1);
				} else {
					result = documentation;
				}
				
				return CleanEmpty (result);
			}
			
			return CleanEmpty (documentation);
		}
		
		static string CleanEmpty (string doc)
		{
			return IsEmptyDocumentation (doc)? null : doc;
		}

		static bool IsEmptyDocumentation (string documentation)
		{
			return string.IsNullOrWhiteSpace (documentation) || documentation.StartsWith ("To be added") || documentation == "we have not entered docs yet";
		}
		
		public static string GetDocumentation (IEntity member)
		{
			if (member == null)
				return null;
			if (member.Documentation != null)
				return CleanEmpty (member.Documentation);
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
		
		public static string UnescapeText (string text)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char ch = text[i];
				if (ch == '&') {
					int end = text.IndexOf (';', i);
					if (end == -1)
						break;
					string entity = text.Substring (i + 1, end - i - 1);
					switch (entity) {
					case "lt":
						sb.Append ('<');
						break;
					case "gt":
						sb.Append ('>');
						break;
					case "amp":
						sb.Append ('&');
						break;
					case "apos":
						sb.Append ('\'');
						break;
					case "quot":
						sb.Append ('"');
						break;
					}
					i = end;
				} else {
					sb.Append (ch);
				}
			}
			return sb.ToString ();	
		}
		
		
		public static string GetDocumentationMarkup (string doc)
		{
			return GetDocumentationMarkup (doc, DocumentationFormatOptions.Empty);
		}
		
		static string ParseBody (XmlTextReader xml, string endTagName, DocumentationFormatOptions options)
		{
			StringBuilder result = new StringBuilder (); 
			bool wasWhiteSpace = true;
			
			while (xml.Read ()) {
				switch (xml.NodeType) {
				case XmlNodeType.EndElement:
					if (xml.Name == endTagName) 
						goto end;
					break;
				case XmlNodeType.Element:
					switch (xml.Name.ToLower ()) {
					case "para":
						result.AppendLine (ParseBody (xml, xml.Name, options));
						break;
					case "see":
						if (!wasWhiteSpace) {
							result.Append (' ');
							wasWhiteSpace = true;
						}
						result.Append ("<i>");
						string name = (GetCref (xml ["cref"]) + xml ["langword"]).Trim ();
						if (options.Ambience != null)
							name = options.Ambience.GetIntrinsicTypeName (name);
						result.Append (EscapeText (name));
						result.Append ("</i>");
						break;
					case "paramref":
						if (!wasWhiteSpace) {
							result.Append (' ');
							wasWhiteSpace = true;
						}
						result.Append ("<i>");
						result.Append (EscapeText (xml ["name"].Trim ()));
						result.Append ("</i>");
						break;
					}
					break;
				case XmlNodeType.Text:
					if (IsEmptyDocumentation (xml.Value))
						break;
					foreach (char ch in xml.Value) {
						if (Char.IsWhiteSpace (ch) || ch == '\n' || ch == '\r') {
							if (!wasWhiteSpace) {
								result.Append (' ');
								wasWhiteSpace = true;
							}
							continue;
						}
						wasWhiteSpace = false;
						result.Append (EscapeText (ch.ToString ()));
					}
					break;
				}
			}
		end:
			return result.ToString ();
		}
		
		public static string GetDocumentationMarkup (string doc, DocumentationFormatOptions options)
		{
			if (string.IsNullOrEmpty (doc))
				return null;
			System.IO.StringReader reader = new System.IO.StringReader ("<docroot>" + doc + "</docroot>");
			XmlTextReader xml = new XmlTextReader (reader);
			StringBuilder ret = new StringBuilder (70);
			StringBuilder parameterBuilder = new StringBuilder ();
			StringBuilder exceptions = new StringBuilder ();
			
			exceptions.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Exceptions:")));
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
							var summary = options.FormatBody (ParseBody (xml, xml.Name, options));
							if (!IsEmptyDocumentation (summary)) {
								//							ret.AppendLine (GetHeading ("Summary:", options));
								ret.Append (summary);
								if (summaryEnd < 0)
									summaryEnd = ret.Length;
							}
							break;
						case "remarks":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Remarks:")));
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
							exceptions.Append (EscapeText (GetCref (xml ["cref"])));
							exceptions.Append (": ");
							exceptions.Append ("</b>");
							if (options.SmallText)
								exceptions.Append ("</small>");
							
							exceptions.AppendLine (options.FormatBody (ParseBody (xml, xml.Name, options)));
							break;
						case "returns":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Returns:")));
								ret.Append (options.FormatBody (ParseBody (xml, xml.Name, options)));
							} else {
								options.FormatBody (ParseBody (xml, xml.Name, options));
							}
							break;
						case "param":
							string paramName = xml.GetAttribute ("name") != null ? xml ["name"].Trim (): "";
								
							var body = options.FormatBody (ParseBody (xml, xml.Name, options));
							if (!IsEmptyDocumentation (body)) {
								paramCount++;
								parameterBuilder.Append ("<i>");
								if (options.HighlightParameter == paramName)
									parameterBuilder.Append ("<b>");
								if (options.SmallText)
									parameterBuilder.Append ("<small>");
								parameterBuilder.Append (EscapeText (paramName));
								if (options.SmallText)
									parameterBuilder.Append ("</small>");
								if (options.HighlightParameter == paramName)
									parameterBuilder.Append ("</b>");
								parameterBuilder.Append (":</i> ");
								parameterBuilder.Append (body);
							}
							break;
						case "value":
							ret.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Value:")));
							ret.AppendLine (options.FormatBody (ParseBody (xml, xml.Name, options)));
							break;
						case "seealso":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.Append (options.FormatHeading (GettextCatalog.GetString ("See also:")));
								ret.Append (" " + EscapeText (GetCref (xml ["cref"]) + xml ["langword"]));
							}
							break;
						}
					}
				} while (xml.Read ());
			
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError (ex.ToString ());
				return EscapeText (doc);
			}
			if (IsEmptyDocumentation (ret.ToString ()) && IsEmptyDocumentation (parameterBuilder.ToString ()))
				return null;
			if (string.IsNullOrEmpty (options.HighlightParameter) && exceptionCount > 0)
				ret.Append (exceptions.ToString ());
			
			string result = ret.ToString ();
			if (summaryEnd < 0)
				summaryEnd = result.Length;
			if (paramCount > 0) {
				var paramSb = new StringBuilder ();
				if (result.Length > 0)
					paramSb.AppendLine ();
				paramSb.Append ("<small>");
				paramSb.AppendLine (options.FormatHeading (GettextCatalog.GetPluralString ("Parameter:", "Parameters:", paramCount)));
				paramSb.Append ("</small>");
				paramSb.Append (parameterBuilder.ToString ());
				result = result.Insert (summaryEnd, paramSb.ToString ());
			}
			result = result.Trim ();
			if (result.EndsWith (Environment.NewLine + "</small>"))
				result = result.Substring (0, result.Length - (Environment.NewLine + "</small>").Length) + "</small>";
			return result;
		}
		#endregion
	}
}
