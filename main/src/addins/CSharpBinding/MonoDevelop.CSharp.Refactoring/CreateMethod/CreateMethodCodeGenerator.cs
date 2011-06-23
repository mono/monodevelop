// 
// CreateMethod.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Text;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Parser;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Formatting;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.CSharp.Refactoring.CreateMethod
{
	public class CreateMethodCodeGenerator : RefactoringOperation
	{
		public CreateMethodCodeGenerator ()
		{
			Name = "Create Method";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			try {
				return Analyze (options);
			} catch (Exception e) {
				LoggingService.LogError ("Exception while create method analyzation", e);
				return false;
			}
		}

		InvocationExpression GetInvocation (ICSharpCode.NRefactory.CSharp.CompilationUnit unit, TextEditorData data)
		{
			var containingNode = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
			var curNode = containingNode;
			while (curNode != null && !(curNode is InvocationExpression)) {
				curNode = curNode.Parent;
			}
			return curNode as InvocationExpression;
		}
		
		bool AnalyzeTargetExpression (RefactoringOptions options, ICSharpCode.NRefactory.CSharp.CompilationUnit unit)
		{
			var data = options.GetTextEditorData ();
			var ctx = options.Document.TypeResolveContext;
			var target = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
			if (target == null)
				return false;
			if (target.Parent is MemberReferenceExpression && ((MemberReferenceExpression)target.Parent).GetChildByRole (MemberReferenceExpression.Roles.Identifier) == target) {
				var memberReference = (MemberReferenceExpression)target.Parent;
				target = memberReference.Target;
				var targetResult = options.Resolve (target);
//				if (targetResult is TypeResolveResult)
//					modifiers = MonoDevelop.Projects.Dom.Modifiers.Static;
				if (targetResult == null)
					return false;
				var type = targetResult.Type.Resolve (ctx);
				if (type == null || type.GetDefinition () == null)
					return false;
				declaringType = type.GetDefinition ().GetParts ().First ();
				methodName = memberReference.MemberName;
			} else if (target is Identifier) {
				declaringType = options.Document.ParsedDocument.GetTypeDefinition (options.Location);
				methodName = data.GetTextBetween (target.StartLocation.Line, target.StartLocation.Column, target.EndLocation.Line, target.EndLocation.Column);
			}
			if (declaringType != null && !HasCompatibleMethod (ctx, declaringType, methodName, invocation)) {
/*				if (declaringType.GetDefinition ().GetParts ())
					declaringType = declaringType.Parts.FirstOrDefault (t => t.GetDefinition ().Region.FileName == options.Document.FileName) ?? declaringType;
				if (declaringType == null || declaringType.CompilationUnit == null)
					return false;*/
				var doc = IdeApp.Workbench.OpenDocument (declaringType.Region.FileName);
				declaringType = doc.ParsedDocument.GetTypeDefinition (declaringType.Region.Begin) ?? declaringType;
				return true;
			}
			return false;
		}
		
		IType GetDelegateType (RefactoringOptions options, ICSharpCode.NRefactory.CSharp.CompilationUnit unit)
		{
			var data = options.GetTextEditorData ();
			var containingNode = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
			var parent = containingNode.Parent;
			
			while (parent != null) {
				if (parent is AssignmentExpression) {
					AssignmentExpression assignment = (AssignmentExpression)parent;
					if (assignment.Operator != AssignmentOperatorType.Add && assignment.Operator != AssignmentOperatorType.Subtract && assignment.Operator != AssignmentOperatorType.Assign)
						return null;
					
					var resolveResult = ResolveAssignment (options, assignment);
					if (resolveResult == null)
						return null;
					IType type = resolveResult.Type.Resolve (options.Document.TypeResolveContext);
					if (type == null || !type.IsDelegate ())
						return null;
					return type;
				}
				parent = parent.Parent;
			}
			return null;
		}
		
		public bool HasCompatibleMethod (ITypeResolveContext ctx, IType type, string methodName, InvocationExpression invocation)
		{
			// TODO: add argument type check for overloads. 
			if (invocation == null || type == null)
				return false;
			int invocationArguments = invocation.Arguments.Count ();
			return type.GetMethods (ctx).Any (m =>  m.Name == methodName && m.Parameters.Count == invocationArguments);
		}
		
		ResolveResult ResolveAssignment (RefactoringOptions options, AssignmentExpression assignment)
		{
			return options.Resolve (assignment.Left);
		}

		IType GuessReturnType (RefactoringOptions options)
		{
			AstNode node = invocation;
			while (node != null) {
				if (node.Parent is VariableInitializer) {
					var resolveResult = options.Resolve (node.Parent);
					return resolveResult.Type;
				}
				if (node.Parent is AssignmentExpression) {
					var resolveResult = ResolveAssignment (options, (AssignmentExpression)node.Parent);
					if (resolveResult != null)
						return resolveResult.Type;
				}
				if (node.Parent is InvocationExpression) {
					var parentInvocation = (InvocationExpression)node.Parent;
					int idx = 0;
					foreach (var arg in parentInvocation.Arguments) {
						if (arg == node)
							break;
						idx++;
					}
					var resolveResult = options.Resolve (parentInvocation);
					if (resolveResult != null) {
						return resolveResult.Type;
					}
					return options.Document.TypeResolveContext.GetTypeDefinition (typeof(object));
				}
				
				node = node.Parent;
			}
			return KnownTypeReference.Void.Resolve (options.Document.TypeResolveContext);
		}
		
		public bool Analyze (RefactoringOptions options)
		{
			var data = options.GetTextEditorData ();
			if (data.Document.MimeType != CSharpFormatter.MimeType)
				return false;
			var unit = options.Document.ParsedDocument.Annotation<CompilationUnit> ();
			
			if (!AnalyzeTargetExpression (options, unit))
				return false;
			invocation = GetInvocation (unit, data);
			if (invocation != null) 
				return AnalyzeInvocation (options);
			delegateType = GetDelegateType (options, unit);
			return delegateType != null;
		}
		Accessibility accessibility = Accessibility.None;
		public bool AnalyzeInvocation (RefactoringOptions options)
		{
			bool isInInterface = declaringType.ClassType == ClassType.Interface;
			if (isInInterface) {
	//			modifiers = MonoDevelop.Projects.Dom.Modifiers.None;
			} else {
	//			bool isStatic = (modifiers & MonoDevelop.Projects.Dom.Modifiers.Static) != 0;
				if (options.ResolveResult != null) {
			//		modifiers = options.ResolveResult.CallingMember.Modifiers;
		//			if (declaringType.DecoratedFullName != options.ResolveResult.CallingType.DecoratedFullName) {
						accessibility = Accessibility.Public;
		//				if (options.ResolveResult.CallingMember.IsStatic)
		//					isStatic = true;
		//			}
				} else {
//					var member = options.Document.CompilationUnit.GetMemberAt (options.Document.Editor.Caret.Line, options.Document.Editor.Caret.Column);
//					if (member != null)
//						modifiers = member.Modifiers;
				}
//				if (isStatic)
//					modifiers |= MonoDevelop.Projects.Dom.Modifiers.Static;
			}
			returnType = GuessReturnType (options);
			
			return true;
		}
		
		ITypeDefinition declaringType;
		public ITypeDefinition DeclaringType {
			get {
				return this.declaringType;
			}
		}
		
		ITypeReference returnType;
		string methodName;
		
		InvocationExpression invocation;
		IType delegateType;
		TextEditorData data;
		
		InsertionPoint insertionPoint;
		public void SetInsertionPoint (InsertionPoint point)
		{
			this.insertionPoint = point;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Create Method");
		}
		
		public override void Run (RefactoringOptions options)
		{
			fileName = declaringType.GetDefinition ().Region.FileName;
			
			var openDocument = IdeApp.Workbench.OpenDocument (fileName);
			if (openDocument == null) {
				MessageService.ShowError (string.Format (GettextCatalog.GetString ("Can't open file {0}."), fileName));
				return;
			}
			data = openDocument.Editor;
			if (data == null)
				return;
			openDocument.RunWhenLoaded (delegate {
				try {
					indent = data.Document.GetLine (declaringType.Region.BeginLine).GetIndentation (data.Document) ?? "";
				} catch (Exception) {
					indent = "";
				}
				indent += "\t";
				
				InsertionCursorEditMode mode = new InsertionCursorEditMode (data.Parent, CodeGenerationService.GetInsertionPoints (openDocument, declaringType));
				if (fileName == options.Document.FileName) {
					for (int i = 0; i < mode.InsertionPoints.Count; i++) {
						var point = mode.InsertionPoints [i];
						if (point.Location < data.Caret.Location) {
							mode.CurIndex = i;
						} else {
							break;
						}
					}
				}
				
				ModeHelpWindow helpWindow = new ModeHelpWindow ();
				helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
				helpWindow.TitleText = GettextCatalog.GetString ("<b>Create Method -- Targeting</b>");
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Up</b>"), GettextCatalog.GetString ("Move to <b>previous</b> target point.")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Down</b>"), GettextCatalog.GetString ("Move to <b>next</b> target point.")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Enter</b>"), GettextCatalog.GetString ("<b>Declare new method</b> at target point.")));
				helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this refactoring.")));
				mode.HelpWindow = helpWindow;
				mode.StartMode ();
				mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
					if (args.Success) {
						SetInsertionPoint (args.InsertionPoint);
						BaseRun (options);
						if (string.IsNullOrEmpty (fileName))
							return;
						data.ClearSelection ();
						data.Caret.Offset = selectionEnd;
						data.SetSelection (selectionStart, selectionEnd);
					}
				};
			});
		}
		
		//so anonymous delegate can access base.Run verifiably
		void BaseRun (RefactoringOptions options)
		{
			base.Run (options);
		}
		
		static bool IsValidIdentifier (string name)
		{
			if (string.IsNullOrEmpty (name) || !(name[0] == '_' || char.IsLetter (name[0])))
				return false;
			for (int i = 1; i < name.Length; i++) {
				if (!(name[i] == '_' || char.IsLetter (name[i])))
					return false;
			}
			return true;
		}
		
		string fileName, indent;
		int selectionStart;
		int selectionEnd;
		
		IMethod ConstructMethod (RefactoringOptions options)
		{
			if (invocation != null)
				return ConstructMethodFromInvocation (options);
			return ConstructMethodFromDelegate (options);
		}
		
		IMethod ConstructMethodFromDelegate (RefactoringOptions options)
		{
			var result = new DefaultMethod (declaringType, methodName);
			var invocation = delegateType.GetDelegateInvokeMethod ();
			foreach (var arg in invocation.Parameters) {
				result.Parameters.Add (arg);
			}
			result.Accessibility = accessibility;
			result.ReturnType = invocation.ReturnType;
			return result;
		}
		
		IMethod ConstructMethodFromInvocation (RefactoringOptions options)
		{
			var result = new DefaultMethod (declaringType, methodName);
			result.Accessibility = accessibility;
			result.ReturnType = returnType;
			int i = 1;
			foreach (var argument in invocation.Arguments) {
				string name;
				if (argument is MemberReferenceExpression) {
					name = ((MemberReferenceExpression)argument).MemberName;
				} else if (argument is IdentifierExpression) {
					name = ((IdentifierExpression)argument).Identifier;
					int idx = name.LastIndexOf ('.');
					if (idx >= 0)
						name = name.Substring (idx + 1);
				} else {
					name = "par" + i++;
				}
				
				name = char.ToLower (name[0]) + name.Substring (1);
				
				ITypeReference type;
				var resolveResult = options.Resolve (argument);
				if (resolveResult != null && !resolveResult.IsError) {
					type = resolveResult.Type;
				} else {
					type = KnownTypeReference.Object;
				}
				
				var newArgument = new DefaultParameter (type, name);
				if (argument is DirectionExpression) {
					var de = (DirectionExpression)argument;
					newArgument.IsOut = de.FieldDirection == FieldDirection.Out;
					newArgument.IsRef = de.FieldDirection == FieldDirection.Ref;
				}
				result.Parameters.Add (newArgument);
			}
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			if (data == null)
				data = options.GetTextEditorData ();
			List<Change> result = new List<Change> ();
			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.FileName = fileName;
			insertNewMethod.RemovedChars = 0;//insertionPoint.LineBefore == NewLineInsertion.Eol ? 0 : insertionPoint.Location.Column - 1;
			int insertionOffset = data.Document.LocationToOffset (insertionPoint.Location);
			insertNewMethod.Offset = insertionOffset /*- insertNewMethod.RemovedChars*/;
			
			StringBuilder sb = new StringBuilder ();
			switch (insertionPoint.LineBefore) {
			case NewLineInsertion.Eol:
				sb.Append (data.EolMarker);
				break;
			case NewLineInsertion.BlankLine:
				sb.Append (data.EolMarker);
				sb.Append (indent);
				sb.Append (data.EolMarker);
				break;
			}

			var generator = options.CreateCodeGenerator ();
			sb.Append (generator.CreateMemberImplementation (options.Document.TypeResolveContext, declaringType, ConstructMethod (options), false).Code);
			sb.Append (data.EolMarker);
			switch (insertionPoint.LineAfter) {
			case NewLineInsertion.Eol:
				break;
			case NewLineInsertion.BlankLine:
				sb.Append (indent);
				sb.Append (data.EolMarker);
				break;
			}
			insertNewMethod.InsertedText = sb.ToString ();
			result.Add (insertNewMethod);
			selectionStart = selectionEnd = insertNewMethod.Offset;
			int idx = insertNewMethod.InsertedText.IndexOf ("throw");
			if (idx >= 0) {
				selectionStart = insertNewMethod.Offset + idx;
				selectionEnd = insertNewMethod.Offset + insertNewMethod.InsertedText.IndexOf (';', idx) + 1;
			} else {
				selectionStart = selectionEnd = insertNewMethod.Offset;
			}
			return result;
		}
	}
}
