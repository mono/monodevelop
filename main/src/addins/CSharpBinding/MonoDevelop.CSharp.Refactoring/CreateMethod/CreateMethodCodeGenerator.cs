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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Text;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Dom;

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

		InvocationExpression GetInvocation (MonoDevelop.CSharp.Dom.CompilationUnit unit, TextEditorData data)
		{
			var containingNode = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
			var curNode = containingNode;
			while (curNode != null && !(curNode is InvocationExpression)) {
				curNode = curNode.Parent;
			}
			return curNode as InvocationExpression;
		}
		
		bool AnalyzeTargetExpression (RefactoringOptions options, MonoDevelop.CSharp.Dom.CompilationUnit unit)
		{
			var data = options.GetTextEditorData ();
			var target = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
			if (target == null)
				return false;
			if (target.Parent is MemberReferenceExpression && ((MemberReferenceExpression)target.Parent).Identifier == target) {
				var memberReference = (MemberReferenceExpression)target.Parent;
				target = memberReference.Target;
				var targetResult = options.GetResolver ().Resolve (new ExpressionResult (data.GetTextBetween (target.StartLocation.Line, target.StartLocation.Column, target.EndLocation.Line, target.EndLocation.Column)), resolvePosition);
				if (targetResult.StaticResolve)
					modifiers = MonoDevelop.Projects.Dom.Modifiers.Static;
				declaringType = options.Dom.GetType (targetResult.ResolvedType);
				methodName = memberReference.Identifier.Name;
			} else if (target is Identifier) {
				declaringType = options.ResolveResult.CallingType;
				methodName = data.GetTextBetween (target.StartLocation.Line, target.StartLocation.Column, target.EndLocation.Line, target.EndLocation.Column);
			}
			return declaringType != null && !HasCompatibleMethod (declaringType, methodName, invocation);
		}
		
		IType GetDelegateType (RefactoringOptions options, MonoDevelop.CSharp.Dom.CompilationUnit unit)
		{
			var data = options.GetTextEditorData ();
			var containingNode = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
			var parent = containingNode.Parent;
			
			while (parent != null) {
				if (parent is AssignmentExpression) {
					AssignmentExpression assignment = (AssignmentExpression)parent;
					if (assignment.AssignmentOperatorType != AssignmentOperatorType.Add && assignment.AssignmentOperatorType != AssignmentOperatorType.Subtract && assignment.AssignmentOperatorType != AssignmentOperatorType.Assign)
						return null;
					
					var resolveResult = ResolveAssignment (options, assignment);
					if (resolveResult == null)
						return null;
					IType type = options.Dom.GetType (resolveResult.ResolvedType);
					if (type == null || type.ClassType != ClassType.Delegate)
						return null;
					return type;
				}
				parent = parent.Parent;
			}
			return null;
		}
		
		public bool HasCompatibleMethod (IType type, string methodName, InvocationExpression invocation)
		{
			// TODO: add argument type check for overloads. 
			if (invocation == null || type == null)
				return false;
			int invocationArguments = invocation.Arguments.Count ();
			return type.SearchMember (methodName, true).Any (m => m is IMethod && m.Parameters.Count == invocationArguments);
		}
		
		ResolveResult ResolveAssignment (RefactoringOptions options, AssignmentExpression assignment)
		{
			var resolver = options.GetResolver ();
			var data = options.GetTextEditorData ();
			string expression;
			if (assignment.Left is IdentifierExpression) {
				expression = ((IdentifierExpression)assignment.Left).Identifier;
			} else {
				var left = assignment.Left;
				expression = data.GetTextBetween (left.StartLocation.Line, left.StartLocation.Column, left.EndLocation.Line, left.EndLocation.Column);
			}
			return resolver.Resolve (new ExpressionResult (expression), resolvePosition);
		}

		IReturnType GuessReturnType (RefactoringOptions options)
		{
			var resolver = options.GetResolver ();
			var data = options.GetTextEditorData ();
			DomNode node = invocation;
			while (node != null) {
				if (node.Parent is VariableInitializer) {
					var initializer = (VariableInitializer)node.Parent;
					var resolveResult = resolver.Resolve (new ExpressionResult (initializer.Name), resolvePosition);
					return resolveResult.ResolvedType;
				}
				if (node.Parent is AssignmentExpression) {
					var resolveResult = ResolveAssignment (options, (AssignmentExpression)node.Parent);
					if (resolveResult != null)
						return resolveResult.ResolvedType;
				}
				if (node.Parent is InvocationExpression) {
					var parentInvocation = (InvocationExpression)node.Parent;
					int idx = 0;
					foreach (var arg in parentInvocation.Arguments) {
						if (arg == node)
							break;
						idx++;
					}
					var resolveResult = resolver.Resolve (new ExpressionResult (data.GetTextBetween (parentInvocation.StartLocation.Line, parentInvocation.StartLocation.Column, parentInvocation.EndLocation.Line, parentInvocation.EndLocation.Column)), resolvePosition) as MethodResolveResult;
					if (resolveResult != null) {
						if (idx < resolveResult.MostLikelyMethod.Parameters.Count)
							return resolveResult.MostLikelyMethod.Parameters[idx].ReturnType;
					}
					return DomReturnType.Object;
				}
				
				node = node.Parent;
			}
			return DomReturnType.Void;
		}
		
		public bool Analyze (RefactoringOptions options)
		{
			var data = options.GetTextEditorData ();
			var parser = new CSharpParser ();
			var unit = parser.Parse (data);
			if (unit == null)
				return false;
			resolvePosition = new DomLocation (data.Caret.Line, data.Caret.Column);
			
			if (!AnalyzeTargetExpression (options, unit))
				return false;
			
			invocation = GetInvocation (unit, data);
			if (invocation != null) 
				return AnalyzeInvocation (options);
			delegateType = GetDelegateType (options, unit);
			return delegateType != null;
		}
		
		public bool AnalyzeInvocation (RefactoringOptions options)
		{
			bool isInInterface = declaringType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface;
			if (isInInterface) {
				modifiers = MonoDevelop.Projects.Dom.Modifiers.None;
			} else {
				bool isStatic = (modifiers & MonoDevelop.Projects.Dom.Modifiers.Static) != 0;
				modifiers = options.ResolveResult.CallingMember.Modifiers;
				if (declaringType.DecoratedFullName != options.ResolveResult.CallingType.DecoratedFullName) {
					modifiers = MonoDevelop.Projects.Dom.Modifiers.Public;
					if (options.ResolveResult.CallingMember.IsStatic)
						isStatic = true;
				}
				if (isStatic)
					modifiers |= MonoDevelop.Projects.Dom.Modifiers.Static;
			}
			returnType = GuessReturnType (options);
			
			return true;
		}
		
		DomLocation resolvePosition;
		
		IType declaringType;
		public IType DeclaringType {
			get {
				return this.declaringType;
			}
		}
		
		IReturnType returnType;
		string methodName;
		
		InvocationExpression invocation;
		IType delegateType;
		
		MonoDevelop.Projects.Dom.Modifiers modifiers;
		
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
			TextEditorData data = options.GetTextEditorData ();
			
			fileName = declaringType.CompilationUnit.FileName;
			
			var openDocument = IdeApp.Workbench.OpenDocument (fileName);
			if (openDocument == null) {
				MessageService.ShowError (string.Format (GettextCatalog.GetString ("Can't open file {0}."), fileName));
				return;
			}
			data = openDocument.Editor;
			if (data == null)
				return;
			
			if (data == null)
				throw new InvalidOperationException ("Can't open file:" + modifiers);
			try {
				indent = data.Document.GetLine (declaringType.Location.Line).GetIndentation (data.Document) ?? "";
			} catch (Exception) {
				indent = "";
			}
			indent += "\t";
			
			InsertionCursorEditMode mode = new InsertionCursorEditMode (data.Parent, MonoDevelop.Refactoring.HelperMethods.GetInsertionPoints (options.Document, declaringType));
			if (fileName == options.Document.FileName) {
				for (int i = 0; i < mode.InsertionPoints.Count; i++) {
					var point = mode.InsertionPoints[i];
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
					base.Run (options);
					if (string.IsNullOrEmpty (fileName))
						return;
					MonoDevelop.Ide.Gui.Document document = IdeApp.Workbench.OpenDocument (fileName);
					TextEditorData docData = document.Editor;
					if (docData != null) {
						docData.ClearSelection ();
						docData.Caret.Offset = selectionEnd;
						docData.SetSelection (selectionStart, selectionEnd);
					}
				}
			};
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
			DomMethod result = new DomMethod (methodName, modifiers, MethodModifier.None, DomLocation.Empty, DomRegion.Empty, returnType);
			result.DeclaringType = new DomType ("GeneratedType") { ClassType = declaringType.ClassType };
			IMethod invocation = (IMethod)delegateType.SearchMember ("Invoke", true).First ();
			foreach (var arg in invocation.Parameters) {
				result.Add (arg);
			}
			result.ReturnType = invocation.ReturnType;
			return result;
		}
		
		IMethod ConstructMethodFromInvocation (RefactoringOptions options)
		{
			var resolver = options.GetResolver ();
			var data = options.GetTextEditorData ();
			
			DomMethod result = new DomMethod (methodName, modifiers, MethodModifier.None, DomLocation.Empty, DomRegion.Empty, returnType);
			result.DeclaringType = new DomType ("GeneratedType") { ClassType = declaringType.ClassType };
			int i = 1;
			foreach (var curArg in invocation.Arguments) {
				var argument = curArg;
				DomParameter arg = new DomParameter ();
				if (argument is DirectionExpression) {
					var de = (DirectionExpression)argument;
					arg.ParameterModifiers = de.FieldDirection == FieldDirection.Out ? ParameterModifiers.Out : ParameterModifiers.Ref; 
					argument = de.Expression;
				}
				
				string argExpression = data.GetTextBetween (argument.StartLocation.Line, argument.StartLocation.Column, argument.EndLocation.Line, argument.EndLocation.Column);
				var resolveResult = resolver.Resolve (new ExpressionResult (argExpression), resolvePosition);
				
				if (argument is MemberReferenceExpression) {
					arg.Name = ((MemberReferenceExpression)argument).Identifier.Name;
				} else if (argument is IdentifierExpression) {
					arg.Name = ((IdentifierExpression)argument).Identifier;
					int idx = arg.Name.LastIndexOf ('.');
					if (idx >= 0)
						arg.Name = arg.Name.Substring (idx + 1);
				} else {
					arg.Name = "par" + i++;
				}
				
				arg.Name = char.ToLower (arg.Name[0]) + arg.Name.Substring (1);
				
				if (resolveResult != null) {
					arg.ReturnType = resolveResult.ResolvedType;
				} else {
					arg.ReturnType = DomReturnType.Object;
				}
				
				result.Add (arg);
			}
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.FileName = fileName;
			insertNewMethod.RemovedChars = insertionPoint.LineBefore == NewLineInsertion.Eol ? 0 : insertionPoint.Location.Column;
			var data = options.GetTextEditorData ();
			int insertionOffset = data.Document.LocationToOffset (insertionPoint.Location);
			insertNewMethod.Offset = insertionOffset - insertNewMethod.RemovedChars;
			
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ();
			switch (insertionPoint.LineBefore) {
			case NewLineInsertion.Eol:
				sb.AppendLine ();
				break;
			case NewLineInsertion.BlankLine:
				sb.AppendLine ();
				sb.Append (indent);
				sb.AppendLine ();
				break;
			}

			var generator = options.Document.CreateCodeGenerator ();
			sb.Append (generator.CreateMemberImplementation (declaringType, ConstructMethod (options), false).Code);
			
			switch (insertionPoint.LineAfter) {
			case NewLineInsertion.Eol:
				sb.AppendLine ();
				break;
			case NewLineInsertion.BlankLine:
				sb.AppendLine ();
				sb.AppendLine ();
				sb.Append (indent);
				break;
			}
			
			insertNewMethod.InsertedText = sb.ToString ();
			result.Add (insertNewMethod);
			selectionStart = selectionEnd = insertNewMethod.Offset;
			int idx = insertNewMethod.InsertedText.IndexOf ("throw");
			if (idx >= 0) {
				selectionStart = insertNewMethod.Offset + idx;
				selectionEnd   = insertNewMethod.Offset + insertNewMethod.InsertedText.IndexOf (';', idx) + 1;
			} else {
				selectionStart = selectionEnd = insertNewMethod.Offset;
			}
			return result;
		}
	}
}
