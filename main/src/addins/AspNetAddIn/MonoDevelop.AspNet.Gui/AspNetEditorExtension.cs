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
	
	
	public class AspNetEditorExtension : CompletionTextEditorExtension, IOutlinedDocument, IPathedDocument
	{
		object lockObj = new object ();
		DocumentStateTracker<S.Parser> tracker;
		MonoDevelop.AspNet.Parser.AspNetCompilationUnit lastCU = null;
		
		Gtk.TreeView outlineTreeView;
		Gtk.TreeStore outlineTreeStore;
		
		#region Setup and teardown
		
		public AspNetEditorExtension () : base ()
		{
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			string[] supportedExtensions = {".aspx", ".ascx", ".master"};
			return (doc.Project is AspNetAppProject) 
				&& Array.IndexOf (supportedExtensions, System.IO.Path.GetExtension (doc.Title)) > -1;
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			S.Parser parser = new S.Parser (new AspNetFreeState (), false);
			tracker = new DocumentStateTracker<S.Parser> (parser, Editor);
			
			MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.ParseInformationChanged += OnParseInformationChanged;
			
			//ensure that the schema service is initialised, or code completion may take a couple of seconds to trigger
			HtmlSchemaService.Initialise ();
		}
		
		public override void Dispose ()
		{
			if (tracker != null) {
				tracker = null;
				MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.ParseInformationChanged 
					-= OnParseInformationChanged;
				base.Dispose ();
			}
		}
		
		void OnParseInformationChanged (object sender, MonoDevelop.Projects.Parser.ParseInformationEventArgs args)
		{
			if (args.FileName == FileName)
				lastCU = args.ParseInformation.MostRecentCompilationUnit
					as MonoDevelop.AspNet.Parser.AspNetCompilationUnit;
			
			RefreshOutline ();
		}
		
		#endregion
		
		#region Convenience accessors
		
		MonoDevelop.AspNet.Parser.AspNetCompilationUnit CU {
			get { lock (lockObj) { return lastCU; } }
			set { lock (lockObj) { lastCU = value; } }
		}
		
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
			tracker.UpdateEngine ();
			MonoDevelop.AspNet.Parser.AspNetCompilationUnit CU = this.CU;
			
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
				+ " currentChar='{3}', forced='{4}'", tracker.Engine.CurrentState, tracker.Engine.CurrentStateLength,
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
				if (tracker.Engine.CurrentState is S.XmlFreeState && !(currentChar == '<' || currentChar == '>'))
					return null;
				
				if (tracker.Engine.CurrentState is S.XmlNameState 
				    && tracker.Engine.CurrentState.Parent is S.XmlAttributeState && previousChar != ' ')
					return null;
				
				if (tracker.Engine.CurrentState is S.XmlAttributeValueState 
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
				
				if (tracker.Engine.CurrentState is S.XmlFreeState) {
					
					S.XElement el = tracker.Engine.Nodes.Peek () as S.XElement;
					S.XName parentName = (el != null && el.IsNamed)? el.Name : new S.XName ();
					
					AddHtmlTagCompletionData (cp, schema, parentName);
					AddHtmlMiscBegins (cp);
					AddAspTags (cp, CU == null? null : CU.Document, parentName);
					AddCloseTag (cp, tracker.Engine.Nodes);
					
//						if (line < 3) {
//						cp.AddCompletionData (new CodeCompletionData ("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>"));
				
					if (line < 4 && string.IsNullOrEmpty (doctype))
						cp.AddCompletionData (new CodeCompletionData ("!DOCTYPE", "md-literal"));
				}
				
				AddAspBeginExpressions (cp, CU == null? null : CU.Document);
				return cp;
			}
			
			//closing tag completion
			if (tracker.Engine.CurrentState is S.XmlFreeState && currentPosition - 1 > 0 && currentChar == '>') {
				//get name of current node in document that's being ended
				S.XElement el = tracker.Engine.Nodes.Peek () as S.XElement;
				if (el != null && el.Position.End >= currentPosition && !el.IsClosed && el.IsNamed) {
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					cp.AddCompletionData (
					    new MonoDevelop.XmlEditor.Completion.XmlTagCompletionData (
					        String.Concat ("</", el.Name.FullName, ">"), 0, true)
					    );
					return cp;
				}
			}
			
			//attributes names within tags
			if (tracker.Engine.CurrentState is S.XmlTagState && forced || 
				(tracker.Engine.CurrentState is S.XmlNameState 
			 	 && tracker.Engine.CurrentState.Parent is S.XmlAttributeState
			         && tracker.Engine.CurrentStateLength == 1)
			) {
				S.XElement el = (tracker.Engine.CurrentState is S.XmlTagState)?
					(S.XElement) tracker.Engine.Nodes.Peek () :
					(S.XElement) tracker.Engine.Nodes.Peek (1);
				
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
			if ((tracker.Engine.CurrentState is S.XmlDoubleQuotedAttributeValueState
			    || tracker.Engine.CurrentState is S.XmlSingleQuotedAttributeValueState)
			    //trigger on the opening quote
			    && (tracker.Engine.CurrentStateLength == 0
			        //or trigger on first letter of value, if unforced
			        || (!forced && tracker.Engine.CurrentStateLength == 1))
			    ) {
				S.XAttribute att = (S.XAttribute) tracker.Engine.Nodes.Peek ();
				
				if (att.IsNamed) {
					S.XElement el = (S.XElement) tracker.Engine.Nodes.Peek (1);
					
					char next = ' ';
					if (currentPosition + 1 < buf.Length)
						next = buf.GetCharAt (currentPosition + 1);
					
					char compareChar = (tracker.Engine.CurrentStateLength == 0)? currentChar : previousChar;
					
					if ((compareChar == '"' || compareChar == '\'') 
					    && (next == compareChar || char.IsWhiteSpace (next))
					) {
						//if triggered by first letter of value, grab that letter
						if (tracker.Engine.CurrentStateLength == 1)
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
			foreach (MonoDevelop.Projects.Parser.IClass cls in doc.ReferenceManager.ListControlClasses ())
				provider.AddCompletionData (
				    new CodeCompletionData ("asp:" + cls.Name, Gtk.Stock.GoForward, cls.Documentation));
		}
		
		static void AddAspAttributeCompletionData (CodeCompletionDataProvider provider,
		    Document doc, S.XName name, Dictionary<string, string> existingAtts)
		{
			Debug.Assert (name.IsValid);
			Debug.Assert (name.HasPrefix);
			Debug.Assert (doc != null);
			
			//get a parser context
			MonoDevelop.Projects.Parser.IParserContext ctx = null;
			if (doc.Project != null)
				ctx = MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.GetProjectParserContext (doc.Project);
			else
				//FIXME use correct runtime
				ctx = MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.GetAssemblyParserContext ("System.Web");
			if (ctx == null) {
				LoggingService.LogWarning ("Could not obtain parser context in AddAspAttributeCompletionData");
				return;
			}
			
			MonoDevelop.Projects.Parser.IClass controlClass = doc.ReferenceManager.GetControlType (name.Prefix, name.Name);
			if (controlClass == null) {
				controlClass = ctx.GetClass ("System.Web.UI.WebControls.WebControl");
				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IClass for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			AddControlMembers (provider, ctx, doc, controlClass, existingAtts);
		}
		
		static void AddControlMembers (CodeCompletionDataProvider provider,
		    MonoDevelop.Projects.Parser.IParserContext ctx, Document doc,
		    MonoDevelop.Projects.Parser.IClass controlClass, Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (MonoDevelop.Projects.Parser.IProperty prop 
			    in GetUniqueMembers<MonoDevelop.Projects.Parser.IProperty> (GetAllProperties (ctx, controlClass)))
				if (prop.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					provider.AddCompletionData (new CodeCompletionData (prop.Name, "md-property", prop.Documentation));
			
			//similarly add events
			foreach (MonoDevelop.Projects.Parser.IEvent eve 
			    in GetUniqueMembers<MonoDevelop.Projects.Parser.IEvent> (GetAllEvents (ctx, controlClass))) {
				string eveName = "On" + eve.Name;
				if (eve.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					provider.AddCompletionData (new CodeCompletionData (eveName, "md-event", eve.Documentation));
			}
		}
		
		static void AddAspAttributeValueCompletionData (CodeCompletionDataProvider provider,
		    MonoDevelop.AspNet.Parser.AspNetCompilationUnit cu, S.XName tagName, S.XName attName,
		    Dictionary<string, string> existingAtts)
		{
			Debug.Assert (tagName.IsValid && tagName.HasPrefix);
			Debug.Assert (attName.IsValid && !attName.HasPrefix);
			
			MonoDevelop.Projects.Parser.IClass controlClass = null;
			if (cu != null)
				cu.Document.ReferenceManager.GetControlType (tagName.Prefix, tagName.Name);
			if (controlClass == null) {
				//FIXME: respect runtime version
				MonoDevelop.Projects.Parser.IParserContext sysWebContext =
					MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.GetAssemblyParserContext ("System.Web");
				if (sysWebContext == null)
					return;
				
				controlClass = sysWebContext.GetClass ("System.Web.UI.WebControls.WebControl");
				if (controlClass == null)
					LoggingService.LogWarning ("Could not obtain IClass for System.Web.UI.WebControls.WebControl");
			}
			
			//find the codebehind class
			MonoDevelop.Projects.Parser.IClass codeBehindClass = null;
			MonoDevelop.Projects.Parser.IParserContext projectContext = null;
			if (cu != null && cu.Document.Project != null)
				projectContext = 
					MonoDevelop.Ide.Gui.IdeApp.Workspace.ParserDatabase.GetProjectParserContext (
					    cu.Document.Project);
			if (projectContext != null && !string.IsNullOrEmpty (cu.PageInfo.InheritedClass))
				codeBehindClass = projectContext.GetClass (cu.PageInfo.InheritedClass);
			
			//if it's an event, suggest compatible methods 
			if (codeBehindClass != null && attName.Name.StartsWith ("On")) {
				string eventName = attName.Name.Substring (2);
				foreach (MonoDevelop.Projects.Parser.IEvent ev in GetAllEvents (projectContext, controlClass)) {
					if (ev.Name == eventName) {
						System.CodeDom.CodeMemberMethod domMethod = 
							BindingService.MDDomToCodeDomMethod (ev, projectContext);
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
							(projectContext, codeBehindClass, suggestedIdentifier);
						provider.AddCompletionData (
						    new SuggestedHandlerCompletionData (cu.Document.Project, domMethod, codeBehindClass,
						        MonoDevelop.AspNet.CodeBehind.GetNonDesignerClass (codeBehindClass))
						    );
						return;
					}
				}
			}
			
			//FIXME: respect runtime version
			if (projectContext == null)
				projectContext = 
					MonoDevelop.Ide.Gui.IdeApp.Workspace.
					ParserDatabase.GetAssemblyParserContext ("System.Web");
			if (projectContext == null)
				return;
			
			//if it's a property and is an enum or bool, suggest valid values
			foreach (MonoDevelop.Projects.Parser.IProperty prop in GetAllProperties (projectContext, controlClass)) {
				if (prop.Name != attName.Name)
					continue;
				
				//boolean completion
				if (prop.ReturnType.FullyQualifiedName == "System.Boolean") {
					AddBooleanCompletionData (provider);
					return;
				}
				
				//color completion
				if (prop.ReturnType.FullyQualifiedName == "System.Drawing.Color") {
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
				MonoDevelop.Projects.Parser.IClass retCls = 
					projectContext.GetClass (prop.ReturnType.FullyQualifiedName, true, false);
				if (retCls != null && retCls.ClassType == MonoDevelop.Projects.Parser.ClassType.Enum) {
					foreach (MonoDevelop.Projects.Parser.IField enumVal in retCls.Fields)
						if (enumVal.IsPublic && enumVal.IsStatic)
							provider.AddCompletionData (
							    new CodeCompletionData (enumVal.Name, "md-literal", enumVal.Documentation));
					return;
				}
			}
		}
		
		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : MonoDevelop.Projects.Parser.IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		static IEnumerable<MonoDevelop.Projects.Parser.IProperty> GetAllProperties (
		    MonoDevelop.Projects.Parser.IParserContext ctx,
		    MonoDevelop.Projects.Parser.IClass cls)
		{
			foreach (MonoDevelop.Projects.Parser.IProperty prop in cls.Properties)
				yield return prop;
			
			foreach (MonoDevelop.Projects.Parser.IReturnType rt in cls.BaseTypes) {
				MonoDevelop.Projects.Parser.IClass baseCls = ctx.GetClass (rt.FullyQualifiedName);
				if (baseCls != null)
					foreach (MonoDevelop.Projects.Parser.IProperty prop in GetAllProperties (ctx, baseCls))
					    yield return prop;
			}
		}
		
		static IEnumerable<MonoDevelop.Projects.Parser.IEvent> GetAllEvents (
		    MonoDevelop.Projects.Parser.IParserContext ctx,
		    MonoDevelop.Projects.Parser.IClass cls)
		{
			foreach (MonoDevelop.Projects.Parser.IEvent ev in cls.Events)
				yield return ev;
			
			foreach (MonoDevelop.Projects.Parser.IReturnType rt in cls.BaseTypes) {
				MonoDevelop.Projects.Parser.IClass baseCls = ctx.GetClass (rt.FullyQualifiedName, true, false);
				if (baseCls != null)
					foreach (MonoDevelop.Projects.Parser.IEvent ev in GetAllEvents (ctx, baseCls))
					    yield return ev;
			}
		}
		
		static void AddBooleanCompletionData (CodeCompletionDataProvider provider)
		{
			provider.AddCompletionData (new CodeCompletionData ("true", "md-literal"));
			provider.AddCompletionData (new CodeCompletionData ("false", "md-literal"));
		}
		
		#endregion
		
		
		static void AddCloseTag (CodeCompletionDataProvider provider, S.NodeStack stack)
		{
			//FIXME: check against fully parsed doc to see if tag's closed already
			foreach (S.XObject ob in stack) {
				S.XElement el = ob as S.XElement;
				if (el != null && el.IsNamed && !el.IsClosed) {
					string name = el.Name.FullName;
					provider.AddCompletionData (new CodeCompletionData ("/" + name + ">",
						Gtk.Stock.GoBack, "Closing tag for '" + name + "'"));
					return;
				}
			}
		}
		

		#region IPathedDocument
		
		string[] currentPath;
		int selectedPathIndex;
		bool pathUpdateQueued = false;
		
		
		public override void CursorPositionChanged ()
		{
			if (pathUpdateQueued)
				return;
			pathUpdateQueued = true;
			GLib.Timeout.Add (500, delegate {
				pathUpdateQueued = false;
				UpdatePath ();
				return false;
			});
				
		}

		public void SelectPath (int depth)
		{
			SelectPath (depth, false);
		}

		public void SelectPathContents (int depth)
		{
			SelectPath (depth, true);
		}
		
		void SelectPath (int depth, bool contents)
		{
			//clone the parser and put it in tree mode
			S.Parser treeParser = this.tracker.Engine.GetTreeParser ();
			
			//locate the node
			List<S.XObject> path = new List<S.XObject> (treeParser.Nodes);
			
			//note: list is backwards, and we want ignore the root XDocument
			S.XObject ob = path [path.Count - (depth + 2)];
			S.XNode node = ob as S.XNode;
			S.XElement el = node as S.XElement;
			
			//hoist this as it may not be cheap to evaluate (P/Invoke), but won't be changing during the loop
			int textLen = Editor.TextLength;
			
			//run the parser until the tag's closed, or we move to its sibling or parent
			if (node != null) {
				while (node.NextSibling == null &&
					treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
					if (el != null && el.IsClosed && el.ClosingTag.IsComplete)
						break;
				}
			} else {
				while (ob.Position.End < ob.Position.Start &&
			       		treeParser.Position < textLen && treeParser.Nodes.Peek () != ob.Parent)
				{
					char c = Editor.GetCharAt (treeParser.Position);
					treeParser.Push (c);
				}
			}
			
			if (el == null) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}", ob.Position);
				int s = ob.Position.Start;
				int e = ob.Position.End;
				if (s > -1 && e > s)
					Editor.Select (s, e);
			}
			else if (el.IsClosed) {
				MonoDevelop.Core.LoggingService.LogDebug ("Selecting {0}-{1}",
				    el.Position, el.ClosingTag.Position);
				
				if (el.IsSelfClosing)
					contents = false;
				
				//pick out the locations, with some offsets to account for the parsing model
				int s = contents? el.Position.End : el.Position.Start;
				int e = contents? el.ClosingTag.Position.Start : el.ClosingTag.Position.End;
				
				if (s > -1 && e > s)
					Editor.Select (s, e);
			} else {
				MonoDevelop.Core.LoggingService.LogDebug ("No end tag found for selection");
			}
		}
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		protected void OnPathChanged (string[] oldPath, int oldSelectedIndex)
		{
			if (PathChanged != null)
				PathChanged (this, new DocumentPathChangedEventArgs (oldPath, oldSelectedIndex));
		}
		
		S.XName GetCompleteName ()
		{
			Debug.Assert (this.tracker.Engine.CurrentState is S.XmlNameState);
			
			int pos = this.tracker.Engine.Position;
			
			//hoist this as it may not be cheap to evaluate (P/Invoke), but won't be changing during the loop
			int textLen = Editor.TextLength;
			
			//try to find the end of the name, but don't go too far
			for (int len = 0; pos < textLen && len < 30; pos++, len++) {
				char c = Editor.GetCharAt (pos);
				if (!char.IsLetterOrDigit (c) && c != ':' && c != '_')
					break;
			}
			
			return new S.XName (Editor.GetText (this.tracker.Engine.Position - this.tracker.Engine.CurrentStateLength, pos));
		}
		
		List<S.XObject> GetCurrentPath ()
		{
			this.tracker.UpdateEngine ();
			List<S.XObject> path = new List<S.XObject> (this.tracker.Engine.Nodes);
			
			//remove the root XDocument
			path.RemoveAt (path.Count - 1);
			
			//complete incomplete XName if present
			if (this.tracker.Engine.CurrentState is S.XmlNameState && path[0] is S.INamedXObject) {
				path[0] = (S.XObject) path[0].ShallowCopy ();
				S.XName completeName = GetCompleteName ();
				((S.INamedXObject)path[0]).Name = completeName;
			}
			path.Reverse ();
			return path;
		}
		
		void UpdatePath ()
		{
			List<S.XObject> l = GetCurrentPath ();
			
			//build the list
			string[] path = new string[l.Count];
			for (int i = 0; i < l.Count; i++) {
				if (l[i].FriendlyPathRepresentation == null) System.Console.WriteLine(l[i].GetType ());
				path[i] = l[i].FriendlyPathRepresentation ?? "<>";
			}
			
			string[] oldPath = currentPath;
			int oldIndex = selectedPathIndex;
			currentPath = path;
			selectedPathIndex = currentPath.Length - 1;
			
			OnPathChanged (oldPath, oldIndex);
		}
		
		public string[] CurrentPath {
			get { return currentPath; }
		}
		
		public int SelectedIndex {
			get { return selectedPathIndex; }
		}
		
		#endregion
		
		#region IOutlinedDocument
		
		bool refreshingOutline = false;
		
		Gtk.Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;
			
			outlineTreeStore = new Gtk.TreeStore (typeof (Node));
			outlineTreeView = new Gtk.TreeView (outlineTreeStore);
			
			System.Reflection.PropertyInfo prop = typeof (Gtk.TreeView).GetProperty ("EnableTreeLines");
			if (prop != null)
				prop.SetValue (outlineTreeView, true, null);
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			crt.Xpad = 0;
			crt.Ypad = 0;
			outlineTreeView.AppendColumn ("Node", crt, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
			outlineTreeView.HeadersVisible = false;
			
			outlineTreeView.Realized += delegate { refillOutlineStore (); };
			outlineTreeView.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				Node n = (Node) outlineTreeStore.GetValue (iter, 0);
				SelectNode (n);
			};
			
			refillOutlineStore ();
			
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
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
		
		void RefreshOutline ()
		{
			if (refreshingOutline || outlineTreeView == null )
				return;
			refreshingOutline = true;
			GLib.Timeout.Add (3000, refillOutlineStoreIdleHandler);
		}
		
		bool refillOutlineStoreIdleHandler ()
		{
			refreshingOutline = false;
			refillOutlineStore ();
			return false;
		}
		
		void refillOutlineStore ()
		{
			MonoDevelop.Core.Gui.DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized)
				return;
			
			outlineTreeStore.Clear ();
			if (lastCU != null) {
				DateTime start = DateTime.Now;
				ParentNode p = lastCU.Document.RootNode;
//				Gtk.TreeIter iter = outlineTreeStore.AppendValues (System.IO.Path.GetFileName (lastCU.Document.FilePath), p);
				BuildTreeChildren (outlineTreeStore, Gtk.TreeIter.Zero, p);
				outlineTreeView.ExpandAll ();
				LoggingService.LogDebug ("Built ASP.NET outline in {0}ms", (DateTime.Now - start).Milliseconds);
			}
			
			Gdk.Threads.Leave ();
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

		void IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			
			Gtk.ScrolledWindow w = (Gtk.ScrolledWindow) outlineTreeView.Parent;
			w.Destroy ();
			w.Dispose ();
			outlineTreeView.Destroy ();
			outlineTreeView.Dispose ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
		}
		
		#endregion
		
	}
}
