// MemberCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.IO;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharp.Completion
{
	class MemberCompletionData : CompletionData, IEntityCompletionData
	{
		CSharpCompletionTextEditorExtension editorCompletion;
		OutputFlags flags;
		bool hideExtensionParameter = true;
		static CSharpAmbience ambience = new CSharpAmbience ();
		string completionString;
		string displayText;
		Dictionary<string, CompletionData> overloads;

		Mono.TextEditor.TextEditorData Editor {
			get {
				return editorCompletion.TextEditorData;
			}
		}

		MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy Policy {
			get {
				return editorCompletion.FormattingPolicy;
			}
		}

		public override string Description {
			get {
				return "";
			}
		}

		public override string CompletionText {
			get { return completionString; }
			set { completionString = value; }
		}

		public override string DisplayText {
			get {
				if (displayText == null) {
					displayText = ambience.GetString (Entity.SymbolKind == SymbolKind.Constructor ? Entity.DeclaringTypeDefinition : Entity, flags | OutputFlags.HideGenericParameterNames);
				}
				return displayText; 
			}
		}

		public override IconId Icon {
			get {
				return (Entity.SymbolKind == SymbolKind.Constructor ? Entity.DeclaringTypeDefinition : Entity).GetStockIcon ();
			}
		}

		public bool HideExtensionParameter {
			get {
				return hideExtensionParameter;
			}
			set {
				hideExtensionParameter = value;
			}
		}

		bool isDelegateExpected;
		public bool IsDelegateExpected {
			get {
				return isDelegateExpected || factory != null && factory.Engine.PossibleDelegates.Count > 0;
			} 
			set {
				isDelegateExpected = value;
			}
		}

		ICompilation compilation;
		CSharpUnresolvedFile file;
		CSharpCompletionTextEditorExtension.CompletionDataFactory factory;

		public MemberCompletionData (CSharpCompletionTextEditorExtension.CompletionDataFactory factory, IEntity entity, OutputFlags flags) : this(factory.ext, entity, flags)
		{
			this.factory = factory;
		}

		public MemberCompletionData (CSharpCompletionTextEditorExtension editorCompletion, IEntity entity, OutputFlags flags)
		{
			compilation = editorCompletion.UnresolvedFileCompilation;
			file = editorCompletion.CSharpUnresolvedFile;

			this.editorCompletion = editorCompletion;
			this.flags = flags;
			SetMember (entity);
			DisplayFlags = DisplayFlags.DescriptionHasMarkup;
			var m = Entity as IMember;
			if (m != null && m.IsObsolete ())
				DisplayFlags |= DisplayFlags.Obsolete;
		}

		public bool SearchBracket (int start, out int pos)
		{
			pos = -1;
			
			for (int i = start; i < Editor.Length; i++) {
				char ch = Editor.GetCharAt (i);
				if (ch == '(') {
					pos = i + 1;
					return true;
				}
				if (!char.IsWhiteSpace (ch))
					return false;
			}
			return false;
		}

		static bool HasNonMethodMembersWithSameName (IMember member)
		{
			return member.DeclaringType.GetFields ().Cast<INamedElement> ()
				.Concat (member.DeclaringType.GetProperties ().Cast<INamedElement> ())
				.Concat (member.DeclaringType.GetEvents ().Cast<INamedElement> ())
				.Concat (member.DeclaringType.GetNestedTypes ().Cast<INamedElement> ())
				.Any (e => e.Name == member.Name);
		}

		static bool HasAnyOverloadWithParameters (IMethod method)
		{
			if (method.SymbolKind == SymbolKind.Constructor) 
				return method.DeclaringType.GetConstructors ().Any (m => m.Parameters.Count > 0);
			return method.DeclaringType.GetMethods ().Any (m => m.Name == method.Name && m.Parameters.Count > 0);
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			InsertCompletionText (window, ref ka, closeChar, keyChar, modifier, CompletionTextEditorExtension.AddParenthesesAfterCompletion, CompletionTextEditorExtension.AddOpeningOnly);
		}

		bool IsBracketAlreadyInserted (IMethod method)
		{
			int offset = Editor.Caret.Offset;
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsLetterOrDigit (ch))
					break;
				offset++;
			}
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsWhiteSpace (ch))
					return ch == '(' || ch == '<' && RequireGenerics (method);
				offset++;
			}
			return false;
		}

		bool InsertSemicolon (int exprStart)
		{
			int offset = exprStart;
			while (offset > 0) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsWhiteSpace (ch)) {
					if (ch != '{' && ch != '}' && ch != ';')
						return false;
					break;
				}
				offset--;
			}

			offset = Editor.Caret.Offset;
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsLetterOrDigit (ch))
					break;
				offset++;
			}
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsWhiteSpace (ch))
					return char.IsLetter (ch) || ch == '}';
				offset++;
			}
			return true;
		}

		public void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier, bool addParens, bool addOpeningOnly)
		{
			string text = CompletionText;
			string partialWord = GetCurrentWord (window);
			int skipChars = 0;
			bool runParameterCompletionCommand = false;
			var method = Entity as IMethod;
			if (addParens && !IsDelegateExpected && method != null && !HasNonMethodMembersWithSameName ((IMember)Entity) && !IsBracketAlreadyInserted (method)) {
				var line = Editor.GetLine (Editor.Caret.Line);
				var start = window.CodeCompletionContext.TriggerOffset + partialWord.Length + 2;
				var end = line.Offset + line.Length;
				string textToEnd = start < end ? Editor.GetTextBetween (start, end) : "";
				bool addSpace = Policy.BeforeMethodCallParentheses && CSharpTextEditorIndentation.OnTheFlyFormatting;

				int exprStart = window.CodeCompletionContext.TriggerOffset - 1;
				while (exprStart > line.Offset) {
					char ch = Editor.GetCharAt (exprStart);
					if (ch != '.' && ch != '_' && /*ch != '<' && ch != '>' && */!char.IsLetterOrDigit (ch))
						break;
					exprStart--;
				}
				bool insertSemicolon = InsertSemicolon(exprStart);
				if (Entity.SymbolKind == SymbolKind.Constructor)
					insertSemicolon = false;
				//int pos;

				Gdk.Key[] keys = new [] { Gdk.Key.Return, Gdk.Key.Tab, Gdk.Key.space, Gdk.Key.KP_Enter, Gdk.Key.ISO_Enter };
				if (keys.Contains (closeChar) || keyChar == '.') {
					if (HasAnyOverloadWithParameters (method)) {
						if (addOpeningOnly) {
							text += RequireGenerics (method) ? "<|" : (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (keyChar == '.') {
								if (RequireGenerics (method)) {
									text += addSpace ? "<> ()" : "<>()";
								} else {
									text += addSpace ? " ()" : "()";
								}
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ();" : "<|>();";
										skipChars = addSpace ? 5 : 4;
									} else {
										text += addSpace ? " (|);" : "(|);";
										skipChars = 2;
									}
								} else {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ()" :  "<|>()";
										skipChars = addSpace ? 4 : 3;
									} else {
										text += addSpace ? " (|)" : "(|)";
										skipChars = 1;
									}
								}
							}
						}
						runParameterCompletionCommand = true;
					} else {
						if (addOpeningOnly) {
							text += RequireGenerics (method) ? "<|" : (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (keyChar == '.') {
								if (RequireGenerics (method)) {
									text += addSpace ? "<> ().|" : "<>().|";
								} else {
									text += addSpace ? " ().|" : "().|";
								}
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ();" : "<|>();";
									} else {
										text += addSpace ? " ();|" : "();|";
									}

								} else {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ()" : "<|>()";
									} else {
										text += addSpace ? " ()|" : "()|";
									}

								}
							}
						}
					}
					if (keyChar == '(') {
						var skipChar = Editor.SkipChars.LastOrDefault ();
						if (skipChar != null && skipChar.Offset == (window.CodeCompletionContext.TriggerOffset + partialWord.Length) && skipChar.Char == ')')
							Editor.Remove (skipChar.Offset, 1);
					}
				}
				ka |= KeyActions.Ignore;
			}
			if ((DisplayFlags & DisplayFlags.NamedArgument) == DisplayFlags.NamedArgument &&
			    (closeChar == Gdk.Key.Tab ||
				 closeChar == Gdk.Key.KP_Tab ||
				 closeChar == Gdk.Key.ISO_Left_Tab ||
				 closeChar == Gdk.Key.Return ||
				 closeChar == Gdk.Key.KP_Enter ||
				 closeChar == Gdk.Key.ISO_Enter)) {
				if (Policy.AroundAssignmentParentheses)
					text += " ";
				text += "=";
				if (Policy.AroundAssignmentParentheses)
					text += " ";
			}
			window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, partialWord, text);
			int offset = Editor.Caret.Offset;
			for (int i = 0; i < skipChars; i++) {
				Editor.SetSkipChar (offset, Editor.GetCharAt (offset));
				offset++;
			}
			
			if (runParameterCompletionCommand)
				editorCompletion.RunParameterCompletionCommand ();
		}

		bool ContainsType (IType testType, IType searchType)
		{
			if (testType == searchType)
				return true;
			foreach (var arg in testType.TypeArguments)
				if (ContainsType (arg, searchType))
					return true;
			return false;
		}

		bool RequireGenerics (IMethod method)
		{
			if (method.SymbolKind == SymbolKind.Constructor)
				return method.DeclaringType.TypeParameterCount > 0;
			var testMethod = method.ReducedFrom ?? method;
			return testMethod.TypeArguments.Any (t => !testMethod.Parameters.Any (p => ContainsType(p.Type, t)));
		}

		void SetMember (IEntity entity)
		{
			this.Entity = entity;
			this.completionString = displayText = (Entity.SymbolKind == SymbolKind.Constructor ? Entity.DeclaringTypeDefinition : Entity).Name;
		}

		TypeSystemAstBuilder GetBuilder (ICompilation compilation)
		{
			var ctx = editorCompletion.CSharpUnresolvedFile.GetTypeResolveContext (editorCompletion.UnresolvedFileCompilation, editorCompletion.Document.Editor.Caret.Location) as CSharpTypeResolveContext;
			var state = new CSharpResolver (ctx);
			var builder = new TypeSystemAstBuilder (state);
			builder.AddAnnotations = true;
			var dt = state.CurrentTypeDefinition;
			var declaring = ctx.CurrentTypeDefinition != null ? ctx.CurrentTypeDefinition.DeclaringTypeDefinition : null;
			if (declaring != null) {
				while (dt != null) {
					if (dt.Equals (declaring)) {
						builder.AlwaysUseShortTypeNames = true;
						break;
					}
					dt = dt.DeclaringTypeDefinition;
				}
			}
			return builder;
		}

		internal class MyAmbience  : IAmbience
		{
			TypeSystemAstBuilder builder;

			public MyAmbience (TypeSystemAstBuilder builder)
			{
				this.builder = builder;
				ConversionFlags = ICSharpCode.NRefactory.TypeSystem.ConversionFlags.StandardConversionFlags;
			}

			public ConversionFlags ConversionFlags { get; set; }
			#region ConvertEntity
			public string ConvertEntity (IEntity entity)
			{
				if (entity == null)
					throw new ArgumentNullException ("entity");
				
				StringWriter writer = new StringWriter ();
				ConvertEntity (entity, new TextWriterOutputFormatter (writer), FormattingOptionsFactory.CreateMono ());
				return writer.ToString ();
			}

			public void ConvertEntity (IEntity entity, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				if (entity == null)
					throw new ArgumentNullException ("entity");
				if (formatter == null)
					throw new ArgumentNullException ("formatter");
				if (formattingPolicy == null)
					throw new ArgumentNullException ("options");
				
				TypeSystemAstBuilder astBuilder = CreateAstBuilder ();
				EntityDeclaration node = astBuilder.ConvertEntity (entity);
				PrintModifiers (node.Modifiers, formatter);
				
				if ((ConversionFlags & ConversionFlags.ShowDefinitionKeyword) == ConversionFlags.ShowDefinitionKeyword) {
					if (node is TypeDeclaration) {
						switch (((TypeDeclaration)node).ClassType) {
						case ClassType.Class:
							formatter.WriteKeyword ("class");
							break;
						case ClassType.Struct:
							formatter.WriteKeyword ("struct");
							break;
						case ClassType.Interface:
							formatter.WriteKeyword ("interface");
							break;
						case ClassType.Enum:
							formatter.WriteKeyword ("enum");
							break;
						default:
							throw new Exception ("Invalid value for ClassType");
						}
						formatter.Space ();
					} else if (node is DelegateDeclaration) {
						formatter.WriteKeyword ("delegate");
						formatter.Space ();
					} else if (node is EventDeclaration) {
						formatter.WriteKeyword ("event");
						formatter.Space ();
					}
				}
				
				if ((ConversionFlags & ConversionFlags.ShowReturnType) == ConversionFlags.ShowReturnType) {
					var rt = node.GetChildByRole (Roles.Type);
					if (!rt.IsNull) {
						rt.AcceptVisitor (new CSharpOutputVisitor (formatter, formattingPolicy));
						formatter.Space ();
					}
				}
				
				if (entity is ITypeDefinition)
					WriteTypeDeclarationName ((ITypeDefinition)entity, formatter, formattingPolicy);
				else
					WriteMemberDeclarationName ((IMember)entity, formatter, formattingPolicy);
				
				if ((ConversionFlags & ConversionFlags.ShowParameterList) == ConversionFlags.ShowParameterList && HasParameters (entity)) {
					formatter.WriteToken (entity.SymbolKind == SymbolKind.Indexer ? "[" : "(");
					bool first = true;
					foreach (var param in node.GetChildrenByRole(Roles.Parameter)) {
						if (first) {
							first = false;
						} else {
							formatter.WriteToken (",");
							formatter.Space ();
						}
						param.AcceptVisitor (new CSharpOutputVisitor (formatter, formattingPolicy));
					}
					formatter.WriteToken (entity.SymbolKind == SymbolKind.Indexer ? "]" : ")");
				}
				
				if ((ConversionFlags & ConversionFlags.ShowBody) == ConversionFlags.ShowBody && !(node is TypeDeclaration)) {
					IProperty property = entity as IProperty;
					if (property != null) {
						formatter.Space ();
						formatter.WriteToken ("{");
						formatter.Space ();
						if (property.CanGet) {
							formatter.WriteKeyword ("get");
							formatter.WriteToken (";");
							formatter.Space ();
						}
						if (property.CanSet) {
							formatter.WriteKeyword ("set");
							formatter.WriteToken (";");
							formatter.Space ();
						}
						formatter.WriteToken ("}");
					} else {
						formatter.WriteToken (";");
					}
				}
			}

			bool HasParameters (IEntity e)
			{
				switch (e.SymbolKind) {
				case SymbolKind.TypeDefinition:
					return ((ITypeDefinition)e).Kind == TypeKind.Delegate;
				case SymbolKind.Indexer:
				case SymbolKind.Method:
				case SymbolKind.Operator:
				case SymbolKind.Constructor:
				case SymbolKind.Destructor:
					return true;
				default:
					return false;
				}
			}
			public string ConvertConstantValue (object constantValue)
			{
				if (constantValue == null)
					return "null";
				return constantValue.ToString ();
			}

			TypeSystemAstBuilder CreateAstBuilder ()
			{
				return builder;
			}

			void WriteTypeDeclarationName (ITypeDefinition typeDef, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder ();
				if (typeDef.DeclaringTypeDefinition != null) {
					WriteTypeDeclarationName (typeDef.DeclaringTypeDefinition, formatter, formattingPolicy);
					formatter.WriteToken (".");
				} else if ((ConversionFlags & ConversionFlags.UseFullyQualifiedTypeNames) == ConversionFlags.UseFullyQualifiedTypeNames) {
					formatter.WriteIdentifier (typeDef.Namespace);
					formatter.WriteToken (".");
				}
				formatter.WriteIdentifier (typeDef.Name);
				if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList) {
					var outputVisitor = new CSharpOutputVisitor (formatter, formattingPolicy);
					outputVisitor.WriteTypeParameters (astBuilder.ConvertEntity (typeDef).GetChildrenByRole (Roles.TypeParameter));
				}
			}

			void WriteMemberDeclarationName (IMember member, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder ();
				if ((ConversionFlags & ConversionFlags.ShowDeclaringType) == ConversionFlags.ShowDeclaringType) {
					ConvertType (member.DeclaringType, formatter, formattingPolicy);
					formatter.WriteToken (".");
				}
				switch (member.SymbolKind) {
				case SymbolKind.Indexer:
					formatter.WriteKeyword ("this");
					break;
				case SymbolKind.Constructor:
					formatter.WriteIdentifier (member.DeclaringType.Name);
					break;
				case SymbolKind.Destructor:
					formatter.WriteToken ("~");
					formatter.WriteIdentifier (member.DeclaringType.Name);
					break;
				case SymbolKind.Operator:
					switch (member.Name) {
					case "op_Implicit":
						formatter.WriteKeyword ("implicit");
						formatter.Space ();
						formatter.WriteKeyword ("operator");
						formatter.Space ();
						ConvertType (member.ReturnType, formatter, formattingPolicy);
						break;
					case "op_Explicit":
						formatter.WriteKeyword ("explicit");
						formatter.Space ();
						formatter.WriteKeyword ("operator");
						formatter.Space ();
						ConvertType (member.ReturnType, formatter, formattingPolicy);
						break;
					default:
						formatter.WriteKeyword ("operator");
						formatter.Space ();
						var operatorType = OperatorDeclaration.GetOperatorType (member.Name);
						if (operatorType.HasValue)
							formatter.WriteToken (OperatorDeclaration.GetToken (operatorType.Value));
						else
							formatter.WriteIdentifier (member.Name);
						break;
					}
					break;
				default:
					formatter.WriteIdentifier (member.Name);
					break;
				}
				if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList && member.SymbolKind == SymbolKind.Method) {
					var outputVisitor = new CSharpOutputVisitor (formatter, formattingPolicy);
					outputVisitor.WriteTypeParameters (astBuilder.ConvertEntity (member).GetChildrenByRole (Roles.TypeParameter));
				}
			}

			void PrintModifiers (Modifiers modifiers, IOutputFormatter formatter)
			{
				foreach (var m in CSharpModifierToken.AllModifiers) {
					if ((modifiers & m) == m) {
						formatter.WriteKeyword (CSharpModifierToken.GetModifierName (m));
						formatter.Space ();
					}
				}
			}
#endregion
			
			public string ConvertVariable (IVariable v)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder ();
				AstNode astNode = astBuilder.ConvertVariable (v);
				return astNode.ToString ().TrimEnd (';', '\r', '\n');
			}

			public string ConvertType (IType type)
			{
				if (type == null)
					throw new ArgumentNullException ("type");
				
				TypeSystemAstBuilder astBuilder = CreateAstBuilder ();
				AstType astType = astBuilder.ConvertType (type);
				return astType.ToString ();
			}

			public void ConvertType (IType type, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder ();
				AstType astType = astBuilder.ConvertType (type);
				astType.AcceptVisitor (new CSharpOutputVisitor (formatter, formattingPolicy));
			}

			public string WrapComment (string comment)
			{
				return "// " + comment;
			}
		}

		public static TooltipInformation CreateTooltipInformation (CSharpCompletionTextEditorExtension editorCompletion, CSharpResolver resolver, IEntity entity, bool smartWrap)
		{
			return CreateTooltipInformation (editorCompletion.UnresolvedFileCompilation, editorCompletion.CSharpUnresolvedFile, resolver, editorCompletion.TextEditorData, editorCompletion.FormattingPolicy, entity, smartWrap);
		}

		public static TooltipInformation CreateTooltipInformation (ICompilation compilation, CSharpUnresolvedFile file, TextEditorData textEditorData, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy formattingPolicy, IEntity entity, bool smartWrap, bool createFooter = false)
		{
			return CreateTooltipInformation (compilation, file, null, textEditorData, formattingPolicy, entity, smartWrap, createFooter);
		}

		public static TooltipInformation CreateTooltipInformation (ICompilation compilation, CSharpUnresolvedFile file, CSharpResolver resolver, TextEditorData textEditorData, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy formattingPolicy, IEntity entity, bool smartWrap, bool createFooter = false)
		{
			var tooltipInfo = new TooltipInformation ();
			if (resolver == null)
				resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
			var sig = new SignatureMarkupCreator (resolver, formattingPolicy.CreateOptions ());
			sig.BreakLineAfterReturnType = smartWrap;
			try {
				tooltipInfo.SignatureMarkup = sig.GetMarkup (entity);
			} catch (Exception e) {
				LoggingService.LogError ("Got exception while creating markup for :" + entity, e);
				return new TooltipInformation ();
			}
			tooltipInfo.SummaryMarkup = AmbienceService.GetSummaryMarkup (entity) ?? "";
			
			if (entity is IMember) {
				var evt = (IMember)entity;
				if (evt.ReturnType.Kind == TypeKind.Delegate) {
					tooltipInfo.AddCategory (GettextCatalog.GetString ("Delegate Info"), sig.GetDelegateInfo (evt.ReturnType));
				}
			}
			if (entity is IMethod) {
				var method = (IMethod)entity;
				if (method.IsExtensionMethod) {
					tooltipInfo.AddCategory (GettextCatalog.GetString ("Extension Method from"), method.DeclaringTypeDefinition.FullName);
				}
			}
			if (createFooter) {
				if (entity is IType) {
					var type = entity as IType;
					var def = type.GetDefinition ();
					if (def != null) {
						if (!string.IsNullOrEmpty (def.ParentAssembly.AssemblyName)) {
							var project = def.GetSourceProject ();
							if (project != null) {
								var relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, def.Region.FileName);
								tooltipInfo.FooterMarkup = "<small>" + GettextCatalog.GetString ("Project:\t{0}", AmbienceService.EscapeText (def.ParentAssembly.AssemblyName)) + "</small>" + Environment.NewLine +
									"<small>" + GettextCatalog.GetString ("File:\t\t{0} (line {1})", AmbienceService.EscapeText (relPath), def.Region.Begin.Line) + "</small>";
							}
						}
					}

				} else if (entity.DeclaringTypeDefinition != null) {
					var project = entity.DeclaringTypeDefinition.GetSourceProject ();
					if (project != null) {
						var relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, entity.Region.FileName);
						tooltipInfo.FooterMarkup = 
							"<small>" + GettextCatalog.GetString ("Project:\t{0}", AmbienceService.EscapeText (project.Name)) + "</small>" + Environment.NewLine +
							"<small>" + GettextCatalog.GetString ("From:\t{0}", AmbienceService.EscapeText (entity.DeclaringType.FullName)) + "</small>" + Environment.NewLine +
							"<small>" + GettextCatalog.GetString ("File:\t\t{0} (line {1})", AmbienceService.EscapeText (relPath), entity.Region.Begin.Line) + "</small>";
					}
				}
			}
			return tooltipInfo;
		}

		public static TooltipInformation CreateTooltipInformation (ICompilation compilation, CSharpUnresolvedFile file, TextEditorData textEditorData, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy formattingPolicy, IType type, bool smartWrap, bool createFooter = false)
		{
			var tooltipInfo = new TooltipInformation ();
			var resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
			var sig = new SignatureMarkupCreator (resolver, formattingPolicy.CreateOptions ());
			sig.BreakLineAfterReturnType = smartWrap;
			try {
				tooltipInfo.SignatureMarkup = sig.GetMarkup (type.IsParameterized ? type.GetDefinition () : type);
			} catch (Exception e) {
				LoggingService.LogError ("Got exception while creating markup for :" + type, e);
				return new TooltipInformation ();
			}
			if (type.IsParameterized) {
				var typeInfo = new StringBuilder ();
				for (int i = 0; i < type.TypeParameterCount; i++) {
					typeInfo.AppendLine (type.GetDefinition ().TypeParameters [i].Name + " is " + sig.GetTypeReferenceString (type.TypeArguments [i]));
				}
				tooltipInfo.AddCategory ("Type Parameters", typeInfo.ToString ());
			}

			var def = type.GetDefinition ();
			if (def != null) {
				if (createFooter && !string.IsNullOrEmpty (def.ParentAssembly.AssemblyName))
					tooltipInfo.FooterMarkup = "<small> From " + AmbienceService.EscapeText (def.ParentAssembly.AssemblyName) + "</small>";
				tooltipInfo.SummaryMarkup = AmbienceService.GetSummaryMarkup (def) ?? "";
			}
			return tooltipInfo;
		}

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			return CreateTooltipInformation (compilation, file, editorCompletion.TextEditorData, editorCompletion.FormattingPolicy, Entity, smartWrap);
		}
		#region IOverloadedCompletionData implementation 
	
		class OverloadSorter : IComparer<ICompletionData>
		{
			public OverloadSorter ()
			{
			}

			public int Compare (ICompletionData x, ICompletionData y)
			{
				var mx = ((MemberCompletionData)x).Entity as IMember;
				var my = ((MemberCompletionData)y).Entity as IMember;
				int result;
				
				if (mx is ITypeDefinition && my is ITypeDefinition) {
					result = ((((ITypeDefinition)mx).TypeParameters.Count).CompareTo (((ITypeDefinition)my).TypeParameters.Count));
					if (result != 0)
						return result;
				}
				
				if (mx is IMethod && my is IMethod) {
					return MethodParameterDataProvider.MethodComparer ((IMethod)mx, (IMethod)my);
				}
				string sx = mx.ReflectionName;// ambience.GetString (mx, flags);
				string sy = my.ReflectionName;// ambience.GetString (my, flags);
				result = sx.Length.CompareTo (sy.Length);
				return result == 0 ? string.Compare (sx, sy) : result;
			}
		}

		public override IEnumerable<ICompletionData> OverloadedData {
			get {
				if (overloads == null)
					return new CompletionData[] { this };
				
				var sorted = new List<ICompletionData> (overloads.Values);
				sorted.Add (this);
				sorted.Sort (new OverloadSorter ());
				return sorted;
			}
		}

		public override bool HasOverloads {
			get { return overloads != null && overloads.Count > 0; }
		}

		public override void AddOverload (ICSharpCode.NRefactory.Completion.ICompletionData data)
		{
			AddOverload ((MemberCompletionData)data);
		}

		public void AddOverload (MemberCompletionData overload)
		{
			if (overloads == null)
				overloads = new Dictionary<string, CompletionData> ();
			
			if (overload.Entity is IMember && Entity is IMember) {
				// filter overriden members that came from base classes
				// note that the overload tree is traversed top down.
				var member = Entity as IMember;
				if (member.IsOverride)
					return;

				string MemberId = (overload.Entity as IMember).GetIdString ();
				if (MemberId != (this.Entity as IMember).GetIdString () && !overloads.ContainsKey (MemberId)) {
					overloads [MemberId] = overload;
					
					//if any of the overloads is obsolete, we should not mark the item obsolete
					if (!(overload.Entity as IMember).IsObsolete ())
						DisplayFlags &= ~DisplayFlags.Obsolete;
				}
			}
		}
		#endregion

		#region IEntityCompletionData implementation
		public IEntity Entity {
			get;
			set;
		}
		#endregion

		public override int CompareTo (object obj)
		{
			int result = base.CompareTo (obj);
			if (result == 0) {
				var mcd = obj as MemberCompletionData;
				if (mcd != null) {
					var mc = mcd;
					if (this.Entity.SymbolKind == SymbolKind.Method) {
						var method = (IMethod)this.Entity;
						if (method.IsExtensionMethod)
							return 1;
					}
					if (mc.Entity.SymbolKind == SymbolKind.Method) {
						var method = (IMethod)mc.Entity;
						if (method.IsExtensionMethod)
							return -1;
					}
				} else {
					return -1;
				}
			}
			return result;
		}

		public override string ToString ()
		{
			return string.Format ("[MemberCompletionData: Entity={0}]", Entity);
		}
	}
}
