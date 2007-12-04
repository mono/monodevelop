//  DefaultMethod.cs
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
using System.Reflection;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultMethod : AbstractMember, IMethod
	{
		protected IRegion bodyRegion;
		
		protected GenericParameterList genericParameters;
		protected ParameterCollection parameters = new ParameterCollection();

		public DefaultMethod ()
		{
		}
		
		public DefaultMethod (string name, IReturnType type, ModifierEnum m, IRegion region, IRegion bodyRegion)
		{
			Name = name;
			returnType = type;
			this.region     = region;
			this.bodyRegion = bodyRegion;
			modifiers = m;
		}
		
		public virtual IRegion BodyRegion {
			get { return bodyRegion; }
			set { bodyRegion = value; }
		}
		
		public virtual ParameterCollection Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
			}
		}
		
		/// <summary>
		/// Contains a list of formal parameters to a generic method. 
		/// <p>If this property returns null or an empty collection, the method
		/// is not generic.</p>
		/// </summary>
		public virtual GenericParameterList GenericParameters {
			get {
				return genericParameters;
			}
			set {
				genericParameters = value;
			}
		}

		public virtual bool IsConstructor {
			get {
				return returnType == null || Name == "ctor" || Name == ".ctor";
			}
		}

		public override string ToString()
		{
			return String.Format("[DefaultMethod: FullyQualifiedName={0}, ReturnType = {1}, IsConstructor={2}, Modifier={3}, ExplicitDeclaration={4}]",
			                     FullyQualifiedName,
			                     ReturnType,
			                     IsConstructor,
			                     base.Modifiers,
			                     base.ExplicitDeclaration == null ? "null" : base.ExplicitDeclaration.ToString ());
		}
		
		public override int CompareTo (object value)
		{
			int cmp = base.CompareTo (value);
			if (cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Parameters, ((IMethod)value).Parameters);
			if (cmp != 0)
				return cmp;
				
			if (GenericParameters == ((IMethod)value).GenericParameters)
				return 0;
			else
				return DiffUtility.Compare(GenericParameters, ((IMethod)value).GenericParameters);
		}
		
		public override bool Equals (object ob)
		{
			IMethod other = ob as IMethod;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}
}
