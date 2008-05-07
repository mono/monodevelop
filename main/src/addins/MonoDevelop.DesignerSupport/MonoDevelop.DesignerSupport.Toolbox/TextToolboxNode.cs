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
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Serializable]
	public class TextToolboxNode : ItemToolboxNode, ITextToolboxNode
	{
		private string text = "";
		string domain = MonoDevelop.Core.GettextCatalog.GetString ("Text Snippets");
		
		public TextToolboxNode (string text)
		{
			Text = text;
			ItemFilters.Add (new ToolboxItemFilterAttribute ("text/plain", ToolboxItemFilterType.Allow));
		}
		
		public override bool Filter (string keyword)
		{
			return base.Filter (keyword)
				   || ((Text==null)? false : (Text.IndexOf (keyword) >= 0));
		}
		
		public string Text {
			get { return text; }
			set { text = value; }
		}
		
		public virtual string GetTextForFile (string path, MonoDevelop.Projects.Project project)
		{
			return text;
		}
		
		public override string ItemDomain {
			get { return domain; }
		}

		public virtual string[] AllowedMimetypes {
			get { return new string[] { "text/plain" }; }
		}

		public string[] AllowedExtensions {
			get { return new string[] { "*" }; }
		}
	}
}
