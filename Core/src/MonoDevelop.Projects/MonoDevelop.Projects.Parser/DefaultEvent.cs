//  DefaultEvent.cs
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

namespace MonoDevelop.Projects.Parser {
	[Serializable]
	public class DefaultEvent : AbstractMember, IEvent
	{
		protected IRegion          bodyRegion;
		protected EventAttributes  eventAttributes;
		protected IMethod          addMethod;
		protected IMethod          removeMethod;
		protected IMethod          raiseMethod;
		
		public DefaultEvent ()
		{
		}
		
		public DefaultEvent (string name, IReturnType type, ModifierEnum m, IRegion region, IRegion bodyRegion)
		{
			Name = name;
			returnType         = type;
			this.region        = region;
			this.bodyRegion    = bodyRegion;
			modifiers          = m;
		}

		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}
		

		public virtual EventAttributes EventAttributes {
			get {
				return eventAttributes;
			}
		}

		public override int CompareTo (object ob) 
		{
			IEvent value = (IEvent) ob;
			return base.CompareTo (value);
		}
		
		public override bool Equals (object ob)
		{
			IEvent other = ob as IEvent;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = base.GetHashCode () + (int) eventAttributes;
			if (bodyRegion != null)
				c += bodyRegion.GetHashCode ();
			if (addMethod != null)
				c += addMethod.GetHashCode ();
			if (removeMethod != null)
				c += removeMethod.GetHashCode ();
			if (raiseMethod != null)
				c += raiseMethod.GetHashCode ();
				
			return c; 
		}
		
		public virtual IMethod AddMethod {
			get {
				return addMethod;
			}
		}
		
		public virtual IMethod RemoveMethod {
			get {
				return removeMethod;
			}
		}
		
		public virtual IMethod RaiseMethod {
			get {
				return raiseMethod;
			}
		}
		
	}
}
