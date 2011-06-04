// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public abstract class AttributedNode : AstNode
	{
		public static readonly Role<AttributeSection> AttributeRole = new Role<AttributeSection> ("Attribute");
		public static readonly Role<CSharpModifierToken> ModifierRole = new Role<CSharpModifierToken> ("Modifier");
		
		public AstNodeCollection<AttributeSection> Attributes {
			get { return base.GetChildrenByRole (AttributeRole); }
		}
		
		public Modifiers Modifiers {
			get { return GetModifiers (this); }
			set { SetModifiers (this, value); }
		}
		
		public IEnumerable<CSharpModifierToken> ModifierTokens {
			get { return GetChildrenByRole (ModifierRole); }
		}
		
		internal static Modifiers GetModifiers (AstNode node)
		{
			Modifiers m = 0;
			foreach (CSharpModifierToken t in node.GetChildrenByRole (ModifierRole)) {
				m |= t.Modifier;
			}
			return m;
		}
		
		internal static void SetModifiers (AstNode node, Modifiers newValue)
		{
			Modifiers oldValue = GetModifiers (node);
			AstNode insertionPos = node.GetChildrenByRole (AttributeRole).LastOrDefault ();
			foreach (Modifiers m in CSharpModifierToken.AllModifiers) {
				if ((m & newValue) != 0) {
					if ((m & oldValue) == 0) {
						// Modifier was added
						var newToken = new CSharpModifierToken (AstLocation.Empty, m);
						node.InsertChildAfter (insertionPos, newToken, ModifierRole);
						insertionPos = newToken;
					} else {
						// Modifier already exists
						insertionPos = node.GetChildrenByRole (ModifierRole).First (t => t.Modifier == m);
					}
				} else {
					if ((m & oldValue) != 0) {
						// Modifier was removed
						node.GetChildrenByRole (ModifierRole).First (t => t.Modifier == m).Remove ();
					}
				}
			}
		}
		
		protected bool MatchAttributesAndModifiers (AttributedNode o, PatternMatching.Match match)
		{
			return (this.Modifiers == Modifiers.Any || this.Modifiers == o.Modifiers) && this.Attributes.DoMatch (o.Attributes, match);
		}
		
		#region Modifier accessibility properties
		/// <summary>
		/// Gets a value indicating whether this instance is private. This means that either the <c>private</c> modifier is used or none of the following modifiers: <c>public</c>, <c>protected</c>, <c>internal</c>
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is private; otherwise, <c>false</c>.
		/// </value>
		public bool IsPrivate {
			get {
				return (Modifiers & (Modifiers.Internal | Modifiers.Protected | Modifiers.Public)) == 0;
			}
		}
		
		public bool IsPublic {
			get {
				return (Modifiers & Modifiers.Public) == Modifiers.Public;
			}
		}
		
		public bool IsProtected {
			get {
				return (Modifiers & Modifiers.Protected) == Modifiers.Protected;
			}
		}
		
		public bool IsAbstract {
			get {
				return (Modifiers & Modifiers.Abstract) == Modifiers.Abstract;
			}
		}
		
		public bool IsVirtual {
			get {
				return (Modifiers & Modifiers.Virtual) == Modifiers.Virtual;
			}
		}
		
		public bool IsSealed {
			get {
				return (Modifiers & Modifiers.Sealed) == Modifiers.Sealed;
			}
		}
		
		public bool IsStatic {
			get {
				return (Modifiers & Modifiers.Static) == Modifiers.Static;
			}
		}
		
		public bool IsOverride {
			get {
				return (Modifiers & Modifiers.Override) == Modifiers.Override;
			}
		}
		
		public bool IsReadonly {
			get {
				return (Modifiers & Modifiers.Readonly) == Modifiers.Readonly;
			}
		}
		
		public bool IsConst {
			get {
				return (Modifiers & Modifiers.Const) == Modifiers.Const;
			}
		}
		
		public bool IsNew {
			get {
				return (Modifiers & Modifiers.New) == Modifiers.New;
			}
		}
		
		public bool IsPartial {
			get {
				return (Modifiers & Modifiers.Partial) == Modifiers.Partial;
			}
		}
		
		public bool IsExtern {
			get {
				return (Modifiers & Modifiers.Extern) == Modifiers.Extern;
			}
		}
		
		public bool IsVolatile {
			get {
				return (Modifiers & Modifiers.Volatile) == Modifiers.Volatile;
			}
		}
		
		public bool IsUnsafe {
			get {
				return (Modifiers & Modifiers.Unsafe) == Modifiers.Unsafe;
			}
		}
		
		public bool IsFixed {
			get {
				return (Modifiers & Modifiers.Fixed) == Modifiers.Fixed;
			}
		}
		#endregion
	}
}
