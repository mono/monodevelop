//  FileFilterCodon.cs
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
using System.Collections;
using System.ComponentModel;

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="A file filter to be used in the Open File dialog.")]
	internal class FileFilterCodon : TypeExtensionNode
	{
		[NodeAttribute ("_label", true, "Display name of the filter.")]
		string filtername = null;
		
		[NodeAttribute("extensions", true, "Extensions to use as filter.")]
		string[] extensions = null;
		
		public string FilterName {
			get {
				return filtername;
			}
			set {
				filtername = value;
			}
		}
		
		public string[] Extensions {
			get {
				return extensions;
			}
			set {
				extensions = value;
			}
		}
		
		public override object CreateInstance ()
		{
			return StringParserService.Parse (filtername) + "|" + String.Join(";", extensions);
		}
	}
}
