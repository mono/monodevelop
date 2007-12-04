//
// DatabaseConfiguration.cs
//
// Author:
//   Lluis Sanchez Gual
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
using System.Collections;
using System.Xml;

namespace Mono.Addins.Database
{
	internal class DatabaseConfiguration
	{
		Hashtable addinStatus = new Hashtable ();
		
		public bool IsEnabled (string addinId, bool defaultValue)
		{
			if (addinStatus.Contains (addinId))
				return addinStatus [addinId] != null;
			else
				return defaultValue;
		}
		
		public void SetStatus (string addinId, bool enabled, bool defaultValue)
		{
			if (enabled == defaultValue)
				addinStatus.Remove (addinId);
			else if (enabled)
				addinStatus [addinId] = this;
			else
				addinStatus [addinId] = null;
		}
		
		public static DatabaseConfiguration Read (string file)
		{
			DatabaseConfiguration config = new DatabaseConfiguration ();
			
			StreamReader s = new StreamReader (file);
			using (s) {
				XmlTextReader tr = new XmlTextReader (s);
				tr.MoveToContent ();
				if (tr.IsEmptyElement)
					return config;
				
				tr.ReadStartElement ("Configuration");
				tr.MoveToContent ();
				
				while (tr.NodeType != XmlNodeType.EndElement) {
					
					if (tr.NodeType != XmlNodeType.Element || tr.IsEmptyElement) {
						tr.Skip ();
					}
					else if (tr.LocalName == "DisabledAddins") {
						// For back compatibility
						tr.ReadStartElement ();
						tr.MoveToContent ();
						while (tr.NodeType != XmlNodeType.EndElement) {
							if (tr.NodeType == XmlNodeType.Element && tr.LocalName == "Addin")
								config.addinStatus [tr.ReadElementString ()] = null;
							else
								tr.Skip ();
							tr.MoveToContent ();
						}
						tr.ReadEndElement ();
					}
					else if (tr.LocalName == "AddinStatus") {
						tr.ReadStartElement ();
						tr.MoveToContent ();
						while (tr.NodeType != XmlNodeType.EndElement) {
							if (tr.NodeType == XmlNodeType.Element && tr.LocalName == "Addin") {
								string aid = tr.GetAttribute ("id");
								string senabled = tr.GetAttribute ("enabled");
								if (senabled.Length == 0 || senabled == "True")
									config.addinStatus [aid] = config;
								else
									config.addinStatus [aid] = null;
							}
							tr.Skip ();
							tr.MoveToContent ();
						}
						tr.ReadEndElement ();
					}
					tr.MoveToContent ();
				}
			}
			return config;
		}
		
		public void Write (string file)
		{
			StreamWriter s = new StreamWriter (file);
			using (s) {
				XmlTextWriter tw = new XmlTextWriter (s);
				tw.Formatting = Formatting.Indented;
				tw.WriteStartElement ("Configuration");
				tw.WriteStartElement ("AddinStatus");
				foreach (DictionaryEntry e in addinStatus) {
					tw.WriteStartElement ("Addin");
					tw.WriteAttributeString ("id", (string)e.Key);
					tw.WriteAttributeString ("enabled", (e.Value != null).ToString ());
					tw.WriteEndElement ();
				}
				tw.WriteEndElement ();
				tw.WriteEndElement ();
			}
		}
	}
}
