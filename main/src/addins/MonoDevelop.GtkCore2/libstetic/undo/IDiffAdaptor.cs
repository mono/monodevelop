
using System;
using System.Collections;
using System.Xml;

namespace Stetic.Undo
{
	interface IDiffAdaptor
	{
		IEnumerable GetChildren (object parent);
		string GetUndoId (object childObject);
		object FindChild (object parent, string undoId);
		void RemoveChild (object parent, string undoId);
		void AddChild (object parent, XmlElement data, string insertAfter);
		XmlElement SerializeChild (object child);
		IDiffAdaptor GetChildAdaptor (object child);
		
		IEnumerable GetProperties (object obj);
		object GetPropertyByName (object obj, string name);
		string GetPropertyName (object property);
		string GetPropertyValue (object obj, object property);
		void SetPropertyValue (object obj, string name, string value);
		void ResetPropertyValue (object obj, string name);
		
		IEnumerable GetSignals (object obj);
		object GetSignal (object obj, string name, string handler);
		void GetSignalInfo (object signal, out string name, out string handler);
		void AddSignal (object obj, string name, string handler);
		void RemoveSignal (object obj, string name, string handler);
	}
}
