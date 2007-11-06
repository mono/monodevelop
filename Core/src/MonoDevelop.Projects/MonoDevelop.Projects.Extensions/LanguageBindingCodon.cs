//  LanguageBindingCodon.cs
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
using System.Diagnostics;
using System.ComponentModel;

using Mono.Addins;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (Description="A language binding. The specified class must implement MonoDevelop.Projects.ILanguageBinding")]
	internal class LanguageBindingCodon : TypeExtensionNode
	{
		[NodeAttribute("supportedextensions", "File extensions supported by this binding (to be shown in the Open File dialog)")]
		string[] supportedExtensions;
		
		public string[] Supportedextensions {
			get {
				return supportedExtensions;
			}
			set {
				supportedExtensions = value;
			}
		}
		
		public ILanguageBinding LanguageBinding {
			get {
				return (ILanguageBinding) GetInstance ();
			}
		}
	}
}
