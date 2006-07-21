 /* 
 * TextToolboxNode.cs - A ToolboxNode for text fragments
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
using Gtk;

namespace AspNetEdit.Gui.Toolbox
{
	public class TextToolboxNode : ItemToolboxNode
	{
		protected string Text = "";
		
		public TextToolboxNode (string text)
		{
			Text = text;
			
			//TODO: Use additional filters to limit to a specific host
			ToolboxItemFilterAttribute[] filters  = new ToolboxItemFilterAttribute [1];
			filters[0] = new ToolboxItemFilterAttribute ("AspNetEdit.RawText", ToolboxItemFilterType.Require);
			base.ItemFilters = filters;
		}
		
		public override bool Filter (string keyword)
		{
			return base.Filter (keyword)
				   || ((Text==null)? false : (Text.IndexOf (keyword) >= 0));
		}
		
		public override void Activate (object host)
		{
			AspNetEdit.Editor.ComponentModel.DesignerHost deshost =
				host as AspNetEdit.Editor.ComponentModel.DesignerHost;

			if (deshost != null)
				deshost.RootDocument.InsertFragment (Text);
			else
				throw new NotImplementedException ("We need an interface to insert text into documents in other hosts");
		}
	}	
}
