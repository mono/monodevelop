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

namespace MonoDevelop.CSharp.Completion
{
	public class MemberCompletionData : CompletionData, IEntityCompletionData
	{
		CSharpCompletionTextEditorExtension editorCompletion;
		OutputFlags flags;
		bool hideExtensionParameter = true;
		static CSharpAmbience ambience = new CSharpAmbience ();
		bool descriptionCreated = false;
		
		string description, completionString;
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
				CheckDescription ();
				return description;
			}
		}
		
		public override string CompletionText {
			get { return completionString; }
			set { completionString = value; }
		}
		
		public override string DisplayText {
			get {
				if (displayText == null) {
					displayText = ambience.GetString (Entity, flags | OutputFlags.HideGenericParameterNames);
				}
				return displayText; 
			}
		}
		
		public override IconId Icon {
			get {
				return Entity.GetStockIcon ();
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
		
		public bool IsDelegateExpected { get; set; }
		
		public MemberCompletionData (CSharpCompletionTextEditorExtension  editorCompletion, IEntity entity, OutputFlags flags)
		{
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
		
		bool HasNonMethodMembersWithSameName (IMember member)
		{
			return member.DeclaringType.GetFields ().Cast<INamedElement> ()
				.Concat (member.DeclaringType.GetProperties ().Cast<INamedElement> ())
				.Concat (member.DeclaringType.GetEvents ().Cast<INamedElement> ())
				.Concat (member.DeclaringType.GetNestedTypes ().Cast<INamedElement> ())
				.Any (e => e.Name == member.Name);
		}
		
		bool HasAnyOverloadWithParameters (IMethod method)
		{
			return method.DeclaringType.GetMethods ().Any (m => m.Parameters.Count > 0);
		}
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			string text = CompletionText;
			string partialWord = GetCurrentWord (window);
			int skipChars = 0;
			bool runParameterCompletionCommand = false;
			
			if (keyChar == '(' && !IsDelegateExpected && Entity is IMethod && !HasNonMethodMembersWithSameName ((IMember)Entity)) {
				
				var line = Editor.GetLine (Editor.Caret.Line);
				var method = (IMethod)Entity;
				var start = window.CodeCompletionContext.TriggerOffset + partialWord.Length + 2;
				var end = line.Offset + line.Length;
				string textToEnd = start < end ? Editor.GetTextBetween (start, end) : "";
				if (Policy.BeforeMethodCallParentheses)
					text += " ";
				
				int exprStart = window.CodeCompletionContext.TriggerOffset - 1;
				while (exprStart > line.Offset) {
					char ch = Editor.GetCharAt (exprStart);
					if (ch != '.' && ch != '_' && /*ch != '<' && ch != '>' && */!char.IsLetterOrDigit (ch))
						break;
					exprStart--;
				}
				string textBefore = Editor.GetTextBetween (line.Offset, exprStart);
				bool insertSemicolon = false;
				if (string.IsNullOrEmpty ((textBefore + textToEnd).Trim ()))
					insertSemicolon = true;
			
			
				
				int pos;
				if (SearchBracket (window.CodeCompletionContext.TriggerOffset + partialWord.Length, out pos)) {
					window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, partialWord, text);
					ka |= KeyActions.Ignore;
					int bracketOffset = pos + text.Length - partialWord.Length;
					
					// correct white space before method call.
					char charBeforeBracket = bracketOffset > 1 ? Editor.GetCharAt (bracketOffset - 2) : '\0';
					if (Policy.BeforeMethodCallParentheses) {
						if (charBeforeBracket != ' ') {
							Editor.Insert (bracketOffset - 1, " ");
							bracketOffset++;
						}
					} else { 
						if (char.IsWhiteSpace (charBeforeBracket)) {
							while (bracketOffset > 1 && char.IsWhiteSpace (Editor.GetCharAt (bracketOffset - 2))) {
								Editor.Remove (bracketOffset - 1, 1);
								bracketOffset--;
							}
						}
					}
					Editor.Caret.Offset = bracketOffset;
					if (insertSemicolon && Editor.GetCharAt (bracketOffset) == ')') {
						Editor.Insert (bracketOffset + 1, ";");
						// Need to reinsert the ')' as skip char because we inserted the ';' after the ')' and skip chars get deleted 
						// when an insert after the skip char position occur.
						Editor.SetSkipChar (bracketOffset, ')');
						Editor.SetSkipChar (bracketOffset + 1, ';');
					}
					if (runParameterCompletionCommand)
						editorCompletion.RunParameterCompletionCommand ();
					return;
				}
				
				if (HasAnyOverloadWithParameters (method)) {
					if (insertSemicolon) {
						text += "(|);";
						skipChars = 2;
					} else {
						text += "(|)";
						skipChars = 1;
					}
					runParameterCompletionCommand = true;
				} else {
					if (insertSemicolon) {
						text += "();|";
					} else {
						text += "()|";
					}
				}
				if (keyChar == '(') {
					var skipChar = Editor.SkipChars.LastOrDefault ();
					if (skipChar != null && skipChar.Offset == (window.CodeCompletionContext.TriggerOffset + partialWord.Length) && skipChar.Char == ')')
						Editor.Remove (skipChar.Offset, 1);
				}
				
				ka |= KeyActions.Ignore;
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
		
		void SetMember (IEntity entity)
		{
			this.Entity = entity;
			descriptionCreated = false;
			this.completionString = displayText = entity.Name;
		}

		TypeSystemAstBuilder GetBuilder (ICompilation compilation)
		{
			var ctx = editorCompletion.CSharpUnresolvedFile.GetTypeResolveContext (compilation, editorCompletion.Document.Editor.Caret.Location) as CSharpTypeResolveContext;
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
			public string ConvertEntity(IEntity entity)
			{
				if (entity == null)
					throw new ArgumentNullException("entity");
				
				StringWriter writer = new StringWriter();
				ConvertEntity(entity, new TextWriterOutputFormatter(writer), FormattingOptionsFactory.CreateMono ());
				return writer.ToString();
			}
			
			public void ConvertEntity(IEntity entity, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				if (entity == null)
					throw new ArgumentNullException("entity");
				if (formatter == null)
					throw new ArgumentNullException("formatter");
				if (formattingPolicy == null)
					throw new ArgumentNullException("options");
				
				TypeSystemAstBuilder astBuilder = CreateAstBuilder();
				EntityDeclaration node = astBuilder.ConvertEntity(entity);
				PrintModifiers(node.Modifiers, formatter);
				
				if ((ConversionFlags & ConversionFlags.ShowDefinitionKeyword) == ConversionFlags.ShowDefinitionKeyword) {
					if (node is TypeDeclaration) {
						switch (((TypeDeclaration)node).ClassType) {
						case ClassType.Class:
							formatter.WriteKeyword("class");
							break;
						case ClassType.Struct:
							formatter.WriteKeyword("struct");
							break;
						case ClassType.Interface:
							formatter.WriteKeyword("interface");
							break;
						case ClassType.Enum:
							formatter.WriteKeyword("enum");
							break;
						default:
							throw new Exception("Invalid value for ClassType");
						}
						formatter.Space();
					} else if (node is DelegateDeclaration) {
						formatter.WriteKeyword("delegate");
						formatter.Space();
					} else if (node is EventDeclaration) {
						formatter.WriteKeyword("event");
						formatter.Space();
					}
				}
				
				if ((ConversionFlags & ConversionFlags.ShowReturnType) == ConversionFlags.ShowReturnType) {
					var rt = node.GetChildByRole(Roles.Type);
					if (!rt.IsNull) {
						rt.AcceptVisitor(new CSharpOutputVisitor(formatter, formattingPolicy));
						formatter.Space();
					}
				}
				
				if (entity is ITypeDefinition)
					WriteTypeDeclarationName((ITypeDefinition)entity, formatter, formattingPolicy);
				else
					WriteMemberDeclarationName((IMember)entity, formatter, formattingPolicy);
				
				if ((ConversionFlags & ConversionFlags.ShowParameterList) == ConversionFlags.ShowParameterList && HasParameters(entity)) {
					formatter.WriteToken(entity.EntityType == EntityType.Indexer ? "[" : "(");
					bool first = true;
					foreach (var param in node.GetChildrenByRole(Roles.Parameter)) {
						if (first) {
							first = false;
						} else {
							formatter.WriteToken(",");
							formatter.Space();
						}
						param.AcceptVisitor(new CSharpOutputVisitor(formatter, formattingPolicy));
					}
					formatter.WriteToken(entity.EntityType == EntityType.Indexer ? "]" : ")");
				}
				
				if ((ConversionFlags & ConversionFlags.ShowBody) == ConversionFlags.ShowBody && !(node is TypeDeclaration)) {
					IProperty property = entity as IProperty;
					if (property != null) {
						formatter.Space();
						formatter.WriteToken("{");
						formatter.Space();
						if (property.CanGet) {
							formatter.WriteKeyword("get");
							formatter.WriteToken(";");
							formatter.Space();
						}
						if (property.CanSet) {
							formatter.WriteKeyword("set");
							formatter.WriteToken(";");
							formatter.Space();
						}
						formatter.WriteToken("}");
					} else {
						formatter.WriteToken(";");
					}
				}
			}
			
			bool HasParameters(IEntity e)
			{
				switch (e.EntityType) {
				case EntityType.TypeDefinition:
					return ((ITypeDefinition)e).Kind == TypeKind.Delegate;
				case EntityType.Indexer:
				case EntityType.Method:
				case EntityType.Operator:
				case EntityType.Constructor:
				case EntityType.Destructor:
					return true;
				default:
					return false;
				}
			}
			
			TypeSystemAstBuilder CreateAstBuilder()
			{
				return builder;
			}
			
			void WriteTypeDeclarationName(ITypeDefinition typeDef, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder();
				if (typeDef.DeclaringTypeDefinition != null) {
					WriteTypeDeclarationName(typeDef.DeclaringTypeDefinition, formatter, formattingPolicy);
					formatter.WriteToken(".");
				} else if ((ConversionFlags & ConversionFlags.UseFullyQualifiedTypeNames) == ConversionFlags.UseFullyQualifiedTypeNames) {
					formatter.WriteIdentifier(typeDef.Namespace);
					formatter.WriteToken(".");
				}
				formatter.WriteIdentifier(typeDef.Name);
				if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList) {
					var outputVisitor = new CSharpOutputVisitor(formatter, formattingPolicy);
					outputVisitor.WriteTypeParameters(astBuilder.ConvertEntity(typeDef).GetChildrenByRole(Roles.TypeParameter));
				}
			}
			
			void WriteMemberDeclarationName(IMember member, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder();
				if ((ConversionFlags & ConversionFlags.ShowDeclaringType) == ConversionFlags.ShowDeclaringType) {
					ConvertType(member.DeclaringType, formatter, formattingPolicy);
					formatter.WriteToken(".");
				}
				switch (member.EntityType) {
				case EntityType.Indexer:
					formatter.WriteKeyword("this");
					break;
				case EntityType.Constructor:
					formatter.WriteIdentifier(member.DeclaringType.Name);
					break;
				case EntityType.Destructor:
					formatter.WriteToken("~");
					formatter.WriteIdentifier(member.DeclaringType.Name);
					break;
				case EntityType.Operator:
					switch (member.Name) {
					case "op_Implicit":
						formatter.WriteKeyword("implicit");
						formatter.Space();
						formatter.WriteKeyword("operator");
						formatter.Space();
						ConvertType(member.ReturnType, formatter, formattingPolicy);
						break;
					case "op_Explicit":
						formatter.WriteKeyword("explicit");
						formatter.Space();
						formatter.WriteKeyword("operator");
						formatter.Space();
						ConvertType(member.ReturnType, formatter, formattingPolicy);
						break;
					default:
						formatter.WriteKeyword("operator");
						formatter.Space();
						var operatorType = OperatorDeclaration.GetOperatorType(member.Name);
						if (operatorType.HasValue)
							formatter.WriteToken(OperatorDeclaration.GetToken(operatorType.Value));
						else
							formatter.WriteIdentifier(member.Name);
						break;
					}
					break;
				default:
					formatter.WriteIdentifier(member.Name);
					break;
				}
				if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList && member.EntityType == EntityType.Method) {
					var outputVisitor = new CSharpOutputVisitor(formatter, formattingPolicy);
					outputVisitor.WriteTypeParameters(astBuilder.ConvertEntity(member).GetChildrenByRole(Roles.TypeParameter));
				}
			}
			
			void PrintModifiers(Modifiers modifiers, IOutputFormatter formatter)
			{
				foreach (var m in CSharpModifierToken.AllModifiers) {
					if ((modifiers & m) == m) {
						formatter.WriteKeyword(CSharpModifierToken.GetModifierName(m));
						formatter.Space();
					}
				}
			}
#endregion
			
			public string ConvertVariable(IVariable v)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder();
				AstNode astNode = astBuilder.ConvertVariable(v);
				return astNode.GetText().TrimEnd(';', '\r', '\n');
			}
			
			public string ConvertType(IType type)
			{
				if (type == null)
					throw new ArgumentNullException("type");
				
				TypeSystemAstBuilder astBuilder = CreateAstBuilder();
				AstType astType = astBuilder.ConvertType(type);
				return astType.GetText();
			}
			
			public void ConvertType(IType type, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
			{
				TypeSystemAstBuilder astBuilder = CreateAstBuilder();
				AstType astType = astBuilder.ConvertType(type);
				astType.AcceptVisitor(new CSharpOutputVisitor(formatter, formattingPolicy));
			}
			
			public string WrapComment(string comment)
			{
				return "// " + comment;
			}
		}
		void CheckDescription ()
		{
			if (descriptionCreated)
				return;
			
			var sb = new StringBuilder ();

			descriptionCreated = true;
			if (Entity is IMethod && ((IMethod)Entity).IsExtensionMethod)
				sb.Append (GettextCatalog.GetString ("(Extension) "));
			try {
				var amb = new MyAmbience (GetBuilder (Entity.Compilation));
				sb.Append (GLib.Markup.EscapeText (amb.ConvertEntity (Entity)));
			} catch (Exception e) {
				sb.Append (e.ToString ());
			}

			var m = (IMember)Entity;
			if (m.IsObsolete ()) {
				sb.AppendLine ();
				sb.Append (GettextCatalog.GetString ("[Obsolete]"));
				DisplayFlags |= DisplayFlags.Obsolete;
			}
			
			var returnType = m.ReturnType;
			if (returnType.Kind == TypeKind.Delegate) {
				sb.AppendLine ();
				sb.AppendLine (GettextCatalog.GetString ("Delegate information"));
				sb.Append (ambience.GetString (returnType, OutputFlags.ReformatDelegates | OutputFlags.IncludeReturnType | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName));
			}
			
			string docMarkup = AmbienceService.GetDocumentationMarkup ("<summary>" + AmbienceService.GetDocumentationSummary ((IMember)Entity) + "</summary>", new AmbienceService.DocumentationFormatOptions {
				Ambience = ambience
			});
			
			if (!string.IsNullOrEmpty (docMarkup)) {
				sb.AppendLine ();
				sb.Append (docMarkup);
			}
			description = sb.ToString ();
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
					IMethod mmx = (IMethod) mx;//, mmy = (IMethod) my;
					result = (mmx.TypeParameters.Count).CompareTo (mmx.TypeParameters.Count);
					if (result != 0)
						return result;
					result = (mmx.Parameters.Count).CompareTo (mmx.Parameters.Count);
					if (result != 0)
						return result;
				}
				
				string sx = mx.ReflectionName;// ambience.GetString (mx, flags);
				string sy = my.ReflectionName;// ambience.GetString (my, flags);
				result = sx.Length.CompareTo (sy.Length);
				return result == 0? string.Compare (sx, sy) : result;
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
			AddOverload ((MemberCompletionData) data);
		}
		
		public void AddOverload (MemberCompletionData overload)
		{
			if (overloads == null)
				overloads = new Dictionary<string, CompletionData> ();
			
			if (overload.Entity is IMember && Entity is IMember) {
				// filter virtual & overriden members that came from base classes
				// note that the overload tree is traversed top down.
				var member = Entity as IMember;
				if ((member.IsVirtual || member.IsOverride) && member.DeclaringType != null && ((IMember)overload.Entity).DeclaringType != null && member.DeclaringType.ReflectionName != ((IMember)overload.Entity).DeclaringType.ReflectionName) {
					string str1 = ambience.GetString (member as IMember, flags);
					string str2 = ambience.GetString (overload.Entity as IMember, flags);
					if (str1 == str2) {
						if (string.IsNullOrEmpty (AmbienceService.GetDocumentationSummary ((IMember)Entity)) && !string.IsNullOrEmpty (AmbienceService.GetDocumentationSummary ((IMember)overload.Entity)))
							SetMember (overload.Entity as IMember);
						return;
					}
				}
				
				string MemberId = (overload.Entity as IMember).GetIdString ();
				if (Entity is IMethod && overload.Entity is IMethod) {
					string signature1 = ambience.GetString (Entity as IMember, OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics | OutputFlags.GeneralizeGenerics);
					string signature2 = ambience.GetString (overload.Entity as IMember, OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics | OutputFlags.GeneralizeGenerics);
					if (signature1 == signature2)
						return;
				}
				
				if (MemberId != (this.Entity as IMember).GetIdString () && !overloads.ContainsKey (MemberId)) {
//					if (((IMethod)overload.Member).IsPartial)
//						return;
					overloads[MemberId] = overload;
					
					//if any of the overloads is obsolete, we should not mark the item obsolete
					if (!(overload.Entity as IMember).IsObsolete ())
						DisplayFlags &= ~DisplayFlags.Obsolete;
/*					
					//make sure that if there are generic overloads, we show a generic signature
					if (overload.Member is IType && Member is IType && ((IType)Member).TypeParameters.Count == 0 && ((IType)overload.Member).TypeParameters.Count > 0) {
						displayText = overload.DisplayText;
					}
					if (overload.Member is IMethod && Member is IMethod && ((IMethod)Member).TypeParameters.Count == 0 && ((IMethod)overload.Member).TypeParameters.Count > 0) {
						displayText = overload.DisplayText;
					}*/
				}
			}
			
			
			// always set the member with the least type parameters as the main member.
//			if (Member is ITypeParameterMember && overload.Member is ITypeParameterMember) {
//				if (((ITypeParameterMember)Member).TypeParameters.Count > ((ITypeParameterMember)overload.Member).TypeParameters.Count) {
//					INode member = Member;
//					SetMember (overload.Member);
//					overload.Member = member;
//				}
//			}
			
		}
		
		#endregion

		#region IEntityCompletionData implementation
		public IEntity Entity {
			get;
			set;
		}
		#endregion


	}
}
