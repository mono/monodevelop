// MemberCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Text;
using System.Xml;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharpBinding
{
	public class MemberCompletionData : MonoDevelop.Projects.Gui.Completion.CodeCompletionData
	{
		IMember member;
		
		public override string Description {
			get {
				CheckDescription ();
				return base.Description;
			}
		}
		public override string DescriptionPango {
			get {
				CheckDescription ();
				return base.DescriptionPango;
			}
		}
		
		public MemberCompletionData (IMember member) : base (ambience.GetString (member, OutputFlags.ClassBrowserEntries), member.StockIcon)
		{
			this.member = member;
		}
		
		static CSharpAmbience ambience = new CSharpAmbience ();
		bool descriptionCreated = false;
		
		void CheckDescription ()
		{
			if (descriptionCreated)
				return;
			
			descriptionCreated = true;
			
			string doc = ambience.GetString (member, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName);
			XmlNode node = member.GetMonodocDocumentation ();
			if (node != null) {
				node = node.SelectSingleNode ("summary");
				if (node != null) {
					doc += Environment.NewLine + GetDocumentation (node.InnerXml);
				}
			}
			base.Documentation = "";
			base.Description = base.DescriptionPango = doc;
		}
	
		public static string FormatText (string text)
		{
			StringBuilder result = new StringBuilder ();
			bool wasWhitespace = false;
			foreach (char ch in text) {
				switch (ch) {
					case '\n':
					case '\r':
						break;
					case '<':
						result.Append ("&lt;");
						break;
					case '>':
						result.Append ("&gt;");
						break;
					case '&':
						result.Append ("&amp;");
						break;
					default:
						if (wasWhitespace && Char.IsWhiteSpace (ch))
							break;
						wasWhitespace = Char.IsWhiteSpace (ch);
						result.Append (ch);
						break;
				}
			}
			return result.ToString ();
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
		public static string GetDocumentation (string doc)
		{
			System.IO.StringReader reader = new System.IO.StringReader("<docroot>" + doc + "</docroot>");
			XmlTextReader xml   = new XmlTextReader(reader);
			StringBuilder ret   = new StringBuilder(70);
			
			try {
				xml.Read();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						string elname = xml.Name.ToLower();
						if (elname == "remarks") {
							ret.Append("Remarks:\n");
						// skip <example>-nodes
						} else if (elname == "example") {
							xml.Skip();
							xml.Skip();
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
						ret.Append(xml.Value);
					}
				} while (xml.Read ());
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return doc;
			}
			return ret.ToString ();
		}
		
	}
}
