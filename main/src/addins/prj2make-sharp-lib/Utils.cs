//
// Utils.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Projects;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public static class Utils
	{
		public const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";

		public static string GetValidPath (IProgressMonitor monitor, string basePath, string relPath)
		{
			string path = MapAndResolvePath (basePath, relPath);
			if (path != null)
				return path;

			LoggingService.LogWarning (GettextCatalog.GetString ("File name '{0}' is invalid. Ignoring.", relPath));
			monitor.ReportWarning (GettextCatalog.GetString ("File name '{0}' is invalid. Ignoring.", relPath));
			return null;
		}

		public static XmlNode MoveToChild (XmlNode node, string localName)
		{
			if (!node.HasChildNodes)
				return null;

			foreach (XmlNode n in node.ChildNodes)
				if (n.LocalName == localName)
					return n;

			return null;
		}

		public static void RemoveChild (XmlNode parent, string localName)
		{
			XmlNode child = MoveToChild (parent, localName);
			if (child != null)
				parent.RemoveChild (child);
		}

		public static void EnsureChildValue (XmlNode node, string localName, bool val)
		{
			EnsureChildValue (node, localName, val.ToString ().ToLower ());
		}

		public static void EnsureChildValue (XmlNode node, string localName, object val)
		{
			XmlNode n = MoveToChild (node, localName);
			if (n == null) {
				//Child not found, create it
				XmlElement e = node.OwnerDocument.CreateElement (localName, ns);
				e.InnerText = val.ToString ();

				node.AppendChild (e);
			} else {
				n.InnerText = val.ToString ();
			}
		}

		public static bool ReadAsString (XmlNode node, string localName, ref string val, bool allowEmpty)
		{
			//Assumption: Number of child nodes is small enough
			//that xpath query would be more expensive than
			//linear traversal
			if (node == null || !node.HasChildNodes)
				return false;

			foreach (XmlNode n in node.ChildNodes) {
				//Case sensitive compare
				if (n.LocalName != localName)
					continue;

				//FIXME: Use XmlChar.WhitespaceChars ?
				string s= n.InnerText.Trim ();
				if (s.Length == 0 && !allowEmpty)
					return false;

				val = s;
				return true;
			}

			return false;
		}

		public static bool ReadAsString (XPathNavigator nav, string localName, ref string val, bool allowEmpty)
		{
			if (!nav.MoveToChild (localName, ns))
				return false;

			//FIXME: Use XmlChar.WhitespaceChars ?
			string s = nav.Value.Trim ();
			nav.MoveToParent ();

			if (s.Length == 0 && !allowEmpty)
				return false;

			val = s;
			return true;
		}

		public static bool ReadAsBool (XPathNavigator nav, string localName, ref bool val)
		{
			string str = String.Empty;
			if (!ReadAsString (nav, localName, ref str, false))
				return false;

			switch (str.ToUpper ()) {
			case "TRUE":
				val = true;
				break;
			case "FALSE":
				val = false;
				break;
			default:
				return false;
			}

			return true;
		}

		public static bool ReadOffOnAsBool (XPathNavigator nav, string localName, ref bool val)
		{
			string str = String.Empty;
			if (!ReadAsString (nav, localName, ref str, false))
				return false;

			switch (str.ToUpper ()) {
			case "ON":
				val = true;
				break;
			case "OFF":
				val = false;
				break;
			default:
				return false;
			}

			return true;
		}

		public static bool ReadAsInt (XPathNavigator nav, string localName, ref int val)
		{
			if (!nav.MoveToChild (localName, ns))
				return false;

			try {
				val = nav.ValueAsInt;
			} catch {
				return false;
			} finally {
				nav.MoveToParent ();
			}

			return true;
		}

		public static bool ReadAsInt (XmlNode node, string localName, ref int val)
		{
			string str_tmp = null;
			if (!ReadAsString (node, localName, ref str_tmp, false))
				return false;

			try {
				val = Int32.Parse (str_tmp);
			} catch (FormatException) {
				return false;
			} catch (OverflowException) {
				return false;
			}

			return true;
		}

		//Creates a <localName>Value</localName>
		public static XmlElement AppendChild (XmlElement e, string localName, string value)
		{
			XmlElement elem = e.OwnerDocument.CreateElement (localName, ns);
			elem.InnerText = value;
			e.AppendChild (elem);

			return elem;
		}

		public static XmlElement GetXmlElement (XmlDocument doc, XmlNode node, string path, bool create)
		{
			XmlNode parent = node;
			foreach (string name in path.Split (new char [] {'/'}, StringSplitOptions.RemoveEmptyEntries)) {
				XmlNode n = MoveToChild (parent, name);
				if (n == null) {
					if (!create)
						return null;
					n = parent.AppendChild (doc.CreateElement (name, ns));
				}
				parent = n;
			}

			return (XmlElement) parent;
		}

		public static string MapAndResolvePath (string basePath, string relPath)
		{
			return SlnMaker.MapPath (basePath, relPath);
		}

		public static string CanonicalizePath (string path)
		{
			if (String.IsNullOrEmpty (path))
				return path;

			string ret = FileService.NormalizeRelativePath (path);
			if (ret.Length == 0)
				return ".";

			return Escape (ret).Replace ('/', '\\');
		}

		static char [] charToEscapeArray = {'$', '%', '\'', '(', ')', '*', ';', '?', '@'};
		static string charsToEscapeString = "$%'()*;?@";

		// Escape and Unescape taken from : class/Microsoft.Build.Engine/Microsoft.Build.BuildEngine/Utilities.cs
		public static string Escape (string unescapedExpression)
		{
			if (unescapedExpression.IndexOfAny (charToEscapeArray) < 0)
				return unescapedExpression;

			StringBuilder sb = new StringBuilder ();

			foreach (char c in unescapedExpression) {
				if (charsToEscapeString.IndexOf (c) < 0)
					sb.Append (c);
				else
					sb.AppendFormat ("%{0:x2}", (int) c);
			}

			return sb.ToString ();
		}

		public static string Unescape (string escapedExpression)
		{
			if (escapedExpression.IndexOf ('%') < 0)
				return escapedExpression;

			StringBuilder sb = new StringBuilder ();

			int i = 0;
			while (i < escapedExpression.Length) {
				sb.Append (Uri.HexUnescape (escapedExpression, ref i));
			}

			return sb.ToString ();
		}

		public static MSBuildData GetMSBuildData (CombineEntry entry)
		{
			if (entry.ExtendedProperties.Contains (typeof (MSBuildFileFormat)))
				return entry.ExtendedProperties [typeof (MSBuildFileFormat)] as MSBuildData;
			return null;
		}

		public static string GetLanguage (string fileName)
		{
			foreach (MSBuildProjectExtension extn in MSBuildFileFormat.Extensions) {
				if (extn.IsLanguage && extn.Supports (null, fileName, null))
					return extn.LanguageId;
			}

			return null;
		}

		//Given a filename like foo.it.resx, splits it into - foo, it, resx
		//Returns true only if a valid culture is found
		//Note: hand-written as this can get called lotsa times
		//Note: code duplicated in DotNetProject.GetCulture
		public static bool TrySplitResourceName (string fname, out string only_filename, out string culture, out string extn)
		{
			only_filename = culture = extn = null;

			int last_dot = -1;
			int culture_dot = -1;
			int i = fname.Length - 1;
			while (i >= 0) {
				if (fname [i] == '.') {
					last_dot = i;
					break;
				}
				i --;
			}
			if (i < 0)
				return false;

			i--;
			while (i >= 0) {
				if (fname [i] == '.') {
					culture_dot = i;
					break;
				}
				i --;
			}
			if (culture_dot < 0)
				return false;

			culture = fname.Substring (culture_dot + 1, last_dot - culture_dot - 1);
			if (!CultureNamesTable.ContainsKey (culture))
				return false;

			only_filename = fname.Substring (0, culture_dot);
			extn = fname.Substring (last_dot + 1);
			return true;
		}

		static Dictionary<string, string> cultureNamesTable;
		static Dictionary<string, string> CultureNamesTable {
			get {
				if (cultureNamesTable == null) {
					cultureNamesTable = new Dictionary<string, string> ();
					foreach (CultureInfo ci in CultureInfo.GetCultures (CultureTypes.AllCultures))
						cultureNamesTable [ci.Name] = ci.Name;
				}

				return cultureNamesTable;
			}
		}

	}
}
