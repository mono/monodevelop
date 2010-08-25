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
			CompilerCompilationUnit top;
			ErrorReportPrinter errorReportPrinter = new ErrorReportPrinter ();
			using (var stream = new MemoryStream (Encoding.Default.GetBytes (content))) {
				top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v", "-unsafe"}, stream, fileName, errorReportPrinter);
			}
			if (top == null)
				return null;
			var conversionVisitor = new ConversionVisitor (top.LocationsBag);
			var unit =  new MonoDevelop.Projects.Dom.CompilationUnit (fileName);;
			conversionVisitor.Unit = unit;
			conversionVisitor.ParsedDocument = new ParsedDocument (fileName);
			conversionVisitor.ParsedDocument.CompilationUnit = unit;
			top.ModuleCompiled.Accept (conversionVisitor);
			errorReportPrinter.Errors.ForEach (e => conversionVisitor.ParsedDocument.Add (e));
			return conversionVisitor.ParsedDocument;
		}

		class ConversionVisitor : StructuralVisitor
		{
			ProjectDom Dom {
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
			
			public ConversionVisitor (LocationsBag locationsBag)
			{
				this.LocationsBag = locationsBag;
			}
			
			public static DomLocation Convert (Mono.CSharp.Location loc)
			{
				return new DomLocation (loc.Row, loc.Column);
			}
			
			public static DomRegion ConvertRegion (Mono.CSharp.Location start, Mono.CSharp.Location end)
			{
				return new DomRegion (Convert (start), Convert (end));
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
			
			IReturnType ConvertReturnType (FullNamedExpression typeName)
			{
				if (typeName is ATypeNameExpression) {
					var sn = (ATypeNameExpression)typeName;
					return new DomReturnType (sn.Name);
				}
				return DomReturnType.Void;
			}
			
			DomReturnType ConvertReturnType (TypeSpec type)
			{
				return new DomReturnType (type.Name);
			}
			
			#region Global
			public override void Visit (ModuleCompiled mc)
			{
				base.Visit (mc);
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
				newType.Name = c.MemberName.Name;
				newType.Location = Convert (c.MemberName.Location);
				newType.ClassType = ClassType.Class;
				var location = LocationsBag.GetMemberLocation (c);
				newType.BodyRegion = location != null ? ConvertRegion (location[1], location[2]) : DomRegion.Empty;
				newType.Modifiers = ConvertModifiers (c.ModFlags);
				
				/*
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
				*/
				
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
			
			public override void Visit (Mono.CSharp.Delegate d)
			{
				List<IParameter> parameters = new List<IParameter> ();
				
				for (int i = 0; i < d.Parameters.Count; i++) {
					var p = d.Parameters.FixedParameters[i];
					var t = d.Parameters.Types[i];
					// TODO: location & modifiers
					DomParameter parameter = new DomParameter ();
					parameter.Name = p.Name;
//					result.Location = Convert (pde.StartLocation);
					parameter.ReturnType = ConvertReturnType (t);
//					parameter.ParameterModifiers = ConvertParameterModifiers (pde.ParamModifier);
					parameters.Add (parameter);
				}
				
				DomType delegateType = DomType.CreateDelegate (Unit, d.MemberName.Name, Convert (d.MemberName.Location), ConvertReturnType (d.ReturnType), parameters);
				delegateType.SourceProjectDom = Dom;
//				delegateType.Documentation = RetrieveDocumentation (delegateDeclaration.StartLocation.Line);
				delegateType.Modifiers = ConvertModifiers (d.ModFlags);
//				AddAttributes (delegateType, delegateDeclaration.Attributes);
				
				parameters.ForEach (p => p.DeclaringMember = delegateType);
				
/*				if (delegateDeclaration.Templates != null && delegateDeclaration.Templates.Count > 0) {
					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in delegateDeclaration.Templates) {
						delegateType.AddTypeParameter (ConvertTemplateDefinition (template));
					}
				}*/
				
				/*
				if (d.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (d.MemberName);
					if (typeArgLocation != null)
						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
//					AddTypeArguments (newDelegate, typeArgLocation, d.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newDelegate, d);
				}
				*/
				AddType (delegateType);
			}
			#endregion
			
			#region Type members

			
			public override void Visit (FixedField f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
//				field.Documentation = RetrieveDocumentation (fieldDeclaration.StartLocation.Line);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
				field.ReturnType = ConvertReturnType (f.TypeName);
//				AddAttributes (field, fieldDeclaration.Attributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			
			public override void Visit (Field f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
//				field.Documentation = RetrieveDocumentation (fieldDeclaration.StartLocation.Line);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
				field.ReturnType = ConvertReturnType (f.TypeName);
//				AddAttributes (field, fieldDeclaration.Attributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			
			public override void Visit (Const f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
//				field.Documentation = RetrieveDocumentation (fieldDeclaration.StartLocation.Line);
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = ConvertModifiers (f.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.Fixed;
				field.ReturnType = ConvertReturnType (f.TypeName);
//				AddAttributes (field, fieldDeclaration.Attributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			

			public override void Visit (EventField e)
			{
				DomEvent evt = new DomEvent ();
				evt.Name = e.MemberName.Name;
//				evt.Documentation = RetrieveDocumentation (eventDeclaration.StartLocation.Line);
				evt.Location = Convert (e.MemberName.Location);
				evt.Modifiers = ConvertModifiers (e.ModFlags);
				evt.ReturnType = ConvertReturnType (e.TypeName);
//				evt.BodyRegion = ConvertRegion (eventDeclaration.BodyStart, eventDeclaration.BodyEnd);
//				if (eventDeclaration.AddRegion != null && !eventDeclaration.AddRegion.IsNull) {
//					DomMethod addMethod = new DomMethod ();
//					addMethod.Name = "add";
//					addMethod.BodyRegion = ConvertRegion (eventDeclaration.AddRegion.StartLocation, eventDeclaration.AddRegion.EndLocation);
//					evt.AddMethod = addMethod;
//				}
//				if (eventDeclaration.RemoveRegion != null && !eventDeclaration.RemoveRegion.IsNull) {
//					DomMethod removeMethod = new DomMethod ();
//					removeMethod.Name = "remove";
//					removeMethod.BodyRegion = ConvertRegion (eventDeclaration.RemoveRegion.StartLocation, eventDeclaration.RemoveRegion.EndLocation);
//					evt.RemoveMethod = removeMethod;
//				}
//				AddAttributes (evt, eventDeclaration.Attributes);
//				AddExplicitInterfaces (evt, eventDeclaration.InterfaceImplementations);
				evt.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (evt);
			}
			
			
			public override void Visit (Property p)
			{
				DomProperty property = new DomProperty ();
				property.Name = p.MemberName.Name;
//				property.Documentation = RetrieveDocumentation (propertyDeclaration.StartLocation.Line);
				property.Location = Convert (p.MemberName.Location);
				var location = LocationsBag.GetMemberLocation (p);
				if (location != null)
					property.BodyRegion = ConvertRegion (location[0], location[1]);
				property.ReturnType = ConvertReturnType (p.TypeName);
				
//				AddAttributes (property, propertyDeclaration.Attributes);
//				AddExplicitInterfaces (property, propertyDeclaration.InterfaceImplementations);
				
				if (p.Get != null) {
					property.PropertyModifier |= PropertyModifier.HasGet;
					property.GetterModifier = ConvertModifiers (p.Get.ModFlags);
					if (p.Get.Block != null)
						property.GetRegion = ConvertRegion (p.Get.Location, p.Get.Block.EndLocation);
				}
				
				if (p.Set != null) {
					property.PropertyModifier |= PropertyModifier.HasSet;
					property.SetterModifier = ConvertModifiers (p.Set.ModFlags);
					if (p.Set.Block != null)
						property.SetRegion = ConvertRegion (p.Set.Location, p.Set.Block.EndLocation);
				}
				property.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (property);
			}

			public void AddParameter (MonoDevelop.Projects.Dom.AbstractMember member, AParametersCollection parameters)
			{
/*				for (int i = 0; i < parameters.Count; i++) {
					var p = parameters.FixedParameters[i];
					var t = parameters.Types[i];
					// TODO: location & modifiers
					DomParameter parameter = new DomParameter ();
					parameter.Name = p.Name;
//					result.Location = Convert (pde.StartLocation);
					parameter.ReturnType = ConvertReturnType (t);
//					parameter.ParameterModifiers = ConvertParameterModifiers (pde.ParamModifier);
					member.Add (parameter);
				}*/
			}

			public override void Visit (Indexer i)
			{
				DomProperty indexer = new DomProperty ();
				indexer.Name = "this";
//				property.Documentation = RetrieveDocumentation (propertyDeclaration.StartLocation.Line);
				indexer.Location = Convert (i.Location);
				var location = LocationsBag.GetMemberLocation (i);
				if (location != null)
					indexer.BodyRegion = ConvertRegion (location[0], location[1]);
				
				indexer.ReturnType = ConvertReturnType (i.TypeName);
				AddParameter (indexer, i.Parameters);
				
//				AddAttributes (property, propertyDeclaration.Attributes);
//				AddExplicitInterfaces (property, propertyDeclaration.InterfaceImplementations);
				
				if (i.Get != null) {
					indexer.PropertyModifier |= PropertyModifier.HasGet;
					indexer.GetterModifier = ConvertModifiers (i.Get.ModFlags);
					if (i.Get.Block != null)
						indexer.GetRegion = ConvertRegion (i.Get.Location, i.Get.Block.EndLocation);
				}
				
				if (i.Set != null) {
					indexer.PropertyModifier |= PropertyModifier.HasSet;
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
//				method.Documentation = RetrieveDocumentation (methodDeclaration.StartLocation.Line);
				method.Location = Convert (m.MemberName.Location);
				if (m.Block != null)
					method.BodyRegion = ConvertRegion (m.Block.StartLocation, m.Block.EndLocation);
				method.Modifiers = ConvertModifiers (m.ModFlags);
//				if (methodDeclaration.IsExtensionMethod)
//					method.MethodModifier |= MethodModifier.IsExtension;
				method.ReturnType = ConvertReturnType (m.TypeName);
//				AddAttributes (method, methodDeclaration.Attributes);
				AddParameter (method, m.ParameterInfo);
//				AddExplicitInterfaces (method, methodDeclaration.InterfaceImplementations);
				
//				if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
//					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in methodDeclaration.Templates) {
//						TypeParameter parameter = ConvertTemplateDefinition (template);
//						method.AddTypeParameter (parameter);
//					}
//				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}

			public override void Visit (Operator o)
			{
				DomMethod method = new DomMethod ();
				method.Name = o.MemberName.Name;
//				method.Documentation = RetrieveDocumentation (methodDeclaration.StartLocation.Line);
				method.Location = Convert (o.MemberName.Location);
				if (o.Block != null)
					method.BodyRegion = ConvertRegion (o.Block.StartLocation, o.Block.EndLocation);
				method.Modifiers = ConvertModifiers (o.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.SpecialName;
//				if (methodDeclaration.IsExtensionMethod)
//					method.MethodModifier |= MethodModifier.IsExtension;
				method.ReturnType = ConvertReturnType (o.TypeName);
//				AddAttributes (method, methodDeclaration.Attributes);
				AddParameter (method, o.ParameterInfo);
//				AddExplicitInterfaces (method, methodDeclaration.InterfaceImplementations);
				
//				if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
//					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in methodDeclaration.Templates) {
//						TypeParameter parameter = ConvertTemplateDefinition (template);
//						method.AddTypeParameter (parameter);
//					}
//				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}
			
			
			public override void Visit (Constructor c)
			{
				DomMethod method = new DomMethod ();
				method.Name = ".ctor";
//				method.Documentation = RetrieveDocumentation (methodDeclaration.StartLocation.Line);
				method.Location = Convert (c.MemberName.Location);
				if (c.Block != null)
					method.BodyRegion = ConvertRegion (c.Block.StartLocation, c.Block.EndLocation);
				method.Modifiers = ConvertModifiers (c.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.SpecialName;
				method.MethodModifier |= MethodModifier.IsConstructor;
//				if (methodDeclaration.IsExtensionMethod)
//					method.MethodModifier |= MethodModifier.IsExtension;
//				AddAttributes (method, methodDeclaration.Attributes);
				AddParameter (method, c.ParameterInfo);
//				AddExplicitInterfaces (method, methodDeclaration.InterfaceImplementations);
				
//				if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
//					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in methodDeclaration.Templates) {
//						TypeParameter parameter = ConvertTemplateDefinition (template);
//						method.AddTypeParameter (parameter);
//					}
//				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}
			
			public override void Visit (Destructor d)
			{
				DomMethod method = new DomMethod ();
				method.Name = ".dtor";
//				method.Documentation = RetrieveDocumentation (methodDeclaration.StartLocation.Line);
				method.Location = Convert (d.MemberName.Location);
				if (d.Block != null)
					method.BodyRegion = ConvertRegion (d.Block.StartLocation, d.Block.EndLocation);
				method.Modifiers = ConvertModifiers (d.ModFlags) | MonoDevelop.Projects.Dom.Modifiers.SpecialName;
				method.MethodModifier |= MethodModifier.IsFinalizer;
//				if (methodDeclaration.IsExtensionMethod)
//					method.MethodModifier |= MethodModifier.IsExtension;
//				AddAttributes (method, methodDeclaration.Attributes);
//				AddExplicitInterfaces (method, methodDeclaration.InterfaceImplementations);
				
//				if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
//					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in methodDeclaration.Templates) {
//						TypeParameter parameter = ConvertTemplateDefinition (template);
//						method.AddTypeParameter (parameter);
//					}
//				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
			}
			
			public override void Visit (EnumMember f)
			{
				DomField field = new DomField ();
				field.Name = f.MemberName.Name;
//				field.ReturnType = new DomReturnType (typeStack.Peek ());
				field.Location = Convert (f.MemberName.Location);
				field.Modifiers = MonoDevelop.Projects.Dom.Modifiers.Const | MonoDevelop.Projects.Dom.Modifiers.SpecialName| MonoDevelop.Projects.Dom.Modifiers.Public;
//				AddAttributes (field, fieldDeclaration.Attributes);
				field.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (field);
			}
			
			#endregion
		}
	}
}