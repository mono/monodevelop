
using System;
using System.Text;
using System.Xml;
using System.CodeDom;
using System.Collections;

namespace Stetic.Wrapper
{
	
	public class ActionTree: ActionTreeNode
	{
		public event EventHandler Changed;
		
		public ActionTree()
		{
		}
		
		public void GenerateBuildCode (GeneratorContext ctx, CodeFieldReferenceExpression uiManager)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<ui>");
			GenerateUiString (sb);
			sb.Append ("</ui>");
			
			CodeMethodInvokeExpression exp = new CodeMethodInvokeExpression (
				uiManager,
				"AddUiFromString",
				new CodePrimitiveExpression (sb.ToString ())
			);
			ctx.Statements.Add (exp);
		}
		
		public ActionGroup[] GetRequiredGroups ()
		{
			ArrayList list = new ArrayList ();
			GetRequiredGroups (list);
			return (ActionGroup[]) list.ToArray (typeof(ActionGroup));
		}
		
		internal override void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
	}
	
	public class ActionTreeNode: IDisposable
	{
		Gtk.UIManagerItemType type;
		string name;
		Action action;
		ActionTreeNodeCollection children;
		ActionTreeNode parentNode;
		bool loading;
		string lastActionName;
		
		public ActionTreeNode ()
		{
		}
		
		public ActionTreeNode Clone ()
		{
			return new ActionTreeNode (type, name, action);
		}
		
		public ActionTreeNode (Gtk.UIManagerItemType type, string name, Action action)
		{
			this.type = type;
			this.name = name;
			this.action = action;
			if (this.action != null) {
				lastActionName = this.action.Name;
				this.action.Deleted += OnActionDeleted;
				this.action.ObjectChanged += OnActionChanged;
			}
		}
		
		public virtual void Dispose ()
		{
			if (action != null) {
				action.Deleted -= OnActionDeleted;
				action.ObjectChanged -= OnActionChanged;
			}
			if (children != null)
				foreach (ActionTreeNode node in children)
					node.Dispose ();
		}
		
		void OnActionDeleted (object s, EventArgs args)
		{
			if (parentNode != null)
				parentNode.Children.Remove (this);
		}
		
		void OnActionChanged (object s, ObjectWrapperEventArgs args)
		{
			// If the name of the action changes, consider it a change in
			// the node, since the generated xml will be different

			Action ac = (Action) args.Wrapper;
			if (ac.Name != lastActionName) {
				lastActionName = ac.Name;
				NotifyChanged ();
			}
		}

		internal virtual void NotifyChanged ()
		{
			if (parentNode != null)
				parentNode.NotifyChanged ();
		}
		
		public XmlElement Write (XmlDocument doc, FileFormat format)
		{
			XmlElement elem = doc.CreateElement ("node");
			if (name != null && name.Length > 0)
				elem.SetAttribute ("name", name);
			elem.SetAttribute ("type", type.ToString ());
			if (action != null)
				elem.SetAttribute ("action", action.Name);
			
			if (children != null) {
				foreach (ActionTreeNode child in children) {
					if (child.Action != null && child.Action.Name.Length == 0)
						continue;
					elem.AppendChild (child.Write (doc, format));
				}
			}
			return elem;
		}
		
		public void Read (Wrapper.Widget baseWidget, XmlElement elem)
		{
			name = elem.GetAttribute ("name");
			if (elem.HasAttribute ("type"))
				type = (Gtk.UIManagerItemType) Enum.Parse (typeof(Gtk.UIManagerItemType), elem.GetAttribute ("type"));
			
			// The name of an action may be empty in some situations (e.g. when adding a new action but before entering the name)
			XmlAttribute actionAt = elem.Attributes ["action"];
			if (actionAt != null) {
				string aname = actionAt.Value;
				foreach (ActionGroup grp in baseWidget.LocalActionGroups) {
					action = grp.GetAction (aname);
					if (action != null)
						break;
				}
				if (action == null) {
					foreach (ActionGroup group in baseWidget.Project.ActionGroups) {
						action = group.GetAction (aname);
						if (action != null)
							break;
					}
				}
				if (action != null) {
					lastActionName = action.Name;
					action.Deleted += OnActionDeleted;
					action.ObjectChanged += OnActionChanged;
				}
			}
			
			try {
				loading = true;
				foreach (XmlElement child in elem.SelectNodes ("node")) {
					ActionTreeNode node = new ActionTreeNode ();
					node.Read (baseWidget, child);
					Children.Add (node);
				}
			} finally {
				loading = false;
			}
		}
		
		public virtual void GenerateBuildCode (GeneratorContext ctx, CodeVariableReferenceExpression uiManager, string path)
		{
			CodeMethodInvokeExpression exp = new CodeMethodInvokeExpression (
				uiManager,
				"AddUi",
				new CodePrimitiveExpression (0),
				new CodePrimitiveExpression (path),
				new CodePrimitiveExpression (name),
				new CodePrimitiveExpression (action != null ? action.Name : null),
				new CodeFieldReferenceExpression (
					new CodeTypeReferenceExpression (new CodeTypeReference (typeof(Gtk.UIManagerItemType), CodeTypeReferenceOptions.GlobalReference)),
					type.ToString()
				),
				new CodePrimitiveExpression (false)
			);
			ctx.Statements.Add (exp);
			
			string localName = (name != null && name.Length > 0 ? name : (action != null ? action.Name : null));
			if (localName != null) {
				if (path != "/")
					path = path + "/" + localName;
				else
					path += localName;
			}
			
			foreach (ActionTreeNode node in Children)
				node.GenerateBuildCode (ctx, uiManager, path);
		}
		
		public void GenerateUiString (StringBuilder sb)
		{
			sb.Append ('<').Append (type.ToString().ToLower());

			string name = this.name;
			if (String.IsNullOrEmpty (name) && action != null)
				name = action.Name;

			if (!String.IsNullOrEmpty (name))
				sb.Append (" name='").Append (name).Append ("'");
			if (action != null)
				sb.Append (" action='").Append (action.Name).Append ("'");
				
			if (Children.Count > 0) {
				sb.Append ('>');
				foreach (ActionTreeNode node in Children)
					node.GenerateUiString (sb);
				sb.Append ("</").Append (type.ToString().ToLower()).Append ('>');
			} else
				sb.Append ("/>");
		}
		
		protected void GetRequiredGroups (ArrayList list)
		{
			if (action != null && action.ActionGroup != null && !list.Contains (action.ActionGroup))
				list.Add (action.ActionGroup);
			foreach (ActionTreeNode node in Children)
				node.GetRequiredGroups (list);
		}
		
		public Gtk.UIManagerItemType Type {
			get { return type; }
			set { type = value; NotifyChanged (); }
		}
		
		public string Name {
			get { return name; }
			set { name = value; NotifyChanged (); }
		}
		
		public Action Action {
			get { return action; }
		}
		
		public ActionTreeNode ParentNode {
			get { return parentNode; }
		}
		
		public ActionTreeNodeCollection Children {
			get {
				if (children == null)
					children = new ActionTreeNodeCollection (this);
				return children;
			}
		}
		
		internal void NotifyChildAdded (ActionTreeNode node)
		{
			node.parentNode = this;
			if (!loading) {
				NotifyChanged ();
				if (ChildNodeAdded != null)
					ChildNodeAdded (this, new ActionTreeNodeArgs (node));
			}
		}
		
		internal void NotifyChildRemoved (ActionTreeNode node)
		{
			node.parentNode = null;
			if (!loading) {
				NotifyChanged ();
				if (ChildNodeRemoved != null)
					ChildNodeRemoved (this, new ActionTreeNodeArgs (node));
			}
		}
		
		public event ActionTreeNodeHanlder ChildNodeAdded;
		public event ActionTreeNodeHanlder ChildNodeRemoved;
	}
	
	public class ActionTreeNodeCollection: CollectionBase
	{
		ActionTreeNode parent;
		
		public ActionTreeNodeCollection (ActionTreeNode parent)
		{
			this.parent = parent;
		}
		
		public void Add (ActionTreeNode node)
		{
			List.Add (node);
		}
		
		public void Insert (int index, ActionTreeNode node)
		{
			List.Insert (index, node);
		}
		
		public int IndexOf (ActionTreeNode node)
		{
			return List.IndexOf (node);
		}
		
		public void Remove (ActionTreeNode node)
		{
			if (List.Contains (node))
				List.Remove (node);
		}
		
		public ActionTreeNode this [int n] {
			get { return (ActionTreeNode) List [n]; }
			set { List [n] = value; }
		}

		protected override void OnInsertComplete (int index, object val)
		{
			parent.NotifyChildAdded ((ActionTreeNode) val);
		}
		
		protected override void OnRemoveComplete (int index, object val)
		{
			parent.NotifyChildRemoved ((ActionTreeNode)val);
		}
		
		protected override void OnSetComplete (int index, object oldv, object newv)
		{
			parent.NotifyChildRemoved ((ActionTreeNode) oldv);
			parent.NotifyChildAdded ((ActionTreeNode) newv);
		}
	}
	
	public delegate void ActionTreeNodeHanlder (object ob, ActionTreeNodeArgs args);
	
	public class ActionTreeNodeArgs: EventArgs
	{
		readonly ActionTreeNode node;
		
		public ActionTreeNodeArgs (ActionTreeNode node)
		{
			this.node = node;
		}
		
		public ActionTreeNode Node {
			get { return node; }
		}
	}
	
}
