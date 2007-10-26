
using System;
using System.CodeDom;
using System.Xml;
using System.Collections;
using Stetic.Undo;

namespace Stetic.Wrapper
{
	public sealed class ActionGroup: ObjectWrapper
	{
		string name;
		ActionCollection actions;
		ObjectWrapper owner;
		bool generatePublic = true;
		
		public event ActionEventHandler ActionAdded;
		public event ActionEventHandler ActionRemoved;
		public event ActionEventHandler ActionChanged;
		
		public ActionGroup ()
		{
			actions = new ActionCollection (this);
		}
		
		public ActionGroup (string name): this ()
		{
			this.name = name;
		}
		
		public override void Dispose ()
		{
			foreach (Action a in actions)
				a.Dispose ();
			base.Dispose ();
		}
		
		public ActionCollection Actions {
			get { return actions; }
		}
		
		public string Name {
			get { return name; }
			set { 
				name = value;
				NotifyChanged ();
			}
		}
		
		public bool GeneratePublic {
			get { return generatePublic; }
			set { generatePublic = value; }
		}
		
		public Action GetAction (string name)
		{
			foreach (Action ac in actions)
				if (ac.Name == name)
					return ac;
			return null;
		}
		
		internal string GetValidName (Action reqAction, string name)
		{
			int max = 0;
			bool found = false;
			foreach (Action ac in Actions) {
				if (ac == reqAction)
					continue;
					
				string bname;
				int index;
				WidgetUtils.ParseWidgetName (ac.Name, out bname, out index);
				
				if (name == ac.Name)
					found = true;
				if (name == bname && index > max)
					max = index;
			}
			if (found)
				return name + (max+1);
			else
				return name;
		}
		
		public override XmlElement Write (ObjectWriter writer)
		{
			XmlElement group = writer.XmlDocument.CreateElement ("action-group");
			group.SetAttribute ("name", name);
			if (writer.CreateUndoInfo)
				group.SetAttribute ("undoId", UndoId);
			foreach (Action ac in actions) {
				if (ac.Name.Length > 0)
					group.AppendChild (writer.WriteObject (ac));
			}
			return group;
		}
		
		public override void Read (ObjectReader reader, XmlElement elem)
		{
			name = elem.GetAttribute ("name");
			string uid = elem.GetAttribute ("undoId");
			if (uid.Length > 0)
				UndoId = uid;
			foreach (XmlElement child in elem.SelectNodes ("action")) {
				Action ac = new Action ();
				ac.Read (reader, child);
				actions.Add (ac);
			}
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			return new CodeObjectCreateExpression (
				typeof(Gtk.ActionGroup),
				new CodePrimitiveExpression (Name)
			);
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			foreach (Action action in Actions) {
				// Create the action
				CodeExpression acVarExp = ctx.GenerateInstanceExpression (action, action.GenerateObjectCreation (ctx));
				ctx.GenerateBuildCode (action, acVarExp);
				ctx.Statements.Add (
					new CodeMethodInvokeExpression (
						var,
						"Add",
						acVarExp,
						new CodePrimitiveExpression (action.Accelerator)
					)
				);
			}
		}
		
		internal void SetOwner (ObjectWrapper owner)
		{
			this.owner = owner;
		}
		
		internal override UndoManager GetUndoManagerInternal ()
		{
			if (owner != null)
				return owner.UndoManager;
			else
				return base.GetUndoManagerInternal ();
		}
		
		public override ObjectWrapper FindObjectByUndoId (string id)
		{
			ObjectWrapper ow = base.FindObjectByUndoId (id);
			if (ow != null) return ow;
			
			foreach (Action ac in Actions) {
				ow = ac.FindObjectByUndoId (id);
				if (ow != null)
					return ow;
			}
			return null;
		}
		
		DiffGenerator GetDiffGenerator ()
		{
			DiffGenerator gen = new DiffGenerator ();
			gen.CurrentStatusAdaptor = new ActionDiffAdaptor (Project);
			XmlDiffAdaptor xad = new XmlDiffAdaptor ();
			xad.ChildElementName = "action";
			gen.NewStatusAdaptor = xad;
			return gen;
		}
		
		public override object GetUndoDiff ()
		{
			XmlElement oldElem = UndoManager.GetObjectStatus (this);
			UndoWriter writer = new UndoWriter (oldElem.OwnerDocument, UndoManager);
			
			XmlElement newElem = Write (writer);
			ObjectDiff actionsDiff = GetDiffGenerator().GetDiff (this, oldElem);
			UndoManager.UpdateObjectStatus (this, newElem);
			return actionsDiff;
		}
		
		public override object ApplyUndoRedoDiff (object diff)
		{
			ObjectDiff actionsDiff = (ObjectDiff) diff;
			
			XmlElement status = UndoManager.GetObjectStatus (this);
			
			DiffGenerator differ = GetDiffGenerator();
			differ.ApplyDiff (this, actionsDiff);
			actionsDiff = differ.GetDiff (this, status);
			
			UndoWriter writer = new UndoWriter (status.OwnerDocument, UndoManager);
			XmlElement newElem = Write (writer);
			UndoManager.UpdateObjectStatus (this, newElem);
			
			return actionsDiff;
		}
		
		internal void NotifyActionAdded (Action ac)
		{
			ac.SetActionGroup (this);
			ac.ObjectChanged += OnActionChanged;
			ac.SignalAdded += OnSignalAdded;
			ac.SignalRemoved += OnSignalRemoved;
			ac.SignalChanged += OnSignalChanged;
			
			ac.UpdateNameIndex ();
			
			NotifyChanged ();
			
			if (ActionAdded != null)
				ActionAdded (this, new ActionEventArgs (ac));
		}
		
		internal void NotifyActionRemoved (Action ac)
		{
			ac.SetActionGroup (null);
			ac.ObjectChanged -= OnActionChanged;
			ac.SignalAdded -= OnSignalAdded;
			ac.SignalRemoved -= OnSignalRemoved;
			ac.SignalChanged -= OnSignalChanged;

			NotifyChanged ();
			
			if (ActionRemoved != null)
				ActionRemoved (this, new ActionEventArgs (ac));
		}
		
		void OnActionChanged (object s, ObjectWrapperEventArgs args)
		{
			NotifyChanged ();
			if (ActionChanged != null)
				ActionChanged (this, new ActionEventArgs ((Action) args.Wrapper));
		}
		
		void OnSignalAdded (object s, SignalEventArgs args)
		{
			OnSignalAdded (args);
		}
		
		void OnSignalRemoved (object s, SignalEventArgs args)
		{
			OnSignalRemoved (args);
		}
		
		void OnSignalChanged (object s, SignalChangedEventArgs args)
		{
			OnSignalChanged (args);
		}
	}
	
	public class ActionGroupCollection: CollectionBase
	{
		ActionGroup[] toClear;
		ObjectWrapper owner;
		
		internal void SetOwner (ObjectWrapper owner)
		{
			this.owner = owner;
		}
		
		public void Add (ActionGroup group)
		{
			List.Add (group);
		}
		
		public void Insert (int n, ActionGroup group)
		{
			List.Insert (n, group);
		}
		
		public ActionGroup this [int n] {
			get { return (ActionGroup) List [n]; }
		}
		
		public ActionGroup this [string name] {
			get {
				foreach (ActionGroup grp in List)
					if (grp.Name == name)
						return grp;
				return null;
			}
		}
		
		internal ObjectWrapper FindObjectByUndoId (string id)
		{
			foreach (ActionGroup ag in List) {
				ObjectWrapper ow = ag.FindObjectByUndoId (id);
				if (ow != null)
					return ow;
			}
			return null;
		}
		
		DiffGenerator GetDiffGenerator (IProject prj)
		{
			DiffGenerator gen = new DiffGenerator ();
			gen.CurrentStatusAdaptor = new ActionDiffAdaptor (prj);
			XmlDiffAdaptor xad = new XmlDiffAdaptor ();
			xad.ChildElementName = "action-group";
			xad.ProcessProperties = false;
			xad.ChildAdaptor = new XmlDiffAdaptor ();
			xad.ChildAdaptor.ChildElementName = "action";
			gen.NewStatusAdaptor = xad;
			return gen;
		}
		
		internal ObjectDiff GetDiff (IProject prj, XmlElement elem)
		{
			return GetDiffGenerator (prj).GetDiff (this, elem);
		}
		
		internal void ApplyDiff (IProject prj, ObjectDiff diff)
		{
			GetDiffGenerator (prj).ApplyDiff (this, diff);
		}
		
		public int IndexOf (ActionGroup group)
		{
			return List.IndexOf (group);
		}
		
		public void Remove (ActionGroup group)
		{
			List.Remove (group);
		}

		protected override void OnInsertComplete (int index, object val)
		{
			NotifyGroupAdded ((ActionGroup) val);
		}
		
		protected override void OnRemoveComplete (int index, object val)
		{
			NotifyGroupRemoved ((ActionGroup)val);
		}
		
		protected override void OnSetComplete (int index, object oldv, object newv)
		{
			NotifyGroupRemoved ((ActionGroup) oldv);
			NotifyGroupAdded ((ActionGroup) newv);
		}
		
		protected override void OnClear ()
		{
			toClear = new ActionGroup [Count];
			List.CopyTo (toClear, 0);
		}
		
		protected override void OnClearComplete ()
		{
			foreach (ActionGroup a in toClear)
				NotifyGroupRemoved (a);
			toClear = null;
		}
		
		void NotifyGroupAdded (ActionGroup grp)
		{
			grp.SetOwner (owner);
			grp.ObjectChanged += OnGroupChanged;
			if (ActionGroupAdded != null)
				ActionGroupAdded (this, new ActionGroupEventArgs (grp));
		}
		
		void NotifyGroupRemoved (ActionGroup grp)
		{
			grp.SetOwner (null);
			grp.ObjectChanged -= OnGroupChanged;
			if (ActionGroupRemoved != null)
				ActionGroupRemoved (this, new ActionGroupEventArgs (grp));
		}
		
		void OnGroupChanged (object s, ObjectWrapperEventArgs a)
		{
			if (ActionGroupChanged != null)
				ActionGroupChanged (this, new ActionGroupEventArgs ((ActionGroup)s));
		}
		
		public ActionGroup[] ToArray ()
		{
			ActionGroup[] groups = new ActionGroup [Count];
			List.CopyTo (groups, 0);
			return groups;
		}
		
		public event ActionGroupEventHandler ActionGroupAdded;
		public event ActionGroupEventHandler ActionGroupRemoved;
		public event ActionGroupEventHandler ActionGroupChanged;
	}
	
	
	public delegate void ActionEventHandler (object sender, ActionEventArgs args);
	
	public class ActionEventArgs: EventArgs
	{
		readonly Action action;
		
		public ActionEventArgs (Action ac)
		{
			action = ac;
		}
		
		public Action Action {
			get { return action; }
		}
	}
	
	public delegate void ActionGroupEventHandler (object sender, ActionGroupEventArgs args);
	
	public class ActionGroupEventArgs: EventArgs
	{
		readonly ActionGroup action;
		
		public ActionGroupEventArgs (ActionGroup ac)
		{
			action = ac;
		}
		
		public ActionGroup ActionGroup {
			get { return action; }
		}
	}
}
