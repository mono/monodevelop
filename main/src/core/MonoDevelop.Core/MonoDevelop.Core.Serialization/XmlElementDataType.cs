// XmlElementDataType.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//

using System;
using System.Xml;

namespace MonoDevelop.Core.Serialization
{
	public class XmlElementDataType: DataType
	{
		public XmlElementDataType (): base (typeof(XmlElement))
		{
		}
		
		protected internal override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			XmlElement elem = (XmlElement) value;
			XmlConfigurationReader sr = new XmlConfigurationReader ();
			return sr.Read (elem);
		}

		protected internal override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			XmlConfigurationWriter sw = new XmlConfigurationWriter ();
			XmlDocument doc = new XmlDocument ();
			return sw.Write (doc, data);
		}

		public override bool IsSimpleType {
			get { return false; }
		}

		public override bool CanCreateInstance {
			get { return true; }
		}
		
		public override bool CanReuseInstance {
			get { return false; }
		}
	}
}
