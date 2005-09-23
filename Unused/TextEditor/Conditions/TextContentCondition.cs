// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;


using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.AddIns;

using MonoDevelop.Gui;
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
