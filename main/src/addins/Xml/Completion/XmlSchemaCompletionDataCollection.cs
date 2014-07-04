//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
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

using MonoDevelop.Ide.CodeCompletion;
using System;
using System.Collections.Generic;

namespace MonoDevelop.Xml.Completion
{
	public interface IXmlSchemaCompletionDataCollection: IEnumerable<XmlSchemaCompletionData>
	{
		XmlSchemaCompletionData this [string namespaceUri] { get; }
		XmlCompletionData[] GetNamespaceCompletionData ();
		XmlSchemaCompletionData GetSchemaFromFileName (string fileName);
	}
	
	public class XmlSchemaCompletionDataCollection : List<XmlSchemaCompletionData>, IXmlSchemaCompletionDataCollection
	{
		public XmlSchemaCompletionData this [string namespaceUri] {
			get {
				foreach (XmlSchemaCompletionData item in this)
					if (item.NamespaceUri == namespaceUri)
						return item;
				return null;
			}
		}
		
		public XmlCompletionData[] GetNamespaceCompletionData ()
		{
			List<XmlCompletionData> completionItems = new List<XmlCompletionData> ();
			foreach (XmlSchemaCompletionData schema in this)
				completionItems.Add (new XmlCompletionData (schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri));
			return completionItems.ToArray ();
		}
		
		public XmlSchemaCompletionData GetSchemaFromFileName (string fileName)
		{
			foreach (XmlSchemaCompletionData schema in this)
				if (schema.FileName == fileName)
					return schema;
			return null;
		}
	}
	
	public class MergedXmlSchemaCompletionDataCollection : IXmlSchemaCompletionDataCollection
	{
		XmlSchemaCompletionDataCollection builtin;
		XmlSchemaCompletionDataCollection user;
		
		public MergedXmlSchemaCompletionDataCollection (
		    XmlSchemaCompletionDataCollection builtin,
		    XmlSchemaCompletionDataCollection user)
		{
			this.user = user;
			this.builtin = builtin;
		}
		
		public XmlSchemaCompletionData this [string namespaceUri] {
			get {
				XmlSchemaCompletionData val = user[namespaceUri];
				if (val == null)
					val = builtin[namespaceUri];
				return val;
			}
		}

		public XmlCompletionData[] GetNamespaceCompletionData ()
		{
			Dictionary <string, XmlCompletionData> items = new Dictionary<string,XmlCompletionData> ();
			foreach (XmlSchemaCompletionData schema in builtin)
				items[schema.NamespaceUri] = new XmlCompletionData (schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri);
			foreach (XmlSchemaCompletionData schema in user)
				items[schema.NamespaceUri] = new XmlCompletionData (schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri);
			XmlCompletionData[] result = new XmlCompletionData [items.Count];
			items.Values.CopyTo (result, 0);
			return result;
		}

		public XmlSchemaCompletionData GetSchemaFromFileName (string fileName)
		{
			XmlSchemaCompletionData data = user.GetSchemaFromFileName (fileName);
			if (data == null)
				data = builtin.GetSchemaFromFileName (fileName);
			return data;
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public IEnumerator<XmlSchemaCompletionData> GetEnumerator ()
		{
			foreach (XmlSchemaCompletionData x in builtin)
				if (user[x.NamespaceUri] == null)
					yield return x;
			foreach (XmlSchemaCompletionData x in user)
				yield return x;
		}
	}
}
