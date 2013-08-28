// 
// DocConfig.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.DocFood
{
	class DocConfig
	{
		public Dictionary<string, List<string>> WordLists = new Dictionary<string, List<string>> ();
		public Dictionary<string, string> WordExpansions = new Dictionary<string, string> ();
		public Dictionary<string, string> Macros = new Dictionary<string, string> ();
		public List<Node> Rules = new List<Node> ();
		
		public static DocConfig Instance {
			get;
			private set;
		}
		
		static DocConfig ()
		{
			const string ManifestResourceName = "DocFood.config.xml";
			using (var reader = XmlTextReader.Create (typeof (DocConfig).Assembly.GetManifestResourceStream (ManifestResourceName))) {
				Instance = Read (reader);
			}
		}
		
		public static DocConfig Read (XmlReader reader)
		{
			DocConfig result = new DocConfig ();
			XmlReadHelper.ReadList (reader, "DocFood", delegate () {
				switch (reader.LocalName) {
				case "WordLists":
					XmlReadHelper.ReadList (reader, reader.LocalName, delegate () {
						switch (reader.LocalName) {
						case "List":
							string name = reader.GetAttribute ("name");
							List<string> words = new List<string> ();
							XmlReadHelper.ReadList (reader, reader.LocalName, delegate () {
								switch (reader.LocalName) {
								case "Word":
									words.Add (reader.ReadElementString ());
									return true;
								}
								return false;
							});
							result.WordLists[name] = words;
							return true;
						}
						return false;
					});
					return true;
				case "WordExpansion":
					XmlReadHelper.ReadList (reader, reader.LocalName, delegate () {
						switch (reader.LocalName) {
						case "Word":
							result.WordExpansions[reader.GetAttribute ("from")] = reader.GetAttribute ("to");
							return true;
						}
						return false;
					});
					return true;
				case "Macros":
					XmlReadHelper.ReadList (reader, reader.LocalName, delegate () {
						switch (reader.LocalName) {
						case "Macro":
							result.Macros[reader.GetAttribute ("tag")] = reader.GetAttribute ("value");
							return true;
						}
						return false;
					});
					return true;
				case "Rules":
					result.Rules = Node.ReadNodeList (reader, "Rules");
					return true;
				}
				return false;
			});
			return result;
		}
	}
	
}

