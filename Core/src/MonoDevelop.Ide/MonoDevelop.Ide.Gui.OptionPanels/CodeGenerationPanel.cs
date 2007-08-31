// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
	
	public class CodeGenerationPanel : AbstractOptionPanel
	{
		CodeGenerationPanelWidget widget;
		
		public override void LoadPanelContents ()
		{
			Add (widget = new CodeGenerationPanelWidget ());
		}
		
		public override bool StorePanelContents ()
		{
			widget.Store ();
			return true;
		}
	}
}
