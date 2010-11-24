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
using System.CodeDom;

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
		
		static string GetLangString (LangVersion ver)
		{
			switch (ver) {
			case LangVersion.Default:
				return "Default";
			case LangVersion.ISO_1:
				return "ISO-1";
			case LangVersion.ISO_2:
				return "ISO-2";
			}
			return "Default";
		}
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			lock (CompilerCallableEntryPoint.parseLock) {
				if (string.IsNullOrEmpty (content))
					return null;
				
				List<string> compilerArguments = new List<string> ();
				if (dom != null && dom.Project != null && MonoDevelop.Ide.IdeApp.Workspace != null) {
					DotNetProjectConfiguration configuration = dom.Project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
					CSharpCompilerParameters par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
					if (par != null) {
						if (!string.IsNullOrEmpty (par.DefineSymbols)) {
							compilerArguments.Add ("-define:" + string.Join (";", par.DefineSymbols.Split (';', ',', ' ', '\t')));
						}
						if (par.UnsafeCode)
							compilerArguments.Add ("-unsafe");
						if (par.TreatWarningsAsErrors)
							compilerArguments.Add ("-warnaserror");
						if (!string.IsNullOrEmpty (par.NoWarnings))
							compilerArguments.Add ("-nowarn:"+ string.Join (",", par.NoWarnings.Split (';', ',', ' ', '\t')));
						compilerArguments.Add ("-warn:" + par.WarningLevel);
						compilerArguments.Add ("-langversion:" + GetLangString (par.LangVersion));
						if (par.GenerateOverflowChecks)
							compilerArguments.Add ("-checked");
					}
				}
				
				var unit =  new MonoDevelop.Projects.Dom.CompilationUnit (fileName);
				var result = new ParsedDocument (fileName);
				result.CompilationUnit = unit;
				
				CompilerCompilationUnit top;
				ErrorReportPrinter errorReportPrinter = new ErrorReportPrinter ();
				using (var stream = new MemoryStream (Encoding.Default.GetBytes (content))) {
					top = CompilerCallableEntryPoint.ParseFile (compilerArguments.ToArray (), stream, fileName, errorReportPrinter);
				}
				if (top == null)
					return null;
				
				foreach (var special in top.SpecialsBag.Specials) {
					var comment = special as SpecialsBag.Comment;
					if (comment != null) {
						VisitComment (result, comment);
					} else {
						VisitPreprocessorDirective (result, special as SpecialsBag.PreProcessorDirective);
					}
				}
				
				// convert DOM
				var conversionVisitor = new ConversionVisitor (top.LocationsBag);
				conversionVisitor.Dom = dom;
				conversionVisitor.ParsedDocument = result;
				conversionVisitor.Unit = unit;
				top.UsingsBag.Global.Accept (conversionVisitor);
				
				unit.Tag = top;
				
				
				
				// parser errors
				errorReportPrinter.Errors.ForEach (e => conversionVisitor.ParsedDocument.Add (e));
				return result;
			}
		}
		
		void VisitComment (ParsedDocument result, SpecialsBag.Comment comment)
		{
			var cmt = new MonoDevelop.Projects.Dom.Comment (comment.Content);
			cmt.CommentStartsLine = comment.StartsLine;
			switch (comment.CommentType) {
			case SpecialsBag.CommentType.Multi:
				cmt.CommentType = MonoDevelop.Projects.Dom.CommentType.MultiLine;
				cmt.OpenTag = "/*";
				cmt.ClosingTag = "*/";
				break;
			case SpecialsBag.CommentType.Single:
				cmt.CommentType = MonoDevelop.Projects.Dom.CommentType.SingleLine;
				cmt.OpenTag = "//";
				break;
			case SpecialsBag.CommentType.Documentation:
				cmt.CommentType = MonoDevelop.Projects.Dom.CommentType.SingleLine;
				cmt.IsDocumentation = true;
				cmt.OpenTag = "///";
				break;
			}
			cmt.Region = new DomRegion (comment.Line, comment.Col, comment.EndLine, comment.EndCol);
			result.Comments.Add (cmt);
		}

		Stack<SpecialsBag.PreProcessorDirective> regions = new Stack<SpecialsBag.PreProcessorDirective> ();
		Stack<SpecialsBag.PreProcessorDirective> ifBlocks = new Stack<SpecialsBag.PreProcessorDirective> ();
		List<SpecialsBag.PreProcessorDirective> elifBlocks = new List<SpecialsBag.PreProcessorDirective> ();
		SpecialsBag.PreProcessorDirective elseBlock = null;

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

		void AddCurRegion (ParsedDocument result, int line, int col)
		{
			if (ConditionalRegion == null)
				return;
			ConditionalRegion.End = new DomLocation (line, col);
			result.Add (ConditionalRegion);
			conditionalRegions.Pop ();
		}

		static ICSharpCode.NRefactory.PrettyPrinter.CSharpOutputVisitor visitor = new ICSharpCode.NRefactory.PrettyPrinter.CSharpOutputVisitor ();

		void VisitPreprocessorDirective (ParsedDocument result, SpecialsBag.PreProcessorDirective directive)
		{
			DomLocation loc = new DomLocation (directive.Line, directive.Col);
			switch (directive.Cmd) {
			case Tokenizer.PreprocessorDirective.If:
				conditionalRegions.Push (new ConditionalRegion (visitor.Text));
				ifBlocks.Push (directive);
				ConditionalRegion.Start = loc;
				break;
			case Tokenizer.PreprocessorDirective.Elif:
				CloseConditionBlock (new DomLocation (directive.EndLine, directive.EndCol));
				if (ConditionalRegion != null)
					ConditionalRegion.ConditionBlocks.Add (new ConditionBlock (visitor.Text, loc));
				break;
			case Tokenizer.PreprocessorDirective.Else:
				CloseConditionBlock (new DomLocation (directive.EndLine, directive.EndCol));
				if (ConditionalRegion != null)
					ConditionalRegion.ElseBlock = new DomRegion (loc, DomLocation.Empty);
				break;
			case Tokenizer.PreprocessorDirective.Endif:
				DomLocation endLoc = new DomLocation (directive.EndLine, directive.EndCol);
				CloseConditionBlock (endLoc);
				if (ConditionalRegion != null && !ConditionalRegion.ElseBlock.Start.IsEmpty)
					ConditionalRegion.ElseBlock = new DomRegion (ConditionalRegion.ElseBlock.Start, endLoc);
				AddCurRegion (result, directive.EndLine, directive.EndCol);
				if (ifBlocks.Count > 0) {
					var ifBlock = ifBlocks.Pop ();
					DomRegion dr = new DomRegion (ifBlock.Line, ifBlock.Col, directive.EndLine, directive.EndCol);
					result.Add (new FoldingRegion ("#if " + ifBlock.Arg.Trim (), dr, FoldType.UserRegion, false));
					foreach (var d in elifBlocks) {
						dr.Start = new DomLocation (d.Line, d.Col);
						result.Add (new FoldingRegion ("#elif " + ifBlock.Arg.Trim (), dr, FoldType.UserRegion, false));
					}
					if (elseBlock != null) {
						dr.Start = new DomLocation (elseBlock.Line, elseBlock.Col);
						result.Add (new FoldingRegion ("#else", dr, FoldType.UserRegion, false));
					}
				}
				elseBlock = null;
				break;
			case Tokenizer.PreprocessorDirective.Define:
				result.Add (new PreProcessorDefine (directive.Arg, loc));
				break;
			case Tokenizer.PreprocessorDirective.Region:
				regions.Push (directive);
				break;
			case Tokenizer.PreprocessorDirective.Endregion:
				if (regions.Count > 0) {
					var start = regions.Pop ();
					DomRegion dr = new DomRegion (start.Line, start.Col, directive.EndLine, directive.EndCol);
					result.Add (new FoldingRegion (start.Arg, dr, FoldType.UserRegion, true));
				}
				break;
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
			
			public MonoDevelop.Projects.Dom.CompilationUnit Unit {
				get;
				set;
			}
			
			public ConversionVisitor (LocationsBag locationsBag)
			{
				this.LocationsBag = locationsBag;
			}
			
			int lastComment = 0;
			string RetrieveDocumentation (int upToLine)
			{
				StringBuilder result = null;
				while (lastComment < ParsedDocument.Comments.Count) {
					var cur = ParsedDocument.Comments[lastComment];
					if (cur.Region.Start.Line >= upToLine)
						break;
					if (cur.IsDocumentation) {
						if (result == null)
							result = new StringBuilder ();
						result.Append (cur.Text);
					}
					lastComment++;
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
						return new DomReturnType (DomReturnType.Object.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.string_type)
						return new DomReturnType (DomReturnType.String.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.int32_type)
						return new DomReturnType (DomReturnType.Int32.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.uint32_type)
						return new DomReturnType (DomReturnType.UInt32.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.int64_type)
						return new DomReturnType (DomReturnType.Int64.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.uint64_type)
						return new DomReturnType (DomReturnType.UInt64.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.float_type)
						return new DomReturnType (DomReturnType.Float.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.double_type)
						return new DomReturnType (DomReturnType.Double.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.char_type)
						return new DomReturnType (DomReturnType.Char.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.short_type)
						return new DomReturnType (DomReturnType.Int16.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.decimal_type)
						return new DomReturnType (DomReturnType.Decimal.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.bool_type)
						return new DomReturnType (DomReturnType.Bool.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.sbyte_type)
						return new DomReturnType (DomReturnType.SByte.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.byte_type)
						return new DomReturnType (DomReturnType.Byte.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.ushort_type)
						return new DomReturnType (DomReturnType.UInt16.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.void_type)
						return new DomReturnType (DomReturnType.Void.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.intptr_type)
						return new DomReturnType (DomReturnType.IntPtr.FullName);
					if (typeExpr.Type == Mono.CSharp.TypeManager.uintptr_type)
						return new DomReturnType (DomReturnType.UIntPtr.FullName);
					MonoDevelop.Core.LoggingService.LogError ("Error while converting :" + typeName + " - unknown type value");
					return DomReturnType.Void;
				}
				
				if (typeName is Mono.CSharp.QualifiedAliasMember) {
					var qam = (Mono.CSharp.QualifiedAliasMember)typeName;
					// TODO: Overwork the return type model - atm we don't have a good representation
					// for qualified alias members.
					return new DomReturnType (qam.Name);
				}
				
				if (typeName is MemberAccess) {
					MemberAccess ma = (MemberAccess)typeName;
					var baseType = (DomReturnType)ConvertReturnType (ma.LeftExpression);
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
					if (cc.Spec.IsNullable) {
						return new DomReturnType ("System.Nullable", true, new IReturnType[] { baseType });
					} else if (cc.Spec.IsPointer) {
						baseType.PointerNestingLevel++;
					} else {
						baseType.ArrayDimensions++;
						baseType.SetDimension (baseType.ArrayDimensions - 1, cc.Spec.Dimension - 1);
					}
					return baseType;
				}
				MonoDevelop.Core.LoggingService.LogError ("Error while converting :" + typeName + " - unknown type name");
				return new DomReturnType (DomReturnType.Void.FullName);
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
					string name = ConvertToString (nspace.Name);
					string[] splittedNamespace = name.Split ('.');
					for (int i = splittedNamespace.Length; i > 0; i--) {
						DomUsing domUsing = new DomUsing ();
						domUsing.IsFromNamespace = true;
						domUsing.Region = domUsing.ValidRegion = ConvertRegion (nspace.OpenBrace, nspace.CloseBrace); 
						domUsing.Add (string.Join (".", splittedNamespace, 0, i));
						Unit.Add (domUsing);
					}
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
				
				if (location != null && location.Count > 2) {
					var region = ConvertRegion (c.MemberName.Location, location[2]);
					region.Start = new DomLocation (region.Start.Line, region.Start.Column + c.MemberName.Name.Length);
					newType.BodyRegion =  region;
				} else {
					var region = ConvertRegion (c.MemberName.Location, c.MemberName.Location);
					region.Start = new DomLocation (region.Start.Line, region.Start.Column + c.MemberName.Name.Length);
					region.End = new DomLocation (int.MaxValue, int.MaxValue);
					newType.BodyRegion =  region;
				}
				
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
			
			class CodeDomVisitor : StructuralVisitor
			{
				public override object Visit (Constant constant)
				{
					return new CodePrimitiveExpression (constant.GetValue ());
				}
				
				public override object Visit (Unary unaryExpression)
				{
					var exprResult = (CodeExpression)unaryExpression.Expr.Accept (this);
					
					switch (unaryExpression.Oper) {
					case Unary.Operator.UnaryPlus:
						return exprResult;
					case Unary.Operator.UnaryNegation: // -a => 0 - a
						return new CodeBinaryOperatorExpression (new CodePrimitiveExpression (0), CodeBinaryOperatorType.Subtract, exprResult);
					case Unary.Operator.LogicalNot: // !a => a == false
						return new CodeBinaryOperatorExpression (exprResult, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression (false));
					}
					return exprResult;
				}
				static CodeBinaryOperatorType Convert (Binary.Operator o)
				{
					switch (o) {
					case Binary.Operator.Multiply:
						return CodeBinaryOperatorType.Multiply;
					case Binary.Operator.Division:
						return CodeBinaryOperatorType.Divide;
					case Binary.Operator.Modulus:
						return CodeBinaryOperatorType.Modulus;
					case Binary.Operator.Addition:
						return CodeBinaryOperatorType.Add;
					case Binary.Operator.Subtraction:
						return CodeBinaryOperatorType.Subtract;
					case Binary.Operator.LeftShift:
					case Binary.Operator.RightShift:
						return CodeBinaryOperatorType.Multiply; // unsupported
					case Binary.Operator.LessThan:
						return CodeBinaryOperatorType.LessThan;
					case Binary.Operator.GreaterThan:
						return CodeBinaryOperatorType.GreaterThan;
					case Binary.Operator.LessThanOrEqual:
						return CodeBinaryOperatorType.LessThanOrEqual;
					case Binary.Operator.GreaterThanOrEqual:
						return CodeBinaryOperatorType.GreaterThanOrEqual;
					case Binary.Operator.Equality:
						return CodeBinaryOperatorType.IdentityEquality;
					case Binary.Operator.Inequality:
						return CodeBinaryOperatorType.IdentityInequality;
					case Binary.Operator.BitwiseAnd:
						return CodeBinaryOperatorType.BitwiseAnd;
					case Binary.Operator.ExclusiveOr:
						return CodeBinaryOperatorType.BitwiseOr; // unsupported
					case Binary.Operator.BitwiseOr:
						return CodeBinaryOperatorType.BitwiseOr;
					case Binary.Operator.LogicalAnd:
						return CodeBinaryOperatorType.BooleanAnd;
					case Binary.Operator.LogicalOr:
						return CodeBinaryOperatorType.BooleanOr;
							
					}
					return CodeBinaryOperatorType.Add;
				}
				
				public override object Visit (Binary binaryExpression)
				{
					return new CodeBinaryOperatorExpression (
						(CodeExpression)binaryExpression.Left.Accept (this),
						Convert (binaryExpression.Oper),
						(CodeExpression)binaryExpression.Right.Accept (this));
				}
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
					if (attr.PosArguments != null) {
						for (int i = 0; i < attr.PosArguments.Count; i++) {
							var val = attr.PosArguments[i].Expr as Constant;
							if (val == null) {
								continue;
							}
							domAttribute.AddPositionalArgument (new CodePrimitiveExpression (val.GetValue ()));
						}
					}
					if (attr.NamedArguments != null) {
						for (int i = 0; i < attr.NamedArguments.Count; i++) {
							var val = attr.NamedArguments[i].Expr as Constant;
							if (val == null)
								continue;
							domAttribute.AddNamedArgument (((NamedArgument)attr.NamedArguments[i]).Name, new CodePrimitiveExpression (val.GetValue ()));
						}
					}
					
					member.Add (domAttribute);
				}
			}
			
			MonoDevelop.Projects.Dom.TypeParameter ConvertTemplateDefinition (Mono.CSharp.TypeParameter parameter)
			{
				var result = new MonoDevelop.Projects.Dom.TypeParameter (parameter.Name);
				if (parameter.Constraints != null) {
					foreach (var constraintExpr in parameter.Constraints.ConstraintExpressions) {
						if (constraintExpr is SpecialContraintExpr) {
							var sce = (SpecialContraintExpr)constraintExpr;
							if (sce.Constraint == SpecialConstraint.Struct)
								result.ValueTypeRequired = true;
							if (sce.Constraint == SpecialConstraint.Class)
								result.ClassRequired = true;
							if (sce.Constraint == SpecialConstraint.Constructor)
								result.ConstructorRequired = true;
						} else {
							result.AddConstraint (ConvertReturnType (constraintExpr));
						}
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
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						field = new DomField ();
						field.Name = decl.Name.Value;
						field.Location = Convert (decl.Name.Location);
						field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
						field.ReturnType = ConvertReturnType (f.TypeName);
						AddAttributes (field, f.OptAttributes);
						field.DeclaringType = typeStack.Peek ();
						typeStack.Peek ().Add (field);
					}
				}
			}
			
			public override void Visit (Field f)
			{
				var field = new DomField ();
				field.Name = f.MemberName.Name;
				field.Documentation = RetrieveDocumentation (f.Location.Row);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags);
				field.ReturnType = ConvertReturnType (f.TypeName);
				AddAttributes (field, f.OptAttributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
				
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						field = new DomField ();
						field.Name = decl.Name.Value;
						field.Location = Convert (decl.Name.Location);
						field.Modifiers = ConvertModifiers (f.ModFlags);
						field.ReturnType = ConvertReturnType (f.TypeName);
						AddAttributes (field, f.OptAttributes);
						field.DeclaringType = typeStack.Peek ();
						typeStack.Peek ().Add (field);
					}
				}
			}
			
			public override void Visit (Const f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
				field.Documentation = RetrieveDocumentation (f.Location.Row);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Const;
				field.ReturnType = ConvertReturnType (f.TypeName);
				AddAttributes (field, f.OptAttributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						field = new DomField ();
						field.Name = decl.Name.Value;
						field.Location = Convert (decl.Name.Location);
						field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Const;
						field.ReturnType = ConvertReturnType (f.TypeName);
						AddAttributes (field, f.OptAttributes);
						field.DeclaringType = typeStack.Peek ();
						typeStack.Peek ().Add (field);
					}
				}
			}
			
			void AddExplicitInterfaces (MonoDevelop.Projects.Dom.AbstractMember member, InterfaceMemberBase mcsMember)
			{
				if (!mcsMember.IsExplicitImpl)
					return;
				member.AddExplicitInterface (ConvertReturnType (mcsMember.MemberName.Left));
			}

			public override void Visit (EventField e)
			{
				var evt = new DomEvent ();
				evt.Name = e.MemberName.Name;
				evt.Documentation = RetrieveDocumentation (e.Location.Row);
				evt.Location = Convert (e.MemberName.Location);
				evt.Modifiers = ConvertModifiers (e.ModFlags);
				evt.ReturnType = ConvertReturnType (e.TypeName);
				AddAttributes (evt, e.OptAttributes);
				AddExplicitInterfaces (evt, e);
				evt.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (evt);
				if (e.Declarators != null) {
					foreach (var decl in e.Declarators) {
						evt = new DomEvent ();
						evt.Name = decl.Name.Value;
						evt.Location = Convert (decl.Name.Location);
						evt.Modifiers = ConvertModifiers (e.ModFlags);
						evt.ReturnType = ConvertReturnType (e.TypeName);
						AddAttributes (evt, e.OptAttributes);
						evt.DeclaringType = typeStack.Peek ();
						typeStack.Peek ().Add (evt);
					}
				}
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
				if (location != null && location.Count >= 1) {
					var endLoc = location.Count == 1 ? location[0] : location[1];
					property.BodyRegion = ConvertRegion (location[0], endLoc);
				} else {
					property.BodyRegion = DomRegion.Empty;
				}
				property.ReturnType = ConvertReturnType (p.TypeName);
				
				AddAttributes (property, p.OptAttributes);
				AddExplicitInterfaces (property, p);
				
				if (p.Get != null) {
					property.PropertyModifier |= PropertyModifier.HasGet;
					if ((p.Get.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						property.GetterModifier = ConvertModifiers (p.Get.ModFlags);
					if (p.Get.Block != null) {
						property.GetRegion = ConvertRegion (p.Get.Location, p.Get.Block.EndLocation);
					} else {
						var getLocation = LocationsBag.GetMemberLocation (p.Get);
						property.GetRegion = ConvertRegion (p.Get.Location, getLocation.Count > 0 ? getLocation[0] : p.Get.Location);
					}
				}
				
				if (p.Set != null) {
					property.PropertyModifier |= PropertyModifier.HasSet;
					if ((p.Set.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						property.SetterModifier = ConvertModifiers (p.Set.ModFlags);
					if (p.Set.Block != null) {
						property.SetRegion = ConvertRegion (p.Set.Location, p.Set.Block.EndLocation);
					} else {
						var setLocation = LocationsBag.GetMemberLocation (p.Set);
						property.SetRegion = ConvertRegion (p.Set.Location, setLocation.Count > 0 ? setLocation[0] : p.Set.Location);
					}
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
				if (location != null && location.Count >= 1) {
					var endLoc = location.Count == 1 ? location[0] : location[1];
					indexer.BodyRegion = ConvertRegion (location[0], endLoc);
				} else {
					indexer.BodyRegion = DomRegion.Empty;
				}
				
				indexer.ReturnType = ConvertReturnType (i.TypeName);
				AddParameter (indexer, i.Parameters);
				
				AddAttributes (indexer, i.OptAttributes);
				AddExplicitInterfaces (indexer, i);
				
				if (i.Get != null) {
					indexer.PropertyModifier |= PropertyModifier.HasGet;
					if ((i.Get.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						indexer.GetterModifier = ConvertModifiers (i.Get.ModFlags);
					if (i.Get.Block != null) {
						indexer.GetRegion = ConvertRegion (i.Get.Location, i.Get.Block.EndLocation);
					} else {
						var getLocation = LocationsBag.GetMemberLocation (i.Get);
						indexer.GetRegion = ConvertRegion (i.Get.Location, getLocation.Count > 0 ? getLocation[0] : i.Get.Location);
					}
				}
				
				if (i.Set != null) {
					indexer.PropertyModifier |= PropertyModifier.HasSet;
					if ((i.Set.ModFlags & Mono.CSharp.Modifiers.AccessibilityMask) != 0)
						indexer.SetterModifier = ConvertModifiers (i.Set.ModFlags);
					if (i.Set.Block != null) {
						indexer.SetRegion = ConvertRegion (i.Set.Location, i.Set.Block.EndLocation);
					} else {
						var setLocation = LocationsBag.GetMemberLocation (i.Set);
						indexer.SetRegion = ConvertRegion (i.Set.Location, setLocation.Count > 0 ? setLocation[0] : i.Set.Location);
					}
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
				if (m.Block != null) {
					var location = LocationsBag.GetMemberLocation (m);
					var region = ConvertRegion (location != null ? location[1] : m.Block.StartLocation, m.Block.EndLocation);
					if (location != null)
						region.Start = new DomLocation (region.Start.Line, region.Start.Column + 1);
					method.BodyRegion = region;
				}
				
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
				if (o.Block != null) {
					var location = LocationsBag.GetMemberLocation (o);
					var region = ConvertRegion (location != null ? location[1] : o.Block.StartLocation, o.Block.EndLocation);
					if (location != null)
						region.Start = new DomLocation (region.Start.Line, region.Start.Column + 1);
					method.BodyRegion = region;
				}
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
				if (c.Block != null) {
					var location = LocationsBag.GetMemberLocation (c);
					var region = ConvertRegion (location != null ? location[1] : c.Block.StartLocation, c.Block.EndLocation);
					if (location != null)
						region.Start = new DomLocation (region.Start.Line, region.Start.Column + 1);
					method.BodyRegion = region;
				}
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
				if (d.Block != null) {
					var location = LocationsBag.GetMemberLocation (d);
					var region = ConvertRegion (location != null ? location[1] : d.Block.StartLocation, d.Block.EndLocation);
					if (location != null)
						region.Start = new DomLocation (region.Start.Line, region.Start.Column + 1);
					method.BodyRegion = region;
				}
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
