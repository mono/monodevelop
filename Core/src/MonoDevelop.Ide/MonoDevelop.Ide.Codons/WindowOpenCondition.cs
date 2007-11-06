//  WindowOpenCondition.cs
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
using System.Xml;


using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ConditionAttribute()]
	internal class WindowOpenCondition : AbstractCondition
	{
		[XmlMemberAttribute("openwindow", IsRequired = true)]
		string openwindow;
		
		public string ActiveWindow {
			get {
				return openwindow;
			}
			set {
				openwindow = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			if (openwindow == "*") {
				return IdeApp.Workbench.ActiveDocument != null;
			}
			foreach (Document doc in IdeApp.Workbench.Documents) {
				Type currentType = doc.Window.ViewContent.GetType();
				if (currentType.ToString() == openwindow) {
					return true;
				}
				foreach (Type i in currentType.GetInterfaces()) {
					if (i.ToString() == openwindow) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
