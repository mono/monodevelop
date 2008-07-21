/* 
 * ToolboxConfiguration.cs - A toolbox widget
 * 
 * Authors: 
 *  Lluis Sanchez <lluis@novell.com>
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 * Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	internal class ToolboxConfiguration
	{
		// Items selected by the user to be shown in the toolbox
		ToolboxList itemList = new ToolboxList ();
		
		// DefaultProviders which have already been processed
		List<string> loadedDefaultProviders = new List<string> ();
		
		[ItemProperty]
		public ToolboxList ItemList {
			get {
				return itemList;
			}
		}

		[ItemProperty]
		[ItemProperty ("Provider",Scope="*")]
		public List<string> LoadedDefaultProviders {
			get {
				return loadedDefaultProviders;
			}
		}
		
		public void SaveContents (string fileName)
		{
			using (StreamWriter writer = new StreamWriter (fileName)) {
				XmlDataSerializer serializer = new XmlDataSerializer (MonoDevelop.Projects.Services.ProjectService.DataContext);
				serializer.Serialize (writer, this);
			}
		}
		
		public static ToolboxConfiguration LoadFromFile (string fileName)
		{
			object o;
			
			using (StreamReader reader = new StreamReader (fileName))
			{
				XmlDataSerializer serializer = new XmlDataSerializer (MonoDevelop.Projects.Services.ProjectService.DataContext);
				o = serializer.Deserialize (reader, typeof (ToolboxConfiguration));	
			}
			return (ToolboxConfiguration) o;			
		}
	}
}
