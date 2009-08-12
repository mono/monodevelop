// 
// MD1AspNetCustomDataItem.cs
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
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AspNet.MD1Serialization
{
	
	class MD1AspNetCustomDataItem : MonoDevelop.Projects.Formats.MD1.MD1CustomDataItem
	{
		public override DataCollection Serialize (object obj, ITypeSerializer handler)
		{
			//serialise as normal
			DataCollection data = base.Serialize (obj, handler);
			
			AspNetAppProject proj = obj as AspNetAppProject;
			if (proj == null)
				return data;
			
			//for the old format we need to make sure each Content file that's a web type and is NOT set
			//to copy actually gets changed to the FileCopy build target
			DataItem files = data ["Contents"] as DataItem;
			if (files == null || !files.HasItemData)
				return data;
			
			foreach (object f in files.ItemData) {
				DataItem file = f as DataItem;
				if (file == null || !file.HasItemData)// || file.Name != "File")
					continue;
				
				DataValue ctod = file ["copyToOutputDirectory"] as DataValue;
				if (ctod != null && ctod.Value == "Never")
				{
					DataValue action = file ["buildaction"] as DataValue;
					DataValue name = file ["name"] as DataValue;
					if (action != null && name != null && action.Value == "Content")
					{
						WebSubtype type = AspNetAppProject.DetermineWebSubtype (name.Value);
						if (type != WebSubtype.None && type != WebSubtype.Code) {
							file.Extract ("copyToOutputDirectory");
							int index = file.ItemData.IndexOf (action);
							file.ItemData.Remove (action);
							file.ItemData.Insert (index, new DataValue ("buildaction", "FileCopy"));
						}
					}
				}
			}
			
			return data;
		}
		
		public override void Deserialize (object obj, ITypeSerializer handler, DataCollection data)
		{
			//deserialise as normal
			base.Deserialize (obj, handler, data);
			
			AspNetAppProject proj = obj as AspNetAppProject;
			if (proj == null)
				return;
			
			//find files that are web content and have been deserialised from the obsolete "FileCopy" BuildAction
			foreach (MonoDevelop.Projects.ProjectFile pf in proj.Files) {
				if (pf.BuildAction != MonoDevelop.Projects.BuildAction.Content
				    || pf.CopyToOutputDirectory != MonoDevelop.Projects.FileCopyMode.Always)
					continue;
				
				WebSubtype type = proj.DetermineWebSubtype (pf);
				if (type != WebSubtype.None && type != WebSubtype.Code) {
					//and mark them to not copy, since we don't actually want this; 
					// the obsolete behaviour was a hack
					pf.CopyToOutputDirectory = MonoDevelop.Projects.FileCopyMode.None;
				}
			}
		}
	}
}
