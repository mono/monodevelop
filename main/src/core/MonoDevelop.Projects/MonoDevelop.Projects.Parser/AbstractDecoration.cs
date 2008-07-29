//  AbstractDecoration.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections;
using MonoDevelop.Projects.Utility;
using System.Reflection;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractDecoration : IDecoration
	{
		protected ModifierEnum               modifiers     = ModifierEnum.None;
		protected AttributeSectionCollection attributes    = null;
		string documentation;
		static Hashtable documentationHashtable = new Hashtable();
		
		public abstract string Name {
			get;
			set;
		}
		
		public virtual ModifierEnum Modifiers {
			get { return modifiers;	}
			set { modifiers = value; }
		}

		public void AddModifier (ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public virtual AttributeSectionCollection Attributes {
			get {
				if (attributes == null) {
					attributes = new AttributeSectionCollection();
				}
				return attributes;
			}
			set {
				attributes = value;
			}
		}

		public string Documentation {
			get {
				return documentation == null ? "" : documentation;
			}
			set {
				if (value == null)
					documentation = null;
				else {
					string sharedVal = documentationHashtable [value] as string;
					if (sharedVal == null) {
						documentationHashtable [value] = value;
						documentation = value;
					}
					else
						documentation = sharedVal;
				}
			}
		}
		
		public bool IsAbstract {
			get {
				return (modifiers & ModifierEnum.Abstract) == ModifierEnum.Abstract;
			}
		}

		public bool IsSealed {
			get {
				return (modifiers & ModifierEnum.Sealed) == ModifierEnum.Sealed;
			}
		}

		public bool IsStatic {
			get {
				return (modifiers & ModifierEnum.Static) == ModifierEnum.Static;
			}
		}

		public bool IsVirtual {
			get {
				return (modifiers & ModifierEnum.Virtual) == ModifierEnum.Virtual;
			}
		}

		public bool IsPublic {
			get {
				return (modifiers & ModifierEnum.Public) == ModifierEnum.Public;
			}
		}

		public bool IsProtected {
			get {
				return (modifiers & ModifierEnum.Protected) == ModifierEnum.Protected;
			}
		}

		public bool IsPrivate {
			get {
				return (modifiers & ModifierEnum.Private) == ModifierEnum.Private;
			}
		}

		public bool IsInternal {
			get {
				return (modifiers & ModifierEnum.Internal) == ModifierEnum.Internal;
			}
		}

		public bool IsProtectedAndInternal {
			get {
				return (modifiers & (ModifierEnum.Internal | ModifierEnum.Protected)) == (ModifierEnum.Internal | ModifierEnum.Protected);
			}
		}

		public bool IsProtectedOrInternal {
			get {
				return (modifiers & ModifierEnum.ProtectedOrInternal) == ModifierEnum.ProtectedOrInternal;
			}
		}

		public bool IsLiteral {
			get {
				return (modifiers & ModifierEnum.Const) == ModifierEnum.Const;
			}
		}

		public bool IsReadonly {
			get {
				return (modifiers & ModifierEnum.Readonly) == ModifierEnum.Readonly;
			}
		}

		public bool IsOverride {
			get {
				return (modifiers & ModifierEnum.Override) == ModifierEnum.Override;
			}
		}

		public bool IsFinal {
			get {
				return (modifiers & ModifierEnum.Final) == ModifierEnum.Final;
			}
		}

		public bool IsSpecialName {
			get {
				return (modifiers & ModifierEnum.SpecialName) == ModifierEnum.SpecialName;
			}
		}

		public bool IsNew {
			get {
				return (modifiers & ModifierEnum.New) == ModifierEnum.New;
			}
		}
		
		public virtual int CompareTo (object ob) 
		{
			int cmp;
			IDecoration value = (IDecoration) ob;
			
			if(0 != (cmp = (int)(Modifiers - value.Modifiers)))
				return cmp;
			
			return DiffUtility.Compare(Attributes, value.Attributes);
		}
		
		public override bool Equals (object ob)
		{
			IDecoration other = ob as IDecoration;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return modifiers.GetHashCode () + Name.GetHashCode ();
		}
	}
}
