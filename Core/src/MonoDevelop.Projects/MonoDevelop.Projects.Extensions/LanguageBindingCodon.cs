// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Extensions
{
	[Description ("A language binding. The specified class must implement MonoDevelop.Projects.ILanguageBinding")]
	[CodonNameAttribute("LanguageBinding")]
	internal class LanguageBindingCodon : ClassCodon
	{
		[Description ("File extensions supported by this binding (to be shown in the Open File dialog)")]
		[XmlMemberArrayAttribute("supportedextensions")]
		string[] supportedExtensions;
		
		ILanguageBinding languageBinding;
		
		public string[] Supportedextensions {
			get {
				return supportedExtensions;
			}
			set {
				supportedExtensions = value;
			}
		}
		
		public ILanguageBinding LanguageBinding {
			get {
				return languageBinding;
			}
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
//			if (subItems == null || subItems.Count > 0) {
//				throw new ApplicationException("Tried to buil a command with sub commands, please check the XML definition.");
//			}
			Debug.Assert(Class != null && Class.Length > 0);
			
			languageBinding = (ILanguageBinding)AddIn.CreateObject(Class);
			
			return this;
		}
	}
}
