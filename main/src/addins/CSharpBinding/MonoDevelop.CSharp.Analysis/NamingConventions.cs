// 
// NamingConventions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;

namespace MonoDevelop.CSharp.Analysis
{
	public static class NamingConventions
	{
		public static IEnumerable<Result> CheckNaming (ParsedDocument input)
		{
			var unit = input != null ? input.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit : null;
			Console.WriteLine ("check: " + unit);
			if (unit == null)
				return System.Linq.Enumerable.Empty<Result> ();
			
			
			var visitor = new NamingVisitor (input.CompilationUnit);
			
			unit.AcceptVisitor (visitor, null);
			
			return visitor.results;
		}
	}
	
	public enum NamingStyle {
		None,
		PascalCase,
		CamelCase,
		AllUpper,
		AllLower,
		FirstUpper
	}
	
	public class NamingRule
	{
		public string Prefix {
			get;
			set;
		}
		
		public NamingStyle NamingStyle {
			get;
			set;
		}
		
		public string Suffix {
			get;
			set;
		}
		
		public NamingRule ()
		{
		}
		
		public NamingRule (NamingStyle namingStyle)
		{
			this.NamingStyle = namingStyle;
		}
		
		public string GetPreview ()
		{
			StringBuilder result = new StringBuilder ();
			if (Prefix != null)
				result.Append (Prefix);
			switch (NamingStyle) {
			case NamingStyle.PascalCase:
				result.Append ("PascalCase");
				break;
			case NamingStyle.CamelCase:
				result.Append ("camelCase");
				break;
			case NamingStyle.AllUpper:
				result.Append ("ALL_UPPER");
				break;
			case NamingStyle.AllLower:
				result.Append ("all_lower");
				break;
			case NamingStyle.FirstUpper:
				result.Append ("First_upper");
				break;
			}
			if (Suffix != null)
				result.Append (Suffix);
			return result.ToString ();
		}
		
		public NamingRule (string prefix, NamingStyle namingStyle, string suffix)
		{
			this.Prefix = prefix;
			this.NamingStyle = namingStyle;
			this.Suffix = suffix;
		}
		
		public bool IsValid (string name)
		{
			string id = name;
			if (!string.IsNullOrEmpty (Prefix)) {
				if (!id.StartsWith (Prefix))
					return false;
				id = id.Substring (Prefix.Length);
			}
			
			if (!string.IsNullOrEmpty (Suffix)) {
				if (!id.EndsWith (Suffix))
					return false;
				id = id.Substring (0, id.Length - Suffix.Length);
			}
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				return !id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch));
			case NamingStyle.AllUpper:
				return !id.Any (ch => char.IsLetter (ch) && char.IsLower (ch));
			case NamingStyle.CamelCase:
				return id.Length == 0 || char.IsLower (id [0]);
			case NamingStyle.PascalCase:
				return id.Length == 0 || char.IsUpper (id [0]);
			case NamingStyle.FirstUpper:
				return id.Length == 0 && char.IsUpper (id [0]) && !id.Take (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch));
			}
			return true;
		}

		public string GetErrorMessage (string name)
		{
			string id = name;
			if (!string.IsNullOrEmpty (Prefix)) {
				if (!id.StartsWith (Prefix))
					return string.Format (GettextCatalog.GetString ("Name should start with prefix '{0}'."), Prefix);
				id = id.Substring (Prefix.Length);
			}
			
			if (!string.IsNullOrEmpty (Suffix)) {
				if (!id.EndsWith (Suffix))
					return string.Format (GettextCatalog.GetString ("Name should end with suffix '{0}'."), Suffix);
				id = id.Substring (0, id.Length - Suffix.Length);
			}
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				if (id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch)))
					return string.Format (GettextCatalog.GetString ("'{0}' contains upper case letters."), name);
				break;
			case NamingStyle.AllUpper:
				if (id.Any (ch => char.IsLetter (ch) && char.IsLower (ch)))
					return string.Format (GettextCatalog.GetString ("'{0}' contains lower case letters."), name);
				break;
			case NamingStyle.CamelCase:
				if (id.Length > 0 && char.IsUpper (id [0]))
					return string.Format (GettextCatalog.GetString ("'{0}' should start with a lower case letter."), name);
				break;
			case NamingStyle.PascalCase:
				if (id.Length > 0 && char.IsLower (id [0]))
					return string.Format (GettextCatalog.GetString ("'{0}' should start with an upper case letter."), name);
				break;
			case NamingStyle.FirstUpper:
				if (id.Length > 0 && char.IsLower (id [0]))
					return string.Format (GettextCatalog.GetString ("'{0}' should start with an upper case letter."), name);
				if (id.Take (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch)))
					return string.Format (GettextCatalog.GetString ("'{0}' contains an upper case letter after the first."), name);
				break;
			}
			// should never happen.
			return "no known errors.";
		}
		
		public FixableResult GetFixableResult (AstLocation location, IBaseMember node, string name)
		{
			return new FixableResult (
				new DomRegion (location.Line, location.Column, location.Line, location.Column + name.Length),
				GetErrorMessage (name),
				ResultLevel.Warning, ResultCertainty.High, ResultImportance.Medium,
				new RenameMemberFix (node, name, null));
		}
	}
	
	[PolicyType ("C# naming")]
	public class CSharpNamingPolicy // : IEquatable<CSharpNamingPolicy>
	{
		[ItemProperty]
		public NamingRule Namespace {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Type {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Interface {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule TypeParameter {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Method {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Property{
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Event {
			get;
			set;
		}
		
		
		[ItemProperty]
		public NamingRule LocalVariable {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule LocalConstant {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Parameter {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule Field {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule InstanceField {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule InstanceStaticField {
			get;
			set;
		}
		 
		[ItemProperty]
		public NamingRule Constant {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule InstanceConstant {
			get;
			set;
		}
		
		[ItemProperty]
		public NamingRule EnumMember {
			get;
			set;
		}
		
		public CSharpNamingPolicy ()
		{
			Namespace = new NamingRule (NamingStyle.PascalCase);
			Type = new NamingRule (NamingStyle.PascalCase);
			Interface = new NamingRule ("I", NamingStyle.PascalCase, null);
			TypeParameter = new NamingRule (NamingStyle.PascalCase);
			Method = new NamingRule (NamingStyle.PascalCase);
			Property = new NamingRule (NamingStyle.PascalCase);
			Event = new NamingRule (NamingStyle.PascalCase);
			LocalVariable = new NamingRule (NamingStyle.CamelCase);
			LocalConstant = new NamingRule (NamingStyle.CamelCase);
			Parameter = new NamingRule (NamingStyle.CamelCase);
			Field = new NamingRule (NamingStyle.PascalCase);
			InstanceField = new NamingRule (NamingStyle.CamelCase);
			InstanceStaticField = new NamingRule (NamingStyle.PascalCase);
			Constant = new NamingRule (NamingStyle.PascalCase);
			InstanceConstant = new NamingRule (NamingStyle.PascalCase);
			EnumMember = new NamingRule (NamingStyle.PascalCase);
		}
	}
	
	class NamingVisitor : DepthFirstAstVisitor<object, object>
	{
		CSharpNamingPolicy policy = new CSharpNamingPolicy ();
		MonoDevelop.Projects.Dom.ICompilationUnit unit;
		public readonly List<FixableResult> results = new List<FixableResult> ();
		
		public NamingVisitor (MonoDevelop.Projects.Dom.ICompilationUnit unit)
		{
			this.unit = unit;
		}
		
		public void Check (NamingRule rule, AstLocation loc, string name)
		{
			if (!rule.IsValid (name))
				results.Add (rule.GetFixableResult (loc, null, name));
		}
		
		public void Check (NamingRule rule, AstLocation loc, string name, IBaseMember member)
		{
			if (!rule.IsValid (name))
				results.Add (rule.GetFixableResult (loc, member, name));
		}
		
		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}
		
		public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
		{
			return base.VisitNamespaceDeclaration (namespaceDeclaration, data);
		}
		
		public override object VisitTypeParameterDeclaration (TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			return base.VisitTypeParameterDeclaration (typeParameterDeclaration, data);
		}
		
		public override object VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			return base.VisitEnumMemberDeclaration (enumMemberDeclaration, data);
		}
		
		
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			switch (typeDeclaration.ClassType) {
			case ICSharpCode.NRefactory.TypeSystem.ClassType.Interface:
				Check (policy.Interface, typeDeclaration.NameToken.StartLocation, typeDeclaration.Name);
				break;
			default:
				Check (policy.Type, typeDeclaration.NameToken.StartLocation, typeDeclaration.Name);
				break;
			}
			return base.VisitTypeDeclaration (typeDeclaration, data);
		}
		
		public override object VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			foreach (var var in eventDeclaration.Variables) {
				Check (policy.Event, var.StartLocation, var.Name);
			}
			return base.VisitEventDeclaration (eventDeclaration, data);
		}
		
		public override object VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, object data)
		{
			Check (policy.Event, eventDeclaration.NameToken.StartLocation, eventDeclaration.Name);
			return base.VisitCustomEventDeclaration (eventDeclaration, data);
		}
		
		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			NamingRule namingRule;
			
			bool isPrivate = (fieldDeclaration.Modifiers & (ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Protected | ICSharpCode.NRefactory.CSharp.Modifiers.Internal)) == 0; 
			
			if ((fieldDeclaration.Modifiers & ICSharpCode.NRefactory.CSharp.Modifiers.Const) == ICSharpCode.NRefactory.CSharp.Modifiers.Const) {
				if (isPrivate) {
					namingRule = policy.InstanceConstant;
				} else {
					namingRule = policy.Constant;
				}
			} else {
				if (isPrivate) {
					if ((fieldDeclaration.Modifiers & ICSharpCode.NRefactory.CSharp.Modifiers.Static) == ICSharpCode.NRefactory.CSharp.Modifiers.Static) {
						namingRule = policy.InstanceStaticField;
					} else {
						namingRule = policy.InstanceField;
					}
				} else {
					namingRule = policy.Field;
				}
			}
			
			foreach (var var in fieldDeclaration.Variables) {
				Check (namingRule, var.StartLocation, var.Name);
			}
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			Check (policy.Property, propertyDeclaration.NameToken.StartLocation, propertyDeclaration.Name);
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitParameterDeclaration (ParameterDeclaration parameterDeclaration, object data)
		{
			Check (policy.Parameter, parameterDeclaration.NameToken.StartLocation, parameterDeclaration.Name);
			return base.VisitParameterDeclaration (parameterDeclaration, data);
		}
			
		public override object VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			foreach (var var in fixedFieldDeclaration.Variables) {
				Check (policy.Field, var.StartLocation, var.Name);
			}
			return base.VisitFixedFieldDeclaration (fixedFieldDeclaration, data);
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			var member = unit.GetMemberAt (variableDeclarationStatement.StartLocation.Line, variableDeclarationStatement.StartLocation.Column);
			foreach (var var in variableDeclarationStatement.Variables) {
				var v = new LocalVariable (member, var.Name, DomReturnType.Void, new DomRegion (variableDeclarationStatement.StartLocation.Line, variableDeclarationStatement.StartLocation.Column, variableDeclarationStatement.EndLocation.Line, variableDeclarationStatement.EndLocation.Column));
				if ((variableDeclarationStatement.Modifiers & ICSharpCode.NRefactory.CSharp.Modifiers.Const) == ICSharpCode.NRefactory.CSharp.Modifiers.Const) {
					Check (policy.LocalConstant, var.StartLocation, var.Name, v);
				} else {
					Check (policy.LocalVariable, var.StartLocation, var.Name, v);
				}
			}
			return base.VisitVariableDeclarationStatement (variableDeclarationStatement, data);
		}
	}
}
