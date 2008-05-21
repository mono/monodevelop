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
		
		public override ItemToolboxNode GetNode (Type t, ToolboxItemAttribute tba, 
		    string attributeCategory, string fullPath, MonoDevelop.Core.ClrVersion referencedRuntime)
		{
			if (referencedRuntime != MonoDevelop.Core.ClrVersion.Net_1_1
			    && referencedRuntime != MonoDevelop.Core.ClrVersion.Net_2_0)
				return null;
			
			bool reflectedRuntime1;
			if (typeof (System.Web.UI.Control).IsAssignableFrom (t))
				reflectedRuntime1 = false;
			else if (CanRuntime1 && SWUControl1.IsAssignableFrom (t))
				reflectedRuntime1 = true;
			else
				return null;
			
			Type toolboxItemType = (tba.ToolboxItemType == null) ? typeof (ToolboxItem) : tba.ToolboxItemType;
			
			//FIXME: fix WebControlToolboxItem so that this isn't necessary
			//right now it's totally broken in mono
			if (typeof (System.Web.UI.Design.WebControlToolboxItem).IsAssignableFrom (toolboxItemType))
				toolboxItemType = typeof (ToolboxItem);
			
			//Create the ToolboxItem. The ToolboxItemToolboxNode will destroy it, but need to
			//be able to extract data from it first.
			ToolboxItem item = (ToolboxItem) Activator.CreateInstance (toolboxItemType, new object[] {t});
			AspNetToolboxNode node = new AspNetToolboxNode (item);
			
			//get the default markup for the tag
			string webText = reflectedRuntime1? GetWebText1 (t) : GetWebText (t);
			if (!string.IsNullOrEmpty (webText))
				node.Text = webText;
			
			if (!string.IsNullOrEmpty (attributeCategory))
				node.Category = attributeCategory;
			else if (reflectedRuntime1)
				node.Category = GuessCategory1 (t);
			else
				node.Category = GuessCategory (t);
			
			if (!string.IsNullOrEmpty (fullPath))
				node.Type.AssemblyLocation = fullPath;
			
			//prevent system.web 1.1 from being shown for 2.0 runtime
			if (CanRuntime1 && webAssem1.FullName == t.Assembly.FullName) {
				node.ItemFilters.Add (new ToolboxItemFilterAttribute ("ClrVersion.Net_2_0", ToolboxItemFilterType.Prevent));
			}
			
			//set filters fom supported runtimes
			if (referencedRuntime == MonoDevelop.Core.ClrVersion.Net_1_1) {
				node.ItemFilters.Add (new ToolboxItemFilterAttribute ("ClrVersion.Net_1_1", ToolboxItemFilterType.Require));
			} else if (referencedRuntime == MonoDevelop.Core.ClrVersion.Net_2_0) {
				node.ItemFilters.Add (new ToolboxItemFilterAttribute ("ClrVersion.Net_2_0", ToolboxItemFilterType.Require));
			}
			
			return node;
		}
		
		static string GetWebText (Type t)
		{
			object[] atts = t.GetCustomAttributes (typeof (System.Web.UI.ToolboxDataAttribute), false);
			if (atts != null && atts.Length > 0)
				return ((System.Web.UI.ToolboxDataAttribute)atts[0]).Data;
			else
				return null;
		}
		
		string GetWebText1 (Type t)
		{
			Type textAttType = webAssem1.GetType ("System.Web.UI.ToolboxDataAttribute");
			object[] atts = t.GetCustomAttributes (textAttType, false);
			if (atts != null && atts.Length > 0) {
				return (string) textAttType.GetProperty ("Data").GetValue (atts[0], null);
			} else {
				return null;
			}
		}
		
		static string GuessCategory (Type t)
		{
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
				case "Menu":
				case "SiteMapPath":
				case "TreeView":
					return "Navigation";
				}
			}
			
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
		
		
		//.NET 1.1
		string GuessCategory1 (Type t)
		{
			if (t.IsSubclassOf (webAssem1.GetType ("System.Web.UI.WebControls.BaseValidator")))
				return "Validation";
			else if (t.IsSubclassOf (webAssem1.GetType ("System.Web.UI.WebControls.BaseDataList")))
				return "Data Controls";
			else if (t.Namespace == "System.Web.UI.HtmlControls"
			    && t.IsSubclassOf (webAssem1.GetType ("System.Web.UI.HtmlControls.HtmlControl")))
				return "Html Elements";
			else if (t.IsSubclassOf (webAssem1.GetType ("System.Web.UI.WebControls.WebControl")))
				return "Web Controls";
			
			return "General";
		}
		
		Type SWUControl1;
		System.Reflection.Assembly webAssem1;
		
		bool canRuntime1 = false;
		bool inited = false;
		
		bool CanRuntime1 {
			get {
				if (!inited)
					InitRuntime1 ();
				return canRuntime1;
			}
		}
		
		void InitRuntime1 ()
		{
			inited = true;
			
			//System.Reflection.Assembly.Load won't load things from the 1.0 GAC
			string loc = MonoDevelop.Core.Runtime.SystemAssemblyService.GetAssemblyLocation ( 
			    "System.Web, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
			if (loc != null)
				webAssem1 = System.Reflection.Assembly.LoadFile (loc);
			
			if (webAssem1 != null)
				canRuntime1 = true;
			else
				return;
			
			SWUControl1 = webAssem1.GetType ("System.Web.UI.Control");
		}
	}
}
