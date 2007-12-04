 /* 
 * TextToolboxItem.cs - a ToolboxItem for storing text fragments
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
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
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Drawing.Design;
 
namespace AspNetEdit.Editor.ComponentModel
{
	public class TextToolboxItem : ToolboxItem
	{
		private string text;
		
		public TextToolboxItem (string text)
		: this (text, text)
		{
		}
		
		public TextToolboxItem (string text, string displayName)
			: base ()
		{
			base.DisplayName = displayName;
			this.text = text;
		}
		
		public string Text {
			get { return text; }
		}

		protected override IComponent[] CreateComponentsCore (IDesignerHost host)
		{
			DesignerHost desHost = host as DesignerHost;
			if (desHost == null)
				throw new ArgumentException ("host", "Must be a AspNetEdit.Editor.ComponentModel.DesignerHost");

			OnComponentsCreating(new ToolboxComponentsCreatingEventArgs (host));
			
			desHost.RootDocument.InsertFragment (text);

			OnComponentsCreated(new ToolboxComponentsCreatedEventArgs (new IComponent[]{}));
			return new IComponent[]{};
		}

		protected override void Deserialize (SerializationInfo info, StreamingContext context)
		{
			text = (string) info.GetValue ("AssemblyName", typeof (AssemblyName));
			base.Filter = (ICollection)info.GetValue ("Filter", typeof (ICollection));
			base.DisplayName = info.GetString ("DisplayName");
			if (info.GetBoolean ("Locked")) base.Lock ();
		}

		public override void Initialize (Type type) 
		{
		}

		protected override void Serialize (SerializationInfo info, StreamingContext context)
		{
			info.AddValue ("Text", text);
			info.AddValue ("Filter", base.Filter);
			info.AddValue ("DisplayName", base.DisplayName);
			info.AddValue ("Locked", base.Locked);
		}	
 	}
 }