// 
// AspNetToolboxProvider.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing.Design;

using MonoDevelop.DesignerSupport.Toolbox;

namespace MonoDevelop.AspNet
{
	
	
	public class AspNetToolboxLoader : ToolboxItemToolboxLoader
	{
		public override IList<ItemToolboxNode> Load (System.Reflection.Assembly assem)
		{
			List<ItemToolboxNode> nodes = new List<ItemToolboxNode> ();
			foreach (Type t in AccessibleToolboxTypes (assem)) {
				try {
					AspNetToolboxNode node = GetNode (t);
					if (node == null)
						continue;
					SetFullPath (node, assem);
					nodes.Add (node);
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Error scanning for web toolbox items.", ex);
				}
			}
			return nodes;
		}
		
		AspNetToolboxNode GetNode (Type t)
		{
			ToolboxItemAttribute tba = 
				(ToolboxItemAttribute) t.GetCustomAttributes (typeof (ToolboxItemAttribute), true)[0];
			Type toolboxItemType = (tba.ToolboxItemType == null) ? typeof (ToolboxItem) : tba.ToolboxItemType;
			
			if (toolboxItemType != typeof (System.Web.UI.Design.WebControlToolboxItem))
				return null;
			
			//FIXME: fix WebControlToolboxItem so that this isn't necessary
			//right now it's totally broken in mono
			toolboxItemType = typeof (ToolboxItem);
			
			//Create the ToolboxItem. The ToolboxItemToolboxNode will destroy it, but need to
			//be able to extract data from it first.
			ToolboxItem item = (ToolboxItem) Activator.CreateInstance (toolboxItemType, new object[] {t});
			string text = GetText (t);
			
			AspNetToolboxNode node = new AspNetToolboxNode (item);
			if (!string.IsNullOrEmpty (text))
				node.Text = text;
			node.Category = GetCategory (t);
			return node;
		}
		
		string GetText (Type t)
		{
			object[] atts = t.GetCustomAttributes (typeof (System.Web.UI.ToolboxDataAttribute), false);
			if (atts != null && atts.Length > 0)
				return ((System.Web.UI.ToolboxDataAttribute)atts[0]).Data;
			else
				return null;
		}
		
		string GetCategory (Type t)
		{
			CategoryAttribute ca = GetCategoryAttribute (t);
			if (ca != null)
				return ca.Category;
			
			if (t.Namespace == "System.Web.UI.WebControls") {
				switch (t.Name) {
				case "Repeater":
					return "Data";
				case "Login":
				case "LoginView":
				case "LoginStatus":
				case "LoginName":
				case "CreateUserWizard":
				case "PasswordRecovery":
				case "ChangePassword":
					return "Login";
				}
			}
			
			//FIXME: this shouldn't be hard coded, remove when we ship a default toolbox with categories predefined
			if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseValidator)))
				return "Validation";
			else if (typeof (System.Web.UI.IDataSource).IsAssignableFrom (t))
				return "Data Source";
			else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.BaseDataList)))
				return "Data Controls";
			else if (t.Namespace == "System.Web.UI.HtmlControls"
			    && t.IsSubclassOf (typeof (System.Web.UI.HtmlControls.HtmlControl)))
				return "Html Elements";
			else if (t.IsSubclassOf (typeof (System.Web.UI.WebControls.WebControl)))
				return "Web Controls";
			
			return "General";
		}
	}
}
