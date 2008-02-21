using System;
using System.CodeDom;
using System.Collections;
using System.Xml;

namespace Stetic.Wrapper {

	public class Notebook : Container {

		ArrayList tabs = new ArrayList ();

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized && AllowPlaceholders) {
				if (notebook.Children.Length != 0) {
					// Remove the dummy page Container.Wrap added
					notebook.Remove (notebook.Children[0]);
				}
				InsertPage (0);
			}
			notebook.SwitchPage += OnPageChanged;
		}
		
		public override void Dispose ()
		{
			notebook.SwitchPage -= OnPageChanged;
			base.Dispose ();
		}

		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			if ((string)GladeUtils.GetChildProperty (child_elem, "type", "") == "tab") {
				ObjectWrapper wrapper = reader.ReadObject (child_elem["widget"]);
				Gtk.Widget widget = (Gtk.Widget)wrapper.Wrapped;
				notebook.SetTabLabel (notebook.GetNthPage (notebook.NPages - 1), widget);
				tabs.Add (widget);
				return wrapper;
			} else
				return base.ReadChild (reader, child_elem);
		}

		protected override XmlElement WriteChild (ObjectWriter writer, Widget wrapper)
		{
			XmlElement child_elem = base.WriteChild (writer, wrapper);
			if (tabs.Contains (wrapper.Wrapped))
				GladeUtils.SetChildProperty (child_elem, "type", "tab");
			return child_elem;
		}
		public override void Read (ObjectReader reader, XmlElement element)
		{
			object cp = GladeUtils.ExtractProperty (element, "CurrentPage", 0);
			base.Read (reader, element);
			notebook.CurrentPage = (int) cp;
		}

		protected override void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentExp, Widget wrapper)
		{
			Gtk.Widget child = (Gtk.Widget) wrapper.Wrapped;
			
			if (notebook.PageNum (child) == -1) {
				// It's a tab
				
				ctx.Statements.Add (new CodeCommentStatement ("Notebook tab"));
				Gtk.Widget page = null;
				CodeExpression pageVar;
				
				// Look for the page widget contained in this tab
				for (int n=0; n < notebook.NPages; n++) {
					if (notebook.GetTabLabel (notebook.GetNthPage (n)) == child) {
						page = notebook.GetNthPage (n);
						break;
					}
				}
				
				// If the page contains a placeholder, generate a dummy page
				if (page is Stetic.Placeholder) {
					CodeVariableDeclarationStatement dvar = new CodeVariableDeclarationStatement (
						"Gtk.Label",
						ctx.NewId (),
						new CodeObjectCreateExpression ("Gtk.Label")
					);
					ctx.Statements.Add (dvar);
					ctx.Statements.Add (
						new CodeAssignStatement (
							new CodePropertyReferenceExpression (
								new CodeVariableReferenceExpression (dvar.Name),
								"Visible"
							),
							new CodePrimitiveExpression (true)
						)
					);
					ctx.Statements.Add (
						new CodeMethodInvokeExpression (
							parentExp,
							"Add",
							new CodeVariableReferenceExpression (dvar.Name)
						)
					);
					pageVar = new CodeVariableReferenceExpression (dvar.Name);
				} else
					pageVar = ctx.WidgetMap.GetWidgetExp (page);
				
				// Generate code for the tab
				CodeExpression var = ctx.GenerateNewInstanceCode (wrapper);
				
				// Assign the tab to the page
				CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression (
					parentExp,
					"SetTabLabel",
					pageVar,
					var
				);
				ctx.Statements.Add (invoke);
				
				// Workaround for GTK bug. ShowAll is not propagated to tab labels.
				invoke = new CodeMethodInvokeExpression (
					var,
					"ShowAll"
				);
				ctx.Statements.Add (invoke);
			} else
				base.GenerateChildBuildCode (ctx, parentExp, wrapper);
		}


		private Gtk.Notebook notebook {
			get {
				return (Gtk.Notebook)Wrapped;
			}
		}

		public override void Select (Gtk.Widget widget)
		{
			if (widget != null) {
				int index = tabs.IndexOf (widget);
				if (index != -1 && index != notebook.CurrentPage)
					notebook.CurrentPage = index;
			}
			base.Select (widget);
		}

		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			int index = tabs.IndexOf (oldChild);
			if (index != -1) {
				tabs[index] = newChild;
				Gtk.Widget page = notebook.GetNthPage (index);
				notebook.SetTabLabel (page, newChild);
			} else {
				Gtk.Widget tab = notebook.GetTabLabel (oldChild);
				int current = notebook.CurrentPage;
				base.ReplaceChild (oldChild, newChild);
				notebook.CurrentPage = current;
				notebook.SetTabLabel (newChild, tab);
				Widget ww = Widget.Lookup (tab);
				if (ww != null)
					ww.RequiresUndoStatusUpdate = true;
			}
		}

		int InsertPage (int position)
		{
			Gtk.Label label = (Gtk.Label)Registry.NewInstance ("Gtk.Label", proj);
			label.LabelProp = "page" + (notebook.NPages + 1).ToString ();
			tabs.Insert (position, label);

			Placeholder ph = CreatePlaceholder ();
			int i = notebook.InsertPage (ph, label, position);
			NotifyChildAdded (ph);
			return i;
		}

		internal void PreviousPage ()
		{
			notebook.PrevPage ();
		}

		internal bool CheckPreviousPage ()
		{
			return notebook.CurrentPage > 0;
		}

		internal void NextPage ()
		{
			notebook.NextPage ();
		}

		internal bool CheckNextPage ()
		{
			return notebook.CurrentPage < notebook.NPages - 1;
		}

		internal void DeletePage ()
		{
			tabs.RemoveAt (notebook.CurrentPage);
			notebook.RemovePage (notebook.CurrentPage);
		}
		
		internal bool CheckDeletePage ()
		{
			return notebook.NPages > 0;
		}

		internal void SwapPrevious ()
		{
			object ob = tabs [notebook.CurrentPage];
			tabs [notebook.CurrentPage] = tabs [notebook.CurrentPage - 1];
			tabs [notebook.CurrentPage - 1] = ob;
			
			Gtk.Widget cp = notebook.GetNthPage (notebook.CurrentPage);
			notebook.ReorderChild (cp, notebook.CurrentPage - 1);
		}

		internal void SwapNext ()
		{
			object ob = tabs [notebook.CurrentPage];
			tabs [notebook.CurrentPage] = tabs [notebook.CurrentPage + 1];
			tabs [notebook.CurrentPage + 1] = ob;
			
			Gtk.Widget cp = notebook.GetNthPage (notebook.CurrentPage);
			notebook.ReorderChild (cp, notebook.CurrentPage + 1);
		}

		internal void InsertBefore ()
		{
			notebook.CurrentPage = InsertPage (notebook.CurrentPage);
		}

		internal bool CheckInsertBefore ()
		{
			return notebook.NPages > 0;
		}

		internal void InsertAfter ()
		{
			notebook.CurrentPage = InsertPage (notebook.CurrentPage + 1);
		}

		public override bool HExpandable {
			get {
				foreach (Gtk.Widget w in notebook) {
					if (ChildHExpandable (w)) 
						return true;
				}
				return false;
			}
		}

		public override bool VExpandable {
			get {
				foreach (Gtk.Widget w in notebook) {
					if (ChildVExpandable (w))
						return true;
				}
				return false;
			}
		}
		
		void OnPageChanged (object s, Gtk.SwitchPageArgs args)
		{
			EmitNotify ("CurrentPage");
		}
	}
}
