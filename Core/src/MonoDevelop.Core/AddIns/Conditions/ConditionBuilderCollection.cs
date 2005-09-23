// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	/// A collection containing <code>ConditionBuilder</code> objects.
	/// </summary>
	public class ConditionBuilderCollection : CollectionBase
	{
		
		/// <summary>
		/// Add a new condition builder to the collection.
		/// </summary>
		/// <exception cref="DuplicateConditionException">
		/// When there is already a condition which the same required attributes
		/// in this collection.
		/// </exception>
		/// <exception cref="ConditionWithoutRequiredAttributesException">
		/// When the given condition does not have required attributes.
		/// </exception>
		public void Add(ConditionBuilder builder) 
		{
			foreach (ConditionBuilder b2 in this) {
				if (b2.RequiredAttributes.Equals(builder.RequiredAttributes)) {
					throw new DuplicateConditionException(builder.RequiredAttributes);
				}
			}
			if (builder.RequiredAttributes.Count == 0) {
				throw new ConditionWithoutRequiredAttributesException();
			}
			List.Add(builder);
		}
		
		bool MatchAttributes(StringCollection requiredAttributes, XmlNode conditionNode)
		{
			foreach (string attr in requiredAttributes) {
				if (conditionNode.Attributes[attr] == null) {
					return false;
				}
			}
			return true;
		}
		
		/// <summary>
		/// Returns a <see cref="ConditionBuilder"/> object which is able to construct a new
		/// <see cref="ICondition"/> object with the data collected from <code>conditionNode</code>
		/// </summary>
		/// <param name="conditionNode">
		/// The node with the attributes for the condition.
		/// </param>
		public ConditionBuilder GetBuilderForCondition(XmlNode conditionNode) 
		{
			foreach (ConditionBuilder builder in this) {
				if (MatchAttributes(builder.RequiredAttributes, conditionNode)) {
					return builder;
				}
			}
			return null;
		}
	}
}
