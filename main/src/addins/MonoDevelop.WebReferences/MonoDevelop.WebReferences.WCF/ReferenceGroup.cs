// 
// WebServiceEngineWCF.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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

using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;

namespace MonoDevelop.WebReferences.WCF
{
	[XmlRoot(Namespace = "urn:schemas-microsoft-com:xml-wcfservicemap")]
	public class ReferenceGroup
	{
		ClientOptions options = new ClientOptions ();
		readonly List<MetadataSource> sources = new List<MetadataSource> ();
		readonly List<MetadataFile> metadata = new List<MetadataFile> ();
		
		[XmlAttribute]
		public string ID { get; set; }
		
		public ClientOptions ClientOptions {
			get { return options; }
			set { options = value; }
		}
		
		public List<MetadataSource> MetadataSources {
			get { return sources; }
		}
		
		public List<MetadataFile> Metadata {
			get { return metadata; }
		}
		
		public void Save (string file)
		{
			XmlSerializer ser = new XmlSerializer (typeof(ReferenceGroup));
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			using (XmlWriter w = XmlWriter.Create (file, settings)) {
				ser.Serialize (w, this);
			}
		}
		
		public static ReferenceGroup Read (string file)
		{
			using (StreamReader sr = new StreamReader (file)) {
				XmlSerializer ser = new XmlSerializer (typeof (ReferenceGroup));
				return (ReferenceGroup) ser.Deserialize (sr);
			}
		}
	}
}
