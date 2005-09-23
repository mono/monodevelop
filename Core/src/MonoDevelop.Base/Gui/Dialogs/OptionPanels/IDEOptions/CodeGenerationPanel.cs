// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Collections;

using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Components;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Services;

using Gtk;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Gui.Dialogs.OptionPanels {
	public class CodeGenerationPanel : AbstractOptionPanel {
		
		class CodeGenerationPanelWidget : GladeWidgetExtract {
			PropertyService p = Runtime.Properties;
			
//			[Glade.Widget] Label hdr_code_generation_options;
			
			[Glade.Widget] CheckButton
				chk_blk_on_same_line,
				chk_else_on_same_line,
				chk_blank_lines,
				chk_full_type_names;
			
//			[Glade.Widget] Label hdr_comment_generation_options;
			
			[Glade.Widget] CheckButton
				chk_doc_comments,
				chk_other_comments;
			
			public CodeGenerationPanelWidget () : base ("Base.glade", "CodeGenerationOptionsPanel")
			{
				chk_blk_on_same_line.Active   = p.GetProperty("StartBlockOnSameLine", true);
				chk_else_on_same_line.Active  = p.GetProperty("ElseOnClosing", true);
				chk_blank_lines.Active        = p.GetProperty("BlankLinesBetweenMembers", true);
				chk_full_type_names.Active    = p.GetProperty("UseFullyQualifiedNames", true);
			
				chk_doc_comments.Active       = p.GetProperty("GenerateDocumentComments", true);
				chk_other_comments.Active     = p.GetProperty("GenerateAdditionalComments", true);
				
				chk_blk_on_same_line.Sensitive = false;
				chk_else_on_same_line.Sensitive = false;
				chk_blank_lines.Sensitive = false;
				chk_full_type_names.Sensitive = false;
				chk_doc_comments.Sensitive = false;
				chk_other_comments.Sensitive = false;
			}
			
			public void Store ()
			{
				p.SetProperty ("StartBlockOnSameLine",       chk_blk_on_same_line.Active);
				p.SetProperty ("ElseOnClosing",              chk_else_on_same_line.Active);
				p.SetProperty ("BlankLinesBetweenMembers",   chk_blank_lines.Active);
				p.SetProperty ("UseFullyQualifiedNames",     chk_full_type_names.Active);
				
				p.SetProperty ("GenerateDocumentComments",   chk_doc_comments.Active);
				p.SetProperty ("GenerateAdditionalComments", chk_other_comments.Active);
			}
		}
		
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
