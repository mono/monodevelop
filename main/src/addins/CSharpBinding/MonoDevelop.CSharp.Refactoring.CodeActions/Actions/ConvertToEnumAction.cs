//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.CodeActions;
using ICSharpCode.NRefactory;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.Decompiler.ILAst;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	/// <summary>
	/// Generates an enumeration from const fields
	/// </summary>
	class ConvertToEnumAction : MonoDevelop.CodeActions.CodeActionProvider
	{
		public override IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions(MonoDevelop.Ide.Gui.Document document, object refactoringContext, TextLocation loc, CancellationToken cancellationToken)
		{
			var context = refactoringContext as MDRefactoringContext;

			if (context == null || context.IsInvalid)
				yield break;

			VariableInitializer currentVariable = context.GetNode<VariableInitializer>();
			if (currentVariable == null) {
				yield break;
			}

			FieldDeclaration currentField = currentVariable.Parent as FieldDeclaration;
			if (currentField == null) {
				yield break;
			}

			if (!currentField.Modifiers.HasFlag(Modifiers.Const)) {
				yield break;
			}

			PrimitiveType baseType = TypeToIntegerPrimitive(context, currentField.ReturnType);
			if (baseType == null) {
				//Can't make enums of these types
				yield break;
			}

			TypeDeclaration containerType = currentVariable.GetParent<TypeDeclaration>();

			//Get all the fields/variables that the enum can possibly cover
			//Don't check the name just yet. That'll come later.

			var constFields = containerType.Members.OfType<FieldDeclaration>()
				.Where(field => field.GetParent<TypeDeclaration>() == containerType && field.HasModifier(Modifiers.Const)).ToList();

			var constVariables = constFields.SelectMany(field => field.Variables).ToList();

			//Now, it's time to check the name of the selected variable
			//We'll use this to search for prefixes later

			var names = constVariables.Select(variable => variable.Name).ToList();
			string currentName = currentVariable.Name;

			//Now, find the common name prefixes
			//If the variable is called 'A_B_C_D', then 'A', 'A_B' and 'A_B_C' are
			//the potentially available prefixes.
			//Note that the common prefixes are the ones that more than one variable
			//has.
			//Each prefix has an associated action.

			foreach (var prefix in GetCommonPrefixes (currentName, names)) {
				string title = string.Format(GettextCatalog.GetString("Create enum '{0}'"), prefix);

				yield return new DefaultCodeAction(title, (ctx, script) => {
					PrepareToRunAction (prefix, baseType, containerType, constVariables, cancellationToken, ctx, script);
				});
			}
		}

		void PrepareToRunAction (string prefix, PrimitiveType baseType, TypeDeclaration containerType, List<VariableInitializer> variables, CancellationToken cancellationToken, RefactoringContext context, Script script)
		{
			List<string> names = variables.Select(variable => variable.Name).ToList();
			Dictionary<string, string> newNames = names.ToDictionary(originalName => originalName, originalName => {
				if (!originalName.StartsWith(prefix)) {
					return originalName;
				}
				int startName = prefix.Length;
				while (startName < originalName.Length - 1 && originalName[startName] == '_') {
					++startName;
				}
				return originalName.Substring(startName);
			});

			string enumName;
			using (var dialog = new ConvertToEnumDialog (prefix, variables, variables.Where(variable => variable.Name.StartsWith(prefix, StringComparison.InvariantCulture)
			                                                                                               && VariableHasSpecifiedIntegerType(context, variable, baseType)).ToList(), newNames))
			{
				if (dialog.Run (/*MonoDevelop.Ide.IdeApp.Workbench.RootWindow*/) != Xwt.Command.Ok) {
					return;
				}
				enumName = dialog.EnumName;
				variables = dialog.SelectedVariables;
				newNames = dialog.NewNames;
			}

			RunAction (context, baseType, enumName, newNames, containerType, variables, script);
			
		}

		void RunAction(RefactoringContext context, AstType baseType, string enumName, Dictionary<string, string> newNames, TypeDeclaration containerTypeDeclaration, List<VariableInitializer> variables, Script script)
		{
			var names = variables.Select (variable => variable.Name).ToList ();
			var containerType = (context.Resolve(containerTypeDeclaration) as TypeResolveResult).Type;

			var fields = containerTypeDeclaration.Members.OfType<FieldDeclaration>().Where(field => field.Modifiers.HasFlag(Modifiers.Const)).ToList();
			List<VariableInitializer> variableUnitsToRemove = new List<VariableInitializer>(variables);
			List<FieldDeclaration> fieldsToRemove = new List<FieldDeclaration>();

			foreach (var field in fields) {
				if (field.Variables.All(variableUnitsToRemove.Contains)) {
					fieldsToRemove.Add(field);

					variableUnitsToRemove.RemoveAll(field.Variables.Contains);
				}
			}

			var generatedEnum = CreateEnumDeclaration(baseType, enumName, variables, names, newNames);

			AstNode root = GetRootNodeOf(containerTypeDeclaration);
			var newRoot = root.Clone();

			FixIdentifiers(context, enumName, variables, containerType, baseType, names, newNames, root, newRoot);
			foreach (var member in root.Descendants.OfType<MemberReferenceExpression>().Where (member => names.Contains (member.MemberName))) {
				if (variables.Any(variable => variable.Descendants.Contains(member))) {
					//Already handled
					continue;
				}

				var resolvedIdentifier = context.Resolve(member) as MemberResolveResult;
				if (resolvedIdentifier == null) {
					continue;
				}

				if (resolvedIdentifier.Type.Equals(containerType)) {
					continue;
				}

				var equivalentMember = GetEquivalentNodeFor(root, newRoot, member);
				MemberReferenceExpression memberToReplace = (MemberReferenceExpression)equivalentMember;

				var replacement = CreateReplacementMemberReference(enumName, baseType, newNames, memberToReplace);
				memberToReplace.ReplaceWith(replacement);
			}

			//Fix the file
			InsertAfterEquivalent(root, newRoot, containerTypeDeclaration.LBraceToken, generatedEnum, Roles.TypeMemberRole);

			foreach (var variableToRemove in variableUnitsToRemove) {
				GetEquivalentNodeFor(root, newRoot, variableToRemove).Remove();
			}
			foreach (var fieldToRemove in fieldsToRemove) {
				GetEquivalentNodeFor(root, newRoot, fieldToRemove).Remove();
			}

			script.Replace(root, newRoot);

			ReplaceVariableReferences(context, root, baseType, enumName, script, newNames, variables);
		}

		static void ReplaceVariableReferences(RefactoringContext context, AstNode root, AstType baseType, string enumName, Script script, Dictionary<string, string> newNames, IEnumerable<VariableInitializer> variables)
		{
			var resolveResults = variables.Select(variable => (MemberResolveResult)context.Resolve(variable));
			var resolvedFields = resolveResults.Select(resolveResult => resolveResult.Member);
			script.DoGlobalOperationOn(resolvedFields, (newCtx, newScript, foundNodes) =>  {
				foreach (var foundNode in foundNodes) {
					TypeDeclaration newContainerType = foundNode.GetParent<TypeDeclaration>();
					if (root.Descendants.OfType<TypeDeclaration>().Select(type => ((TypeResolveResult)context.Resolve(type)).Type.FullName).ToList().Contains(((TypeResolveResult)newCtx.Resolve(newContainerType)).Type.FullName)) {
						//This file has already been fixed
						return;
					}
					var identifierExpr = foundNode as IdentifierExpression;
					if (identifierExpr != null) {
						newScript.Replace(identifierExpr, CreateIdentifierReplacement(enumName, baseType, newNames, identifierExpr));
						continue;
					}
					var memberRef = foundNode as MemberReferenceExpression;
					if (memberRef != null) {
						var replacement = CreateReplacementMemberReference(enumName, baseType, newNames, memberRef);
						newScript.Replace(memberRef, replacement);
					}
				}
			});
		}

		TypeDeclaration CreateEnumDeclaration(AstType baseType, string enumName, List<VariableInitializer> variables, List<string> names, Dictionary<string, string> newNames)
		{
			TypeDeclaration generatedEnum = new TypeDeclaration();
			generatedEnum.ClassType = ClassType.Enum;
			generatedEnum.BaseTypes.Add(baseType.Clone());
			generatedEnum.Name = enumName;
			generatedEnum.Modifiers = GetCombinedModifier((Modifiers)variables.Select(variable => ((FieldDeclaration)variable.Parent).Modifiers).Aggregate(0, (prev, newModifier) => prev | (int)newModifier));
			foreach (var variable in variables) {
				var generatedMember = new EnumMemberDeclaration();
				generatedMember.Name = newNames[variable.Name];
				var value = variable.Initializer.Clone();
				foreach (var identifier in value.DescendantsAndSelf.OfType<IdentifierExpression>().Where(identifier => names.Contains(identifier.Identifier))) {
					var newIdentifier = new IdentifierExpression(newNames[identifier.Identifier]);
					if (identifier == value) {
						value = newIdentifier;
						break;
					}
					identifier.ReplaceWith(newIdentifier);
				}
				generatedMember.Initializer = value;
				generatedEnum.Members.Add(generatedMember);
			}
			return generatedEnum;
		}

		/// <summary>
		/// Determines whether the initialized variable has the specified primitive integer type
		/// </summary>
		/// <returns><c>true</c> if the initialized variable has the specified type; otherwise, <c>false</c>.</returns>
		/// <param name="context">The context to use.</param>
		/// <param name="variable">The variable initializer to check.</param>
		/// <param name="type">The type to compare with.</param>
		bool VariableHasSpecifiedIntegerType(RefactoringContext context, VariableInitializer variable, AstType type)
		{
			return TypeToIntegerPrimitive(context, variable.GetParent<FieldDeclaration>().ReturnType).Match(type).Success;
		}

		static Dictionary<string, PrimitiveType> primitiveTypes = new Dictionary<string, PrimitiveType>();

		static ConvertToEnumAction()
		{
			primitiveTypes.Add(typeof(byte).FullName, new PrimitiveType("byte"));
			primitiveTypes.Add(typeof(sbyte).FullName, new PrimitiveType("sbyte"));

			primitiveTypes.Add(typeof(short).FullName, new PrimitiveType("short"));
			primitiveTypes.Add(typeof(int).FullName, new PrimitiveType("int"));
			primitiveTypes.Add(typeof(long).FullName, new PrimitiveType("long"));

			primitiveTypes.Add(typeof(ushort).FullName, new PrimitiveType("ushort"));
			primitiveTypes.Add(typeof(uint).FullName, new PrimitiveType("uint"));
			primitiveTypes.Add(typeof(ulong).FullName, new PrimitiveType("ulong"));
		}

		/// <summary>
		/// Gets a PrimitiveType instance from an AstType.
		/// Only returns integer types (and never the char type)
		/// </summary>
		/// <returns>The integer primitive.</returns>
		/// <param name="context">The context to use.</param>
		/// <param name="type">The AstType to get the primitive from.</param>
		PrimitiveType TypeToIntegerPrimitive(RefactoringContext context, AstType type)
		{
			var resolvedType = context.ResolveType(type) as DefaultResolvedTypeDefinition;

			PrimitiveType primitiveType;
			if (!primitiveTypes.TryGetValue(resolvedType.FullName, out primitiveType)) {
				return null;
			}

			return primitiveType;
		}

		static Expression CreateReplacementMemberReference(string enumName, AstType baseType, Dictionary<string, string> newNames, MemberReferenceExpression memberToReplace)
		{
			return new ParenthesizedExpression(new CastExpression(baseType.Clone(), new MemberReferenceExpression(new MemberReferenceExpression(memberToReplace.Target.Clone(), enumName), newNames [memberToReplace.MemberName])));
		}

		void FixIdentifiers(RefactoringContext context, string enumName, List<VariableInitializer> variables, IType containerType, AstType baseType, List<string> names, Dictionary<string, string> newNames, AstNode root, AstNode newRoot)
		{
			foreach (var identifier in root.Descendants.OfType<IdentifierExpression> ().Where (identifier => names.Contains (identifier.Identifier))) {
				if (variables.Any(variable => variable.Descendants.Contains(identifier))) {
					//Already handled
					continue;
				}
				var resolvedIdentifier = context.Resolve(identifier) as MemberResolveResult;
				if (resolvedIdentifier == null) {
					continue;
				}
				if (resolvedIdentifier.Type.Equals(containerType)) {
					continue;
				}
				var replacement = CreateIdentifierReplacement(enumName, baseType, newNames, identifier);
				GetEquivalentNodeFor(root, newRoot, identifier).ReplaceWith(replacement);
			}
		}

		static ParenthesizedExpression CreateIdentifierReplacement(string enumName, AstType baseType, Dictionary<string, string> newNames, IdentifierExpression identifier)
		{
			var replacement = new ParenthesizedExpression(new CastExpression(baseType.Clone(), new MemberReferenceExpression(new IdentifierExpression(enumName), newNames [identifier.Identifier])));
			return replacement;
		}

		/// <summary>
		/// Finds the corresponding node in another ("new") AST.
		/// Assumes entities have not been renamed and no statements have been removed.
		/// </summary>
		/// <returns>The equivalent node in the new AST.</returns>
		/// <param name="root">The root of the first ("old") AST.</param>
		/// <param name="newRoot">The root of the new AST.</param>
		/// <param name="nodeToFind">Node (from the old AST) to find in the new one.</param>
		AstNode GetEquivalentNodeFor(AstNode root, AstNode newRoot, AstNode nodeToFind)
		{
			if (nodeToFind == null) {
				throw new ArgumentNullException("nodeToFind");
			}

			if (nodeToFind.Parent != root) {
				AstNode foundRoot = GetEquivalentNodeFor(root, newRoot, nodeToFind.Parent);
				if (foundRoot == null) {
					//If the equivalent of the parent does not exist in the new AST,
					//then neither does this node.
					return null;
				}
				newRoot = foundRoot;
				root = nodeToFind.Parent;
			}

			//At this point, the roots are the parents of the nodes to check
			//root is the parent of the nodeToFind, and newRoot is the parent of the node to return

			var block = root as BlockStatement;
			if (block != null && nodeToFind.Role == BlockStatement.StatementRole) {
				//This could be a problem if statements were removed in the new AST,
				//but fortunately that's not the problem we're trying to solve.
				return ((BlockStatement)newRoot).Statements.ElementAt(block.TakeWhile(statement => statement != nodeToFind).Count());
			}

			//First, we'll narrow down the search - the equivalent node *always* has the same type and role as nodeToFind
			//The Role check will help e.g. in binary expressions (where there is a 'Left' and a 'Right' role)
			var candidates = newRoot.Children.Where(child => child.GetType() == nodeToFind.GetType() && child.Role == nodeToFind.Role);
			var entity = nodeToFind as EntityDeclaration;
			if (entity != null) {
				var field = nodeToFind as FieldDeclaration;
				if (field != null) {
					//Fields have to be treated separately because fields have no names
					candidates = candidates.Where(candidate => IsEquivalentField((FieldDeclaration) candidate, field));
				}
				else {
					//Some entities can be distinguished by name.
					candidates = candidates.Where(candidate => ((EntityDeclaration)candidate).Name == entity.Name);

					var method = nodeToFind as MethodDeclaration;
					if (method != null) {
						//Methods, however, can be overloaded - so their names aren't enough.
						candidates = candidates.Where(candidate => CheckIfMethodsHaveSameParameters((MethodDeclaration) candidate, method));
					}
				}
			}

			var ns = nodeToFind as NamespaceDeclaration;
			if (ns != null) {
				candidates = candidates.Where(candidate => ((NamespaceDeclaration)candidate).Name == ns.Name).ToList();
				if (candidates.Count() > 1) {
					throw new NotImplementedException("Two or more namespace declarations with the same name are siblings. This case is not currently supported by this action.");
				}
			}

			var initializer = nodeToFind as VariableInitializer;
			if (initializer != null) {
				candidates = candidates.Where(candidate => ((VariableInitializer)candidate).Name == initializer.Name);
			}

			var equivalentNode = candidates.SingleOrDefault();
			return equivalentNode;
		}

		bool IsEquivalentField(FieldDeclaration field1, FieldDeclaration field2) {
			return field1.Variables.Any(variable1 => {
				return field2.Variables.Any(variable2 => variable1.Name == variable2.Name);
			});
		}

		bool CheckIfMethodsHaveSameParameters(MethodDeclaration methodDeclaration, MethodDeclaration comparedMethod)
		{
			if (methodDeclaration.Parameters.Count != comparedMethod.Parameters.Count) {
				return false;
			}

			ParameterDeclaration param1 = methodDeclaration.Parameters.FirstOrDefault();
			ParameterDeclaration param2 = comparedMethod.Parameters.FirstOrDefault();

			while (param1 != null) {
				//If the names or initializers are different, this will still match.
				//But if the type or order changes, this will complain
				if (!param1.Type.Match(param2.Type).Success) {
					return false;
				}

				param1 = (ParameterDeclaration) param1.GetNextSibling(node => node is ParameterDeclaration);
				param2 = (ParameterDeclaration) param2.GetNextSibling(node => node is ParameterDeclaration);
			}

			return true;
		}

		void InsertAfterEquivalent<T>(AstNode root, AstNode newRoot, AstNode prevNode, T newNode, Role<T> role)
			where T : AstNode
		{
			AstNode equivalentPrevNode = GetEquivalentNodeFor(root, newRoot, prevNode);
			equivalentPrevNode.Parent.InsertChildAfter<T>(equivalentPrevNode, newNode, role);
		}

		/// <summary>
		/// Gets the least permissive access modifier that still allows access to
		/// fields or methods with the specified modifiers.
		/// This will ignore all modifiers unrelated to access - such as const and readonly
		/// </summary>
		/// <returns>A modifier that is at least as permissive as all provided modifiers.</returns>
		/// <param name="modifiers">The modifiers to use.</param>
		Modifiers GetCombinedModifier(Modifiers modifiers)
		{
			if (modifiers.HasFlag(Modifiers.Public))
				return Modifiers.Public;

			Modifiers combinedModifier = 0;
			if (modifiers.HasFlag(Modifiers.Protected)) {
				combinedModifier |= Modifiers.Protected;
			}
			if (modifiers.HasFlag(Modifiers.Internal)) {
				combinedModifier |= Modifiers.Internal;
			}

			//No modifier if the fields are all private.
			return combinedModifier;
		}

		/// <summary>
		/// Gets all prefixes that more than one name have.
		/// </summary>
		/// <returns>The common prefixes.</returns>
		/// <param name="currentName">The name to use.</param>
		/// <param name="names">The names to check.</param>
		IEnumerable<string> GetCommonPrefixes(string currentName, IEnumerable<string> names)
		{
			//Find the indexes that 'split' words in the variable name
			var boundariesForCurrentWord = GetWordsBoundaries(currentName);

			//Get the candidate prefixes
			List<string> proposedPrefixes = boundariesForCurrentWord.Select(boundary => currentName.Substring(0, boundary)).ToList();

			//Return only the prefixes that more than one variable has.
			return proposedPrefixes.Where(prefix => names.Count(name => name.StartsWith(prefix, StringComparison.InvariantCulture)) > 1);
		}

		List<int> GetWordsBoundaries(string name)
		{
			List<int> foundBoundaries = new List<int>();
			for (int i = 1; i < name.Length - 1; ++i) {
				char chr = name [i];
				if (chr == '_') {
					foundBoundaries.Add(i);
					continue;
				}

				if (char.IsUpper(chr) && char.IsLower(name [i - 1])) {
					foundBoundaries.Add(i);
					continue;
				}
			}

			return foundBoundaries;
		}

		AstNode GetRootNodeOf(AstNode node)
		{
			while (node.Parent != null) {
				node = node.Parent;
			}

			return node;
		}
	}
}

