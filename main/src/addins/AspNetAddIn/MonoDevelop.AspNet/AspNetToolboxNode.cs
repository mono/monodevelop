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
			MonoDevelop.Projects.DotNetProject dnp = (MonoDevelop.Projects.DotNetProject) project;
			MonoDevelop.Projects.ProjectReference pr = base.Type.GetProjectReference ();
			
			//add the reference if it doesn't match an existing one
			bool match = false;
			foreach (MonoDevelop.Projects.ProjectReference p in dnp.References)
				if (p.Equals (pr))
					match = true;
			if (!match)
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
			
			MonoDevelop.Projects.Dom.Parser.ProjectDom database =
				MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetDatabaseProjectDom (project);
//FIXME: port to new DOM
//			ctx.UpdateDatabase ();
			MonoDevelop.Projects.Dom.IType cls = database.GetType (Type.TypeName, 0, true, true);
			if (cls == null)
				return tag;
			
			//look up the control prefix
			string mime = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (path);
			MonoDevelop.AspNet.Parser.AspNetCompilationUnit cu = 
				MonoDevelop.Projects.Dom.Parser.ProjectDomService.Parse (project, path, mime).CompilationUnit
					as MonoDevelop.AspNet.Parser.AspNetCompilationUnit;
			
			System.Reflection.AssemblyName assemName = 
				MonoDevelop.Core.Runtime.SystemAssemblyService.ParseAssemblyName (Type.AssemblyName);
			
			string prefix = cu.Document.ReferenceManager.AddAssemblyReferenceToDocument (cls, assemName.Name);
			
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
			ClrVersion clrVersion = ClrVersion.Net_2_0;
			MonoDevelop.Projects.DotNetProject dnp = project as MonoDevelop.Projects.DotNetProject;
			if (dnp != null && dnp.ClrVersion != ClrVersion.Default)
				clrVersion = dnp.ClrVersion;
			
			bool allow = false;
			foreach (ToolboxItemFilterAttribute tbfa in ItemFilters) {
				if (tbfa.FilterString == "ClrVersion.Net_1_1") {
					if (tbfa.FilterType == ToolboxItemFilterType.Require) {
						if (clrVersion == ClrVersion.Net_1_1 || clrVersion == ClrVersion.Net_2_0)
							allow = true;
					} else if (tbfa.FilterType == ToolboxItemFilterType.Prevent) {
						if (clrVersion == ClrVersion.Net_1_1)
							return false;
					}
				} else if (tbfa.FilterString == "ClrVersion.Net_2_0") {
					if (tbfa.FilterType == ToolboxItemFilterType.Require) {
						if (clrVersion == ClrVersion.Net_2_0)
							allow = true;
					} else if (tbfa.FilterType == ToolboxItemFilterType.Prevent) {
						if (clrVersion == ClrVersion.Net_2_0)
							return false;
					}
				}
			}
			if (!allow)
				return false;
			
			if (fileName.EndsWith (".aspx") || fileName.EndsWith (".ascx") || fileName.EndsWith (".master"))
				return true;
			else
				return false;
		}
	}
}
