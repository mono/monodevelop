// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	/// Creates a new <code>ICondition</code> object.
	/// </summary>
	public class ConditionFactory
	{
		ConditionBuilderCollection builders = new ConditionBuilderCollection();
		
		/// <summary>
		/// Returns the <code>ConditionBuilderCollection</code> for this instance.
		/// </summary>
		public ConditionBuilderCollection Builders {
			get {
				return builders;
			}
		}
		
		/// <summary>
		/// Creates a new <code>ICondition</code> object using <code>conditionNode</code>
		/// as a mask of which class to take for creation.
		/// </summary>
		public ICondition CreateCondition(AddIn addIn, XmlNode conditionNode)
		{
			ConditionBuilder builder = builders.GetBuilderForCondition(conditionNode);
			
			if (builder == null) {
				throw new ConditionNotFoundException("unknown condition found");
			}
			
			return builder.BuildCondition(addIn);
		}
		
	}
}
