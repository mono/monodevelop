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
	
	
	public class AspNetEditorExtension : MonoDevelop.XmlEditor.Gui.BaseXmlEditorExtension
	{
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
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			//ensure that the schema service is initialised, or code completion may take a couple of seconds to trigger
			HtmlSchemaService.Initialise ();
		}
		
		#endregion
		
		#region Convenience accessors
		
		protected ITextBuffer Buffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<ITextBuffer> ();
			}
		}
		
		protected IEditableTextBuffer EditableBuffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<IEditableTextBuffer> ();
			}
		}
		
		#endregion
			
		public override ICompletionDataProvider CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			int triggerWordLength = 0;
			ICompletionDataProvider cp = null;
			if (txt.Length > 0)
				cp = HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
			
			return cp;
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (
		    ICodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			int pos = completionContext.TriggerOffset;
			if (pos > 0 && Editor.GetCharAt (pos - 1) == completionChar) {
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, false, ref triggerWordLength);
			}
			return null;
		}
		
		ICompletionDataProvider HandleCodeCompletion (
		    CodeCompletionContext completionContext, bool forced, ref int triggerWordLength)
		{
			Tracker.UpdateEngine ();
			MonoDevelop.AspNet.Parser.AspNetParsedDocument CU
				= (MonoDevelop.AspNet.Parser.AspNetParsedDocument) this.CU;
			
			//FIXME: these may be null at startup, but we should still provive some completion
			if (CU == null || CU.Document == null)
				return null;
			
			//FIXME: lines in completionContext are zero-indexed, but ILocation and buffer are 1-indexed.
			//This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			ITextBuffer buf = this.Buffer;
			
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			int currentPosition = buf.CursorPosition - 1;
			char currentChar = buf.GetCharAt (currentPosition);
			char previousChar = buf.GetCharAt (currentPosition - 1);
			
			LoggingService.LogDebug ("Attempting ASP.NET completion for state '{0}'x{1}, previousChar='{2}'," 
				+ " currentChar='{3}', forced='{4}'", Tracker.Engine.CurrentState, Tracker.Engine.CurrentStateLength,
				previousChar, currentChar, forced);
			
			//doctype completion
			if (line <= 5 && currentChar ==' ' && previousChar == 'E') {
				int start = currentPosition - 9;
				if (start >= 0) {
					string readback = Buffer.GetText (start, currentPosition);
					if (string.Compare (readback, "<!DOCTYPE", System.StringComparison.InvariantCulture) == 0)
						return new DocTypeCompletionDataProvider ();
				}
			}
			
			//decide whether completion will be auto-activated, to avoid unnecessary
			//parsing, which hurts editor responsiveness
			if (!forced) {
				//
				if (Tracker.Engine.CurrentState is S.XmlFreeState && !(currentChar == '<' || currentChar == '>'))
					return null;
				
				if (Tracker.Engine.CurrentState is S.XmlNameState 
				    && Tracker.Engine.CurrentState.Parent is S.XmlAttributeState && previousChar != ' ')
					return null;
				
				if (Tracker.Engine.CurrentState is S.XmlAttributeValueState 
				    && !(previousChar == '\'' || previousChar == '"' || currentChar =='\'' || currentChar == '"'))
					return null;
			}
			
			//lazily load the schema to avoid a multi-second interruption when a schema
			//is first used. While loading, fall back to the default schema (which is pre-loaded) so that
			//the user still gets completion
			HtmlSchema schema = null;
			string doctype = CU != null ? CU.PageInfo.DocType : null;
			if (!string.IsNullOrEmpty (doctype)) {
				schema = HtmlSchemaService.GetSchema (doctype, true);
			}
			if (schema == null)
				schema = HtmlSchemaService.DefaultDocType;
			LoggingService.LogDebug ("AspNetCompletion using completion for doctype {0}", schema.Name);
			
			//determine the node at the current location
//			Node n = doc.RootNode.GetNodeAtPosition (line, col);
//			LoggingService.LogDebug ("AspNetCompletion({0},{1}): {2}", line, col, n==null? "(not found)" : n.ToString ());
			
			//tag completion
			if (currentChar == '<') {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				
				if (Tracker.Engine.CurrentState is S.XmlFreeState) {
					
					S.XElement el = Tracker.Engine.Nodes.Peek () as S.XElement;
					S.XName parentName = (el != null && el.IsNamed)? el.Name : new S.XName ();
					
					AddHtmlTagCompletionData (cp, schema, parentName);
					AddHtmlMiscBegins (cp);
					AddAspTags (cp, CU == null? null : CU.Document, parentName);
					AddCloseTag (cp, Tracker.Engine.Nodes);
					
//						if (line < 3) {
//						cp.AddCompletionData (new CodeCompletionData ("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>"));
				
					if (line < 4 && string.IsNullOrEmpty (doctype))
						cp.AddCompletionData (new CodeCompletionData ("!DOCTYPE", "md-literal"));
				}
				
				AddAspBeginExpressions (cp, CU == null? null : CU.Document);
				return cp;
			}
			
			//closing tag completion
			if (Tracker.Engine.CurrentState is S.XmlFreeState && currentPosition - 1 > 0 && currentChar == '>') {
				//get name of current node in document that's being ended
				S.XElement el = Tracker.Engine.Nodes.Peek () as S.XElement;
				if (el != null && el.Position.End >= currentPosition && !el.IsClosed && el.IsNamed) {
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					cp.AddCompletionData (
					    new MonoDevelop.XmlEditor.Completion.XmlTagCompletionData (
					        String.Concat ("</", el.Name.FullName, ">"), 0, true)
					    );
					return cp;
				}
			}
			
			
			//directive attribute completion
			if (Tracker.Engine.CurrentState is AspNetDirectiveState && forced || 
				(Tracker.Engine.CurrentState is S.XmlNameState 
			 	 && Tracker.Engine.CurrentState.Parent is S.XmlAttributeState
			     && Tracker.Engine.CurrentState.Parent.Parent is AspNetDirectiveState
			         && Tracker.Engine.CurrentStateLength == 1)
			) {
				AspNetDirective dir = (Tracker.Engine.CurrentState is AspNetDirectiveState)?
					(AspNetDirective) Tracker.Engine.Nodes.Peek () :
					(AspNetDirective) Tracker.Engine.Nodes.Peek (1);
				System.Console.WriteLine(dir.Name.FullName);
				//attributes
				if (dir != null && dir.Name.IsValid && (forced ||
					(char.IsWhiteSpace (previousChar) && char.IsLetter (currentChar))))
				{
					//if triggered by first letter of value, grab that letter
					if (Tracker.Engine.CurrentStateLength == 1)
						triggerWordLength = 1;
					
					return DirectiveCompletion.GetAttributes (dir.Name.FullName, ClrVersion.Default);
				}
			}	
			
			//attributes names within tags
			if (Tracker.Engine.CurrentState is S.XmlTagState && forced || 
				(Tracker.Engine.CurrentState is S.XmlNameState 
			 	 && Tracker.Engine.CurrentState.Parent is S.XmlAttributeState
			         && Tracker.Engine.CurrentStateLength == 1)
			) {
				S.XElement el = (Tracker.Engine.CurrentState is S.XmlTagState)?
					(S.XElement) Tracker.Engine.Nodes.Peek () :
					(S.XElement) Tracker.Engine.Nodes.Peek (1);
				
				//attributes
				if (el != null && el.Name.IsValid && (forced ||
					(char.IsWhiteSpace (previousChar) && char.IsLetter (currentChar))))
				{
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					if (!forced)
						triggerWordLength = 1;
					
					Dictionary<string, string> existingAtts = new Dictionary<string,string>
						(StringComparer.InvariantCultureIgnoreCase);
					
					foreach (S.XAttribute att in el.Attributes) {
						existingAtts [att.Name.FullName] = att.Value ?? string.Empty;
					}
					
					if (el.Name.HasPrefix) {
						AddAspAttributeCompletionData (cp, CU == null? null : CU.Document, el.Name, existingAtts);
					} else {
						AddHtmlAttributeCompletionData (cp, schema, el.Name, existingAtts);
					}
					
					if (!existingAtts.ContainsKey ("runat"))
						cp.AddCompletionData (new CodeCompletionData ("runat=\"server\"", "md-literal",
						    GettextCatalog.GetString ("Required for ASP.NET controls.\n") +
						    GettextCatalog.GetString (
						        "Indicates that this tag should be able to be\n" +
						        "manipulated programmatically on the web server.")
						    ));
					
					if (!existingAtts.ContainsKey ("id"))
						cp.AddCompletionData (
						    new CodeCompletionData ("id", "md-literal",
						        GettextCatalog.GetString ("Unique identifier.\n") +
						        GettextCatalog.GetString (
						            "An identifier that is unique within the document.\n" + 
						            "If the tag is a server control, this will be used \n" +
						            "for the corresponding variable name in the CodeBehind.")
						    ));
					return cp;
				}
			}
			
			//attribute values
			//determine whether to trigger completion within attribute values quotes
			if ((Tracker.Engine.CurrentState is S.XmlDoubleQuotedAttributeValueState
			    || Tracker.Engine.CurrentState is S.XmlSingleQuotedAttributeValueState)
			    //trigger on the opening quote
			    && (Tracker.Engine.CurrentStateLength == 0
			        //or trigger on first letter of value, if unforced
			        || (!forced && Tracker.Engine.CurrentStateLength == 1))
			    ) {
				S.XAttribute att = (S.XAttribute) Tracker.Engine.Nodes.Peek ();
				
				if (att.IsNamed) {
					S.XElement el = (S.XElement) Tracker.Engine.Nodes.Peek (1);
					
					char next = ' ';
					if (currentPosition + 1 < buf.Length)
						next = buf.GetCharAt (currentPosition + 1);
					
					char compareChar = (Tracker.Engine.CurrentStateLength == 0)? currentChar : previousChar;
					
					if ((compareChar == '"' || compareChar == '\'') 
					    && (next == compareChar || char.IsWhiteSpace (next))
					) {
						//if triggered by first letter of value, grab that letter
						if (Tracker.Engine.CurrentStateLength == 1)
							triggerWordLength = 1;
						
						CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
						if (el.Name.HasPrefix)
							AddAspAttributeValueCompletionData (cp, CU, el.Name, att.Name, null);
						else
							AddHtmlAttributeValueCompletionData (cp, schema, el.Name, att.Name);
							
						return cp;
					}
				}
			}
			
			return null; 
		}
		
		#region HTML data
		
		void AddHtmlTagCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, S.XName parentName)
		{
			if (schema == null || schema.CompletionProvider == null)
				return;
			
			ICompletionData[] data = null;
			if (parentName.IsValid) {
				data = schema.CompletionProvider.GetChildElementCompletionData (parentName.FullName);
			} else {
				data = schema.CompletionProvider.GetElementCompletionData ();
			}
			
			foreach (ICompletionData datum in data)
				provider.AddCompletionData (datum);			
		}
		
		void AddHtmlAttributeCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, 
		    S.XName tagName, Dictionary<string, string> existingAtts)
		{
			Debug.Assert (tagName.IsValid);
			Debug.Assert (schema != null);
			Debug.Assert (schema.CompletionProvider != null);
			
			//add atts only if they're not aready in the tag
			foreach (ICompletionData datum in schema.CompletionProvider.GetAttributeCompletionData (tagName.FullName))
				if (existingAtts == null || !existingAtts.ContainsKey (datum.Text[0]))
					provider.AddCompletionData (datum);
		}
		
		void AddHtmlAttributeValueCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, 
		    S.XName tagName, S.XName attributeName)
		{
			Debug.Assert (tagName.IsValid);
			Debug.Assert (attributeName.IsValid);
			Debug.Assert (schema.CompletionProvider != null);
			
			foreach (ICompletionData datum 
			    in schema.CompletionProvider.GetAttributeValueCompletionData (tagName.FullName, attributeName.FullName))
			{
				provider.AddCompletionData (datum);
			}
		}
		
		static void AddHtmlMiscBegins (CodeCompletionDataProvider provider)
		{
			provider.AddCompletionData (
			    new CodeCompletionData ("!--",  "md-literal", GettextCatalog.GetString ("Comment"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("![CDATA[", "md-literal", GettextCatalog.GetString ("Character data"))
			    );
		}
		
		#endregion
		
		#region ASP.NET data
		
		static void AddAspBeginExpressions (CodeCompletionDataProvider provider, Document doc)
		{
			provider.AddCompletionData (
			    new CodeCompletionData ("%",  "md-literal", GettextCatalog.GetString ("ASP.NET render block"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%=", "md-literal", GettextCatalog.GetString ("ASP.NET render expression"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%@", "md-literal", GettextCatalog.GetString ("ASP.NET directive"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%#", "md-literal", GettextCatalog.GetString ("ASP.NET databinding expression"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%--", "md-literal", GettextCatalog.GetString ("ASP.NET server-side comment"))
			    );
			
			//valid on 2.0 runtime only
			if (doc.Project == null || doc.Project.ClrVersion == ClrVersion.Net_2_0
			    || doc.Project.ClrVersion == ClrVersion.Default) {
				provider.AddCompletionData (
				    new CodeCompletionData ("%$", "md-literal", GettextCatalog.GetString ("ASP.NET resource expression"))
				    );
			}
		}
		
		static void AddAspTags (CodeCompletionDataProvider provider, Document doc, S.XName parentName)
		{
			foreach (MonoDevelop.Projects.Dom.IType cls in doc.ReferenceManager.ListControlClasses ())
				provider.AddCompletionData (
				    new CodeCompletionData ("asp:" + cls.Name, Gtk.Stock.GoForward, cls.Documentation));
		}
		
		static void AddAspAttributeCompletionData (CodeCompletionDataProvider provider,
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
			
			AddControlMembers (provider, database, doc, controlClass, existingAtts);
		}
		
		static void AddControlMembers (CodeCompletionDataProvider provider,
		    MonoDevelop.Projects.Dom.Parser.ProjectDom database, Document doc,
		    MonoDevelop.Projects.Dom.IType controlClass, Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (MonoDevelop.Projects.Dom.IProperty prop 
			    in GetUniqueMembers<MonoDevelop.Projects.Dom.IProperty> (GetAllProperties (database, controlClass)))
				if (prop.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					provider.AddCompletionData (new CodeCompletionData (prop.Name, prop.StockIcon, prop.Documentation));
			
			//similarly add events
			foreach (MonoDevelop.Projects.Dom.IEvent eve 
			    in GetUniqueMembers<MonoDevelop.Projects.Dom.IEvent> (GetAllEvents (database, controlClass))) {
				string eveName = "On" + eve.Name;
				if (eve.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					provider.AddCompletionData (new CodeCompletionData (eveName, eve.StockIcon, eve.Documentation));
			}
		}
		
		static void AddAspAttributeValueCompletionData (CodeCompletionDataProvider provider,
		    MonoDevelop.AspNet.Parser.AspNetParsedDocument cu, S.XName tagName, S.XName attName,
		    Dictionary<string, string> existingAtts)
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
			MonoDevelop.Projects.Dom.IType codeBehindClass = null;
			MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase = null;
			if (cu != null && cu.Document.Project != null) {
				projectDatabase = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom
					(cu.Document.Project);
				
				if (projectDatabase != null && !string.IsNullOrEmpty (cu.PageInfo.InheritedClass))
					codeBehindClass = projectDatabase.GetType (cu.PageInfo.InheritedClass, false, false);
			}
			
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
							provider.AddCompletionData (new CodeCompletionData (meth, "md-method",
							    "A compatible method in the CodeBehind class"));
						}
						
						string suggestedIdentifier = ev.Name;
						if (existingAtts != null && !existingAtts.ContainsKey ("id") 
						    && !string.IsNullOrEmpty (existingAtts["id"]))
						{
							suggestedIdentifier = existingAtts["id"] + "_" + suggestedIdentifier;
						}
							
						domMethod.Name = BindingService.GenerateIdentifierUniqueInClass
							(projectDatabase, codeBehindClass, suggestedIdentifier);
						provider.AddCompletionData (
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
					AddBooleanCompletionData (provider);
					return;
				}
				
				//color completion
				if (prop.ReturnType.FullName == "System.Drawing.Color") {
					System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter ();
					foreach (System.Drawing.Color c in conv.GetStandardValues (null)) {
						if (c.IsSystemColor)
							continue;
						string hexcol = string.Format ("#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
						provider.AddCompletionData (new CodeCompletionData (c.Name, hexcol));
					}
					return;
				}
				
				//enum completion
				MonoDevelop.Projects.Dom.IType retCls = projectDatabase.GetType (prop.ReturnType);
				if (retCls != null && retCls.ClassType == MonoDevelop.Projects.Dom.ClassType.Enum) {
					foreach (MonoDevelop.Projects.Dom.IField enumVal in retCls.Fields)
						if (enumVal.IsPublic && enumVal.IsStatic)
							provider.AddCompletionData (
							    new CodeCompletionData (enumVal.Name, "md-literal", enumVal.Documentation));
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
		
		static void AddBooleanCompletionData (CodeCompletionDataProvider provider)
		{
			provider.AddCompletionData (new CodeCompletionData ("true", "md-literal"));
			provider.AddCompletionData (new CodeCompletionData ("false", "md-literal"));
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
