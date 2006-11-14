//
// ToolboxList.cs: List of ItemToolboxNodes that can load or save them 
//     from or to an XML file.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Projects.Serialization;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class ToolboxList : List<ItemToolboxNode>
	{
		
		public ToolboxList ()
		{
		}
		
		public void SaveContents (string fileName)
		{
			using (StreamWriter writer = new StreamWriter (fileName)) {
				XmlDataSerializer serializer = new XmlDataSerializer (IdeApp.Services.ProjectService.DataContext);
				serializer.Serialize (writer, this);
			}
		}
		
		//static methods shouldn't reallly replace contructors, but becuase of the way the Deserialize
		//method works, this saves us copying all the items from one ToolboxList to another
		public static ToolboxList LoadFromFile (string fileName)
		{
			object o;
			
			using (StreamReader reader = new StreamReader (fileName))
			{
				XmlDataSerializer serializer = new XmlDataSerializer (IdeApp.Services.ProjectService.DataContext);
				o = serializer.Deserialize (reader, typeof (ToolboxList));	
			}
			
			return (ToolboxList) o;			
		}
	}
}
