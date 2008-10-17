// 
// AspNetEditorExtension.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;

//I initially aliased this as SE, which (unintentionally) looked a little odd with the XDOM types :-)
using S = MonoDevelop.Xml.StateEngine; 
using MonoDevelop.AspNet.StateEngine;

namespace MonoDevelop.AspNet.Gui
{
	
	
	public class AspNetEditorExtension : HtmlEditorExtension
	{
		AspNetParsedDocument AspCU { get { return CU as AspNetParsedDocument; } }
		Document AspDocument { get { return AspCU == null? null : AspCU.Document; } }
		
		#region Setup and teardown
		
		protected override IEnumerable<string> SupportedExtensions {
			get {
				yield return ".aspx";
				yield return ".ascx";
				yield return ".master";
			}
		}
		
		protected override S.RootState CreateRootState ()
		{
			return new AspNetFreeState ();
		}
		
		#endregion
		
		protected override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
		                                                            bool forced, ref int triggerWordLength)
		{
			//FIXME: lines in completionContext are zero-indexed, but ILocation and buffer are 1-indexed.
			//This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			ITextBuffer buf = this.Buffer;
			
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			int currentPosition = buf.CursorPosition - 1;
			char currentChar = currentPosition < 0? ' ' : buf.GetCharAt (currentPosition);
			char previousChar = currentPosition < 1? ' ' : buf.GetCharAt (currentPosition - 1);
			
			//directive names
			if (Tracker.Engine.CurrentState is AspNetDirectiveState) {
				AspNetDirective directive = Tracker.Engine.Nodes.Peek () as AspNetDirective;
				if (directive != null && currentChar == ' ' && directive.Position.Start + 4 == buf.CursorPosition) {
					return DirectiveCompletion.GetDirectives (AspCU.Type);
				}
				return null;
			} else if (Tracker.Engine.CurrentState is S.XmlNameState && Tracker.Engine.CurrentState.Parent is AspNetDirectiveState) {
				AspNetDirective directive = Tracker.Engine.Nodes.Peek () as AspNetDirective;
				if (directive != null && directive.Position.Start + 5 == buf.CursorPosition && char.IsLetter (currentChar)) {
					triggerWordLength = 1;
					return DirectiveCompletion.GetDirectives (AspCU.Type);
				}
				return null;
			}
			
			//non-xml tag completion
			if (currentChar == '<' && !(Tracker.Engine.CurrentState is S.XmlFreeState) ) {
				CompletionDataList list = new CompletionDataList ();
				AddAspBeginExpressions (list, AspDocument);
				return list;
			}
			
			//simple completion for ASP.NET expressions
			if (Tracker.Engine.CurrentState is AspNetExpressionState
			    && previousChar == ' ' && char.IsLetter (currentChar))
			{
				AspNetExpression expr = Tracker.Engine.Nodes.Peek () as AspNetExpression;
				CompletionDataList list = HandleExpressionCompletion (expr);
				if (list != null && !forced)
					triggerWordLength = 1;
				return list;
			}
			
			DocType = AspCU != null ? AspCU.PageInfo.DocType : null;
			
			return base.HandleCodeCompletion (completionContext, forced, ref triggerWordLength);
		}
		
		protected override void GetElementCompletions (CompletionDataList list)
		{
			S.XName parentName = GetParentElementName (0);
			AddAspTags (list, AspDocument, parentName);
			AddAspBeginExpressions (list, AspDocument);
			base.GetElementCompletions (list);
		}
		
		protected override void GetAttributeCompletions (CompletionDataList list, S.IAttributedXObject attributedOb,
		                                                 Dictionary<string, string> existingAtts)
		{
			base.GetAttributeCompletions (list, attributedOb, existingAtts);
			if (attributedOb is S.XElement) {
				if (attributedOb.Name.HasPrefix) {
					AddAspAttributeCompletionData (list, AspDocument, attributedOb.Name, existingAtts);
				}
				if (!existingAtts.ContainsKey ("runat"))
					list.Add ("runat=\"server\"", "md-literal",
						GettextCatalog.GetString ("Required for ASP.NET controls.\n") +
						GettextCatalog.GetString (
							"Indicates that this tag should be able to be\n" +
							"manipulated programmatically on the web server."));
				
				if (!existingAtts.ContainsKey ("id"))
					list.Add ("id", "md-literal",
						GettextCatalog.GetString ("Unique identifier.\n") +
						GettextCatalog.GetString (
							"An identifier that is unique within the document.\n" + 
							"If the tag is a server control, this will be used \n" +
							"for the corresponding variable name in the CodeBehind."));
				
			}  else if (attributedOb is AspNetDirective) {
				//FIXME: use correct ClrVersion
				DirectiveCompletion.GetAttributes (list, attributedOb.Name.FullName, ClrVersion.Net_2_0, existingAtts);
			}
		}
		
		protected override void GetAttributeValueCompletions (CompletionDataList list, S.IAttributedXObject ob, S.XAttribute att)
		{
			base.GetAttributeValueCompletions (list, ob, att);
			if (ob is S.XElement) {
				if (ob.Name.HasPrefix) {
					S.XAttribute idAtt = ob.Attributes[new S.XName ("id")];
					string id = idAtt == null? null : idAtt.Value;
					if (string.IsNullOrEmpty (id) || string.IsNullOrEmpty (id.Trim ()))
						id = null;
					AddAspAttributeValueCompletionData (list, AspCU, ob.Name, att.Name, id);
				}
			} else if (ob is AspNetDirective) {
				//FIXME: use correct ClrVersion
				DirectiveCompletion.GetAttributeValues (list, ob.Name.FullName, att.Name.FullName, ClrVersion.Net_2_0);
			}
		}
		
		CompletionDataList HandleExpressionCompletion (AspNetExpression expr)
		{
			if (!(expr is AspNetDataBindingExpression || expr is AspNetRenderExpression))
				return null;
			
			MonoDevelop.Projects.Dom.IType codeBehindClass;
			MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase;
			GetCodeBehind (AspCU, out codeBehindClass, out projectDatabase);
			
			if (codeBehindClass == null)
				return null;
			
			//list just the class's properties, not properties on base types
			CompletionDataList list = new CompletionDataList ();
			list.AddRange (from p in codeBehindClass.Properties
				where p.IsProtected || p.IsPublic
				select new CompletionData (p.Name, "md-property"));
			list.AddRange (from p in codeBehindClass.Fields
				where p.IsProtected || p.IsPublic
				select new CompletionData (p.Name, "md-property"));
			
			return list.Count > 0? list : null;
		}
		
		static void GetCodeBehind (AspNetParsedDocument cu,
		                           out MonoDevelop.Projects.Dom.IType codeBehindClass,
		                           out MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase)
		{
			codeBehindClass = null;
			projectDatabase = null;
			
			if (cu != null && cu.Document.Project != null) {
				projectDatabase = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom
					(cu.Document.Project);
				
				if (projectDatabase != null && !string.IsNullOrEmpty (cu.PageInfo.InheritedClass))
					codeBehindClass = projectDatabase.GetType (cu.PageInfo.InheritedClass, false, false);
			}
		}
		
		#region ASP.NET data
		
		static void AddAspBeginExpressions (CompletionDataList list, Document doc)
		{
			list.Add ("%",  "md-literal", GettextCatalog.GetString ("ASP.NET render block"));
			list.Add ("%=", "md-literal", GettextCatalog.GetString ("ASP.NET render expression"));
			list.Add ("%@", "md-literal", GettextCatalog.GetString ("ASP.NET directive"));
			list.Add ("%#", "md-literal", GettextCatalog.GetString ("ASP.NET databinding expression"));
			list.Add ("%--", "md-literal", GettextCatalog.GetString ("ASP.NET server-side comment"));
			
			//valid on 2.0 runtime only
			if (doc.Project == null || doc.Project.ClrVersion == ClrVersion.Net_2_0
			    || doc.Project.ClrVersion == ClrVersion.Default) {
				list.Add ("%$", "md-literal", GettextCatalog.GetString ("ASP.NET resource expression"));
			}
		}
		
		static void AddAspTags (CompletionDataList list, Document doc, S.XName parentName)
		{
			foreach (MonoDevelop.Projects.Dom.IType cls in doc.ReferenceManager.ListControlClasses ())
				list.Add ("asp:" + cls.Name, Gtk.Stock.GoForward, cls.Documentation);
		}
		
		static void AddAspAttributeCompletionData (CompletionDataList list,
		    Document doc, S.XName name, Dictionary<string, string> existingAtts)
		{
			Debug.Assert (name.IsValid);
			Debug.Assert (name.HasPrefix);
			Debug.Assert (doc != null);
			
			//get a parser database
			MonoDevelop.Projects.Dom.Parser.ProjectDom database = null;
			
			if (doc.Project != null)
				database = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom (doc.Project);
			else
				database = WebTypeManager.GetSystemWebDom (null);
			
			if (database == null) {
				LoggingService.LogWarning ("Could not obtain project DOM in AddAspAttributeCompletionData");
				return;
			}
			
			MonoDevelop.Projects.Dom.IType controlClass = doc.ReferenceManager.GetControlType (name.Prefix, name.Name);
			if (controlClass == null) {
				controlClass = database.GetType ("System.Web.UI.WebControls.WebControl");
				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			AddControlMembers (list, database, doc, controlClass, existingAtts);
		}
		
		static void AddControlMembers (CompletionDataList list,
		    MonoDevelop.Projects.Dom.Parser.ProjectDom database, Document doc,
		    MonoDevelop.Projects.Dom.IType controlClass, Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (MonoDevelop.Projects.Dom.IProperty prop 
			    in GetUniqueMembers<MonoDevelop.Projects.Dom.IProperty> (GetAllProperties (database, controlClass)))
				if (prop.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					list.Add (prop.Name, prop.StockIcon, prop.Documentation);
			
			//similarly add events
			foreach (MonoDevelop.Projects.Dom.IEvent eve 
			    in GetUniqueMembers<MonoDevelop.Projects.Dom.IEvent> (GetAllEvents (database, controlClass))) {
				string eveName = "On" + eve.Name;
				if (eve.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					list.Add (eveName, eve.StockIcon, eve.Documentation);
			}
		}
		
		static void AddAspAttributeValueCompletionData (CompletionDataList list,
		    MonoDevelop.AspNet.Parser.AspNetParsedDocument cu, S.XName tagName, S.XName attName, string id)
		{
			Debug.Assert (tagName.IsValid && tagName.HasPrefix);
			Debug.Assert (attName.IsValid && !attName.HasPrefix);
			
			MonoDevelop.Projects.Dom.IType controlClass = null;
			if (cu != null)
				controlClass = cu.Document.ReferenceManager.GetControlType (tagName.Prefix, tagName.Name);
			
			if (controlClass == null) {
				MonoDevelop.Projects.Dom.Parser.ProjectDom database =
					WebTypeManager.GetSystemWebDom (cu == null? null : cu.Document.Project);
				controlClass = database.GetType ("System.Web.UI.WebControls.WebControl", true, false);
				
				if (controlClass == null)
					LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
				return;
			}
			
			//find the codebehind class
			MonoDevelop.Projects.Dom.IType codeBehindClass;
			MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase;
			GetCodeBehind (cu, out codeBehindClass, out projectDatabase);
			
			//if it's an event, suggest compatible methods 
			if (codeBehindClass != null && attName.Name.StartsWith ("On")) {
				string eventName = attName.Name.Substring (2);
				
				foreach (MonoDevelop.Projects.Dom.IEvent ev in GetAllEvents (projectDatabase, controlClass)) {
					if (ev.Name == eventName) {
						System.CodeDom.CodeMemberMethod domMethod = 
							BindingService.MDDomToCodeDomMethod (ev, projectDatabase);
						if (domMethod == null)
							return;
						
						foreach (string meth 
						    in BindingService.GetCompatibleMethodsInClass (codeBehindClass, domMethod))
						{
							list.Add (meth, "md-method",
							    GettextCatalog.GetString ("A compatible method in the CodeBehind class"));
						}
						
						string suggestedIdentifier = ev.Name;
						if (id != null) {
							suggestedIdentifier = id + "_" + suggestedIdentifier;
						} else {
							suggestedIdentifier = tagName.Name + "_" + suggestedIdentifier;
						}
							
						domMethod.Name = BindingService.GenerateIdentifierUniqueInClass
							(projectDatabase, codeBehindClass, suggestedIdentifier);
						domMethod.Attributes |= System.CodeDom.MemberAttributes.Family;
						list.Add (
						    new SuggestedHandlerCompletionData (cu.Document.Project, domMethod, codeBehindClass,
						        MonoDevelop.AspNet.CodeBehind.GetNonDesignerClass (codeBehindClass))
						    );
						return;
					}
				}
			}
			
			if (projectDatabase == null) {
				projectDatabase = WebTypeManager.GetSystemWebDom (cu == null? null : cu.Document.Project);
				
				if (projectDatabase == null) {
					LoggingService.LogWarning ("Could not obtain type database in AddAspAttributeCompletionData");
					return;
				}
			}
			
			//if it's a property and is an enum or bool, suggest valid values
			foreach (MonoDevelop.Projects.Dom.IProperty prop in GetAllProperties (projectDatabase, controlClass)) {
				if (prop.Name != attName.Name)
					continue;
				
				//boolean completion
				if (prop.ReturnType.FullName == "System.Boolean") {
					AddBooleanCompletionData (list);
					return;
				}
				
				//color completion
				if (prop.ReturnType.FullName == "System.Drawing.Color") {
					System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter ();
					foreach (System.Drawing.Color c in conv.GetStandardValues (null)) {
						if (c.IsSystemColor)
							continue;
						string hexcol = string.Format ("#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
						list.Add (c.Name, hexcol);
					}
					return;
				}
				
				//enum completion
				MonoDevelop.Projects.Dom.IType retCls = projectDatabase.GetType (prop.ReturnType);
				if (retCls != null && retCls.ClassType == MonoDevelop.Projects.Dom.ClassType.Enum) {
					foreach (MonoDevelop.Projects.Dom.IField enumVal in retCls.Fields)
						if (enumVal.IsPublic && enumVal.IsStatic)
							list.Add (enumVal.Name, "md-literal", enumVal.Documentation);
					return;
				}
			}
		}
		
		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : MonoDevelop.Projects.Dom.IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		static IEnumerable<MonoDevelop.Projects.Dom.IProperty> GetAllProperties (
		    MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase,
		    MonoDevelop.Projects.Dom.IType cls)
		{
			foreach (MonoDevelop.Projects.Dom.IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (MonoDevelop.Projects.Dom.IProperty prop in type.Properties)
					yield return prop;
		}
		
		static IEnumerable<MonoDevelop.Projects.Dom.IEvent> GetAllEvents (
		    MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase,
		    MonoDevelop.Projects.Dom.IType cls)
		{
			foreach (MonoDevelop.Projects.Dom.IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (MonoDevelop.Projects.Dom.IEvent ev in type.Events)
					yield return ev;
		}
		
		static void AddBooleanCompletionData (CompletionDataList list)
		{
			list.Add ("true", "md-literal");
			list.Add ("false", "md-literal");
		}
		
		#endregion
		
		#region Document outline
		
		protected override void RefillOutlineStore (MonoDevelop.Projects.Dom.ParsedDocument doc, Gtk.TreeStore store)
		{
			ParentNode p = ((AspNetParsedDocument)doc).Document.RootNode;
//			Gtk.TreeIter iter = outlineTreeStore.AppendValues (System.IO.Path.GetFileName (CU.Document.FilePath), p);
			BuildTreeChildren (store, Gtk.TreeIter.Zero, p);
		}
		
		protected override void InitializeOutlineColumns (Gtk.TreeView outlineTree)
		{
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			crt.Xpad = 0;
			crt.Ypad = 0;
			outlineTree.AppendColumn ("Node", crt, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
		}
		
		protected override void OutlineSelectionChanged (object selection)
		{
			SelectNode ((Node)selection);
		}
		
		static void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, ParentNode p)
		{
			foreach (Node n in p) {
				if ( !(n is TagNode || n is DirectiveNode || n is ExpressionNode))
					continue;
				Gtk.TreeIter childIter;
				if (!parent.Equals (Gtk.TreeIter.Zero))
					childIter = store.AppendValues (parent, n);
				else
					childIter = store.AppendValues (n);
				ParentNode pChild = n as ParentNode;
				if (pChild != null)
					BuildTreeChildren (store, childIter, pChild);
			}
		}
		
		void outlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText) cell;
			Node n = (Node) model.GetValue (iter, 0);
			string name = null;
			if (n is TagNode) {
				TagNode tn = (TagNode) n;
				name = tn.TagName;
				string att = tn.Attributes["id"] as string;
				if (att != null)
					name = "<" + name + "#" + att + ">";
				else
					name = "<" + name + ">";
			} else if (n is DirectiveNode) {
				DirectiveNode dn = (DirectiveNode) n;
				name = "<%@ " + dn.Name + " %>";
			} else if (n is ExpressionNode) {
				ExpressionNode en = (ExpressionNode) n;
				string expr = en.Expression;
				if (string.IsNullOrEmpty (expr)) {
					name = "<% %>";
				} else {
					if (expr.Length > 10)
						expr = expr.Substring (0, 10) + "...";
					name = "<% " + expr + "%>";
				}
			}
			if (name != null)
				txtRenderer.Text = name;
		}
		
		void SelectNode (Node n)
		{
			ILocation start = n.Location, end;
			TagNode tn = n as TagNode;
			if (tn != null && tn.EndLocation != null)
				end = tn.EndLocation;
			else
				end = start;
			
			//FIXME: why is this offset necessary?
			int offset = n is TagNode? 1 : 0;
			
			int s = Editor.GetPositionFromLineColumn (start.BeginLine, start.BeginColumn + offset);
			int e = Editor.GetPositionFromLineColumn (end.EndLine, end.EndColumn + offset);
			if (e > s && s > -1)
				Editor.Select (s, e);
		}
		
		#endregion
		
	}
}
