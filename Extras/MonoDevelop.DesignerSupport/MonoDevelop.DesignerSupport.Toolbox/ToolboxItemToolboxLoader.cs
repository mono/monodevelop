
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	
	public class ToolboxItemToolboxLoader : IToolboxLoader
	{
		static string[] fileTypes = new string[] {"dll"};
		
		public bool ShouldIsolate {
			get { return true; }
		}
		
		public string [] FileTypes {
			get { return fileTypes; }
		}
		
		public IList<ItemToolboxNode> Load (string filename)
		{
			List<ItemToolboxNode> nodes = new List<ItemToolboxNode> ();
			
			System.Reflection.Assembly scanAssem = System.Reflection.Assembly.LoadFile (filename);
			Type[] types = scanAssem.GetTypes ();

			foreach (Type t in types)
			{
				//skip inaccessible types
				if (t.IsAbstract || t.IsNotPublic) continue;
				if (t.GetConstructor (new Type[] {}) == null) continue;
				
				//get the ToolboxItemAttribute if present
				AttributeCollection atts = TypeDescriptor.GetAttributes (t);
				
				bool containsAtt = false;
				foreach (Attribute a in atts)
					 if (a.GetType() == typeof (ToolboxItemAttribute))
					 	containsAtt = true;
				if (!containsAtt) continue;
				
				ToolboxItemAttribute tba = (ToolboxItemAttribute) atts[typeof(ToolboxItemAttribute)];
				if (tba.Equals (ToolboxItemAttribute.None)) continue;
				
				Type toolboxItemType = (tba.ToolboxItemType == null) ? typeof (ToolboxItem) : tba.ToolboxItemType;				
				//FIXME: fix WebControlToolboxItem so that this isn't necessary
				if (typeof (System.Web.UI.Design.WebControlToolboxItem) == toolboxItemType)
					toolboxItemType = typeof (ToolboxItem);
				
				//create the ToolboxItem. The ToolboxItemToolboxNode will destry it, but need to
				//be able to extract data from it first.
				ToolboxItem item = (ToolboxItem) Activator.CreateInstance (toolboxItemType, new object[] {t});
				ToolboxItemToolboxNode node = new ToolboxItemToolboxNode (item);
				
				//Technically CategoryAttribute shouldn't be used for this purpose (intended for properties in the PropertyGrid)
				//but I can see no harm in doing this.
				CategoryAttribute ca = atts[typeof(CategoryAttribute)] as CategoryAttribute;
				node.Category = (ca != null)? ca.Category : "General";
				
				//FIXME: this shouldn't be hard coded, remove when we ship a default toolbox with categories predefined
				if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseValidator)))
					node.Category = "Validation";
				else if (t.Namespace == "System.Web.UI.HtmlControls"  && t.IsSubclassOf (typeof (System.Web.UI.HtmlControls.HtmlControl)))
					node.Category = "Html Elements";
				else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseDataList)))
					node.Category = "Data Controls";
				else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.WebControl)))
					node.Category = "Web Controls";
				
				nodes.Add (node);
			}
			
			return (nodes);
		}
	}
}
