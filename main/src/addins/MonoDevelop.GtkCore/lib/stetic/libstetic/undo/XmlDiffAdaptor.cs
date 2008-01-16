
using System;
using System.Collections;
using System.Xml;

namespace Stetic.Undo
{
	class XmlDiffAdaptor: IDiffAdaptor
	{
		string childElementName;
		string propsElementName;
		bool processProperties = true;
		XmlDiffAdaptor childAdaptor;
		
		public XmlDiffAdaptor ChildAdaptor {
			get { return childAdaptor; }
			set { childAdaptor = value; }
		}
		
		public string ChildElementName {
			get { return childElementName; }
			set { childElementName = value; }
		}
		
		public string PropsElementName {
			get { return propsElementName; }
			set { propsElementName = value; }
		}
		
		public bool ProcessProperties {
			get { return processProperties; }
			set { processProperties = value; }
		}
		
		public IEnumerable GetChildren (object parent)
		{
			if (childElementName != null)
				return ((XmlElement)parent).SelectNodes (childElementName);
			else
				return new object[0];
		}
		
		public string GetUndoId (object childObject)
		{
			return ((XmlElement)childObject).GetAttribute ("undoId");
		}
		
		public object FindChild (object parent, string undoId)
		{
			return ((XmlElement) parent).SelectSingleNode (childElementName + "[@undoId='" + undoId + "']");
		}
		
		public void RemoveChild (object parent, string undoId)
		{
			XmlElement elem = (XmlElement) parent;
			XmlElement child = (XmlElement) elem.SelectSingleNode (childElementName + "[@undoId='" + undoId + "']");
			if (child != null)
				elem.RemoveChild (child);
		}
		
		public void AddChild (object parent, XmlElement newNode, string insertAfter)
		{
			XmlElement status = (XmlElement) parent;
			if (newNode.OwnerDocument != status.OwnerDocument)
				newNode = (XmlElement) status.OwnerDocument.ImportNode (newNode, true);

			if (insertAfter != null) {
				XmlElement statusChild = (XmlElement) status.SelectSingleNode (childElementName + "[@undoId='" + insertAfter + "']");
				if (statusChild != null)
					status.InsertAfter (newNode, statusChild);
				else
					status.AppendChild (newNode);
			} else {
				if (status.FirstChild != null)
					status.InsertBefore (newNode, status.FirstChild);
				else
					status.AppendChild (newNode);
			}
		}
		
		public XmlElement SerializeChild (object child)
		{
			return (XmlElement) child;
		}
		
		public object DeserializeChild (XmlElement data)
		{
			return data;
		}
		
		public IDiffAdaptor GetChildAdaptor (object child)
		{
			if (childAdaptor != null)
				return childAdaptor;
			else
				return this;
		}
		
		XmlElement GetPropsElem (object obj, bool create)
		{
			if (propsElementName != null) {
				XmlElement elem = ((XmlElement)obj) [propsElementName];
				if (elem == null && create) {
					elem = ((XmlElement)obj).OwnerDocument.CreateElement (propsElementName);
					((XmlElement)obj).AppendChild (elem);
				}
				return elem;
			}
			else
				return (XmlElement) obj;
		}
		
		public IEnumerable GetProperties (object obj)
		{
			XmlElement elem = GetPropsElem (obj, false);
			if (elem != null && processProperties) {
				XmlAttribute at = elem.Attributes ["name"];
				if (at != null)
					yield return at;
				at = elem.Attributes ["id"];
				if (at != null)
					yield return at;
				foreach (XmlElement e in elem.SelectNodes ("property"))
					yield return e;
			} else
				yield break;
		}
		
		public object GetPropertyByName (object obj, string name)
		{
			XmlElement elem = GetPropsElem (obj, false);
			if (elem == null)
				return null;
			XmlElement prop = (XmlElement) elem.SelectSingleNode ("property[@name='" + name + "']");
			if (prop != null)
				return prop;
			return elem.Attributes [name];
		}
		
		public string GetPropertyName (object property)
		{
			if (property is XmlElement)
				return ((XmlElement)property).GetAttribute ("name");
			else
				return ((XmlAttribute)property).LocalName;
		}
		
		public string GetPropertyValue (object obj, object property)
		{
			if (property is XmlElement)
				return ((XmlElement)property).InnerText;
			else
				return ((XmlAttribute)property).Value;
		}
		
		public void SetPropertyValue (object obj, string name, string value)
		{
			XmlElement elem = GetPropsElem (obj, true);
			if (name == "id" || name == "name") {
				elem.SetAttribute (name, value);
			} else {
				XmlElement prop = (XmlElement) elem.SelectSingleNode ("property[@name='" + name + "']");
				if (prop == null) {
					prop = elem.OwnerDocument.CreateElement ("property");
					prop.SetAttribute ("name", name);
					elem.AppendChild (prop);
				}
				prop.InnerText = value;
			}
		}
		
		public void ResetPropertyValue (object obj, string name)
		{
			XmlElement elem = GetPropsElem (obj, false);
			if (elem == null)
				return;
			XmlElement prop = (XmlElement) elem.SelectSingleNode ("property[@name='" + name + "']");
			if (prop != null)
				elem.RemoveChild (prop);
			else {
				XmlAttribute at = elem.Attributes [name];
				if (at != null)
					elem.Attributes.Remove (at);
			}
		}
		
		public IEnumerable GetSignals (object obj)
		{
			XmlElement elem = GetPropsElem (obj, false);
			if (elem != null && processProperties)
				return elem.SelectNodes ("signal");
			else
				return Type.EmptyTypes;
		}
		
		public object GetSignal (object obj, string name, string handler)
		{
			XmlElement elem = GetPropsElem (obj, false);
			if (elem == null)
				return null;
			return elem.SelectSingleNode ("signal[@name='" + name + "' and @handler='" + handler + "']");
		}
		
		public void GetSignalInfo (object signal, out string name, out string handler)
		{
			XmlElement elem = (XmlElement) signal;
			name = elem.GetAttribute ("name");
			handler = elem.GetAttribute ("handler");
		}
		
		public void AddSignal (object obj, string name, string handler)
		{
			XmlElement elem = GetPropsElem (obj, true);
			XmlElement signal = elem.OwnerDocument.CreateElement ("signal");
			signal.SetAttribute ("name", name);
			signal.SetAttribute ("handler", handler);
			elem.AppendChild (signal);
		}
		
		public void RemoveSignal (object obj, string name, string handler)
		{
			XmlElement elem = GetPropsElem (obj, false);
			if (elem == null)
				return;
			XmlElement prop = (XmlElement) elem.SelectSingleNode ("signal[@name='" + name + "' && @handler='" + handler + "']");
			if (prop != null)
				elem.RemoveChild (prop);
		}
	}
}
