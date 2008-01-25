
using System;
using System.Collections;
using System.Xml;
using Stetic.Wrapper;

namespace Stetic.Undo
{
	class DiffGenerator
	{
		IDiffAdaptor currentStatusAdaptor;
		IDiffAdaptor newStatusAdaptor;
		
		public DiffGenerator ()
		{
		}
		
		public DiffGenerator (IDiffAdaptor currentStatusAdaptor, IDiffAdaptor newStatusAdaptor)
		{
			this.currentStatusAdaptor = currentStatusAdaptor;
			this.newStatusAdaptor = newStatusAdaptor;
		}
		
		public IDiffAdaptor CurrentStatusAdaptor {
			get { return currentStatusAdaptor; }
			set { currentStatusAdaptor = value; }
		}
		
		public IDiffAdaptor NewStatusAdaptor {
			get { return newStatusAdaptor; }
			set { newStatusAdaptor = value; }
		}
		
		internal ObjectDiff GetDiff (object oldStatus, object newStatus)
		{
			PropertyDiff[] propChanges = GetPropertyDiff (currentStatusAdaptor, oldStatus, newStatusAdaptor, newStatus);
			PropertyDiff[] signalChanges = GetSignalDiff (currentStatusAdaptor, oldStatus, newStatusAdaptor, newStatus);
			
			ArrayList changes = new ArrayList ();
			Hashtable foundChildren = new Hashtable ();
			
			// Register changed and deleted child elements
			foreach (object oldChild in currentStatusAdaptor.GetChildren (oldStatus)) {
				string cid = currentStatusAdaptor.GetUndoId (oldChild);
				if (cid != null && cid.Length > 0) {
					object newChild = newStatusAdaptor.FindChild (newStatus, cid);
					if (newChild != null) {
						// ChildCreate will work even if the packing element is null
						ObjectDiff odiff = GetChildDiff (oldChild, newChild);
						if (odiff != null) {
							ChildDiff cdiff = new ChildDiff (); 
							cdiff.Id = cid;
							cdiff.Operation = DiffOperation.Update;
							cdiff.Diff = odiff;
							changes.Add (cdiff);
						}
						foundChildren [cid] = cid;
					} else {
						ChildDiff cdiff = new ChildDiff ();
						cdiff.Id = cid;
						cdiff.Operation = DiffOperation.Remove;
						changes.Add (cdiff);
					}
				} else {
					throw new InvalidOperationException ("Found an element of type '" + oldChild.GetType () + "' without ID");
				}
			}
			
			// Register new elements
			
			string lastWidgetId = null;
			foreach (object newChildElem in newStatusAdaptor.GetChildren (newStatus)) {
				string cid = newStatusAdaptor.GetUndoId (newChildElem);
				if (cid.Length > 0) {
					if (!foundChildren.ContainsKey (cid)) {
						ChildDiff cdiff = new ChildDiff ();
						cdiff.Id = cid;
						cdiff.Operation = DiffOperation.Add;
						cdiff.AddContent = newStatusAdaptor.SerializeChild (newChildElem);
						cdiff.InsertAfter = lastWidgetId;
						changes.Add (cdiff);
					}
				} else
					throw new InvalidOperationException ("Found an element of type '" + newChildElem.GetType () + "' without ID");

				lastWidgetId = cid;
			}
			
			ChildDiff[] childChanges = null;
			if (changes.Count > 0)
				childChanges = (ChildDiff[]) changes.ToArray (typeof(ChildDiff));

			if (childChanges != null || propChanges != null || signalChanges != null) {
				ObjectDiff dif = new ObjectDiff ();
				dif.PropertyChanges = propChanges;
				dif.SignalChanges = signalChanges;
				dif.ChildChanges = childChanges;
				return dif;
			}
			else
				return null;
		}
		
		public void ApplyDiff (object status, ObjectDiff diff)
		{
			if (diff.PropertyChanges != null)
				ApplyPropertyChanges (diff.PropertyChanges, currentStatusAdaptor, status);
			
			if (diff.SignalChanges != null)
				ApplySignalChanges (diff.SignalChanges, currentStatusAdaptor, status);
			
			if (diff.ChildChanges != null) {
				foreach (ChildDiff cdiff in diff.ChildChanges) {
					if (cdiff.Operation == DiffOperation.Update) {
						object statusChild = currentStatusAdaptor.FindChild (status, cdiff.Id);
						ApplyChildDiff (statusChild, cdiff.Diff);
					} else if (cdiff.Operation == DiffOperation.Remove) {
						// Remove the child
						currentStatusAdaptor.RemoveChild (status, cdiff.Id);
					} else {
						// Add the child at the correct position
						currentStatusAdaptor.AddChild (status, cdiff.AddContent, cdiff.InsertAfter);
					}
				}
			}
		}
		
		protected virtual ObjectDiff GetChildDiff (object oldChild, object newChild)
		{
			DiffGenerator childGenerator = new DiffGenerator ();
			childGenerator.CurrentStatusAdaptor = currentStatusAdaptor.GetChildAdaptor (oldChild);
			childGenerator.NewStatusAdaptor = newStatusAdaptor.GetChildAdaptor (newChild);
			
			return childGenerator.GetDiff (oldChild, newChild);
		}
		
		protected virtual void ApplyChildDiff (object child, ObjectDiff cdiff)
		{
			DiffGenerator childGenerator = new DiffGenerator ();
			childGenerator.CurrentStatusAdaptor = currentStatusAdaptor.GetChildAdaptor (child);
			childGenerator.ApplyDiff (child, cdiff);
		}
		
		protected virtual PropertyDiff[] GetPropertyDiff (IDiffAdaptor currentAdaptor, object currentObject, IDiffAdaptor newAdaptor, object newObject)
		{
			ArrayList changes = new ArrayList ();
			Hashtable found = new Hashtable ();
			
			// Look for modified and deleted elements
			if (currentObject != null) {
				foreach (object oldProp in currentAdaptor.GetProperties (currentObject)) {
					string name = currentAdaptor.GetPropertyName (oldProp);
					object newProp = newObject != null ? newAdaptor.GetPropertyByName (newObject, name) : null;
					if (newProp == null)
						changes.Add (new PropertyDiff (DiffOperation.Remove, name, null));
					else {
						found [name] = found;
						string newValue = newAdaptor.GetPropertyValue (newObject, newProp);
						if (newValue != currentAdaptor.GetPropertyValue (currentObject, oldProp))
							changes.Add (new PropertyDiff (DiffOperation.Update, name, newValue));
					}
				}
			}
			
			// Look for new elements
			if (newObject != null) {
				foreach (object newProp in newAdaptor.GetProperties (newObject)) {
					string name = newAdaptor.GetPropertyName (newProp);
					if (!found.ContainsKey (name))
						changes.Add (new PropertyDiff (DiffOperation.Add, name, newAdaptor.GetPropertyValue (newObject, newProp)));
				}
			}
			
			if (changes.Count == 0)
				return null;
			return (PropertyDiff[]) changes.ToArray (typeof(PropertyDiff));
		}
		
		protected virtual PropertyDiff[] GetSignalDiff (IDiffAdaptor currentAdaptor, object currentObject, IDiffAdaptor newAdaptor, object newObject)
		{
			ArrayList changes = new ArrayList ();
			Hashtable found = new Hashtable ();
			
			// Look for modified and deleted elements
			if (currentObject != null) {
				foreach (object oldProp in currentAdaptor.GetSignals (currentObject)) {
					string name;
					string handler;
					currentAdaptor.GetSignalInfo (oldProp, out name, out handler);
					object newProp = newObject != null ? newAdaptor.GetSignal (newObject, name, handler) : null;
					if (newProp == null)
						changes.Add (new PropertyDiff (DiffOperation.Remove, name, handler));
					found [name + " " + handler] = found;
				}
			}
			
			// Look for new elements
			if (newObject != null) {
				foreach (object newProp in newAdaptor.GetSignals (newObject)) {
					string name;
					string handler;
					newAdaptor.GetSignalInfo (newProp, out name, out handler);
					if (!found.ContainsKey (name + " " + handler))
						changes.Add (new PropertyDiff (DiffOperation.Add, name, handler));
				}
			}
			
			if (changes.Count == 0)
				return null;
			return (PropertyDiff[]) changes.ToArray (typeof(PropertyDiff));
		}
		
		public virtual void ApplyPropertyChanges (PropertyDiff[] changes, IDiffAdaptor adaptor, object obj)
		{
			foreach (PropertyDiff pdif in changes) {
				if (pdif.Operation == DiffOperation.Add || pdif.Operation == DiffOperation.Update)
					adaptor.SetPropertyValue (obj, pdif.Name, pdif.Text);
				else
					adaptor.ResetPropertyValue (obj, pdif.Name);
			}
		}
		
		public virtual void ApplySignalChanges (PropertyDiff[] changes, IDiffAdaptor adaptor, object obj)
		{
			foreach (PropertyDiff pdif in changes) {
				if (pdif.Operation == DiffOperation.Add)
					adaptor.AddSignal (obj, pdif.Name, pdif.Text);
				else
					adaptor.RemoveSignal (obj, pdif.Name, pdif.Text);
			}
		}
	}
	

	class PropertyDiff
	{
		public PropertyDiff (DiffOperation Operation, string Name, string Text)
		{
			this.Operation = Operation;
			this.Name = Name;
			this.Text = Text;
		}
		
		public DiffOperation Operation;
		public string Name;
		public string Text;
	}
	
	enum DiffOperation
	{
		Add,
		Remove,
		Update
	}
	
	class ChildDiff
	{
		public string Id;
		public DiffOperation Operation;
		public XmlElement AddContent;
		public string InsertAfter;
		public ObjectDiff Diff;
		
		public string ToString (int indent)
		{
			string ind = new string (' ', indent);
			string s = ind + Operation + " id:" + Id + "\n";
			if (Operation == DiffOperation.Update)
				s += Diff.ToString (indent + 2) + "\n";
			if (Operation == DiffOperation.Add) {
				s += ind + "  InsertAfter: " + InsertAfter + "\n";
				s += ind + "  Content: " + AddContent.OuterXml + "\n";
			}
			return s;
		}
	}
	
	class ObjectDiff
	{
		public PropertyDiff[] PropertyChanges;
		public PropertyDiff[] SignalChanges;
		public ChildDiff[] ChildChanges;
		
		public override string ToString ()
		{
			return ToString (0);
		}
		
		internal string ToString (int indent)
		{
			string ind = new string (' ', indent);
			string s = ind + "ObjectDiff:\n";
			
			if (PropertyChanges != null) {
				s += ind + "  Properties:\n";
				foreach (PropertyDiff d in PropertyChanges) {
					s += ind + "    " + d.Operation + ": " + d.Name;
					if (d.Operation != DiffOperation.Remove)
						s += " = " + d.Text;
					s += "\n";
				}
			}

			if (SignalChanges != null) {
				s += ind + "  Signals:\n";
				foreach (PropertyDiff d in SignalChanges)
					s += ind + "    " + d.Operation + ": " + d.Name + " - " + d.Text + "\n";
			}

			if (ChildChanges != null) {
				s += ind + "  Children:\n";
				foreach (ChildDiff cd in ChildChanges)
					s += cd.ToString (indent + 4);
			}
			
			return s;
		}
	}
}
