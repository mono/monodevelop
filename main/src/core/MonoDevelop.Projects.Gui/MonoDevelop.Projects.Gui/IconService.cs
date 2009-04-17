/*
//  IconService.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Resources;
using MonoDevelop.Projects.Dom;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui;
using Stock = MonoDevelop.Core.Gui.Stock;

namespace MonoDevelop.Projects.Gui
{
	public class IconService
	{
		Hashtable extensionHashtable   = new Hashtable ();
		Hashtable projectFileHashtable = new Hashtable ();
		
		internal IconService ()
		{
			InitializeIcons ("/MonoDevelop/ProjectModel/Gui/Icons");
		}
		
		public string GetImageForProjectType (string projectType)
		{
			if (projectFileHashtable [projectType] != null)
				return (string) projectFileHashtable [projectType];
			
			return (string) extensionHashtable [".PRJX"];
		}
		
		public string GetImageForFile (string fileName)
		{
			string extension = Path.GetExtension (fileName).ToUpper ();
			
			if (extensionHashtable.Contains (extension))
				return (string) extensionHashtable [extension];
			
			return Stock.MiscFiles;
		}


		void InitializeIcons (string path)
		{			
			extensionHashtable[".PRJX"] = Stock.Project;
			extensionHashtable[".CMBX"] = Stock.Solution;
			extensionHashtable[".MDS"] = Stock.Solution;
			extensionHashtable[".MDP"] = Stock.Project;
		
			foreach (IconCodon iconCodon in AddinManager.GetExtensionNodes (path, typeof(IconCodon))) {
				string image;
				if (iconCodon.Resource != null)
					image = iconCodon.Resource;
				else
					image = iconCodon.Id;
				
				image = ResourceService.GetStockId (iconCodon.Addin, image);
				
				if (iconCodon.Extensions != null) {
					foreach (string ext in iconCodon.Extensions)
						extensionHashtable [ext.ToUpper()] = image;
				}
				if (iconCodon.Language != null)
					projectFileHashtable [iconCodon.Language] = image;
			}
		}
	}
}
*/