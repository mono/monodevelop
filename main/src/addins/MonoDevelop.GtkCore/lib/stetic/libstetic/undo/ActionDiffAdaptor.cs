
using System;
using System.Xml;
using System.Collections;
using Stetic.Wrapper;

namespace Stetic.Undo
{
	class ActionDiffAdaptor: IDiffAdaptor
	{
		IProject project;
		
		public ActionDiffAdaptor (IProject project)
		{
			this.project = project;
		}
		
		public IEnumerable GetChildren (object parent)
		{
			if (parent is Wrapper.Action)
				yield break;
			else if (parent is ActionGroup) {
				foreach (Wrapper.Action ac in ((ActionGroup)parent).Actions)
					if (ac.Name.Length > 0)
						yield return ac;
			}
			else if (parent is ActionGroupCollection) {
				foreach (ActionGroup ag in (ActionGroupCollection) parent)
					yield return ag;
			}
			else
				throw new NotImplementedException ();
		}
		
		public string GetUndoId (object childObject)
		{
			if (childObject is ActionGroup)
				return ((ActionGroup)childObject).UndoId;
			if (childObject is Wrapper.Action)
				return ((Wrapper.Action)childObject).UndoId;

			throw new NotImplementedException ();
		}
		
		public object FindChild (object parent, string undoId)
		{
			foreach (object ob in GetChildren (parent))
				if (GetUndoId (ob) == undoId) {
					if ((ob is Wrapper.Action) && ((Wrapper.Action)ob).Name.Length == 0)
						continue;
					return ob;
				}
			return null;
		}
		
		public void RemoveChild (object parent, string undoId)
		{
			object child = FindChild (parent, undoId);
			if (child == null)
				return;
			if (parent is ActionGroup) {
				((ActionGroup)parent).Actions.Remove ((Wrapper.Action)child);
			} else if (parent is ActionGroupCollection) {
				((ActionGroupCollection)parent).Remove ((ActionGroup)child);
			} else
				throw new NotImplementedException ();
		}
		
		public void AddChild (object parent, XmlElement node, string insertAfter)
		{
			object data = DeserializeChild (node);
			if (parent is ActionGroup) {
				ActionGroup group = (ActionGroup) parent;
				if (insertAfter == null)
					group.Actions.Insert (0, (Wrapper.Action) data);
				else {
					for (int n=0; n<group.Actions.Count; n++) {
						if (group.Actions [n].UndoId == insertAfter) {
							group.Actions.Insert (n+1, (Wrapper.Action) data);
							return;
						}
					}
					group.Actions.Add ((Wrapper.Action) data);
				}
			}
			if (parent is ActionGroupCollection) {
				ActionGroupCollection col = (ActionGroupCollection) parent;
				if (insertAfter == null)
					col.Insert (0, (ActionGroup) data);
				else {
					for (int n=0; n<col.Count; n++) {
						if (col [n].UndoId == insertAfter) {
							col.Insert (n+1, (ActionGroup) data);
							return;
						}
					}
					col.Add ((ActionGroup) data);
				}
			}
		}
		
		public IEnumerable GetProperties (object obj)
		{
			Wrapper.Action action = obj as Wrapper.Action;
			if (action != null) {
				foreach (ItemGroup iset in action.ClassDescriptor.ItemGroups) {
					foreach (ItemDescriptor it in iset) {
						PropertyDescriptor prop = it as PropertyDescriptor;
						
						if (!prop.VisibleFor (action.Wrapped) || !prop.CanWrite)
							continue;

						object value = prop.GetValue (action.Wrapped);
						
						// If the property has its default value, we don't need to check it
						if (value == null || (prop.HasDefault && prop.IsDefaultValue (value)))
							continue;
						
						yield return it;
					}
				}
			}
			else if (obj is ActionGroup)
				yield return "name";	// ActionGroup only has one property, the name
			else
				yield break;
		}
		
		public XmlElement SerializeChild (object child)
		{
			XmlDocument doc = new XmlDocument ();
			ObjectWriter ow = new ObjectWriter (doc, FileFormat.Native);
			
			if (child is Wrapper.Action) {
				return ((Wrapper.Action)child).Write (ow);
			} else if (child is ActionGroup) {
				return ((ActionGroup)child).Write (ow);
			}
			throw new NotImplementedException ();
		}
		
		public object DeserializeChild (XmlElement data)
		{
			ObjectReader or = new ObjectReader (project, FileFormat.Native);
			if (data.LocalName == "action") {
				Wrapper.Action ac = new Wrapper.Action ();
				ac.Read (or, data);
				return ac;
			} else if (data.LocalName == "action-group") {
				ActionGroup ac = new ActionGroup ();
				ac.Read (or, data);
				return ac;
			}
			throw new NotImplementedException ();
		}
		
		public IDiffAdaptor GetChildAdaptor (object child)
		{
			return this;
		}
		
		public object GetPropertyByName (object obj, string name)
		{
			if (obj is Wrapper.Action) {
				if (name == "id") name = "Name";
				return ((Wrapper.Action)obj).ClassDescriptor [name];
			}
			else if (obj is ActionGroup) {
				if (name == "name") return name;
			}
			throw new NotImplementedException ();
		}
		
		public string GetPropertyName (object property)
		{
			if (property is PropertyDescriptor) {
				PropertyDescriptor prop = (PropertyDescriptor) property;
				if (prop.Name == "Name") return "id";
				return prop.Name;
			}
			else if (property is string)
				return (string) property;

			throw new NotImplementedException ();
		}
		
		public string GetPropertyValue (object obj, object property)
		{
			if (obj is Wrapper.Action) {
				PropertyDescriptor prop = (PropertyDescriptor) property;
				object val = prop.GetValue (((Wrapper.Action)obj).Wrapped);
				return prop.ValueToString (val);
			}
			else if (obj is ActionGroup) {
				if (((string)property) == "name")
					return ((ActionGroup)obj).Name;
			}
			throw new NotImplementedException ();
		}
		
		public void SetPropertyValue (object obj, string name, string value)
		{
			if (obj is Wrapper.Action) {
				if (name == "id") name = "Name";
				PropertyDescriptor prop = (PropertyDescriptor) GetPropertyByName (obj, name);
				if (prop == null)
					throw new InvalidOperationException ("Property '" + name + "' not found in object of type: " + obj.GetType ());
				prop.SetValue (((Wrapper.Action)obj).Wrapped, prop.StringToValue (value));
				return;
			}
			else if (obj is ActionGroup) {
				if (name == "name") {
					((ActionGroup)obj).Name = value;
					return;
				}
			}
			throw new NotImplementedException ();
		}

		public void ResetPropertyValue (object obj, string name)
		{
			if (obj is Wrapper.Action) {
				if (name == "id") name = "Name";
				PropertyDescriptor prop = (PropertyDescriptor) GetPropertyByName (obj, name);
				prop.ResetValue (((Wrapper.Action)obj).Wrapped);
			}
		}
		
		public IEnumerable GetSignals (object obj)
		{
			if (obj is Wrapper.Action) {
				foreach (Signal s in ((Wrapper.Action)obj).Signals)
					yield return s;
			}
			else
				yield break;
		}
		
		public object GetSignal (object obj, string name, string handler)
		{
			foreach (Signal s in ((Wrapper.Action)obj).Signals) {
				if (s.SignalDescriptor.Name == name && s.Handler == handler)
					return s;
			}
			return null;
		}
		
		public void GetSignalInfo (object signal, out string name, out string handler)
		{
			Signal s = (Signal) signal;
			name = s.SignalDescriptor.Name;
			handler = s.Handler;
		}
		
		public void AddSignal (object obj, string name, string handler)
		{
			SignalDescriptor sd = (SignalDescriptor) ((Wrapper.Action)obj).ClassDescriptor.SignalGroups.GetItem (name);
			Signal sig = new Signal (sd);
			sig.Handler = handler;
			((Wrapper.Action)obj).Signals.Add (sig);
		}
		
		public void RemoveSignal (object obj, string name, string handler)
		{
			foreach (Signal sig in ((Wrapper.Action)obj).Signals) {
				if (sig.SignalDescriptor.Name == name && sig.Handler == handler) {
					((Wrapper.Action)obj).Signals.Remove (sig);
					return;
				}
			}
		}
	}
}
