//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Displays the resulting output from an XSL transform.
	/// </summary>
	public class XslOutputViewContent : XmlEditorViewContent
	{
		public XslOutputViewContent()
		{
		}
		
		public static XslOutputViewContent Instance {
			get {
				foreach (XmlEditorViewContent content in XmlEditorService.OpenXmlEditorViews) {
					if (content is XslOutputViewContent) {
						Console.WriteLine("XslOutputViewContent instance exists.");
						return (XslOutputViewContent)content;
					}
				}
				return null;
			}
		}
		
		public override bool IsDirty {
			get {
				return false;
			}
			set {
			}
		}
		
		public override string ContentName {
			get {
				return "XSLT Output";
			}
			set {
			}
		}
	}
}
