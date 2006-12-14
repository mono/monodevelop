// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractMember : AbstractNamedEntity, IMember
	{
		protected IClass declaringType;
		protected IReturnType returnType;
		protected IRegion          region;
		
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
			return c;
		}
	}
}
