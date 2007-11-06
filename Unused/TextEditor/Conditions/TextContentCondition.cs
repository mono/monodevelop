//  TextContentCondition.cs
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

using MonoDevelop.Core.AddIns;

using MonoDevelop.Core.Gui;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor;

namespace MonoDevelop.DefaultEditor.Conditions
{
	[ConditionAttribute()]
	public class TextContentCondition : AbstractCondition
	{
		[XmlMemberAttribute("textcontent", IsRequired = true)]
		string textcontent;
		
		public string TextContent {
			get {
				return textcontent;
			}
			set {
				textcontent = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			if (owner is TextEditorControl) {
				TextEditorControl ctrl = (TextEditorControl)owner;
				if (ctrl.Document != null && ctrl.Document.HighlightingStrategy != null) {
					return textcontent == ctrl.Document.HighlightingStrategy.Name;
				}
			}
			
			if (owner is IViewContent) {
				IViewContent ctrl = (IViewContent) owner;
				if (textcontent == "C#" && ctrl.ContentName.EndsWith (".cs"))
					return true;
			}
			
			return false;
		}
	}
}
