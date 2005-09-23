// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;

using MonoDevelop.Core.AddIns.Conditions;

namespace MonoDevelop.Core.AddIns
{
	public interface IOwnerState {
		System.Enum InternalState {
			get;
		}
	}
	
	[ConditionAttribute()]
	internal class OwnerStateCondition : AbstractCondition
	{
		[XmlMemberAttribute("ownerstate", IsRequired = true)]
		string ownerstate;
		
		public string OwnerState {
			get {
				return ownerstate;
			}
			set {
				ownerstate = value;
			}
		}
		
		public override bool IsValid(object owner)
		{
			if (owner is IOwnerState) {
				try {
					System.Enum state = ((IOwnerState)owner).InternalState;
					System.Enum conditionEnum = (System.Enum)Enum.Parse(state.GetType(), ownerstate);
				
					int stateInt     = Int32.Parse(state.ToString("D"));
					int conditionInt = Int32.Parse(conditionEnum.ToString("D"));
					
					return (stateInt & conditionInt) > 0;
				} catch (Exception) {
					throw new ApplicationException("can't parse '" + ownerstate + "'. Not a valid value.");
				}
			}
			return false;
		}
	}
}
