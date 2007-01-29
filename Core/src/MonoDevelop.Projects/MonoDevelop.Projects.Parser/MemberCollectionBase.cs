using System;
using System.Collections;

namespace MonoDevelop.Projects.Parser 
{
	[Serializable()]
	public class MemberCollectionBase : CollectionBase
	{
		IClass owner;
		
		public MemberCollectionBase (IClass owner)
		{
			this.owner = owner;
		}
		
		protected override void OnValidate (object item)
		{
			base.OnValidate (item);
			if (owner != null) {
				AbstractMember mem = item as AbstractMember;
				if (mem != null) mem.DeclaringType = owner;
			}
		}
	}
}
