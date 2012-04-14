// LanguageItemTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Gtk;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Threading;
using System.Text;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.SourceEditor
{
	public class LanguageItemTooltipProvider: ITooltipProvider
	{
		public LanguageItemTooltipProvider()
		{
		}

		class ToolTipData
		{
			public CompilationUnit Unit;
			public ResolveResult Result;
			public AstNode Node;
			public CSharpAstResolver Resolver;

			public ToolTipData (ICSharpCode.NRefactory.CSharp.CompilationUnit unit, ICSharpCode.NRefactory.Semantics.ResolveResult result, ICSharpCode.NRefactory.CSharp.AstNode node, CSharpAstResolver file)
			{
				this.Unit = unit;
				this.Result = result;
				this.Node = node;
				this.Resolver = file;
			}
		}

		#region ITooltipProvider implementation 
		
		public TooltipItem GetItem (Mono.TextEditor.TextEditor editor, int offset)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.ParsedDocument == null)
				return null;
			var unit = doc.ParsedDocument.GetAst<CompilationUnit> ();
			if (unit == null)
				return null;

			var file = doc.ParsedDocument.ParsedFile as CSharpParsedFile;
			if (file == null)
				return null;
			
			ResolveResult result;
			AstNode node;
			var loc = editor.OffsetToLocation (offset);
			if (!doc.TryResolveAt (loc, out result, out node))
				return null;
			var resolver = new CSharpAstResolver (doc.Compilation, unit, file);
			resolver.ApplyNavigator (new NodeListResolveVisitorNavigator (node), CancellationToken.None);

			int startOffset = offset;
			int endOffset = offset;
			return new TooltipItem (new ToolTipData (unit, result, node, resolver), startOffset, endOffset - startOffset);
		}
		
		ResolveResult lastResult = null;
		LanguageItemWindow lastWindow = null;
		static Ambience ambience = new MonoDevelop.CSharp.CSharpAmbience ();
		public Gtk.Window CreateTooltipWindow (Mono.TextEditor.TextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;

			var titem = (ToolTipData)item.Item;
			string tooltip = null;
			if (titem.Result is UnknownIdentifierResolveResult) {
				tooltip = string.Format ("error CS0103: The name `{0}' does not exist in the current context", ((UnknownIdentifierResolveResult)titem.Result).Identifier);
			} else if (titem.Result is UnknownMemberResolveResult) {
				var ur = (UnknownMemberResolveResult)titem.Result;
				if (ur.TargetType.Kind != TypeKind.Unknown)
					tooltip = string.Format ("error CS0117: `{0}' does not contain a definition for `{1}'", ur.TargetType.FullName, ur.MemberName);
			} else if (titem.Result.IsError) {
				tooltip = "Resolve error.";
			} else if (titem.Result != null) {
				var ev = new ErrorVisitor (titem.Resolver);
				if (titem.Node is AstType && titem.Node.Parent is VariableDeclarationStatement && titem.Node.GetText () == "var") {
					titem.Node.Parent.AcceptVisitor (ev);
				}
				if (ev.ErrorResolveResult != null) {
					Console.WriteLine (ev.ErrorResolveResult);
					tooltip = string.Format ("Error while resolving: '{0}'", ev.ErrorNode.GetText ());
				} else {
					tooltip = CreateTooltip (titem.Result, ambience);
				}
			} else {
				return null;
			}


			if (lastResult != null && lastWindow.IsRealized && 
				titem.Result != null && lastResult.Type.Equals (titem.Result.Type))
				return lastWindow;
			var result = new LanguageItemWindow (tooltip);
			lastWindow = result;
			lastResult = titem.Result;
			if (result.IsEmpty)
				return null;
			return result;
		}

		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string methodStr = GettextCatalog.GetString ("Method");
		
		static string namespaceStr = GettextCatalog.GetString ("Namespace");		
		static string GetString (IType type)
		{
			switch (type.Kind) {
			case TypeKind.Class:
				return GettextCatalog.GetString ("Class");
			case TypeKind.Interface:
				return GettextCatalog.GetString ("Interface");
			case TypeKind.Struct:
				return GettextCatalog.GetString ("Struct");
			case TypeKind.Delegate:
				return GettextCatalog.GetString ("Delegate");
			case TypeKind.Enum:
				return GettextCatalog.GetString ("Enum");
			
			case TypeKind.Dynamic:
				return GettextCatalog.GetString ("Dynamic");
			case TypeKind.TypeParameter:
				return GettextCatalog.GetString ("Type parameter");
			
			case TypeKind.Array:
				return GettextCatalog.GetString ("Array");
			case TypeKind.Pointer:
				return GettextCatalog.GetString ("Pointer");
			}
			
			return null;
		}
		
		static string GetString (IMember member)
		{
			switch (member.EntityType) {
			case EntityType.Field:
				var field = member as IField;
				if (field.IsConst)
					return GettextCatalog.GetString ("Constant");
				return GettextCatalog.GetString ("Field");
			case EntityType.Property:
				return GettextCatalog.GetString ("Property");
			case EntityType.Indexer:
				return GettextCatalog.GetString ("Indexer");
				
			case EntityType.Event:
				return GettextCatalog.GetString ("Event");
			}
			return GettextCatalog.GetString ("Member");
		}
		
		string GetConst (object obj)
		{
			if (obj is string)
				return '"' + obj.ToString () + '"';
			if (obj is char)
				return "'" + obj + "'";
			return obj.ToString ();
		}

		public string CreateTooltip (ResolveResult result, Ambience ambience)
		{
			OutputSettings settings = new OutputSettings (OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeKeywords | OutputFlags.IncludeMarkup | OutputFlags.UseFullName);
			// Approximate value for usual case
			StringBuilder s = new StringBuilder (150);
			string doc = null;
			if (result is UnknownIdentifierResolveResult) {
				s.Append (String.Format (GettextCatalog.GetString ("Unresolved identifier '{0}'"), ((UnknownIdentifierResolveResult)result).Identifier));
			} else if (result.IsError) {
				s.Append (GettextCatalog.GetString ("Resolve error."));
			} else if (result is LocalResolveResult) {
				var lr = (LocalResolveResult)result;
				s.Append ("<small><i>");
				s.Append (lr.IsParameter ? paramStr : localStr);
				s.Append ("</i></small>\n");
				s.Append (ambience.GetString (lr.Variable.Type, settings));
				s.Append (" ");
				s.Append (lr.Variable.Name);
			} else if (result is MethodGroupResolveResult) {
				var mrr = (MethodGroupResolveResult)result;
				s.Append ("<small><i>");
				s.Append (methodStr);
				s.Append ("</i></small>\n");
				var allMethods = new List<IMethod> (mrr.Methods);
				foreach (var l in mrr.GetExtensionMethods ()) {
					allMethods.AddRange (l);
				}
				
				var method = allMethods.FirstOrDefault ();
				if (method != null) {
					s.Append (ambience.GetString (method, settings));
					if (allMethods.Count > 1) {
						int overloadCount = allMethods.Count - 1;
						s.Append (string.Format (GettextCatalog.GetPluralString (" (+{0} overload)", " (+{0} overloads)", overloadCount), overloadCount));
					}
					doc = AmbienceService.GetDocumentationSummary (method);
				}
			} else if (result is MemberResolveResult) {
				var member = ((MemberResolveResult)result).Member;
				s.Append ("<small><i>");
				s.Append (GetString (member));
				s.Append ("</i></small>\n");
				var field = member as IField;
				if (field != null && field.IsConst) {
					s.Append (ambience.GetString (field.Type, settings));
					s.Append (" ");
					s.Append (field.Name);
					s.Append (" = ");
					s.Append (GetConst (field.ConstantValue));
					s.Append (";");
				} else {
					s.Append (ambience.GetString (member, settings));
				}
				doc = AmbienceService.GetDocumentationSummary (member);
			} else if (result is NamespaceResolveResult) {
				s.Append ("<small><i>");
				s.Append (namespaceStr);
				s.Append ("</i></small>\n");
				s.Append (ambience.GetString (((NamespaceResolveResult)result).NamespaceName, settings));
			} else {
				var tr = result;
				var typeString = GetString (tr.Type);
				if (!string.IsNullOrEmpty (typeString)) {
					s.Append ("<small><i>");
					s.Append (typeString);
					s.Append ("</i></small>\n");
				}
				settings.OutputFlags |= OutputFlags.UseFullName | OutputFlags.UseFullInnerTypeName;
				s.Append (ambience.GetString (tr.Type, settings));
				doc = AmbienceService.GetDocumentationSummary (tr.Type.GetDefinition ());
			}
			
			if (!string.IsNullOrEmpty (doc)) {
				s.Append ("\n<small>");
				s.Append (AmbienceService.GetDocumentationMarkup ("<summary>" + doc + "</summary>"));
				s.Append ("</small>");
			}
			return s.ToString ();
		}
		

		class ErrorVisitor : DepthFirstAstVisitor
		{
			readonly CSharpAstResolver resolver;
			readonly CancellationToken cancellationToken;

			ResolveResult errorResolveResult;
			public ResolveResult ErrorResolveResult {
				get {
					return errorResolveResult;
				}
			}			

			AstNode errorNode;

			public AstNode ErrorNode {
				get {
					return errorNode;
				}
			}

			public ErrorVisitor (CSharpAstResolver resolver, CancellationToken cancellationToken = default(CancellationToken))
			{
				this.resolver = resolver;
				this.cancellationToken = cancellationToken;
			}
			
			protected override void VisitChildren (AstNode node)
			{
				if (ErrorResolveResult != null || cancellationToken.IsCancellationRequested)
					return;
				if (node is Expression) {
					var rr = resolver.Resolve (node, cancellationToken);
					if (rr.IsError) {
						errorResolveResult = rr;
						errorNode = node;
					}
				}
				base.VisitChildren (node);
			}
		}

		
		public void GetRequiredPosition (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			LanguageItemWindow win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		
		public bool IsInteractive (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow)
		{
			return false;
		}
		
		#endregion 
		
		public class LanguageItemWindow: MonoDevelop.Components.TooltipWindow
		{
			public bool IsEmpty { get; set; }
			
			public LanguageItemWindow (string tooltip)
			{
				if (string.IsNullOrEmpty (tooltip)|| tooltip == "?") {
					IsEmpty = true;
					return;
				}
	
				var label = new MonoDevelop.Components.FixedWidthWrapLabel () {
					Wrap = Pango.WrapMode.WordChar,
					Indent = -20,
					BreakOnCamelCasing = true,
					BreakOnPunctuation = true,
					Markup = tooltip,
				};
				this.BorderWidth = 3;
				Add (label);
				UpdateFont (label);
				
				EnableTransparencyControl = true;
			}
			
			//return the real width
			public int SetMaxWidth (int maxWidth)
			{
				var label = Child as MonoDevelop.Components.FixedWidthWrapLabel;
				if (label == null)
					return Allocation.Width;
				label.MaxWidth = maxWidth;
				return label.RealWidth;
			}
			
			protected override void OnStyleSet (Style previous_style)
			{
				base.OnStyleSet (previous_style);
				UpdateFont (Child as MonoDevelop.Components.FixedWidthWrapLabel);
			}
			
			void UpdateFont (MonoDevelop.Components.FixedWidthWrapLabel label)
			{
				if (label == null)
					return;
				label.FontDescription = MonoDevelop.Ide.Fonts.FontService.GetFontDescription ("LanguageTooltips");
				
			}
		}
	}
}
