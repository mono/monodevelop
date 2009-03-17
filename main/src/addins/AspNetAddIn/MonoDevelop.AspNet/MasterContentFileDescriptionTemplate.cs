// 
// MasterContentFileDescriptionTemplate.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.AspNet.Gui;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.AspNet
{
	
	
	public class MasterContentFileDescriptionTemplate : AspNetFileDescriptionTemplate
	{
		
		public override void ModifyTags (MonoDevelop.Projects.SolutionItem policyParent, MonoDevelop.Projects.Project project, string language, string identifier, string fileName, ref System.Collections.Hashtable tags)
		{
			base.ModifyTags (policyParent, project, language, identifier, fileName, ref tags);
			if (fileName == null)
				return;
			
			tags["AspNetMaster"] = "";
			tags["AspNetMasterContent"] = "";
			
			AspNetAppProject aspProj = project as AspNetAppProject;
			if (aspProj == null)
				throw new InvalidOperationException ("MasterContentFileDescriptionTemplate is only valid for ASP.NET projects");
			
			ProjectFile masterPage = null;
			string masterContent = "";
			
			using (AspNetFileSelector dialog = new AspNetFileSelector (aspProj, null, "*.master")) {
				dialog.Title = GettextCatalog.GetString ("Select a Master Page...");
				int response = MonoDevelop.Core.Gui.MessageService.ShowCustomDialog (dialog);
				if (response == (int)Gtk.ResponseType.Ok)
					masterPage = dialog.SelectedFile;
			}
			if (masterPage == null)
				return;
			
			tags["AspNetMaster"] = aspProj.LocalToVirtualPath (masterPage);
			
			try {
				AspNetParsedDocument pd	= MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetParsedDocument (MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom (project), masterPage.FilePath)
						as AspNetParsedDocument;
				if (pd == null)
					return;
				
				ContentPlaceHolderVisitor visitor = new ContentPlaceHolderVisitor ();
				pd.Document.RootNode.AcceptVisit (visitor);
				
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				foreach (string id in visitor.PlaceHolders) {
					sb.Append ("<asp:Content ContentPlaceHolderID=\"");
					sb.Append (id);
					sb.Append ("\" ID=\"");
					sb.Append (id);
					sb.Append ("Content\" runat=\"server\">\n</asp:Content>\n");
				}
				
				tags["AspNetMasterContent"] = sb.ToString ();
			}
			catch (Exception ex) {
				//no big loss if we just insert blank space
				//it's just a template for the user to start editing
			}
		}
	}
}
