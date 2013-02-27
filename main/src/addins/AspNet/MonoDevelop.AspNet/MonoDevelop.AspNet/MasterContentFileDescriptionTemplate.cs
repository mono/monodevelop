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
using System.Collections.Generic;
using MonoDevelop.AspNet.Gui;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet
{
	public class MasterContentFileDescriptionTemplate : AspNetFileDescriptionTemplate
	{
		public override void ModifyTags (MonoDevelop.Projects.SolutionItem policyParent, MonoDevelop.Projects.Project project, string language, string identifier, string fileName, ref Dictionary<string,string> tags)
		{
			base.ModifyTags (policyParent, project, language, identifier, fileName, ref tags);
			if (fileName == null)
				return;
			
			tags ["AspNetMaster"] = "";
			tags ["AspNetMasterContent"] = "";
			
			AspNetAppProject aspProj = project as AspNetAppProject;
			if (aspProj == null)
				throw new InvalidOperationException ("MasterContentFileDescriptionTemplate is only valid for ASP.NET projects");
			
			ProjectFile masterPage = null;
			
			var dialog = new MonoDevelop.Ide.Projects.ProjectFileSelectorDialog (aspProj, null, "*.master");
			try {
				dialog.Title = GettextCatalog.GetString ("Select a Master Page...");
				int response = MonoDevelop.Ide.MessageService.RunCustomDialog (dialog);
				if (response == (int)Gtk.ResponseType.Ok)
					masterPage = dialog.SelectedFile;
			} finally {
				dialog.Destroy ();
			}
			if (masterPage == null)
				return;
			
			tags ["AspNetMaster"] = aspProj.LocalToVirtualPath (masterPage);
			
			try {
				var pd = TypeSystemService.ParseFile (project, masterPage.FilePath)
						as AspNetParsedDocument;
				if (pd == null)
					return;
				
				//var visitor = new ContentPlaceHolderVisitor ();
				//pd.RootNode.AcceptVisit (visitor);
				
				List<string> placeHolderIds = new List<string> ();
				BuildPlaceholderList (placeHolderIds, pd.XDocument);
				
				var sb = new System.Text.StringBuilder ();
				foreach (string id in placeHolderIds) {
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
				LoggingService.LogWarning ("Error generating AspNetMasterContent for template", ex);
			}
		}
		
		void BuildPlaceholderList (List<string> placeHolderIds, XDocument xDoc)
		{
			AddPlaceholderElement (placeHolderIds, xDoc.RootElement);
		}
		
		void AddPlaceholderElement (List<string> list, XElement el)
		{
			if (0 == string.Compare (el.Name.FullName, "asp:ContentPlaceHolder", true)) {
				string id = string.Empty;
				
				foreach (XAttribute att in el.Attributes) {
					if (0 == string.Compare (att.Name.Name, "id", true)) {
						id = att.Value;
						break;
					}	
				}
				
				if (id != string.Empty)
					list.Add (id);
			}
			
			foreach (XNode node in el.Nodes) {
				if (node is XElement)
					AddPlaceholderElement (list, node as XElement);
			}
		}
	}
}
