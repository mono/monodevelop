//  DefaultIndexer.cs
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
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultIndexer : AbstractMember, IIndexer
	{
		protected IRegion             bodyRegion;
		protected IRegion             getterRegion;
		protected IRegion             setterRegion;
		protected ParameterCollection parameters = new ParameterCollection();
		
		public DefaultIndexer ()
		{
		}
		
		public DefaultIndexer (IReturnType type, ParameterCollection parameters, ModifierEnum m, IRegion region, IRegion bodyRegion)
		{
			returnType      = type;
			this.parameters = parameters;
			this.region     = region;
			this.bodyRegion = bodyRegion;
			modifiers = m;
		}
		
		public override string FullyQualifiedName {
			get {
				if (declaringType != null)
					return declaringType.FullyQualifiedName;
				else
					return null;
			}
		}
		
		public override string Name {
			get { return declaringType.Name; }
			set { }
		}
		
		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}


		public IRegion GetterRegion {
			get {
				return getterRegion;
			}
		}

		public IRegion SetterRegion {
			get {
				return setterRegion;
			}
		}

		public virtual ParameterCollection Parameters {
			get {
				return parameters;
			}
		}
		
		public override int CompareTo (object ob) 
		{
			int cmp;
			IIndexer value = (IIndexer) ob;
			
			cmp = base.CompareTo (value);
			if (cmp != 0) {
				return cmp;
			}
			
			if (GetterRegion != null) {
				if (value.GetterRegion == null)
					return -1;
				cmp = GetterRegion.CompareTo(value.GetterRegion);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.GetterRegion != null)
				return 1;
			
			if (SetterRegion != null) {
				if (value.SetterRegion == null)
					return -1;
				cmp = SetterRegion.CompareTo(value.SetterRegion);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.SetterRegion != null)
				return 1;
			
			return DiffUtility.Compare(Parameters, value.Parameters);
		}
		
		public override bool Equals (object ob)
		{
			IIndexer other = ob as IIndexer;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = base.GetHashCode ();
			if (GetterRegion != null)
				c += GetterRegion.GetHashCode ();
			if (SetterRegion != null)
				c += SetterRegion.GetHashCode ();
			return c;
		}
	}
}
