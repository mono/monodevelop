//  AbstractMember.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 http://www.icsharpcode.net/ <#Develop>
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

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractMember : AbstractNamedEntity, IMember
	{
		protected IClass declaringType;
		protected IReturnType returnType;
		protected IReturnType explicitDeclaration = null;
		protected IRegion          region;
		
		
		public bool IsExplicitDeclaration {
			get {
				return explicitDeclaration != null;
			}
		}
		
		public IReturnType ExplicitDeclaration {
			get {
				return explicitDeclaration;
			}
			set {
				explicitDeclaration = value;
			}
		}
		
		public virtual IRegion Region {
			get { return region; }
			set { region = value; }
		}
		
		public IClass DeclaringType {
			get {
				return declaringType;
			}
			set {
				declaringType = value;
			}
		}
		
		public IReturnType ReturnType {
			get {
				return returnType;
			}
			set {
				returnType = value;
			}
		}
		
		public virtual string FullyQualifiedName {
			get {
				if (declaringType != null)
					return string.Concat (declaringType.FullyQualifiedName, ".", Name);
				else
					return Name;
			}
		}
		
		public override int CompareTo (object ob) 
		{
			int cmp;
			IMember member = (IMember) ob;
			
			cmp = base.CompareTo (member);
			if (cmp != 0) {
				return cmp;
			}
			
			if (FullyQualifiedName != null) {
				if (member.FullyQualifiedName == null)
					return -1;
				cmp = FullyQualifiedName.CompareTo(member.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			} else if (member.FullyQualifiedName != null)
				return 1;
			
			if (ReturnType != null) {
				if (member.ReturnType == null)
					return -1;
				cmp = ReturnType.CompareTo(member.ReturnType);
				if (cmp != 0) {
					return cmp;
				}
			} else if (member.ReturnType != null)
				return 1;
			
			if (ExplicitDeclaration != null) {
				if (member.ExplicitDeclaration == null)
					return -1;
				cmp = ExplicitDeclaration.CompareTo(member.ExplicitDeclaration);
				if (cmp != 0) {
					return cmp;
				}
			} else if (member.ExplicitDeclaration != null)
				return 1;
			
			if (Region != null) {
				if (member.Region == null)
					return -1;
				return Region.CompareTo(member.Region);
			} else if (member.Region != null)
				return 1;
				
			return 0;
		}
		
		public override bool Equals (object ob)
		{
			IMember other = ob as IMember;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = 0;
			c += (FullyQualifiedName != null) ? FullyQualifiedName.GetHashCode () : 1;
			c += (ReturnType != null) ? ReturnType.GetHashCode () : 2;
			c += (Region != null) ? Region.GetHashCode () : 4;
			c += (ExplicitDeclaration != null) ? ExplicitDeclaration.GetHashCode () : 8;
			return c;
		}
	}
}
