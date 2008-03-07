//  CodeGenerationPanel.cs
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
using System.Collections;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	partial class CodeGenerationPanelWidget : Gtk.Bin
	{
		public CodeGenerationPanelWidget ()
		{
			Build ();
			
			chk_blk_on_same_line.Active   = PropertyService.Get("StartBlockOnSameLine", true);
			chk_else_on_same_line.Active  = PropertyService.Get("ElseOnClosing", true);
			chk_blank_lines.Active        = PropertyService.Get("BlankLinesBetweenMembers", true);
			chk_full_type_names.Active    = PropertyService.Get("UseFullyQualifiedNames", true);
		
			chk_doc_comments.Active       = PropertyService.Get("GenerateDocumentComments", true);
			chk_other_comments.Active     = PropertyService.Get("GenerateAdditionalComments", true);
			
			chk_blk_on_same_line.Sensitive = false;
			chk_else_on_same_line.Sensitive = false;
			chk_blank_lines.Sensitive = false;
			chk_full_type_names.Sensitive = false;
			chk_doc_comments.Sensitive = false;
			chk_other_comments.Sensitive = false;
		}
		
		public void Store ()
		{
			PropertyService.Set ("StartBlockOnSameLine",       chk_blk_on_same_line.Active);
			PropertyService.Set ("ElseOnClosing",              chk_else_on_same_line.Active);
			PropertyService.Set ("BlankLinesBetweenMembers",   chk_blank_lines.Active);
			PropertyService.Set ("UseFullyQualifiedNames",     chk_full_type_names.Active);
			
			PropertyService.Set ("GenerateDocumentComments",   chk_doc_comments.Active);
			PropertyService.Set ("GenerateAdditionalComments", chk_other_comments.Active);
		}
	}
	
	public class CodeGenerationPanel : OptionsPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return (widget = new CodeGenerationPanelWidget ());
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
