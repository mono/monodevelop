//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//

using MonoDevelop.Projects.Gui.Completion;
using System;
using System.Collections.Generic;

namespace MonoDevelop.XmlEditor
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
