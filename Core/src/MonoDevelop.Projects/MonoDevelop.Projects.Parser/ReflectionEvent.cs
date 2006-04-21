// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Xml;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionEvent : AbstractEvent
	{
		public ReflectionEvent(EventDefinition eventInfo, XmlDocument docs)
		{
			FullyQualifiedName = String.Concat(eventInfo.DeclaringType.FullName, ".", eventInfo.Name);

			if (docs != null) {
				XmlNode node = docs.SelectSingleNode ("/Type/Members/Member[@MemberName='" + eventInfo.Name + "']/Docs/summary");
				if (node != null) {
					Documentation = node.InnerXml;
				}
			}

			// get modifiers
			MethodDefinition methodBase = null;
			try {
				methodBase = eventInfo.AddMethod;
			} catch (Exception) {}
			
			if (methodBase == null) {
				try {
					methodBase = eventInfo.RemoveMethod;
				} catch (Exception) {}
			}
			
			if (methodBase != null) {
				modifiers |= ReflectionMethod.GetModifiers (methodBase.Attributes);
			} else { // assume public property, if no methodBase could be get.
				modifiers = ModifierEnum.Public;
			}
			
			returnType = new ReflectionReturnType (eventInfo.EventType);			
		}
	}
}
