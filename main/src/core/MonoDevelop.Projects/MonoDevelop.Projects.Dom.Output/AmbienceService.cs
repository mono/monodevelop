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
			ambiences.TryGetValue (mimeType, out result);
			return result ?? defaultAmbience;
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
				return result;
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
				return sb.ToString ();
			}
			return member.Documentation;
		}
		
		public static string GetDocumentation (IMember member)
		{
			if (member == null)
				return null;
			if (!string.IsNullOrEmpty (member.Documentation))
				return member.Documentation;
			XmlElement node = (XmlElement)member.GetMonodocDocumentation ();
			if (node != null)
				return (node["summary"].InnerXml ?? "").Trim ();
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
		
		public static string GetDocumentationMarkup (string doc, DocumentationFormatOptions options)
		{
			if (string.IsNullOrEmpty (doc))
				return null;
			System.IO.StringReader reader = new System.IO.StringReader ("<docroot>" + doc + "</docroot>");
			XmlTextReader xml = new XmlTextReader (reader);
			StringBuilder ret = new StringBuilder (70);
	//		ret.Append ("<small>");
			int lastLinePos = -1;

			try {
				xml.Read ();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						switch (xml.Name.ToLower ()) {
						case "remarks":
							ret.Append ("Remarks:\n");
							break;
						// skip <example>-nodes
						case "example":
							xml.Skip ();
							xml.Skip ();
							break;
						case "exception":
							ret.Append ("Exception: ");
							ret.Append (GetCref (xml["cref"]));
							ret.Append (":");
							ret.AppendLine ();
							break;
						case "returns":
							ret.Append ("Returns: ");
							break;
						case "see":
							ret.Append (GetCref (xml["cref"]) + xml["langword"]);
							break;
						case "seealso":
							ret.Append ("See also: " + GetCref (xml["cref"]) + xml["langword"]);
							break;
						case "paramref":
							ret.Append (xml["name"]);
							break;
						case "param":
							if (ret.Length > 0 && ret[ret.Length - 1] != '\n')
								ret.AppendLine ();
							string paramName = xml["name"].Trim ();
							if (options.HighlightParameter == paramName)
								ret.Append ("<b>");
							ret.Append (paramName);
							if (options.HighlightParameter == paramName)
								ret.Append ("</b>");
							ret.Append (": ");
							break;
						case "value":
							ret.Append ("Value: ");
							break;
						case "para":
							continue;
							// Keep new line flag
						}
						lastLinePos = -1;
					} else if (xml.NodeType == XmlNodeType.EndElement) {
						string elname = xml.Name.ToLower ();
						if (elname == "para" || elname == "param") {
							if (lastLinePos == -1)
								lastLinePos = ret.Length;
							//ret.Append("<span size=\"2000\">\n\n</span>");
						}
					} else if (xml.NodeType == XmlNodeType.Text) {
						string txt = xml.Value.Replace ("\r", "").Replace ("\n", " ");
						if (lastLinePos != -1)
							txt = txt.TrimStart (' ');

						// Remove duplicate spaces.
						int len;
						do {
							len = txt.Length;
							txt = txt.Replace ("  ", " ");
						} while (len != txt.Length);

						txt = EscapeText (txt);
						ret.Append (txt);
						lastLinePos = -1;
					}
				} while (xml.Read ());
				if (lastLinePos != -1)
					ret.Remove (lastLinePos, ret.Length - lastLinePos);
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError (ex.ToString ());
				return doc;
			}
//			ret.Append ("</small>");
			return ret.ToString ();
		}
		
		#endregion
	}
}
