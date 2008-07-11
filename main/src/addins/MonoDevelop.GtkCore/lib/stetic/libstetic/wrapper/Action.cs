
using System;
using System.Text;
using System.Xml;
using System.CodeDom;
using System.Collections;
using Stetic.Undo;

namespace Stetic.Wrapper
{
	public sealed class Action: Stetic.Wrapper.Object, IRadioGroupManagerProvider
	{
		ActionType type;
		bool drawAsRadio;
		int radioValue;
		bool active;
		string name;
		string accelerator;
		ActionGroup group;
		
		string oldDefaultName;
		string nameRoot;

		static RadioActionGroupManager GroupManager = new RadioActionGroupManager ();
		
		public enum ActionType {
			Action,
			Toggle,
			Radio
		}
		
		public event EventHandler Activated;
		public event EventHandler Toggled;
		public event Gtk.ChangedHandler Changed;
		public event EventHandler Deleted;

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
		}
		
		public Gtk.Action GtkAction {
			get { return (Gtk.Action) Wrapped; }
		}
		
		public string Name {
			get {
				if (name == null || name.Length == 0) {
					name = nameRoot = oldDefaultName = GetDefaultName ();
					if (group != null)
						name = group.GetValidName (this, name);
				}
				return name;
			}
			set {
				name = nameRoot = value;
				if (group != null)
					name = group.GetValidName (this, name);
				EmitNotify ("Name");
			}
		}
		
		public string Label {
			get { return GtkAction.Label; }
			set { GtkAction.Label = value; EmitNotify ("Label"); }
		}
		
		public string StockId {
			get { return GtkAction.StockId; }
			set { GtkAction.StockId = value; EmitNotify ("StockId"); }
		}
		
		public override string WrappedTypeName {
			get { 
				if (type == ActionType.Action)
					return "Gtk.Action";
				else if (type == ActionType.Toggle)
					return "Gtk.ToggleAction";
				else
					return "Gtk.RadioAction";
			}
		}
		
		IRadioGroupManager IRadioGroupManagerProvider.GetGroupManager ()
		{
			return GroupManager;
		}

		internal void UpdateNameIndex ()
		{
			// Adds a number to the action name if the current name already
			// exists in the action group.
			
			string vname = group.GetValidName (this, Name);
			if (vname != Name) {
				name = vname;
				EmitNotify ("Name");
			}
		}

		string GetDefaultName ()
		{
			if (GtkAction.Label != null && GtkAction.Label.Length > 0)
				return GetIdentifier (GtkAction.Label);

			if (GtkAction.StockId != null) {
				string s = GtkAction.StockId.Replace ("gtk-", "");
				return GetIdentifier (s.Replace ("gnome-stock-", ""));
			}
			return "Action";
		}
		
		public ActionType Type {
			get { return type; }
			set {
				if (type == value)
					return;
				type = value;
				if (type == ActionType.Radio)
					Group = GroupManager.LastGroup;
				else
					Group = null;

				EmitNotify ("Type");
			}
		}
		
		public bool DrawAsRadio {
			get { return drawAsRadio; }
			set { drawAsRadio = value; EmitNotify ("DrawAsRadio"); }
		}
		
		public int Value {
			get { return radioValue; }
			set { radioValue = value; EmitNotify ("Value"); }
		}
		
		public string Accelerator {
			get { return accelerator; }
			set { accelerator = value; EmitNotify ("Accelerator"); }
		}
		
		public bool Active {
			get { return active; }
			set { 
				active = value;
				if (Activated != null)
					Activated (this, EventArgs.Empty);
				if (Toggled != null)
					Toggled (this, EventArgs.Empty);
				if (Changed != null)
					Changed (this, new Gtk.ChangedArgs ());
			}
		}
		
		public string MenuLabel {
			get {
				if (GtkAction.Label != null && GtkAction.Label.Length > 0)
					return GtkAction.Label;

				if (GtkAction.StockId == null)
					return "";

				Gtk.StockItem item = Gtk.Stock.Lookup (GtkAction.StockId);
				if (item.Label != null)
					return item.Label;

				return "";
			}
		}
		
		public string ToolLabel {
			get {
				if (GtkAction.ShortLabel != null && GtkAction.ShortLabel.Length > 0)
					return GtkAction.ShortLabel;
				else
					return MenuLabel;
			}
		}
		
		public ActionGroup ActionGroup {
			get { return group; }
		}

		public string Group {
			get {
				return GroupManager.GetGroup (this);
			}
			set {
				if (value != null && value.Length > 0)
					Type = ActionType.Radio;
				GroupManager.SetGroup (this, value);
				EmitNotify ("Group");
			}
		}
		
		public void Delete ()
		{
			if (group != null)
				group.Actions.Remove (this);
			if (Deleted != null)
				Deleted (this, EventArgs.Empty);
			Dispose ();
		}
		
		protected override void EmitNotify (string propertyName)
		{
			if (propertyName == "Label" || propertyName == "StockId") {
				// If the current name is a name generated from label or stockid,
				// we update here the name again
				if (nameRoot == oldDefaultName)
					Name = GetDefaultName ();
				oldDefaultName = GetDefaultName ();
			}
			base.EmitNotify (propertyName);
		}
		
		public override XmlElement Write (ObjectWriter writer)
		{
			XmlElement elem = writer.XmlDocument.CreateElement ("action");
			elem.SetAttribute ("id", Name);
			WidgetUtils.GetProps (this, elem);
			WidgetUtils.GetSignals (this, elem);
			if (writer.CreateUndoInfo)
				elem.SetAttribute ("undoId", UndoId);
			return elem;
		}
		
		public override void Read (ObjectReader reader, XmlElement elem)
		{
			Gtk.Action ac = new Gtk.Action ("", "");
			
			ClassDescriptor klass = Registry.LookupClassByName ("Gtk.Action");
			ObjectWrapper.Bind (reader.Project, klass, this, ac, true);
			
			WidgetUtils.ReadMembers (klass, this, ac, elem);
			name = nameRoot = oldDefaultName = elem.GetAttribute ("id");
			
			string uid = elem.GetAttribute ("undoId");
			if (uid.Length > 0)
				UndoId = uid;
		}
		
		public Action Clone ()
		{
			Action a = (Action) ObjectWrapper.Create (Project, new Gtk.Action ("", ""));
			a.CopyFrom (this);
			return a;
		}
		
		public void CopyFrom (Action action)
		{
			type = action.type;
			drawAsRadio = action.drawAsRadio;
			radioValue = action.radioValue;
			active = action.active;
			name = action.name;
			GtkAction.HideIfEmpty = action.GtkAction.HideIfEmpty;
			GtkAction.IsImportant = action.GtkAction.IsImportant;
			GtkAction.Label = action.GtkAction.Label;
			GtkAction.Sensitive = action.GtkAction.Sensitive;
			GtkAction.ShortLabel = action.GtkAction.ShortLabel;
			GtkAction.StockId = action.GtkAction.StockId;
			GtkAction.Tooltip = action.GtkAction.Tooltip;
			GtkAction.Visible = action.GtkAction.Visible;
			GtkAction.VisibleHorizontal = action.GtkAction.VisibleHorizontal;
			GtkAction.VisibleVertical = action.GtkAction.VisibleVertical;
			
			Signals.Clear ();
			foreach (Signal s in action.Signals)
				Signals.Add (new Signal (s.SignalDescriptor, s.Handler, s.After));
			
			NotifyChanged ();
		}
		
		public Gtk.Widget CreateIcon (Gtk.IconSize size)
		{
			if (GtkAction.StockId == null)
				return null;

			Gdk.Pixbuf px = Project.IconFactory.RenderIcon (Project, GtkAction.StockId, size);
			if (px != null)
				return new Gtk.Image (px);
			else
				return GtkAction.CreateIcon (size);
		}
		
		public Gdk.Pixbuf RenderIcon (Gtk.IconSize size)
		{
			if (GtkAction.StockId == null)
				return null;

			Gdk.Pixbuf px = Project.IconFactory.RenderIcon (Project, GtkAction.StockId, size);
			if (px != null)
				return px;

			Gtk.IconSet iset = Gtk.IconFactory.LookupDefault (GtkAction.StockId);
			if (iset == null)
				return WidgetUtils.MissingIcon;
			else
				return iset.RenderIcon (new Gtk.Style (), Gtk.TextDirection.Ltr, Gtk.StateType.Normal, size, null, "");
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			CodeObjectCreateExpression exp = new CodeObjectCreateExpression ();
			
			PropertyDescriptor prop = (PropertyDescriptor) ClassDescriptor ["Name"];
			exp.Parameters.Add (ctx.GenerateValue (prop.GetValue (Wrapped), prop.RuntimePropertyType));
			
			prop = (PropertyDescriptor) ClassDescriptor ["Label"];
			string lab = (string) prop.GetValue (Wrapped);
			if (lab == "") lab = null;
			exp.Parameters.Add (ctx.GenerateValue (lab, prop.RuntimePropertyType, prop.Translatable));
			
			prop = (PropertyDescriptor) ClassDescriptor ["Tooltip"];
			exp.Parameters.Add (ctx.GenerateValue (prop.GetValue (Wrapped), prop.RuntimePropertyType, prop.Translatable));
			
			prop = (PropertyDescriptor) ClassDescriptor ["StockId"];
			exp.Parameters.Add (ctx.GenerateValue (prop.GetValue (Wrapped), prop.RuntimePropertyType, prop.Translatable));
			
			if (type == ActionType.Action)
				exp.CreateType = new CodeTypeReference ("Gtk.Action");
			else if (type == ActionType.Toggle)
				exp.CreateType = new CodeTypeReference ("Gtk.ToggleAction");
			else {
				exp.CreateType = new CodeTypeReference ("Gtk.RadioAction");
				prop = (PropertyDescriptor) ClassDescriptor ["Value"];
				exp.Parameters.Add (ctx.GenerateValue (prop.GetValue (Wrapped), typeof(int)));
			}
			return exp;
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (Type == ActionType.Radio) {
				CodeExpression groupExp = GroupManager.GenerateGroupExpression (ctx, this);
				ctx.Statements.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (var, "Group"),
						groupExp)
				);
			}
			else if (type == ActionType.Toggle) {
				if (Active) {
					ctx.Statements.Add (
						new CodeAssignStatement (
							new CodePropertyReferenceExpression (var, "Active"),
							new CodePrimitiveExpression (true))
					);
				}
				if (DrawAsRadio) {
					ctx.Statements.Add (
						new CodeAssignStatement (
							new CodePropertyReferenceExpression (var, "DrawAsRadio"),
							new CodePrimitiveExpression (true))
					);
				}
			}
			base.GenerateBuildCode (ctx, var);
		}
		
		internal void SetActionGroup (ActionGroup g)
		{
			group = g;
		}
		
		string GetIdentifier (string name)
		{
			StringBuilder sb = new StringBuilder ();
			
			bool wstart = false;
			foreach (char c in name) {
				if (c == '_' || (int)c > 127)	// No underline, no unicode
					continue;
				if (c == '-' || c == ' ' || !char.IsLetterOrDigit (c)) {
					wstart = true;
					continue;
				}
				if (wstart) {
					sb.Append (char.ToUpper (c));
					wstart = false;
				} else
					sb.Append (c);
			}
			return sb.ToString () + "Action";
		}
		
		internal override UndoManager GetUndoManagerInternal ()
		{
			if (group != null)
				return group.GetUndoManagerInternal ();
			else
				return base.GetUndoManagerInternal ();
		}
		
		DiffGenerator GetDiffGenerator ()
		{
			DiffGenerator gen = new DiffGenerator ();
			gen.CurrentStatusAdaptor = new ActionDiffAdaptor (Project);
			gen.NewStatusAdaptor = new XmlDiffAdaptor ();
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
	}
	
	[Serializable]
	public class ActionCollection: CollectionBase
	{
		[NonSerialized]
		ActionGroup group;
		
		public ActionCollection ()
		{
		}
		
		internal ActionCollection (ActionGroup group)
		{
			this.group = group;
		}
		
		public void Add (Action action)
		{
			List.Add (action);
		}
		
		public void Insert (int i, Action action)
		{
			List.Insert (i, action);
		}
		
		public Action this [int n] {
			get { return (Action) List [n]; }
		}
		
		public void Remove (Action action)
		{
			List.Remove (action);
		}
		
		public bool Contains (Action action)
		{
			return List.Contains (action);
		}
		
		public void CopyTo (Action[] array, int index)
		{
			List.CopyTo (array, index);
		}

		protected override void OnInsertComplete (int index, object val)
		{
			if (group != null)
				group.NotifyActionAdded ((Action) val);
		}
		
		protected override void OnRemoveComplete (int index, object val)
		{
			if (group != null)
				group.NotifyActionRemoved ((Action)val);
		}
		
		protected override void OnSetComplete (int index, object oldv, object newv)
		{
			if (group != null) {
				group.NotifyActionRemoved ((Action) oldv);
				group.NotifyActionAdded ((Action) newv);
			}
		}
	}
}
