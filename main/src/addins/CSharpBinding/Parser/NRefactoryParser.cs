//
// NRefactoryParser.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.CodeDom;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory.Visitors;
using CSharpBinding;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryParser : AbstractParser
	{
		public override bool CanParse (string fileName)
		{
			return Path.GetExtension (fileName) == ".cs";
		}
		
		public NRefactoryParser () : base ("C#", "text/x-csharp")
		{
		}
		
		public override IExpressionFinder CreateExpressionFinder (ProjectDom dom)
		{
			return new MonoDevelop.CSharpBinding.Gui.NewCSharpExpressionFinder (dom);
		}
		
		public override IResolver CreateResolver (ProjectDom dom, object editor, string fileName)
		{
			MonoDevelop.Ide.Gui.Document doc = (MonoDevelop.Ide.Gui.Document)editor;
			return new NRefactoryResolver (dom, doc.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, doc.TextEditor, fileName);
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
				newComment.Text              = comment.CommentText;
				int commentTagLength = comment.CommentType == ICSharpCode.NRefactory.CommentType.Documentation ? 3 : 2;
				int commentEndOffset = comment.CommentType == ICSharpCode.NRefactory.CommentType.Block ? 0 : 1;
				newComment.Region    = new DomRegion (comment.StartPosition.Line, comment.StartPosition.Column - commentTagLength, comment.EndPosition.Line, comment.EndPosition.Column - commentEndOffset);
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
			List<ICSharpCode.NRefactory.PreprocessingDirective>  elifBlocks = new List<ICSharpCode.NRefactory.PreprocessingDirective> ();
			ICSharpCode.NRefactory.PreprocessingDirective  elseBlock = null;
			
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
						ConditionalRegion.ConditionBlocks.Add (new ConditionBlock (visitor.Text, loc));
						visitor.Reset ();
//						elifBlocks.Add (directive);
						break;
					case "#else":
						CloseConditionBlock (new DomLocation (directive.LastLineEnd.Line, directive.LastLineEnd.Column));
						ConditionalRegion.ElseBlock = new DomRegion (loc, DomLocation.Empty);
//						elseBlock = directive;
						break;
					case "#endif":
						DomLocation endLoc = new DomLocation (directive.LastLineEnd.Line, directive.LastLineEnd.Column);
						CloseConditionBlock (endLoc);
						if (!ConditionalRegion.ElseBlock.Start.IsEmpty)
							ConditionalRegion.ElseBlock = new DomRegion (ConditionalRegion.ElseBlock.Start, endLoc);
						AddCurRegion (directive.EndPosition);
						if (ifBlocks.Count > 0) {
							ICSharpCode.NRefactory.PreprocessingDirective ifBlock = ifBlocks.Pop ();
							Console.WriteLine (ifBlock);
							DomRegion dr = new DomRegion (ifBlock.StartPosition.Line, ifBlock.StartPosition.Column, directive.EndPosition.Line, directive.EndPosition.Column);
							result.Add  (new FoldingRegion ("#if " + ifBlock.Arg.Trim (), dr, FoldType.UserRegion, false));
							foreach (ICSharpCode.NRefactory.PreprocessingDirective d in elifBlocks) {
								dr.Start = new DomLocation (d.StartPosition.Line, d.StartPosition.Column);
								result.Add  (new FoldingRegion ("#elif " + ifBlock.Arg.Trim (), dr, FoldType.UserRegion, false));
							}
							if (elseBlock != null) {
								dr.Start = new DomLocation (elseBlock.StartPosition.Line, elseBlock.StartPosition.Column);
								result.Add  (new FoldingRegion ("#else", dr, FoldType.UserRegion, false));
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
							DomRegion dr = new DomRegion (start.StartPosition.Line, 
								start.StartPosition.Column, directive.EndPosition.Line,
								directive.EndPosition.Column);
							result.Add (new FoldingRegion (start.Arg, dr, FoldType.UserRegion, true));
						}
						break;
				}
				return null;
			}
		}
		public ICSharpCode.NRefactory.Ast.CompilationUnit LastUnit {
			get;
			set;
		}
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StringReader (content))) {
				
				ParsedDocument result = new ParsedDocument (fileName);
				result.CompilationUnit = new MonoDevelop.Projects.Dom.CompilationUnit (fileName);
				
				parser.Errors.Error += delegate (int line, int col, string message) {
					result.Add (new Error (ErrorType.Error, line, col, message));
				};
				parser.Lexer.SpecialCommentTags = ProjectDomService.SpecialCommentTags.GetNames ();
				parser.Lexer.EvaluateConditionalCompilation = true;
				if (dom != null && dom.Project != null) {
					DotNetProjectConfiguration conf = dom.Project.DefaultConfiguration as DotNetProjectConfiguration;
					CSharpCompilerParameters par = conf != null ? conf.CompilationParameters as CSharpCompilerParameters : null;
					if (par != null)
						parser.Lexer.SetConditionalCompilationSymbols (par.DefineSymbols);
				}
				parser.Parse ();
				
				SpecialTracker tracker = new SpecialTracker (result);
				foreach (ICSharpCode.NRefactory.ISpecial special in parser.Lexer.SpecialTracker.CurrentSpecials) {
					special.AcceptVisitor (tracker, null);
				}
				
				foreach (ICSharpCode.NRefactory.Parser.TagComment tagComment in parser.Lexer.TagComments) {
					result.Add (new Tag (tagComment.Tag, 
					                     tagComment.CommentText, 
					                     new DomRegion (tagComment.StartPosition.Y, tagComment.StartPosition.X, tagComment.EndPosition.Y, tagComment.EndPosition.X)));
				}
				ConversionVisitior visitor = new ConversionVisitior (result);
				visitor.VisitCompilationUnit (parser.CompilationUnit, null);
				LastUnit = parser.CompilationUnit;
				return result;
			}
		}

		class ConversionVisitior : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			MonoDevelop.Projects.Dom.ParsedDocument result;
			
			public ConversionVisitior (MonoDevelop.Projects.Dom.ParsedDocument result)
			{
				this.result = result;
			}
			
			static DomRegion ConvertRegion (ICSharpCode.NRefactory.Location start, ICSharpCode.NRefactory.Location end)
			{
				return new DomRegion (start.Line, start.Column, end.Line, end.Column);
			}
			
			static DomLocation ConvertLocation (ICSharpCode.NRefactory.Location location)
			{
				return new DomLocation (location.Line, location.Column);
			}

			static Modifiers ConvertModifiers (ICSharpCode.NRefactory.Ast.Modifiers modifiers)
			{
				return (Modifiers)modifiers;
			}
			
			static ClassType ConvertClassType (ICSharpCode.NRefactory.Ast.ClassType nrClassType)
			{
				switch (nrClassType) {
					case ICSharpCode.NRefactory.Ast.ClassType.Class:
						return ClassType.Class;
					case ICSharpCode.NRefactory.Ast.ClassType.Struct:
						return ClassType.Struct;
					case ICSharpCode.NRefactory.Ast.ClassType.Interface:
						return ClassType.Interface;
					case ICSharpCode.NRefactory.Ast.ClassType.Enum:
						return ClassType.Enum;
				}
				return ClassType.Class;
			}

			static DomReturnType ConvertReturnType (ICSharpCode.NRefactory.Ast.TypeReference typeReference)
			{
				DomReturnType result = new DomReturnType (typeReference.SystemType != null ? typeReference.SystemType : typeReference.Type);
				result.PointerNestingLevel = typeReference.PointerNestingLevel;
				result.ArrayDimensions = typeReference.RankSpecifier != null ? typeReference.RankSpecifier.Length : 0;
				for (int i = 0; i < result.ArrayDimensions; i++) {
					result.SetDimension (i, typeReference.RankSpecifier[i]);
				}
				
				if (typeReference.GenericTypes != null && typeReference.GenericTypes.Count > 0) {
					foreach (ICSharpCode.NRefactory.Ast.TypeReference genericArgument in typeReference.GenericTypes) {
						result.AddTypeParameter (ConvertReturnType (genericArgument));
					}
				}
				
				return result;
			}
			
			static void AddAttributes (AbstractMember member, IEnumerable<ICSharpCode.NRefactory.Ast.AttributeSection> attributes)
			{
				CodeDomVisitor domVisitor = new CodeDomVisitor ();
				foreach (ICSharpCode.NRefactory.Ast.AttributeSection attributeSection in attributes) {
					foreach (ICSharpCode.NRefactory.Ast.Attribute attribute in attributeSection.Attributes) {
						DomAttribute domAttribute = new DomAttribute ();
						domAttribute.Name   = attribute.Name;
						domAttribute.Region = ConvertRegion (attribute.StartLocation, attribute.EndLocation);
						member.Add (domAttribute);
						foreach (ICSharpCode.NRefactory.Ast.Expression exp in attribute.PositionalArguments)
							domAttribute.AddPositionalArgument ((CodeExpression)exp.AcceptVisitor (domVisitor, null));
						foreach (ICSharpCode.NRefactory.Ast.NamedArgumentExpression nexp in attribute.NamedArguments)
							domAttribute.AddNamedArgument (nexp.Name, (CodeExpression) nexp.Expression.AcceptVisitor (domVisitor, null));
					}
				}
			}
			
			static void AddExplicitInterfaces (AbstractMember member, IEnumerable<ICSharpCode.NRefactory.Ast.InterfaceImplementation> interfaceImplementations)
			{
				if (interfaceImplementations == null)
					return;
				
				foreach (ICSharpCode.NRefactory.Ast.InterfaceImplementation impl in interfaceImplementations) {
					member.AddExplicitInterface (ConvertReturnType (impl.InterfaceType));
				}
			}
			
			public override object VisitUsingDeclaration (ICSharpCode.NRefactory.Ast.UsingDeclaration usingDeclaration, object data)
			{
				DomUsing domUsing = new DomUsing ();
				domUsing.Region   = ConvertRegion (usingDeclaration.StartLocation, usingDeclaration.EndLocation);
				
				foreach (ICSharpCode.NRefactory.Ast.Using u in usingDeclaration.Usings) {
					if (u.IsAlias) {
						domUsing.Add (u.Name, ConvertReturnType (u.Alias));
					} else {
						domUsing.Add (u.Name);
					}
				}
				((CompilationUnit)result.CompilationUnit).Add (domUsing);
				return data;
			}

			Stack<string> namespaceStack = new Stack<string> ();
			public override object VisitNamespaceDeclaration (ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
			{
				string[] splittedNamespace = namespaceDeclaration.Name.Split ('.');
				for (int i = splittedNamespace.Length; i > 0; i--) {
					DomUsing domUsing = new DomUsing ();
					domUsing.IsFromNamespace = true;
					domUsing.Region   = ConvertRegion (namespaceDeclaration.StartLocation, namespaceDeclaration.EndLocation);
					
					domUsing.Add (String.Join (".", splittedNamespace, 0, i));
					((CompilationUnit)result.CompilationUnit).Add (domUsing);
				}
				
				namespaceStack.Push (namespaceStack.Count == 0 ? namespaceDeclaration.Name : namespaceStack.Peek() + "." + namespaceDeclaration.Name);
				namespaceDeclaration.AcceptChildren (this, data);
				namespaceStack.Pop ();
				return null;
			}

			Stack<DomType> typeStack = new Stack<DomType> ();
			public override object VisitTypeDeclaration (ICSharpCode.NRefactory.Ast.TypeDeclaration typeDeclaration, object data)
			{
				DomType newType = new DomType ();
				newType.Name      = typeDeclaration.Name;
				newType.Location  = ConvertLocation (typeDeclaration.StartLocation);
				newType.ClassType = ConvertClassType (typeDeclaration.Type);
				DomRegion region = ConvertRegion (typeDeclaration.BodyStartLocation, typeDeclaration.EndLocation);
				region.End = new DomLocation (region.End.Line, region.End.Column + 1);
				newType.BodyRegion = region;
				newType.Modifiers  = ConvertModifiers (typeDeclaration.Modifier);
				
				AddAttributes (newType, typeDeclaration.Attributes);
				
				foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in typeDeclaration.Templates) {
					TypeParameter parameter = ConvertTemplateDefinition (template);
					newType.AddTypeParameter (parameter);
				}
				
				if (typeDeclaration.BaseTypes != null) {
					
					foreach (ICSharpCode.NRefactory.Ast.TypeReference type in typeDeclaration.BaseTypes) {
						if (type == typeDeclaration.BaseTypes[0]) {
							newType.BaseType = ConvertReturnType (type);
						} else {
							newType.AddInterfaceImplementation (ConvertReturnType (type));
						}
					}
				}
				AddType (newType);
				
				// visit members
				typeStack.Push (newType);
				typeDeclaration.AcceptChildren (this, data);
				typeStack.Pop ();
				
				return null;
			}

			TypeParameter ConvertTemplateDefinition (ICSharpCode.NRefactory.Ast.TemplateDefinition template)
			{
				TypeParameter parameter = new TypeParameter (template.Name);
				foreach (ICSharpCode.NRefactory.Ast.TypeReference typeRef in template.Bases) {
					DomReturnType rt = ConvertReturnType (typeRef);
					if (rt.FullName == "constraint: struct")
						parameter.ValueTypeRequired = true;
					else if (rt.FullName == "constraint: class")
						parameter.ClassRequired = true;
					else if (rt.FullName == "constraint: new")
						parameter.ConstructorRequired = true;
					else
						parameter.AddConstraint (rt);
				}
				return parameter;
			}

			void AddType (DomType type)
			{
				// add type to compilation unit or outer type
				if (typeStack.Count > 0) {
					DomType outerType = typeStack.Peek ();
					type.DeclaringType = outerType;
					outerType.Add (type);
				} else {
					if (namespaceStack.Count > 0) 
						type.Namespace = namespaceStack.Peek ();
					((CompilationUnit)result.CompilationUnit).Add (type);
				}
			}

			static ParameterModifiers ConvertParameterModifiers (ICSharpCode.NRefactory.Ast.ParameterModifiers modifier)
			{
				if ((modifier & ICSharpCode.NRefactory.Ast.ParameterModifiers.Out) != 0)
					return ParameterModifiers.Out;
				if ((modifier & ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref) != 0)
					return ParameterModifiers.Ref;
				if ((modifier & ICSharpCode.NRefactory.Ast.ParameterModifiers.Params) != 0)
					return ParameterModifiers.Params;
				return ParameterModifiers.None;
			}
			
			static DomParameter ConvertParameter (IMember declaringMember, ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression pde)
			{
				DomParameter result = new DomParameter ();
				result.Name               = pde.ParameterName;
				result.Location           = ConvertLocation (pde.StartLocation);
				result.DeclaringMember    = declaringMember;
				result.ReturnType         = ConvertReturnType (pde.TypeReference);
				result.ParameterModifiers = ConvertParameterModifiers (pde.ParamModifier);
				return result;
			}
			
			static List<IParameter> ConvertParameterList (IMember declaringMember, IEnumerable<ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression> parameters)
			{
				List<IParameter> result = new List<IParameter> ();
				if (parameters != null) {
					foreach (ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression pde in parameters) {
						result.Add (ConvertParameter (declaringMember, pde));
					}
				}
				return result;
			}
			
			public override object VisitDelegateDeclaration (ICSharpCode.NRefactory.Ast.DelegateDeclaration delegateDeclaration, object data)
			{
				List<IParameter> parameter = ConvertParameterList (null, delegateDeclaration.Parameters);
				DomType delegateType = DomType.CreateDelegate (result.CompilationUnit, 
				                                               delegateDeclaration.Name,
				                                               ConvertLocation (delegateDeclaration.StartLocation),
				                                               ConvertReturnType (delegateDeclaration.ReturnType),
				                                               parameter);
				delegateType.Location = ConvertLocation (delegateDeclaration.StartLocation);
				delegateType.Modifiers  = ConvertModifiers (delegateDeclaration.Modifier);
				AddAttributes (delegateType, delegateDeclaration.Attributes);
				
				foreach (DomParameter p in parameter) {
					p.DeclaringMember = delegateType;
				}
				
				AddType (delegateType);
				
				return null;
			}
			
			public override object VisitConstructorDeclaration (ICSharpCode.NRefactory.Ast.ConstructorDeclaration constructorDeclaration, object data)
			{
				DomMethod constructor = new DomMethod ();
				constructor.Name      = ".ctor";
				constructor.MethodModifier |= MethodModifier.IsConstructor;
				constructor.Location  = ConvertLocation (constructorDeclaration.StartLocation);
				constructor.BodyRegion = ConvertRegion (constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				constructor.Modifiers  = ConvertModifiers (constructorDeclaration.Modifier);
				AddAttributes (constructor, constructorDeclaration.Attributes);
				constructor.Add (ConvertParameterList (constructor, constructorDeclaration.Parameters));
				
				constructor.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (constructor);
				return null;
			}
		
			public override object VisitMethodDeclaration (ICSharpCode.NRefactory.Ast.MethodDeclaration methodDeclaration, object data)
			{
				DomMethod method = new DomMethod ();
				method.Name      = methodDeclaration.Name;
				method.Location  = ConvertLocation (methodDeclaration.StartLocation);
				method.BodyRegion = ConvertRegion (methodDeclaration.EndLocation, methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				method.Modifiers  = ConvertModifiers (methodDeclaration.Modifier);
				if (methodDeclaration.IsExtensionMethod)
					method.MethodModifier |= MethodModifier.IsExtension;
				method.ReturnType = ConvertReturnType (methodDeclaration.TypeReference);
				AddAttributes (method, methodDeclaration.Attributes);
				method.Add (ConvertParameterList (method, methodDeclaration.Parameters));
				AddExplicitInterfaces (method, methodDeclaration.InterfaceImplementations);
				
				if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in methodDeclaration.Templates) {
						TypeParameter parameter = ConvertTemplateDefinition (template);
						method.AddTypeParameter (parameter);
					}
				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
				
				return null;
			}

			public override object VisitDestructorDeclaration (ICSharpCode.NRefactory.Ast.DestructorDeclaration destructorDeclaration, object data)
			{
				DomMethod destructor = new DomMethod ();
				destructor.Name      = ".dtor";
				
				destructor.Location  = ConvertLocation (destructorDeclaration.StartLocation);
				destructor.BodyRegion = ConvertRegion (destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				destructor.Modifiers  = ConvertModifiers (destructorDeclaration.Modifier);
				AddAttributes (destructor, destructorDeclaration.Attributes);
				destructor.MethodModifier |= MethodModifier.IsFinalizer;
				
				destructor.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (destructor);
				
				return null;
			}
			
			static string GetOperatorName (ICSharpCode.NRefactory.Ast.OperatorDeclaration operatorDeclaration)
			{
				if (operatorDeclaration == null)
					return null;
				bool isBinary = operatorDeclaration.Parameters.Count == 2;
				switch (operatorDeclaration.OverloadableOperator) {
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Add:
					return isBinary ? "op_Addition" : "op_UnaryPlus";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Subtract:
					return isBinary ? "op_Subtraction" : "op_UnaryNegation";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Multiply:
					return "op_Multiply";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Divide:
					return "op_Division";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Modulus:
					return "op_Modulus";
					
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Not:
					return "op_LogicalNot";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.BitNot:
					return "op_OnesComplement";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.BitwiseAnd:
					return "op_BitwiseAnd";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.BitwiseOr:
					return "op_BitwiseOr";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.ExclusiveOr:
					return "op_ExclusiveOr";
					
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.ShiftLeft:
					return "op_LeftShift";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.ShiftRight:
					return "op_RightShift";
					
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.GreaterThan:
					return "op_GreaterThan";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.GreaterThanOrEqual:
					return "op_GreaterThanOrEqual";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Equality:
					return "op_Equality";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.InEquality:
					return "op_Inequality";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.LessThan:
					return "op_LessThan";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.LessThanOrEqual:
					return "op_LessThanOrEqual";
					
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Increment:
					return "op_Increment";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.Decrement:
					return "op_Decrement";
					
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.IsTrue:
					return "op_True";
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.IsFalse:
					return "op_False";
					
				case ICSharpCode.NRefactory.Ast.OverloadableOperatorType.None:
					switch (operatorDeclaration.ConversionType) {
						case ICSharpCode.NRefactory.Ast.ConversionType.Implicit:
							return "op_Implicit";
						case ICSharpCode.NRefactory.Ast.ConversionType.Explicit:
							return "op_Explicit";
					}
					break;
				}
				return null;
			}
			
			public override object VisitOperatorDeclaration (ICSharpCode.NRefactory.Ast.OperatorDeclaration operatorDeclaration, object data)
			{
				DomMethod method = new DomMethod ();
				method.Name      = GetOperatorName (operatorDeclaration);
				method.Location  = ConvertLocation (operatorDeclaration.StartLocation);
				method.BodyRegion = ConvertRegion (operatorDeclaration.EndLocation, operatorDeclaration.Body != null ? operatorDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				method.Modifiers  = ConvertModifiers (operatorDeclaration.Modifier) | Modifiers.SpecialName;
				if (operatorDeclaration.IsExtensionMethod)
					method.MethodModifier |= MethodModifier.IsExtension;
				method.ReturnType = ConvertReturnType (operatorDeclaration.TypeReference);
				AddAttributes (method, operatorDeclaration.Attributes);
				method.Add (ConvertParameterList (method, operatorDeclaration.Parameters));
				AddExplicitInterfaces (method, operatorDeclaration.InterfaceImplementations);
				
				if (operatorDeclaration.Templates != null && operatorDeclaration.Templates.Count > 0) {
					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition td in operatorDeclaration.Templates) {
						method.AddTypeParameter (ConvertTemplateDefinition (td));
					}
				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
				
				return null;
			}

			public override object VisitFieldDeclaration (ICSharpCode.NRefactory.Ast.FieldDeclaration fieldDeclaration, object data)
			{
				foreach (ICSharpCode.NRefactory.Ast.VariableDeclaration varDecl in fieldDeclaration.Fields) {
					DomField field = new DomField ();
					field.Name      = varDecl.Name;
					field.Location  = ConvertLocation (fieldDeclaration.StartLocation);
					field.Modifiers  = ConvertModifiers (fieldDeclaration.Modifier);
					if (typeStack.Peek ().ClassType == ClassType.Enum) {
						field.ReturnType = new DomReturnType (typeStack.Peek ());
					} else {
						field.ReturnType = ConvertReturnType (fieldDeclaration.TypeReference);
					}
					// Enum fields have an empty type.
					if (field.ReturnType != null && string.IsNullOrEmpty (field.ReturnType.FullName))
						field.ReturnType = null;
					AddAttributes (field, fieldDeclaration.Attributes);
					field.DeclaringType = typeStack.Peek ();
					if (field.DeclaringType.ClassType == ClassType.Enum) {
						field.Modifiers |= Modifiers.Const;
						field.Modifiers |= Modifiers.SpecialName;
						field.Modifiers |= Modifiers.Public;
					}
					typeStack.Peek ().Add (field);
				}
				return null;
			}
			
			public override object VisitPropertyDeclaration (ICSharpCode.NRefactory.Ast.PropertyDeclaration propertyDeclaration, object data)
			{
				DomProperty property = new DomProperty ();
				property.Name      = propertyDeclaration.Name;
				property.Location  = ConvertLocation (propertyDeclaration.StartLocation);
				property.BodyRegion = ConvertRegion (propertyDeclaration.EndLocation, propertyDeclaration.BodyEnd);
				property.Modifiers  = ConvertModifiers (propertyDeclaration.Modifier);
				property.ReturnType = ConvertReturnType (propertyDeclaration.TypeReference);
				AddAttributes (property, propertyDeclaration.Attributes);
				AddExplicitInterfaces (property, propertyDeclaration.InterfaceImplementations);
				if (propertyDeclaration.HasGetRegion) {
					property.PropertyModifier |= PropertyModifier.HasGet;
					property.GetRegion = ConvertRegion (propertyDeclaration.GetRegion.StartLocation, propertyDeclaration.GetRegion.EndLocation);
				}
				if (propertyDeclaration.HasSetRegion) {
					property.PropertyModifier |= PropertyModifier.HasSet;
					property.SetRegion = ConvertRegion (propertyDeclaration.SetRegion.StartLocation, propertyDeclaration.SetRegion.EndLocation);
				}
				property.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (property);
				return null;
			}
			
			public override object VisitIndexerDeclaration (ICSharpCode.NRefactory.Ast.IndexerDeclaration indexerDeclaration, object data)
			{
				DomProperty indexer = new DomProperty ();
				indexer.Name      = "this";
				indexer.PropertyModifier |= PropertyModifier.IsIndexer;
				indexer.Location  = ConvertLocation (indexerDeclaration.StartLocation);
				indexer.BodyRegion = ConvertRegion (indexerDeclaration.EndLocation, indexerDeclaration.BodyEnd);
				indexer.Modifiers  = ConvertModifiers (indexerDeclaration.Modifier);
				indexer.ReturnType = ConvertReturnType (indexerDeclaration.TypeReference);
				indexer.Add (ConvertParameterList (indexer, indexerDeclaration.Parameters));
				
				AddAttributes (indexer, indexerDeclaration.Attributes);
				AddExplicitInterfaces (indexer, indexerDeclaration.InterfaceImplementations);
				
				if (indexerDeclaration.HasGetRegion) {
					indexer.PropertyModifier |= PropertyModifier.HasGet;
					indexer.GetRegion = ConvertRegion (indexerDeclaration.GetRegion.StartLocation, indexerDeclaration.GetRegion.EndLocation);
				}
				if (indexerDeclaration.HasSetRegion) {
					indexer.PropertyModifier |= PropertyModifier.HasSet;
					indexer.SetRegion = ConvertRegion (indexerDeclaration.SetRegion.StartLocation, indexerDeclaration.SetRegion.EndLocation);
				}
				indexer.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (indexer);
				return null;
			}
			
			public override object VisitEventDeclaration (ICSharpCode.NRefactory.Ast.EventDeclaration eventDeclaration, object data)
			{
				DomEvent evt = new DomEvent ();
				evt.Name      = eventDeclaration.Name;
				evt.Location  = ConvertLocation (eventDeclaration.StartLocation);
				evt.Modifiers  = ConvertModifiers (eventDeclaration.Modifier);
				evt.ReturnType = ConvertReturnType (eventDeclaration.TypeReference);
				evt.BodyRegion = ConvertRegion (eventDeclaration.BodyStart, eventDeclaration.BodyEnd);
				if (eventDeclaration.AddRegion != null && !eventDeclaration.AddRegion.IsNull) {
					DomMethod addMethod = new DomMethod ();
					addMethod.Name = "add";
					addMethod.BodyRegion = ConvertRegion (eventDeclaration.AddRegion.StartLocation, eventDeclaration.AddRegion.EndLocation); 
					evt.AddMethod = addMethod;
				}
				if (eventDeclaration.RemoveRegion != null && !eventDeclaration.RemoveRegion.IsNull) {
					DomMethod removeMethod = new DomMethod ();
					removeMethod.Name = "remove";
					removeMethod.BodyRegion = ConvertRegion (eventDeclaration.RemoveRegion.StartLocation, eventDeclaration.RemoveRegion.EndLocation); 
					evt.RemoveMethod = removeMethod;
				}
				AddAttributes (evt, eventDeclaration.Attributes);
				AddExplicitInterfaces (evt, eventDeclaration.InterfaceImplementations);
				evt.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (evt);
				return null;
			}
		}
	}
}
