 /* 
 * ToolboxItemToolboxNode.cs - A ToolboxNode that wraps a System.Drawing.Design.ToolboxItem
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2006 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace AspNetEdit.Gui.Toolbox
{
	public class ToolboxItemToolboxNode : ItemToolboxNode
	{
		private ToolboxItem item;
		
		public ToolboxItemToolboxNode (ToolboxItem item)
		{
			this.item = item;
			base.Name = item.DisplayName;
			if (item.Bitmap != null)
				base.Icon = base.ImageToPixbuf (item.Bitmap);
			
			if (item.AssemblyName != null)
				foreach (System.ComponentModel.CategoryAttribute ca in
					System.Reflection.Assembly.Load (item.AssemblyName)
					.GetType (item.TypeName)
					.GetCustomAttributes (typeof (System.ComponentModel.CategoryAttribute), true))
					this.Category = ca.Category;
		}
		
		public ToolboxItem ToolboxItem
		{
			get {
				//TODO: load assembly and then item, if not already done
				return item;
			}
		}
		
		#region Behaviours
		
		//Runs when item is clicked.
		//We should be able to avoid loading actual ToolboxItem instances and their
		//associated assemblies until they are activated.
		public override void Activate (object host)
		{
			IDesignerHost desHost = host as IDesignerHost;
			
			if (desHost == null)
				throw new Exception("This ToolboxItem should not have been shown for this host. System.Drawing.Design.ToolboxItem requires a host of type System.ComponentModel.Design.IDesignerHost");
			
			//web controls have sample HTML that need to be deserialised
			//TODO: Fix WebControlToolboxItem so we don't have to mess around with type lookups and attributes here
			if ((item is System.Web.UI.Design.WebControlToolboxItem) && host is AspNetEdit.Editor.ComponentModel.DesignerHost)
			{
				AspNetEdit.Editor.ComponentModel.DesignerHost aspDesHost = (AspNetEdit.Editor.ComponentModel.DesignerHost) desHost;
				
				if (item.AssemblyName != null && item.TypeName != null) {
					//look up and register the type
					ITypeResolutionService typeRes = (ITypeResolutionService) aspDesHost.GetService(typeof(ITypeResolutionService));					
					typeRes.ReferenceAssembly (item.AssemblyName);
					Type controlType = typeRes.GetType (item.TypeName, true);
						
					//read the WebControlToolboxItem data from the attribute
					AttributeCollection atts = TypeDescriptor.GetAttributes (controlType);
					System.Web.UI.ToolboxDataAttribute tda = (System.Web.UI.ToolboxDataAttribute) atts[typeof(System.Web.UI.ToolboxDataAttribute)];
						
					//if it's present
					if (tda != null && tda.Data.Length > 0) {
						//look up the tag's prefix and insert it into the data						
						System.Web.UI.Design.IWebFormReferenceManager webRef = aspDesHost.GetService (typeof (System.Web.UI.Design.IWebFormReferenceManager)) as System.Web.UI.Design.IWebFormReferenceManager;
						if (webRef == null)
							throw new Exception("Host does not provide an IWebFormReferenceManager");
						string aspText = String.Format (tda.Data, webRef.GetTagPrefix (controlType));
						System.Diagnostics.Trace.WriteLine ("Toolbox processing ASP.NET item data: " + aspText);
							
						//and add it to the document
						aspDesHost.RootDocument.DeserializeAndAdd (aspText);
						return;
					}
				}
			}
				
			//No ToolboxDataAttribute? Get the ToolboxItem to create the components itself
			item.CreateComponents (desHost);
		}
		
		protected override bool EvaluateCustomFilter (IToolboxUser user)
		{
			return user.GetToolSupported (item);
		}
		
		
		//TODO: drag source
		
		#endregion Behaviours
	}	
}
