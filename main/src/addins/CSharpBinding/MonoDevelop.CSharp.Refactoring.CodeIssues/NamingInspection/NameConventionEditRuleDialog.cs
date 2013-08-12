// 
// NamingConventionEditRuleDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	partial class NameConventionEditRuleDialog : Gtk.Dialog
	{
		static readonly Dictionary<AffectedEntity, string> EntityName = new Dictionary<AffectedEntity, string> ();
		static readonly Dictionary<Modifiers, string> AccessibilityName = new Dictionary<Modifiers, string> ();
		
		static NameConventionEditRuleDialog ()
		{
			EntityName [AffectedEntity.Namespace] = GettextCatalog.GetString ("Namespaces");

			EntityName [AffectedEntity.Class] = GettextCatalog.GetString ("Classes");
			EntityName [AffectedEntity.Struct] = GettextCatalog.GetString ("Structs");
			EntityName [AffectedEntity.Enum] = GettextCatalog.GetString ("Enums");
			EntityName [AffectedEntity.Interface] = GettextCatalog.GetString ("Interfaces");
			EntityName [AffectedEntity.Delegate] = GettextCatalog.GetString ("Delegates");
	
			EntityName [AffectedEntity.CustomAttributes] = GettextCatalog.GetString ("Attributes");
			EntityName [AffectedEntity.CustomEventArgs] = GettextCatalog.GetString ("Event Arguments");
			EntityName [AffectedEntity.CustomExceptions] = GettextCatalog.GetString ("Exceptions");
	
			EntityName [AffectedEntity.Property] = GettextCatalog.GetString ("Properties");
			EntityName [AffectedEntity.AsyncMethod] = GettextCatalog.GetString ("Async methods");
			EntityName [AffectedEntity.Method] = GettextCatalog.GetString ("Methods");
			EntityName [AffectedEntity.Field] = GettextCatalog.GetString ("Fields");
			EntityName [AffectedEntity.ConstantField] = GettextCatalog.GetString ("Constant fields");
			EntityName [AffectedEntity.ReadonlyField] = GettextCatalog.GetString ("Readonly fields");
			EntityName [AffectedEntity.Event] = GettextCatalog.GetString ("Events");
			EntityName [AffectedEntity.EnumMember] = GettextCatalog.GetString ("Enum Members");
	
			EntityName [AffectedEntity.Parameter] = GettextCatalog.GetString ("Parameters");
			EntityName [AffectedEntity.TypeParameter] = GettextCatalog.GetString ("Type Parameters");
	
			// Unit test special case
			EntityName [AffectedEntity.TestType] = GettextCatalog.GetString ("Test Types");
			EntityName [AffectedEntity.TestMethod] = GettextCatalog.GetString ("Test Methods");
	
			// private entities
			EntityName [AffectedEntity.LambdaParameter] = GettextCatalog.GetString ("Lambda Parameters");
			EntityName [AffectedEntity.LocalVariable] = GettextCatalog.GetString ("Local Variables");
			EntityName [AffectedEntity.LocalConstant] = GettextCatalog.GetString ("Local Constants");
			EntityName [AffectedEntity.Label] = GettextCatalog.GetString ("Labels");

			AccessibilityName [Modifiers.Public] = GettextCatalog.GetString ("Public");
			AccessibilityName [Modifiers.Private] = GettextCatalog.GetString ("Private");
			AccessibilityName [Modifiers.Internal] = GettextCatalog.GetString ("Internal");
			AccessibilityName [Modifiers.Protected] = GettextCatalog.GetString ("Protected");
		}

		NameConventionRule rule;

		TreeStore entityStore       = new TreeStore (typeof(string), typeof(AffectedEntity), typeof(bool));
		TreeStore accessibiltyStore = new TreeStore (typeof(string), typeof(Modifiers), typeof(bool));

		public NameConventionEditRuleDialog (NameConventionRule rule)
		{
			if (rule == null)
				throw new System.ArgumentNullException ("rule");
			this.rule = rule;
			this.Build ();

			treeviewEntities.AppendColumn ("Entity", new CellRendererText (), "text", 0);
			var ct1 = new CellRendererToggle ();
			ct1.Toggled += delegate(object o, Gtk.ToggledArgs args) {
				TreeIter iter;
				if (!entityStore.GetIterFromString (out iter, args.Path))
					return;
				entityStore.SetValue (iter, 2, !(bool)entityStore.GetValue (iter, 2));
			};
			treeviewEntities.AppendColumn ("IsChecked", ct1, "active", 2);
			treeviewEntities.Model = entityStore;
			
			treeviewAccessibility.AppendColumn ("Entity", new CellRendererText (), "text", 0);
			var ct2 = new CellRendererToggle ();
			ct2.Toggled += delegate(object o, Gtk.ToggledArgs args) {
				TreeIter iter;
				if (!accessibiltyStore.GetIterFromString (out iter, args.Path))
					return;
				accessibiltyStore.SetValue (iter, 2, !(bool)accessibiltyStore.GetValue (iter, 2));
			};
			treeviewAccessibility.AppendColumn ("IsChecked", ct2, "active", 2);
			treeviewAccessibility.Model = accessibiltyStore;
			buttonOk.Clicked += (sender, e) => Apply ();

			FillDialog ();
		}

		public void FillDialog ()
		{
			entryRuleName.Text = rule.Name ?? "";
			if (rule.RequiredPrefixes != null)
				entryPrefix.Text = rule.RequiredPrefixes.FirstOrDefault (); 
			if (rule.AllowedPrefixes != null)
				entryPrefixAllowed.Text = string.Join (", ", rule.AllowedPrefixes); 
			if (rule.RequiredSuffixes != null)
				entrySuffix.Text = rule.RequiredSuffixes.FirstOrDefault (); 

			radiobuttonPascalCase.Active = rule.NamingStyle == NamingStyle.PascalCase;
			radiobuttonCamelCase.Active = rule.NamingStyle == NamingStyle.CamelCase;
			radiobuttonAllLower.Active = rule.NamingStyle == NamingStyle.AllLower;
			radiobuttonAllUpper.Active = rule.NamingStyle == NamingStyle.AllUpper;
			radiobuttonFirstUpper.Active = rule.NamingStyle == NamingStyle.FirstUpper;
			radiobuttonPascalCaseU1.Active = rule.NamingStyle == NamingStyle.PascalCaseWithUpperLetterUnderscore;
			radiobuttonPascalCaseU2.Active = rule.NamingStyle == NamingStyle.PascalCaseWithLowerLetterUnderscore;
			radiobuttonCamelCaseU1.Active = rule.NamingStyle == NamingStyle.CamelCaseWithUpperLetterUnderscore;
			radiobuttonCamelCaseU2.Active = rule.NamingStyle == NamingStyle.CamelCaseWithLowerLetterUnderscore;

			foreach (AffectedEntity ae in Enum.GetValues (typeof (AffectedEntity))) {
				if (!EntityName.ContainsKey (ae))
					continue;
				entityStore.AppendValues (EntityName [ae], ae, rule.AffectedEntity.HasFlag (ae));
			}
			
			foreach (Modifiers mod in Enum.GetValues (typeof (Modifiers))) {
				if (!AccessibilityName.ContainsKey (mod))
					continue;
				accessibiltyStore.AppendValues (AccessibilityName [mod], mod, rule.VisibilityMask.HasFlag (mod));
			}

			checkbuttonStatic.Active = rule.IncludeStaticEntities;
			checkbuttonInstanceMembers.Active = rule.IncludeInstanceMembers;

		}

		public void Apply ()
		{
			rule.Name = entryRuleName.Text;
			if (radiobuttonPascalCase.Active) {
				rule.NamingStyle = NamingStyle.PascalCase;
			} else if (radiobuttonCamelCase.Active) {
				rule.NamingStyle = NamingStyle.CamelCase;
			} else if (radiobuttonAllLower.Active) {
				rule.NamingStyle = NamingStyle.AllLower;
			} else if (radiobuttonAllUpper.Active) {
				rule.NamingStyle = NamingStyle.AllUpper;
			} else if (radiobuttonFirstUpper.Active) {
				rule.NamingStyle = NamingStyle.FirstUpper;
			} else if (radiobuttonPascalCaseU1.Active) {
				rule.NamingStyle = NamingStyle.PascalCaseWithUpperLetterUnderscore;
			} else if (radiobuttonPascalCaseU2.Active) {
				rule.NamingStyle = NamingStyle.PascalCaseWithLowerLetterUnderscore;
			} else if (radiobuttonCamelCaseU1.Active) {
				rule.NamingStyle = NamingStyle.CamelCaseWithUpperLetterUnderscore;
			} else if (radiobuttonCamelCaseU2.Active) {
				rule.NamingStyle = NamingStyle.CamelCaseWithLowerLetterUnderscore;
			}

			var prefix = entryPrefix.Text.Trim ();
			if (string.IsNullOrEmpty (prefix)) {
				rule.RequiredPrefixes = null;
			} else {
				rule.RequiredPrefixes = new [] { prefix };
			}
			
			var allowedPrefix = entryPrefixAllowed.Text.Trim ();
			if (string.IsNullOrEmpty (allowedPrefix)) {
				rule.AllowedPrefixes = null;
			} else {
				rule.AllowedPrefixes = allowedPrefix.Split (',', ';').Select (s => s.Trim ()).ToArray ();
			}
			
			var suffix = entrySuffix.Text.Trim ();
			if (string.IsNullOrEmpty (suffix)) {
				rule.RequiredSuffixes = null;
			} else {
				rule.RequiredSuffixes = new [] { suffix };
			}

			var ae = AffectedEntity.None;
			TreeIter iter;
			if (entityStore.GetIterFirst (out iter)) {
				do {
					var entity = (AffectedEntity)entityStore.GetValue (iter, 1);
					var include = (bool)entityStore.GetValue (iter, 2);
					if (include)
						ae |= entity;
				} while (entityStore.IterNext (ref iter));
			}
			rule.AffectedEntity = ae;

			var mod = Modifiers.None;
			if (accessibiltyStore.GetIterFirst (out iter)) {
				do {
					var entity = (Modifiers)accessibiltyStore.GetValue (iter, 1);
					var include = (bool)accessibiltyStore.GetValue (iter, 2);
					if (include)
						mod |= entity;
				} while (accessibiltyStore.IterNext (ref iter));
			}
			rule.VisibilityMask = mod;
			rule.IncludeStaticEntities = checkbuttonStatic.Active;
			rule.IncludeInstanceMembers = checkbuttonInstanceMembers.Active;
		}
	}
}

