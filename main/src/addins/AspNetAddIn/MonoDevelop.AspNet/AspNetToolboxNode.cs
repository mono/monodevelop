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
using MonoDevelop.Projects.Serialization;

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
			//not needed since text editor implements custom filtering for ITextToolboxNode
//			ItemFilters.Add (new ToolboxItemFilterAttribute ("mime:application/x-aspx", ToolboxItemFilterType.Allow));
//			ItemFilters.Add (new ToolboxItemFilterAttribute ("mime:application/x-ascx", ToolboxItemFilterType.Allow));
//			ItemFilters.Add (new ToolboxItemFilterAttribute ("mime:application/x-master-page", ToolboxItemFilterType.Allow));
//			ItemFilters.Add (new ToolboxItemFilterAttribute ("text:.aspx", ToolboxItemFilterType.Allow));
//			ItemFilters.Add (new ToolboxItemFilterAttribute ("text:.ascx", ToolboxItemFilterType.Allow));
//			ItemFilters.Add (new ToolboxItemFilterAttribute ("text:.master", ToolboxItemFilterType.Allow));
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
			MonoDevelop.Projects.DotNetProject dnp = (MonoDevelop.Projects.DotNetProject) project;
			MonoDevelop.Projects.ProjectReference pr = base.Type.GetProjectReference ();
			if (!dnp.References.Contains (pr))
				dnp.References.Add (pr);
		}

		public string GetTextForFile (string path, MonoDevelop.Projects.Project project)
		{
			string tag = Text;
			
			if  (!tag.Contains ("{0}"))
				return tag;
			
			if (base.Type.AssemblyName.StartsWith ("System.Web.UI.WebControls"))
				return string.Format (tag, "asp");
			
			//register the assembly and look up the class
			RegisterReference (project);
			MonoDevelop.Projects.Parser.IParserContext ctx = 
				MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
			ctx.UpdateDatabase ();
			MonoDevelop.Projects.Parser.IClass cls = ctx.GetClass (base.Type.TypeName);
			if (cls == null)
				return tag;
			
			//look up the control prefix
			MonoDevelop.AspNet.Parser.AspNetCompilationUnit cu = 
				ctx.GetParseInformation (path).BestCompilationUnit
					as MonoDevelop.AspNet.Parser.AspNetCompilationUnit;
			string prefix = cu.Document.ReferenceManager.AddReference (cls);
			
			if (prefix != null)
				return string.Format (tag, prefix);
			return tag;
		}
		
		[Browsable(false)]
		public override string ItemDomain {
			get { return aspNetDomain; }
		}
		
		public bool IsCompatibleWith (string fileName, MonoDevelop.Projects.Project project)
		{
			if (fileName.EndsWith (".aspx") || fileName.EndsWith (".ascx") || fileName.EndsWith (".master"))
				return true;
			else
				return false;
		}
	}
}
