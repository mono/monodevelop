// 
// McsParser.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Mono.CSharp;
using System.Text;
using Mono.TextEditor;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;

namespace MonoDevelop.CSharp.Parser
{
	public class McsParser : AbstractParser
	{
		public override IExpressionFinder CreateExpressionFinder (ProjectDom dom)
		{
			return new NewCSharpExpressionFinder (dom);
		}

		public override IResolver CreateResolver (ProjectDom dom, object editor, string fileName)
		{
			MonoDevelop.Ide.Gui.Document doc = (MonoDevelop.Ide.Gui.Document)editor;
			return new NRefactoryResolver (dom, doc.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, doc.Editor, fileName);
		}
		
		class ErrorReportPrinter : ReportPrinter
		{
			public readonly List<Error> Errors = new List<Error> ();
			
			public override void Print (AbstractMessage msg)
			{
				base.Print (msg);
				Error newError = new Error (msg.IsWarning ? ErrorType.Warning : ErrorType.Error, msg.Location.Row, msg.Location.Column, msg.Text);
				Errors.Add (newError);
			}
		}
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			if (string.IsNullOrEmpty (content))
				return null;
			
			List<string> compilerArguments = new List<string> ();
			
			var unit =  new MonoDevelop.Projects.Dom.CompilationUnit (fileName);;
			var result = new ParsedDocument (fileName);
			result.CompilationUnit = unit;
			
			ICSharpCode.NRefactory.Parser.CSharp.Lexer lexer = new ICSharpCode.NRefactory.Parser.CSharp.Lexer (new StringReader (content));
			lexer.SpecialCommentTags = ProjectDomService.SpecialCommentTags.GetNames ();
			lexer.EvaluateConditionalCompilation = true;
			if (dom != null && dom.Project != null && MonoDevelop.Ide.IdeApp.Workspace != null) {
				DotNetProjectConfiguration configuration = dom.Project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
				CSharpCompilerParameters par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
				if (par != null) {
					lexer.SetConditionalCompilationSymbols (par.DefineSymbols);
					if (!string.IsNullOrEmpty (par.DefineSymbols)) {
						compilerArguments.Add ("-define:" + string.Join (";", par.DefineSymbols.Split (';', ',', ' ', '\t')));
					}
					if (par.UnsafeCode)
						compilerArguments.Add ("-unsafe");
				}
			}
			while (lexer.NextToken ().Kind != ICSharpCode.NRefactory.Parser.CSharp.Tokens.EOF)
				;
			
			CompilerCompilationUnit top;
			ErrorReportPrinter errorReportPrinter = new ErrorReportPrinter ();
			using (var stream = new MemoryStream (Encoding.Default.GetBytes (content))) {
				top = CompilerCallableEntryPoint.ParseFile (compilerArguments.ToArray (), stream, fileName, errorReportPrinter);
			}
			if (top == null)
				return null;
			
			SpecialTracker tracker = new SpecialTracker (result);
			foreach (ICSharpCode.NRefactory.ISpecial special in lexer.SpecialTracker.CurrentSpecials) {
				special.AcceptVisitor (tracker, null);
			}	
			
			
			// convert DOM
			var conversionVisitor = new ConversionVisitor (new Document (content), top.LocationsBag, lexer.SpecialTracker.CurrentSpecials);
			conversionVisitor.Dom = dom;
			conversionVisitor.Unit = unit;
			conversionVisitor.ParsedDocument = result;
			top.UsingsBag.Global.Accept (conversionVisitor);
			
			unit.Tag = top;
			
			// parser errors
			errorReportPrinter.Errors.ForEach (e => conversionVisitor.ParsedDocument.Add (e));
			return result;
		}
		
		class SpecialTracker : ICSharpCode.NRefactory.ISpecialVisitor
		{
			ParsedDocument result;

			public SpecialTracker (ParsedDocument result)
			{
				this.result = result;
			}

			public object Visit (ICSharpCode.NRefactory.ISpecial special, object data)
			{
				return null;
			}

			public object Visit (ICSharpCode.NRefactory.BlankLine special, object data)
			{
				return null;
			}

			public object Visit (ICSharpCode.NRefactory.Comment comment, object data)
			{
				MonoDevelop.Projects.Dom.Comment newComment = new MonoDevelop.Projects.Dom.Comment ();
				newComment.CommentStartsLine = comment.CommentStartsLine;
				newComment.Text = comment.CommentText;
				int commentTagLength = comment.CommentType == ICSharpCode.NRefactory.CommentType.Documentation ? 3 : 2;
				int commentEndOffset = comment.CommentType == ICSharpCode.NRefactory.CommentType.Block ? 0 : 1;
				newComment.Region = new DomRegion (comment.StartPosition.Line, comment.StartPosition.Column - commentTagLength, 
					comment.EndPosition.Line, comment.EndPosition.Column - commentEndOffset);
				
				switch (comment.CommentType) {
				case ICSharpCode.NRefactory.CommentType.Block:
					newComment.CommentType = MonoDevelop.Projects.Dom.CommentType.MultiLine;
					break;
				case ICSharpCode.NRefactory.CommentType.Documentation:
					newComment.CommentType = MonoDevelop.Projects.Dom.CommentType.SingleLine;
					newComment.IsDocumentation = true;
					break;
				default:
					newComment.CommentType = MonoDevelop.Projects.Dom.CommentType.SingleLine;
					break;
				}

				result.Add (newComment);
				return null;
			}

			Stack<ICSharpCode.NRefactory.PreprocessingDirective> regions = new Stack<ICSharpCode.NRefactory.PreprocessingDirective> ();
			Stack<ICSharpCode.NRefactory.PreprocessingDirective> ifBlocks = new Stack<ICSharpCode.NRefactory.PreprocessingDirective> ();
			List<ICSharpCode.NRefactory.PreprocessingDirective> elifBlocks = new List<ICSharpCode.NRefactory.PreprocessingDirective> ();
			ICSharpCode.NRefactory.PreprocessingDirective elseBlock = null;

			Stack<ConditionalRegion> conditionalRegions = new Stack<ConditionalRegion> ();
			ConditionalRegion ConditionalRegion {
				get {
					return conditionalRegions.Count > 0 ? conditionalRegions.Peek () : null;
				}
			}

			void CloseConditionBlock (DomLocation loc)
			{
				if (ConditionalRegion == null || ConditionalRegion.ConditionBlocks.Count == 0 || !ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End.IsEmpty)
					return;
				ConditionalRegion.ConditionBlocks[ConditionalRegion.ConditionBlocks.Count - 1].End = loc;
			}

			void AddCurRegion (ICSharpCode.NRefactory.Location loc)
			{
				if (ConditionalRegion == null)
					return;
				ConditionalRegion.End = new DomLocation (loc.Line, loc.Column);
				result.Add (ConditionalRegion);
				conditionalRegions.Pop ();
			}

			static ICSharpCode.NRefactory.PrettyPrinter.CSharpOutputVisitor visitor = new ICSharpCode.NRefactory.PrettyPrinter.CSharpOutputVisitor ();

			public object Visit (ICSharpCode.NRefactory.PreprocessingDirective directive, object data)
			{
				DomLocation loc = new DomLocation (directive.StartPosition.Line, directive.StartPosition.Column);
				switch (directive.Cmd) {
				case "#if":
					directive.Expression.AcceptVisitor (visitor, null);
					conditionalRegions.Push (new ConditionalRegion (visitor.Text));
					visitor.Reset ();
					ifBlocks.Push (directive);
					ConditionalRegion.Start = loc;
					break;
				case "#elif":
					CloseConditionBlock (new DomLocation (directive.LastLineEnd.Line, directive.LastLineEnd.Column));
					directive.Expression.AcceptVisitor (visitor, null);
					if (ConditionalRegion != null)
						ConditionalRegion.ConditionBlocks.Add (new ConditionBlock (visitor.Text, loc));
					visitor.Reset ();
					//						elifBlocks.Add (directive);
					break;
				case "#else":
					CloseConditionBlock (new DomLocation (directive.LastLineEnd.Line, directive.LastLineEnd.Column));
					if (ConditionalRegion != null)
						ConditionalRegion.ElseBlock = new DomRegion (loc, DomLocation.Empty);
					//						elseBlock = directive;
					break;
				case "#endif":
					DomLocation endLoc = new DomLocation (directive.LastLineEnd.Line, directive.LastLineEnd.Column);
					CloseConditionBlock (endLoc);
					if (ConditionalRegion != null && !ConditionalRegion.ElseBlock.Start.IsEmpty)
						ConditionalRegion.ElseBlock = new DomRegion (ConditionalRegion.ElseBlock.Start, endLoc);
					AddCurRegion (directive.EndPosition);
					if (ifBlocks.Count > 0) {
						ICSharpCode.NRefactory.PreprocessingDirective ifBlock = ifBlocks.Pop ();
						DomRegion dr = new DomRegion (ifBlock.StartPosition.Line, ifBlock.StartPosition.Column, directive.EndPosition.Line, directive.EndPosition.Column);
						result.Add (new FoldingRegion ("#if " + ifBlock.Arg.Trim (), dr, FoldType.UserRegion, false));
						foreach (ICSharpCode.NRefactory.PreprocessingDirective d in elifBlocks) {
							dr.Start = new DomLocation (d.StartPosition.Line, d.StartPosition.Column);
							result.Add (new FoldingRegion ("#elif " + ifBlock.Arg.Trim (), dr, FoldType.UserRegion, false));
						}
						if (elseBlock != null) {
							dr.Start = new DomLocation (elseBlock.StartPosition.Line, elseBlock.StartPosition.Column);
							result.Add (new FoldingRegion ("#else", dr, FoldType.UserRegion, false));
						}
					}
					elseBlock = null;
					break;
				case "#define":
					result.Add (new PreProcessorDefine (directive.Arg, loc));
					break;
				case "#region":
					regions.Push (directive);
					break;
				case "#endregion":
					if (regions.Count > 0) {
						ICSharpCode.NRefactory.PreprocessingDirective start = regions.Pop ();
						DomRegion dr = new DomRegion (start.StartPosition.Line, start.StartPosition.Column, directive.EndPosition.Line, directive.EndPosition.Column);
						result.Add (new FoldingRegion (start.Arg, dr, FoldType.UserRegion, true));
					}
					break;
				}
				return null;
			}
		}

		
		class ConversionVisitor : StructuralVisitor
		{
			public ProjectDom Dom {
				get;
				set;
			}
			
			public ParsedDocument ParsedDocument {
				get;
				set;
			}
			
			public LocationsBag LocationsBag  {
				get;
				private set;
			}
			
			internal MonoDevelop.Projects.Dom.CompilationUnit Unit;
			Mono.TextEditor.Document data;
			public ConversionVisitor (Mono.TextEditor.Document data, LocationsBag locationsBag, List<ICSharpCode.NRefactory.ISpecial> specials)
			{
				this.data = data;
				this.LocationsBag = locationsBag;
				this.specials = specials;
			}
			
			int lastSpecial = 0;
			List<ICSharpCode.NRefactory.ISpecial> specials;
			string RetrieveDocumentation (int upToLine)
			{
				StringBuilder result = null;
				while (lastSpecial < specials.Count) {
					var cur = specials[lastSpecial];
					if (cur.StartPosition.Line >= upToLine)
						break;
					ICSharpCode.NRefactory.Comment comment = cur as ICSharpCode.NRefactory.Comment;
					if (comment != null && comment.CommentType == ICSharpCode.NRefactory.CommentType.Documentation) {
						if (result == null)
							result = new StringBuilder ();
						result.Append (comment.CommentText);
					}
					lastSpecial++;
				}
				return result == null ? null : result.ToString ();
			}
			
			public static DomLocation Convert (Mono.CSharp.Location loc)
			{
				return new DomLocation (loc.Row, loc.Column);
			}
			
			public DomRegion ConvertRegion (Mono.CSharp.Location start, Mono.CSharp.Location end)
			{
				DomLocation startLoc = Convert (start);
				int lineNr = start.Row;
				var line = data.GetLine (lineNr);
				if (line != null) {
					if (line.GetIndentation (data).Length + 1 == start.Column) {
						lineNr--;
						while (lineNr > 1) {
							line = data.GetLine (lineNr);
							if (data.GetLine (lineNr).EditableLength != line.GetIndentation (data).Length)
								break;
							lineNr--;
						}
						startLoc = new DomLocation (lineNr, line.EditableLength + 1);
					}
				}
				
				var endLoc = Convert (end);
				endLoc.Column++;
				return new DomRegion (startLoc, endLoc);
			}
			
			static MonoDevelop.Projects.Dom.Modifiers ConvertModifiers (Mono.CSharp.Modifiers modifiers)
			{
				MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
				
				if ((modifiers & Mono.CSharp.Modifiers.PUBLIC) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Public;
				if ((modifiers & Mono.CSharp.Modifiers.PRIVATE) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Private;
				if ((modifiers & Mono.CSharp.Modifiers.PROTECTED) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Protected;
				if ((modifiers & Mono.CSharp.Modifiers.INTERNAL) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Internal;
				
				if ((modifiers & Mono.CSharp.Modifiers.ABSTRACT) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Abstract;
				if ((modifiers & Mono.CSharp.Modifiers.NEW) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.New;
				if ((modifiers & Mono.CSharp.Modifiers.SEALED) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Sealed;
				if ((modifiers & Mono.CSharp.Modifiers.READONLY) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Readonly;
				if ((modifiers & Mono.CSharp.Modifiers.VIRTUAL) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Virtual;
				if ((modifiers & Mono.CSharp.Modifiers.OVERRIDE) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Override;
				if ((modifiers & Mono.CSharp.Modifiers.EXTERN) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Extern;
				if ((modifiers & Mono.CSharp.Modifiers.VOLATILE) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Volatile;
				if ((modifiers & Mono.CSharp.Modifiers.UNSAFE) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Unsafe;
				
				if ((modifiers & Mono.CSharp.Modifiers.STATIC) != 0)
					result |= MonoDevelop.Projects.Dom.Modifiers.Static;
				return result;
			}
			
			void AddType (IType child)
			{
				if (typeStack.Count > 0) {
					typeStack.Peek ().Add (child);
				} else {
					Unit.Add (child);
				}
			}
			
			
			void AddTypeArguments (ATypeNameExpression texpr, DomReturnType result)
			{
				if (!texpr.HasTypeArguments)
					return;
				foreach (var arg in texpr.TypeArguments.Args) {
					result.AddTypeParameter (ConvertReturnType (arg));
				}
			}
			
			IReturnType ConvertReturnType (Expression typeName)
			{
				if (typeName is TypeExpression) {
					var typeExpr = (Mono.CSharp.TypeExpression)typeName;
					if (typeExpr.Type == Mono.CSharp.TypeManager.object_type)
						return DomReturnType.Object;
					if (typeExpr.Type == Mono.CSharp.TypeManager.string_type)
						return DomReturnType.String;
					if (typeExpr.Type == Mono.CSharp.TypeManager.int32_type)
						return DomReturnType.Int32;
					if (typeExpr.Type == Mono.CSharp.TypeManager.uint32_type)
						return DomReturnType.UInt32;
					if (typeExpr.Type == Mono.CSharp.TypeManager.int64_type)
						return DomReturnType.Int64;
					if (typeExpr.Type == Mono.CSharp.TypeManager.uint64_type)
						return DomReturnType.UInt64;
					if (typeExpr.Type == Mono.CSharp.TypeManager.float_type)
						return DomReturnType.Float;
					if (typeExpr.Type == Mono.CSharp.TypeManager.double_type)
						return DomReturnType.Double;
					if (typeExpr.Type == Mono.CSharp.TypeManager.char_type)
						return DomReturnType.Char;
					if (typeExpr.Type == Mono.CSharp.TypeManager.short_type)
						return DomReturnType.Int16;
					if (typeExpr.Type == Mono.CSharp.TypeManager.decimal_type)
						return DomReturnType.Decimal;
					if (typeExpr.Type == Mono.CSharp.TypeManager.bool_type)
						return DomReturnType.Bool;
					if (typeExpr.Type == Mono.CSharp.TypeManager.sbyte_type)
						return DomReturnType.SByte;
					if (typeExpr.Type == Mono.CSharp.TypeManager.byte_type)
						return DomReturnType.Byte;
					if (typeExpr.Type == Mono.CSharp.TypeManager.ushort_type)
						return DomReturnType.UInt16;
					if (typeExpr.Type == Mono.CSharp.TypeManager.void_type)
						return DomReturnType.Void;
					if (typeExpr.Type == Mono.CSharp.TypeManager.intptr_type)
						return DomReturnType.IntPtr;
					if (typeExpr.Type == Mono.CSharp.TypeManager.uintptr_type)
						return DomReturnType.UIntPtr;
					MonoDevelop.Core.LoggingService.LogError ("Error while converting :" + typeName + " - unknown type value");
					return DomReturnType.Void;
				}
				
				if (typeName is MemberAccess) {
					MemberAccess ma = (MemberAccess)typeName;
					var baseType = (DomReturnType)ConvertReturnType (ma.LeftExpression);
					// type expressions are global constants that never should be altered.
					if (ma.LeftExpression is TypeExpression)
						baseType = new DomReturnType (baseType.FullName);
					baseType.Parts.Add (new ReturnTypePart (ma.Name));
					AddTypeArguments (ma, baseType);
					return baseType;
				}
				
				if (typeName is SimpleName) {
					var sn = (SimpleName)typeName;
					var result = new DomReturnType (sn.Name);
					AddTypeArguments (sn, result);
					return result;
				}
				
				if (typeName is ComposedCast) {
					var cc = (ComposedCast)typeName;
					var baseType = (DomReturnType)ConvertReturnType (cc.Left);
					// type expressions are global constants that never should be altered.
					if (cc.Left is TypeExpression)
						baseType = new DomReturnType (baseType.FullName);
					if (cc.Spec.IsNullable) {
						baseType.IsNullable = true;
					} else if (cc.Spec.IsPointer) {
						baseType.PointerNestingLevel++;
					} else {
						baseType.ArrayDimensions++;
						baseType.SetDimension (baseType.ArrayDimensions - 1, cc.Spec.Dimension - 1);
					}
					return baseType;
				}
				if (typeName is SpecialContraintExpr) {
					var sce = (SpecialContraintExpr)typeName;
					if (sce.Constraint == SpecialConstraint.Struct)
						return new DomReturnType (DomReturnType.ValueType.FullName);
					if (sce.Constraint == SpecialConstraint.Class)
						return new DomReturnType (DomReturnType.Object.FullName);
					// atm we've no model in the dom to model new()
					return new DomReturnType (DomReturnType.Object.FullName);
				}
				MonoDevelop.Core.LoggingService.LogError ("Error while converting :" + typeName + " - unknown type name");
				return DomReturnType.Void;
			}
			
			
			IReturnType ConvertReturnType (MemberName name)
			{
				return ConvertReturnType (name.GetTypeExpression ());
			}
			
			#region Global
			string currentNamespaceName = "";
			Stack<UsingsBag.Namespace> currentNamespace = new Stack<UsingsBag.Namespace> ();
			
			string ConvertToString (MemberName name)
			{
				if (name == null)
					return "";
				
				return name.Left != null ? ConvertToString (name.Left)  + "." + name.Name : name.Name;
				
			}
			

			public override void Visit (UsingsBag.Namespace nspace)
			{
				string oldNamespace = currentNamespaceName;
				currentNamespace.Push (nspace);
				if (nspace.Name != null) { // no need to push the global namespace
					DomUsing domUsing = new DomUsing ();
					domUsing.IsFromNamespace = true;
					domUsing.Region = domUsing.ValidRegion = ConvertRegion (nspace.OpenBrace, nspace.CloseBrace); 
					string name = ConvertToString (nspace.Name);
					domUsing.Add (name);
					Unit.Add (domUsing);
					currentNamespaceName = string.IsNullOrEmpty (currentNamespaceName) ? name : currentNamespaceName + "." + name;
				}
				
				VisitNamespaceUsings (nspace);
				VisitNamespaceBody (nspace);
				currentNamespace.Pop ();
				currentNamespaceName = oldNamespace;
			}
			
			public override void Visit (UsingsBag.Using u)
			{
				DomUsing domUsing = new DomUsing ();
				domUsing.Region = ConvertRegion (u.UsingLocation, u.SemicolonLocation);
				domUsing.ValidRegion = ConvertRegion (currentNamespace.Peek ().OpenBrace, currentNamespace.Peek ().CloseBrace); 
				domUsing.Add (ConvertToString (u.NSpace));
				Unit.Add (domUsing);
			}
			
			public override void Visit (UsingsBag.AliasUsing u)
			{
				DomUsing domUsing = new DomUsing ();
				domUsing.Region = ConvertRegion (u.UsingLocation, u.SemicolonLocation);
				domUsing.ValidRegion = ConvertRegion (currentNamespace.Peek ().OpenBrace, currentNamespace.Peek ().CloseBrace); 
				domUsing.Add (u.Identifier.Value, new DomReturnType (ConvertToString (u.Nspace)));
				Unit.Add (domUsing);
			}
			
			public override void Visit (MemberCore member)
			{
				Console.WriteLine ("Unknown member:");
				Console.WriteLine (member.GetType () + "-> Member {0}", member.GetSignatureForError ());
			}
			
			Stack<DomType> typeStack = new Stack<DomType> ();
			
			void VisitType (TypeContainer c, ClassType classType)
			{
				DomType newType = new DomType ();
				newType.SourceProjectDom = Dom;
				newType.CompilationUnit = Unit;
				if (typeStack.Count == 0 && !string.IsNullOrEmpty (currentNamespaceName))
					newType.Namespace = currentNamespaceName;
				
				newType.Name = c.MemberName.Name;
				newType.Location = Convert (c.MemberName.Location);
				newType.ClassType = classType;
				var location = LocationsBag.GetMemberLocation (c);
				newType.BodyRegion = location != null && location.Count > 2 ? ConvertRegion (location[1], location[2]) : DomRegion.Empty;
				newType.Modifiers = ConvertModifiers (c.ModFlags);
				AddAttributes (newType, c.OptAttributes);
				AddTypeParameter (newType, c);
				
				if (c.TypeBaseExpressions != null) {
					foreach (var type in c.TypeBaseExpressions) {
						var baseType = ConvertReturnType (type);
					//	Console.WriteLine (newType.Name + " -- " + baseType);
						if (newType.BaseType == null) {
							newType.BaseType = baseType;
						} else {
							newType.AddInterfaceImplementation (baseType);
						}
					}
				}
				
				AddType (newType);
				// visit members
				typeStack.Push (newType);
				foreach (MemberCore member in c.OrderedAllMembers) {
					member.Accept (this);
				}
				typeStack.Pop ();
			}
			
			public override void Visit (Class c)
			{
				VisitType (c, ClassType.Class);
			}
			
			public override void Visit (Struct s)
			{
				VisitType (s, ClassType.Struct);
			}
			
			public override void Visit (Interface i)
			{
				VisitType (i, ClassType.Interface);
			}
			
			public override void Visit (Mono.CSharp.Enum e)
			{
				VisitType (e, ClassType.Enum);
			}
			
			public void AddAttributes (MonoDevelop.Projects.Dom.AbstractMember member, Attributes optAttributes)
			{
				if (optAttributes == null || optAttributes.Attrs == null)
					return;
				foreach (var attr in optAttributes.Attrs) {
					DomAttribute domAttribute = new DomAttribute ();
					domAttribute.Name = attr.Name;
					domAttribute.Region = ConvertRegion (attr.Location, attr.Location);
					domAttribute.AttributeType = new DomReturnType (attr.Name);
					member.Add (domAttribute);
				}
			}
			
			MonoDevelop.Projects.Dom.TypeParameter ConvertTemplateDefinition (Mono.CSharp.TypeParameter parameter)
			{
				var result = new MonoDevelop.Projects.Dom.TypeParameter (parameter.Name);
				if (parameter.Constraints != null) {
					foreach (var constraintExpr in parameter.Constraints.ConstraintExpressions) {
						result.AddConstraint (ConvertReturnType (constraintExpr));
					}
				}
				return result;
			}

			public void AddTypeParameter (AbstractTypeParameterMember member, DeclSpace decl)
			{
				if (!decl.IsGeneric || decl.CurrentTypeParameters == null)
					return;
				
				foreach (var typeParametr in decl.CurrentTypeParameters) {
					member.AddTypeParameter (ConvertTemplateDefinition (typeParametr));
					
				}
			}

			public override void Visit (Mono.CSharp.Delegate d)
			{
				DomType delegateType = DomType.CreateDelegate (Unit, d.MemberName.Name, Convert (d.MemberName.Location), ConvertReturnType (d.ReturnType), null);
				delegateType.SourceProjectDom = Dom;
				delegateType.Location = Convert (d.MemberName.Location);
				delegateType.Documentation = RetrieveDocumentation (d.MemberName.Location.Row);
				delegateType.Modifiers = ConvertModifiers (d.ModFlags);
				AddAttributes (delegateType, d.OptAttributes);
				
				AddParameter ((MonoDevelop.Projects.Dom.AbstractMember)delegateType.Methods.First (), d.Parameters);
				AddTypeParameter (delegateType, d);
				AddType (delegateType);
			
			}
			#endregion
			
			#region Type members

			
			public override void Visit (FixedField f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
				field.Documentation = RetrieveDocumentation (f.Location.Row);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
				field.ReturnType = ConvertReturnType (f.TypeName);
				AddAttributes (field, f.OptAttributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			
			public override void Visit (Field f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
				field.Documentation = RetrieveDocumentation (f.Location.Row);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
				field.ReturnType = ConvertReturnType (f.TypeName);
				AddAttributes (field, f.OptAttributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			
			public override void Visit (Const f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
				field.Documentation = RetrieveDocumentation (f.Location.Row);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
				field.ReturnType = ConvertReturnType (f.TypeName);
				AddAttributes (field, f.OptAttributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			
			void AddExplicitInterfaces (MonoDevelop.Projects.Dom.AbstractMember member, InterfaceMemberBase mcsMember)
			{
				if (!mcsMember.IsExplicitImpl)
					return;
				member.AddExplicitInterface (ConvertReturnType (mcsMember.MemberName.Left));
			}

			public override void Visit (EventField e)
			{
				DomEvent evt = new DomEvent ();
				evt.Name = e.MemberName.Name;
				evt.Documentation = RetrieveDocumentation (e.Location.Row);
				evt.Location = Convert (e.MemberName.Location);
				evt.Modifiers = ConvertModifiers (e.ModFlags);
				evt.ReturnType = ConvertReturnType (e.TypeName);
				AddAttributes (evt, e.OptAttributes);
				AddExplicitInterfaces (evt, e);
				evt.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (evt);
			}
			
			public override void Visit (EventProperty e)
			{
				DomEvent evt = new DomEvent ();
				evt.Name = e.MemberName.Name;
				evt.Documentation = RetrieveDocumentation (e.Location.Row);
				evt.Location = Convert (e.MemberName.Location);
				evt.Modifiers = ConvertModifiers (e.ModFlags);
				evt.ReturnType = ConvertReturnType (e.TypeName);
				var location = LocationsBag.GetMemberLocation (e);
				if (location != null)
					evt.BodyRegion = ConvertRegion (location[0], location[1]);
				
//				if (e.Add != null) {
//					property.GetterModifier = ConvertModifiers (p.Get.ModFlags);
//					if (p.Get.Block != null)
//						property.GetRegion = ConvertRegion (p.Get.Location, p.Get.Block.EndLocation);
//				}
//				
//				if (e.Remove != null) {
//					property.GetterModifier = ConvertModifiers (p.Get.ModFlags);
//					if (p.Get.Block != null)
//						property.GetRegion = ConvertRegion (p.Get.Location, p.Get.Block.EndLocation);
//				}
				
				AddAttributes (evt, e.OptAttributes);
				AddExplicitInterfaces (evt, e);
				
				evt.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (evt);
			}
			
			public override void Visit (Property p)
			{
				DomProperty property = new DomProperty ();
				property.Name = p.MemberName.Name;
				property.Documentation = RetrieveDocumentation (p.Location.Row);
				property.Location = Convert (p.MemberName.Location);
				property.GetterModifier = property.SetterModifier = ConvertModifiers (p.ModFlags);
				
				var location = LocationsBag.GetMemberLocation (p);
				if (location != null)
					property.BodyRegion = ConvertRegion (location[0], location[1]);
				property.ReturnType = ConvertReturnType (p.TypeName);
				
				AddAttributes (property, p.OptAttributes);
				AddExplicitInterfaces (property, p);
				
				if (p.Get != null) {
					property.PropertyModifier |= PropertyModifier.HasGet;
					if ((p.Get.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						property.GetterModifier = ConvertModifiers (p.Get.ModFlags);
					if (p.Get.Block != null)
						property.GetRegion = ConvertRegion (p.Get.Location, p.Get.Block.EndLocation);
				}
				
				if (p.Set != null) {
					property.PropertyModifier |= PropertyModifier.HasSet;
					if ((p.Set.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						property.SetterModifier = ConvertModifiers (p.Set.ModFlags);
					if (p.Set.Block != null)
						property.SetRegion = ConvertRegion (p.Set.Location, p.Set.Block.EndLocation);
				}
				property.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (property);
			}

			public void AddParameter (MonoDevelop.Projects.Dom.AbstractMember member, AParametersCollection parameters)
			{
				for (int i = 0; i < parameters.Count; i++) {
					var p = (Parameter)parameters.FixedParameters[i];
					DomParameter parameter = new DomParameter ();
					parameter.Name = p.Name;
					parameter.Location = Convert (p.Location);
					parameter.ReturnType = ConvertReturnType (p.TypeExpression);
					var modifiers = MonoDevelop.Projects.Dom.ParameterModifiers.None;
					if ((p.ParameterModifier & Parameter.Modifier.OUT) == Parameter.Modifier.OUT)
						modifiers |= MonoDevelop.Projects.Dom.ParameterModifiers.Out;
					if ((p.ParameterModifier & Parameter.Modifier.REF) == Parameter.Modifier.REF)
						modifiers |= MonoDevelop.Projects.Dom.ParameterModifiers.Ref;
					if ((p.ParameterModifier & Parameter.Modifier.PARAMS) == Parameter.Modifier.PARAMS)
						modifiers |= MonoDevelop.Projects.Dom.ParameterModifiers.Params;
					if ((p.ParameterModifier & Parameter.Modifier.This) == Parameter.Modifier.This)
						modifiers |= MonoDevelop.Projects.Dom.ParameterModifiers.This;
					parameter.ParameterModifiers = modifiers;
					member.Add (parameter);
				}
			}

			public override void Visit (Indexer i)
			{
				DomProperty indexer = new DomProperty ();
				indexer.Name = "this";
				indexer.Documentation = RetrieveDocumentation (i.Location.Row);
				indexer.Location = Convert (i.Location);
				indexer.GetterModifier = indexer.SetterModifier = ConvertModifiers (i.ModFlags);
				var location = LocationsBag.GetMemberLocation (i);
				if (location != null)
					indexer.BodyRegion = ConvertRegion (location[0], location[1]);
				
				indexer.ReturnType = ConvertReturnType (i.TypeName);
				AddParameter (indexer, i.Parameters);
				
				AddAttributes (indexer, i.OptAttributes);
				AddExplicitInterfaces (indexer, i);
				
				if (i.Get != null) {
					indexer.PropertyModifier |= PropertyModifier.HasGet;
					if ((i.Get.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						indexer.GetterModifier = ConvertModifiers (i.Get.ModFlags);
					if (i.Get.Block != null)
						indexer.GetRegion = ConvertRegion (i.Get.Location, i.Get.Block.EndLocation);
				}
				
				if (i.Set != null) {
					indexer.PropertyModifier |= PropertyModifier.HasSet;
					if ((i.Set.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						indexer.SetterModifier = ConvertModifiers (i.Set.ModFlags);
					if (i.Set.Block != null)
						indexer.SetRegion = ConvertRegion (i.Set.Location, i.Set.Block.EndLocation);
				}
				indexer.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (indexer);
			}

			public override void Visit (Method m)
			{
				DomMethod method = new DomMethod ();
				method.Name = m.MemberName.Name;
				method.Documentation = RetrieveDocumentation (m.Location.Row);
				method.Location = Convert (m.MemberName.Location);
				method.Modifiers = ConvertModifiers (m.ModFlags);
				if (m.Block != null)
					method.BodyRegion = ConvertRegion (m.Block.StartLocation, m.Block.EndLocation);
				method.ReturnType = ConvertReturnType (m.TypeName);
				AddAttributes (method, m.OptAttributes);
				AddParameter (method, m.ParameterInfo);
				AddExplicitInterfaces (method, m);
				method.Modifiers = ConvertModifiers (m.ModFlags);
				if (method.IsStatic && method.Parameters.Count > 0 && method.Parameters[0].ParameterModifiers == ParameterModifiers.This)
					method.MethodModifier |= MethodModifier.IsExtension;
				if (m.GenericMethod != null)
					AddTypeParameter (method, m.GenericMethod);
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}

			public override void Visit (Operator o)
			{
				DomMethod method = new DomMethod ();
				method.Name = o.MemberName.Name;
				method.Documentation = RetrieveDocumentation (o.Location.Row);
				method.Location = Convert (o.MemberName.Location);
				method.Modifiers = ConvertModifiers (o.ModFlags);
				if (o.Block != null)
					method.BodyRegion = ConvertRegion (o.Block.StartLocation, o.Block.EndLocation);
				method.Modifiers = ConvertModifiers (o.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.SpecialName;
				method.ReturnType = ConvertReturnType (o.TypeName);
				AddAttributes (method, o.OptAttributes);
				AddParameter (method, o.ParameterInfo);
				AddExplicitInterfaces (method, o);
				
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}
			
			
			public override void Visit (Constructor c)
			{
				DomMethod method = new DomMethod ();
				method.Name = ".ctor";
				method.Documentation = RetrieveDocumentation (c.Location.Row);
				method.Location = Convert (c.MemberName.Location);
				method.Modifiers = ConvertModifiers (c.ModFlags);
				if (c.Block != null)
					method.BodyRegion = ConvertRegion (c.Block.StartLocation, c.Block.EndLocation);
				method.Modifiers = ConvertModifiers (c.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.SpecialName;
				method.MethodModifier |= MethodModifier.IsConstructor;
				AddAttributes (method, c.OptAttributes);
				AddParameter (method, c.ParameterInfo);
				AddExplicitInterfaces (method, c);
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}
			
			public override void Visit (Destructor d)
			{
				DomMethod method = new DomMethod ();
				method.Name = ".dtor";
				method.Documentation = RetrieveDocumentation (d.Location.Row);
				method.Location = Convert (d.MemberName.Location);
				if (d.Block != null)
					method.BodyRegion = ConvertRegion (d.Block.StartLocation, d.Block.EndLocation);
				method.Modifiers = ConvertModifiers (d.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.SpecialName;
				method.MethodModifier |= MethodModifier.IsFinalizer;
				AddAttributes (method, d.OptAttributes);
				AddExplicitInterfaces (method, d);
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}
			
			public override void Visit (EnumMember f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
				field.Documentation = RetrieveDocumentation (f.Location.Row);
				// return types for enum fields are == null
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = MonoDevelop.Projects.Dom.Modifiers.Const | MonoDevelop.Projects.Dom.Modifiers.SpecialName| MonoDevelop.Projects.Dom.Modifiers.Public;
				AddAttributes (field, f.OptAttributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			#endregion
		}
	}
}