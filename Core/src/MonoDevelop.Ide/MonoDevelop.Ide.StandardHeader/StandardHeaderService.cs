//
// StandardHeaderService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.StandardHeaders
{
	public static class StandardHeaderService
	{
		const string version          = "1.0";
		const string templateFileName = "StandardHeader.xml";
		
		static string header;
		
		public static string Header {
			get { 
				return header; 
			}
			set {
				if (header != value) {
					header = value;
					Save (ConfigLocation);
				}
			}
		}
		
		static string ConfigLocation {
			get {
				PropertyService propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));
				return Path.Combine (propertyService.ConfigDirectory, templateFileName);
			}
		}
		
		static string GetComment (string language)
		{
			switch (language) {
			case "C#":
				return "//";
			case "VBNet":
				return "'";
			}
			return null;
		}
		public static string GetHeader (string language)
		{
			if (Header == null || GetComment (language) == null) {
				return "";
			}
			StringBuilder result = new StringBuilder ();
			
			char ch = Environment.NewLine.ToCharArray () [0];
			string[] lines = Header.Split (ch);
			foreach (string line in lines) {
				result.Append (GetComment (language));
				result.Append (line);
				result.Append (Environment.NewLine);
			}
			return result.ToString();
		}
		
		static StandardHeaderService ()
		{
			if (File.Exists (ConfigLocation)) {
				Load (ConfigLocation);
				return;
			}
			PropertyService propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));
			string file = Path.Combine (Path.Combine (propertyService.DataDirectory, "options"), templateFileName);
			if (File.Exists (file))
				Load (file);
		}
		
#region I/O
		const string Node             = "StandardHeader";
		const string VersionAttribute = "version";
		
		static void Load (string fileName)
		{
			using (XmlReader reader = XmlTextReader.Create (fileName)) {
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case Node:
							header = reader.ReadString ();
							break;
						}
					}
				}
			}
		}
		
		static void Save (string fileName)
		{
			using (XmlWriter writer = XmlTextWriter.Create (fileName)) {
				writer.Settings.Indent = true;
				writer.WriteStartElement (Node);
				writer.WriteAttributeString (VersionAttribute, version);
				writer.WriteString (header);
				writer.WriteEndElement (); // Node
			}
		}
#endregion
		
	}
}
