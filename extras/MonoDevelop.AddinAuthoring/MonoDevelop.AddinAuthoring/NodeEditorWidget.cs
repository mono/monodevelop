
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Gtk;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components.PropertyGrid;

namespace MonoDevelop.AddinAuthoring
{
	public class NodeEditorWidget: VBox
	{
		ExtensionNodeDescription node;
		
		ComboBox insBeforeCombo;
		ComboBox insAfterCombo;
		Gtk.Tooltips tips;
		DotNetProject project;
		PropertyGrid grid;
		
		Hashtable atts = new Hashtable ();
		
		public NodeEditorWidget (DotNetProject project, AddinRegistry reg, ExtensionNodeType ntype, AddinDescription parentAddinDescription, string parentPath, ExtensionNodeDescription node)
		{
			this.node = node;
			this.project = project;
			tips = new Tooltips ();
			Spacing = 0;
			
			// Header
			
			Label label = new Label ();
			label.Wrap = true;
			label.WidthRequest = 480;
			string txt = "<b>" + node.NodeName + "</b>";
			if (ntype.Description.Length > 0)
				txt += "\n" + GLib.Markup.EscapeText (ntype.Description);
			label.Markup = txt;
			label.Xalign = 0f;
			PackStart (label, false, false, 6);
			PackStart (new HSeparator (), false, false, 0);
			
			// Attributes
			
			grid = new PropertyGrid ();
			grid.CurrentObject = new NodeWrapper (project, reg, ntype, parentAddinDescription, parentPath, node);
			
			PackStart (grid, true, true, 0);
			
			ShowAll ();
			
			grid.ShowHelp = true;
			grid.ShowToolbar = false;
			
		}
		
		public void Save ()
		{
			grid.CurrentObject = null;
		}
	}
	
	class NodeWrapper: CustomTypeDescriptor
	{
		PropertyDescriptorCollection properties;
		
		public NodeWrapper (DotNetProject project, AddinRegistry reg, ExtensionNodeType ntype, AddinDescription parentAddinDescription, string parentPath, ExtensionNodeDescription node)
		{
			List<PropertyDescriptor> props = new List<PropertyDescriptor> ();
			
			string mainCategory = AddinManager.CurrentLocalizer.GetString ("Node Attributes");

			PropertyDescriptor prop = new MyPropertyDescriptor ("id", typeof(String), AddinManager.CurrentLocalizer.GetString ("Identifier of the extension node"), mainCategory, node);
			props.Add (prop);
			
			foreach (NodeTypeAttribute att in ntype.Attributes) {
				Type pt = Type.GetType (att.Type);
				if (pt == null)
					pt = typeof(string);
				prop = new MyPropertyDescriptor (att.Name, pt, att.Description, mainCategory, node);
				props.Add (prop);
			}
			
/*			int n = 1;
			foreach (ExtensionNodeDescription en in AddinData.GetExtensionNodes (reg, parentAddinDescription, parentPath)) {
				if (en.Id.Length > 0) {
					insBeforeCombo.AppendText (en.Id);
					insAfterCombo.AppendText (en.Id);
					if (en.Id == node.InsertBefore)
						insBeforeCombo.Active = n;
					if (en.Id == node.InsertAfter)
						insAfterCombo.Active = n;
				}
				n++;
			}
			*/
			
			prop = new MyPropertyDescriptor ("insertBefore", typeof(String), AddinManager.CurrentLocalizer.GetString ("Insert Before"), AddinManager.CurrentLocalizer.GetString ("Placement"), node);
			props.Add (prop);
			
			prop = new MyPropertyDescriptor ("insertAfter", typeof(String), AddinManager.CurrentLocalizer.GetString ("Insert After"), AddinManager.CurrentLocalizer.GetString ("Placement"), node);
			props.Add (prop);
			
			properties = new PropertyDescriptorCollection (props.ToArray ());
		}
		
		public override PropertyDescriptorCollection GetProperties ()
		{
			return properties;
		}
	}
	
	class MyPropertyDescriptor: PropertyDescriptor
	{
		string name;
		Type type;
		string desc;
		ExtensionNodeDescription node;
		string category;
		Type editorType;
		
		public MyPropertyDescriptor (string name, Type type, string desc, string category, ExtensionNodeDescription node): base (name, new Attribute [0])
		{
			if (type == typeof(Type)) {
				type = typeof(string);
				editorType = typeof(TypeCellEditor);
			}
			
			this.name = name;
			this.type = type;
			this.node = node;
			this.desc = desc;
			this.category = category;
		}
		
		protected override void FillAttributes (System.Collections.IList attributeList)
		{
			base.FillAttributes (attributeList);
			if (desc != null)
				attributeList.Add (new DescriptionAttribute (desc));
			if (category != null)
				attributeList.Add (new CategoryAttribute (category));
			if (editorType != null)
				attributeList.Add (new EditorAttribute (editorType, typeof(PropertyEditorCell)));
		}
				                   
		
		public override Type ComponentType {
			get {
				return typeof(NodeWrapper);
			}
		}
		
		public override bool IsReadOnly {
			get {
				return false;
			}
		}
		
		public override Type PropertyType {
			get {
				return type;
			}
		}
		
		public override bool CanResetValue (object component)
		{
			return false;
		}

		public override object GetValue (object component)
		{
			string sval;
			if (name == "insertBefore")
				sval = node.InsertBefore;
			else if (name == "insertAfter")
				sval = node.InsertAfter;
			else if (name == "id")
				sval = node.Id;
			else
				sval = node.GetAttribute (name);
			try {
				return Convert.ChangeType (sval, type);
			} catch {
				return Activator.CreateInstance (type);
			}
		}

		public override void SetValue (object component, object value)
		{
			string sval = Convert.ToString (value, CultureInfo.InvariantCulture);
			if (name == "insertBefore")
				node.InsertBefore = sval;
			else if (name == "insertAfter")
				node.InsertAfter = sval;
			else if (name == "id")
				node.Id = sval;
			else
				node.SetAttribute (name, sval);
		}

		public override void ResetValue (object component)
		{
		}

		public override bool ShouldSerializeValue (object component)
		{
			return true;
		}
	}
}
