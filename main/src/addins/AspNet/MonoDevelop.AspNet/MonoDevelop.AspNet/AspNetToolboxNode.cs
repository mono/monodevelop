// 
// AspNetToolboxNode.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.ComponentModel;
using System.Drawing.Design;

using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNet
{
	
	[Serializable]
	public class AspNetToolboxNode : ToolboxItemToolboxNode, ITextToolboxNode
	{
		
		[ItemProperty ("text")]
		string text;
		
		[NonSerialized]
		string defaultText;
		
		static readonly string aspNetDomain = GettextCatalog.GetString ("ASP.NET Controls");
		
		public AspNetToolboxNode () : base () {}
		public AspNetToolboxNode (ToolboxItem item) : base (item)
		{
		}
		
		public string Text {
			get {
				if (string.IsNullOrEmpty (text)) {
					if (defaultText == null) {
						string ctrlName = base.Type.TypeName.Substring (base.Type.TypeName.LastIndexOf ('.') + 1);
						defaultText = "<{0}:" + ctrlName + " runat=\"server\"></{0}:" + ctrlName + ">";
					}
					return defaultText;
				}
				return text;
			}
			set {
				if (value != Text)
					text = value;
			}
		}
		
		public override bool Equals (object obj)
		{
			AspNetToolboxNode other = obj as AspNetToolboxNode;
			return (other != null) && (this.text != other.text) && base.Equals (other);
		}
		
		public override int GetHashCode ()
		{
			int code = base.GetHashCode ();
			if (text != null)
				code += text.GetHashCode ();
			return code;
		}
		
		void RegisterReference (MonoDevelop.Projects.Project project)
		{
			var dnp = (MonoDevelop.Projects.DotNetProject) project;
			var pr = base.Type.GetProjectReference ();
			
			//add the reference if it doesn't match an existing one
			bool match = false;
			foreach (var p in dnp.References)
				if (p.Equals (pr))
					match = true;
			if (!match)
				dnp.References.Add (pr);
		}
		
		public void InsertAtCaret (MonoDevelop.Ide.Gui.Document document)
		{
			var tag = GetTextWithDirective (document, true);
			document.TextEditor.InsertText (document.TextEditor.CursorPosition, tag);
		}
		
		string GetTextWithDirective (MonoDevelop.Ide.Gui.Document document, bool insertDirective)
		{
			string tag = Text;
			
			if (!tag.Contains ("{0}"))
				return tag;
			
			if (base.Type.AssemblyName.StartsWith ("System.Web.UI.WebControls"))
				return string.Format (tag, "asp");
			
			//register the assembly and look up the class
			//FIXME: only do this on the insert, not the preview - or remove it afterwards
			RegisterReference (document.Project);
			
			var database = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom (document.Project);
			
			var cls = database.GetType (Type.TypeName);
			if (cls == null)
				return tag;
			
			var doc = document.ParsedDocument as MonoDevelop.AspNet.Parser.AspNetParsedDocument;
			if (doc == null)
				return tag;
			
			var assemName = SystemAssemblyService.ParseAssemblyName (Type.AssemblyName);
			
			var refMan = new DocumentReferenceManager ((AspNetAppProject)document.Project) {
				Doc = doc,
			};
			
			RegisterDirective directive;
			string prefix = refMan.GetTagPrefixWithNewDirective (cls, assemName.Name, null, out directive);
			
			if (prefix == null)
				return tag;
			
			tag = string.Format (tag, prefix);
			
			if (directive != null && insertDirective)
				refMan.AddRegisterDirective (directive, document.TextEditor, true);
			
			return tag;
		}
		
		[Browsable(false)]
		public override string ItemDomain {
			get { return aspNetDomain; }
		}
		
		public bool IsCompatibleWith (MonoDevelop.Ide.Gui.Document document)
		{
			switch (AspNetAppProject.DetermineWebSubtype (document.FileName)) {
			case WebSubtype.WebForm:
			case WebSubtype.MasterPage:
			case WebSubtype.WebControl:
				break;
			default:
				return false;
			}
			
			var clrVersion = ClrVersion.Net_2_0;
			var aspProj = document.Project as AspNetAppProject;
			if (aspProj != null && aspProj.TargetFramework.ClrVersion != ClrVersion.Default)
				clrVersion = aspProj.TargetFramework.ClrVersion;
			
			foreach (var tbfa in ItemFilters) {
				ClrVersion filterVersion;
				switch (tbfa.FilterString) {
				case "ClrVersion.Net_1_1":
					filterVersion = ClrVersion.Net_1_1;
					break;
				case "ClrVersion.Net_2_0":
					filterVersion = ClrVersion.Net_2_0;
					break;
				case "ClrVersion.Net_4_0":
					filterVersion = ClrVersion.Net_4_0;
					break;
				default:
					continue;
				}
				
				if (tbfa.FilterType == ToolboxItemFilterType.Require && filterVersion != clrVersion)
					return false;
				
				if (tbfa.FilterType == ToolboxItemFilterType.Prevent && filterVersion == clrVersion)
					return false;
			}
			return true;
		}
		
		public string GetDragPreview (MonoDevelop.Ide.Gui.Document document)
		{
			return GetTextWithDirective (document, false);
		}
	}
}
