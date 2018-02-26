//
// Ambience.cs
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
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Xml;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using System.Threading.Tasks;
using System.IO;

namespace MonoDevelop.Ide.TypeSystem
{

	public static class Ambience
	{
		public static readonly SymbolDisplayFormat LabelFormat =
			new SymbolDisplayFormat(
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
				propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
				memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
				parameterOptions:
				SymbolDisplayParameterOptions.IncludeDefaultValue |
				SymbolDisplayParameterOptions.IncludeExtensionThis |
				SymbolDisplayParameterOptions.IncludeType |
				SymbolDisplayParameterOptions.IncludeName |
				SymbolDisplayParameterOptions.IncludeParamsRefOut,
				miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			);
		/// <summary>
		/// Standard format for displaying to the user.
		/// </summary>
		/// <remarks>
		/// No return type.
		/// </remarks>
		public static readonly SymbolDisplayFormat NameFormat =
			new SymbolDisplayFormat(
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
				propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
				memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
				parameterOptions:
				SymbolDisplayParameterOptions.IncludeParamsRefOut |
				SymbolDisplayParameterOptions.IncludeExtensionThis |
				SymbolDisplayParameterOptions.IncludeType |
				SymbolDisplayParameterOptions.IncludeName,
				miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
		static Ambience ()
		{
			// may not have been initialized in testing environment.
			if (AddinManager.IsInitialized) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/AmbienceTooltipProviders", delegate(object sender, ExtensionNodeEventArgs args) {
					var node = args.ExtensionNode as MimeTypeExtensionNode;
					switch (args.Change) {
					case ExtensionChange.Add:
						tooltipProviders.Add ((AmbienceTooltipProvider)node.CreateInstance ());
						break;
					case ExtensionChange.Remove:
						tooltipProviders.Remove ((AmbienceTooltipProvider)node.CreateInstance ());
						break;
					}
				});
			}
		}

		public static string Format (string str)
		{
			if (String.IsNullOrEmpty (str))
				return string.Empty;
			
			var sb = StringBuilderCache.Allocate ();
			MarkupUtilities.AppendEscapedString (sb, str, 0, str.Length);
			return StringBuilderCache.ReturnAndFree(sb); 
		}

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
				var str = body.Trim ();
				if (string.IsNullOrEmpty (str))
					return "";
				return SmallText ? "<small>" + str + Environment.NewLine + "</small>" : str + Environment.NewLine;
			}
		}

		public static string GetSummaryMarkup (ISymbol member, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (member == null)
				return null;
			string documentation = GetDocumentation (member);
			if (string.IsNullOrEmpty (documentation))
				return null;

			if (!string.IsNullOrEmpty (documentation)) {
				int idx1 = documentation.IndexOf ("<summary>", StringComparison.Ordinal);
				int idx2 = documentation.LastIndexOf ("</summary>", StringComparison.Ordinal);
				string result;
				if (idx1 >= 0 && idx2 > idx1) {
					try {
						var xmlText = documentation.Substring (idx1, idx2 - idx1 + "</summary>".Length);
						return ParseBody (member,
							new XmlTextReader (xmlText, XmlNodeType.Element, null),
							"summary", 
							DocumentationFormatOptions.Empty
						);
					} catch (Exception e) {
						LoggingService.LogWarning ("Malformed documentation xml detected:" + documentation, e);
						// may happen on malformed xml.
						var len = idx2 - idx1 - "</summary>".Length;
						result = len > 0 ? documentation.Substring (idx1 + "<summary>".Length, len) : documentation;
					}
				} else if (idx1 >= 0) {
					result = documentation.Substring (idx1 + "<summary>".Length);
				} else if (idx2 >= 0) {
					result = documentation.Substring (0, idx2 - 1);
				} else {
					result = documentation;
				}
				return GetDocumentationMarkup (member, CleanEmpty (result));
			}
			
			return GetDocumentationMarkup (member, CleanEmpty (documentation));
		}
		
		static string CleanEmpty (string doc)
		{
			return IsEmptyDocumentation (doc)? null : doc;
		}

		static bool IsEmptyDocumentation (string documentation)
		{
			return string.IsNullOrWhiteSpace (documentation) || documentation.StartsWith ("To be added") || documentation == "we have not entered docs yet";
		}
		
		public static string GetDocumentation (ISymbol member)
		{
			if (member == null)
				return null;
			var documentation = member.GetDocumentationCommentXml ();
			if (string.IsNullOrEmpty (documentation))
				documentation = MonoDocDocumentationProvider.GetDocumentation (member);
			if (documentation != null)
				return CleanEmpty (documentation);
			return null;
		}
		
		static string GetCref (ICSharpCode.NRefactory.TypeSystem.ITypeResolveContext ctx, string cref)
		{
			if (cref == null)
				return "";

			if (cref.Length < 2)
				return cref;
			try {
				var entity = new ICSharpCode.NRefactory.Documentation.DocumentationComment ("", ctx).ResolveCref (cref.Replace("<", "{").Replace(">", "}"));
	
				if (entity != null) {
					var ambience = new ICSharpCode.NRefactory.CSharp.CSharpAmbience ();
					ambience.ConversionFlags = ICSharpCode.NRefactory.TypeSystem.ConversionFlags.ShowParameterList | ICSharpCode.NRefactory.TypeSystem.ConversionFlags.ShowParameterNames | ICSharpCode.NRefactory.TypeSystem.ConversionFlags.ShowTypeParameterList;
					return ambience.ConvertSymbol (entity);
				}
			} catch (Exception e) {
				LoggingService.LogWarning ("Invalid cref:" + cref, e);
			}

			if (cref[1] == ':')
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
			StringBuilder result = StringBuilderCache.Allocate ();
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
			return StringBuilderCache.ReturnAndFree(result);
		}
		
		public static string EscapeText (string text)
		{
			if (text == null)
				return null;
			StringBuilder result = StringBuilderCache.Allocate ();
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
			return StringBuilderCache.ReturnAndFree (result);
		}
		
		public static string UnescapeText (string text)
		{
			var sb = StringBuilderCache.Allocate ();
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
			return StringBuilderCache.ReturnAndFree (sb);	
		}
		
		
		public static string GetDocumentationMarkup (ISymbol member, string doc)
		{
			return GetDocumentationMarkup (member, doc, DocumentationFormatOptions.Empty);
		}
		
		static string ParseBody (ISymbol member, XmlTextReader xml, string endTagName, DocumentationFormatOptions options)
		{
			StringBuilder result = StringBuilderCache.Allocate (); 
			bool wasWhiteSpace = true;
			bool appendSpace = false;
			string listType = "bullet";
			int listItem = 1;
			//ITypeResolveContext ctx = member.Compilation.TypeResolveContext;
			while (xml.Read ()) {
				switch (xml.NodeType) {
				case XmlNodeType.EndElement:
					if (xml.Name == endTagName) 
						goto end;
					break;
				case XmlNodeType.Element:
					switch (xml.Name.ToLower ()) {
					case "para":
						result.AppendLine (ParseBody (member, xml, xml.Name, options));
						wasWhiteSpace = true;
						break;
					case "list":
						listType = xml ["type"] ?? listType;
						listItem = 1;
						result.AppendLine ();
						result.AppendLine ();
						break;
					case "term": {
						string inner = "<root>" + xml.ReadInnerXml () + "</root>";
						result.Append ("<i>").Append (ParseBody (member, new XmlTextReader (new StringReader (inner)), "root", options)).Append (" </i>");
						break;
					}
					case "description": {
						string inner = "<root>" + xml.ReadInnerXml () + "</root>";
						result.Append (ParseBody (member, new XmlTextReader (new StringReader (inner)), "root", options));
						break;
					}
					case "listheader":  {
					string inner = "<root>" + xml.ReadInnerXml () + "</root>";
						string prefix;
						switch (listType) {
						case "number":
							prefix = "     ";
							break;
						case "table":
							prefix = "    ";
							break;
						default: // bullet;
							prefix = "    ";
							break;
						}
						result.Append (prefix);
						result.AppendLine (ParseBody (member, new XmlTextReader (new StringReader (inner)), "root", options));
						break;
					}
					case "item": {
						string inner = "<root>" + xml.ReadInnerXml () + "</root>";
							string prefix;
							switch (listType) {
							case "number":
								prefix = string.Format ("  {0:##}. ", listItem++);
								break;
							case "table":
								prefix = "    ";
								break;
							default: // bullet;
								prefix = "  \u2022 ";
								break;
							}
							result.Append (prefix);
							result.AppendLine (ParseBody (member, new XmlTextReader (new StringReader (inner)), "root", options));
						break;
					}
					case "see":
						if (!wasWhiteSpace) {
							result.Append (' ');
							wasWhiteSpace = true;
						}
						result.Append ("<i>");
						string name = (xml ["cref"] + xml ["langword"]).Trim ();
						result.Append (EscapeText (FormatCref(name)));
						result.Append ("</i>");
						wasWhiteSpace = false;
						appendSpace = true;
						break;
					case "paramref":
						if (!wasWhiteSpace) {
							result.Append (' ');
							wasWhiteSpace = true;
						}
						result.Append ("<i>");
						result.Append (EscapeText (xml ["name"].Trim ()));
						result.Append ("</i>");
						appendSpace = true;
						wasWhiteSpace = false;
						break;
					}
					break;
				case XmlNodeType.Text:
					if (IsEmptyDocumentation (xml.Value))
						break;
					foreach (char ch in xml.Value) {
						if (!Char.IsWhiteSpace (ch) && appendSpace) {
							result.Append (' ');
							appendSpace = false;
						}
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
			return StringBuilderCache.ReturnAndFree (result).Trim ();
		}

		static string FormatCref (string cref)
		{
			if (cref.Length > 2 && cref [1] == ':')
				return cref.Substring (2);
			return cref;
		}

		public static string GetDocumentationMarkup (ISymbol member, string doc, DocumentationFormatOptions options)
		{
			if (string.IsNullOrEmpty (doc))
				return null;
			System.IO.StringReader reader = new System.IO.StringReader ("<docroot>" + doc + "</docroot>");
			XmlTextReader xml = new XmlTextReader (reader);
			StringBuilder ret = StringBuilderCache.Allocate ();
			StringBuilder parameterBuilder = StringBuilderCache.Allocate ();
			StringBuilder exceptions = StringBuilderCache.Allocate ();
			exceptions.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Exceptions:")));
			//		ret.Append ("<small>");
			int paramCount = 0, exceptionCount = 0, summaryEnd = -1;
			try {
				xml.Read ();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						switch (xml.Name.ToLower ()) {
						case "para":
							ret.Append (options.FormatBody (ParseBody (member, xml, xml.Name, options)));
							if (summaryEnd < 0)
								summaryEnd = ret.Length;
							break;
						case "member":
						case "summary":
							var summary = options.FormatBody (ParseBody (member, xml, xml.Name, options));
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
								ret.Append (options.FormatBody (ParseBody (member, xml, xml.Name, options)));
								if (summaryEnd < 0)
									summaryEnd = ret.Length;
							} else {
								options.FormatBody (ParseBody (member, xml, xml.Name, options));
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
							exceptions.Append (EscapeText (xml ["cref"]));
							exceptions.Append (": ");
							exceptions.Append ("</b>");
							if (options.SmallText)
								exceptions.Append ("</small>");

							exceptions.AppendLine (options.FormatBody (ParseBody (member, xml, xml.Name, options)));
							break;
						case "returns":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Returns:")));
								ret.Append (options.FormatBody (ParseBody (member, xml, xml.Name, options)));
							} else {
								options.FormatBody (ParseBody (member, xml, xml.Name, options));
							}
							break;
						case "param":
							string paramName = xml.GetAttribute ("name") != null ? xml ["name"].Trim () : "";

							var body = options.FormatBody (ParseBody (member, xml, xml.Name, options));
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
							} else {
								return null;
							}
							break;
						case "value":
							ret.AppendLine (options.FormatHeading (GettextCatalog.GetString ("Value:")));
							ret.AppendLine (options.FormatBody (ParseBody (member, xml, xml.Name, options)));
							break;
						case "seealso":
							if (string.IsNullOrEmpty (options.HighlightParameter)) {
								ret.Append (options.FormatHeading (GettextCatalog.GetString ("See also:")));
								ret.Append (" ");
								ret.Append (EscapeText (xml ["cref"]));
								ret.Append (EscapeText (xml ["langword"]));
							}
							break;
						}
					}
				} while (xml.Read ());

				if (IsEmptyDocumentation (ret.ToString ()) && IsEmptyDocumentation (parameterBuilder.ToString ()))
					return EscapeText (doc);
				if (string.IsNullOrEmpty (options.HighlightParameter) && exceptionCount > 0)
					ret.Append (exceptions.ToString ());

				string result = ret.ToString ();
				if (summaryEnd < 0)
					summaryEnd = result.Length;
				if (paramCount > 0) {
					var paramSb = StringBuilderCache.Allocate ();
					if (result.Length > 0)
						paramSb.AppendLine ();/*
				paramSb.Append ("<small>");
				paramSb.AppendLine (options.FormatHeading (GettextCatalog.GetPluralString ("Parameter:", "Parameters:", paramCount)));
				paramSb.Append ("</small>");*/
					paramSb.Append (parameterBuilder.ToString ());
					result = result.Insert (summaryEnd, StringBuilderCache.ReturnAndFree(paramSb));
				}
				result = result.Trim ();
				if (result.EndsWith (Environment.NewLine + "</small>"))
					result = result.Substring (0, result.Length - (Environment.NewLine + "</small>").Length) + "</small>";
				return result;
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError (ex.ToString ());
				return EscapeText (doc);
			} finally {
				StringBuilderCache.Free (ret);
				StringBuilderCache.Free (parameterBuilder);
				StringBuilderCache.Free (exceptions);
			}
		}
		#endregion

		#region Tooltips
		static List<AmbienceTooltipProvider> tooltipProviders = new List<AmbienceTooltipProvider>();

		public static async Task<TooltipInformation> GetTooltip (CancellationToken token, ISymbol symbol)
		{
			foreach (var tp in tooltipProviders) {
				var result = await tp.GetTooltip (token, symbol);
				if (result != null)
					return result;
			}
			return null;
		}
		#endregion
	}
}
