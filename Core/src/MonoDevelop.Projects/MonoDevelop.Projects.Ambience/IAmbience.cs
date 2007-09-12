// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Ambience
{
	[Flags]
	public enum ConversionFlags {
		None                   = 0,
		ShowParameterNames     = 1,
		UseFullyQualifiedNames = 1<<1,
		ShowMemberModifiers    = 1<<2,
		ShowInheritanceList    = 1<<3,
		ShowAccessibility      = 1<<4,
		IncludeHTMLMarkup      = 1<<5,
		UseLinkArrayList       = 1<<6,
		QualifiedNamesOnlyForReturnTypes = 1<<7,
		IncludeBodies          = 1<<8,
		IncludePangoMarkup     = 1<<9,
		ShowClassModifiers     = 1<<10,
		ShowGenericParameters  = 1<<11,
		UseIntrinsicTypeNames  = 1<<12,
		
		StandardConversionFlags = ShowParameterNames | 
		                          UseFullyQualifiedNames | 
		                          ShowMemberModifiers |
		                          ShowClassModifiers |
		                          ShowGenericParameters |
		                          UseIntrinsicTypeNames,
		                          
		All = ShowParameterNames | 
		      ShowAccessibility | 
		      UseFullyQualifiedNames |
		      ShowMemberModifiers |
		      ShowClassModifiers |
		      ShowInheritanceList |
              ShowGenericParameters |
              UseIntrinsicTypeNames,

		      
		AssemblyScoutDefaults = StandardConversionFlags |
		                        ShowAccessibility |	
		                        QualifiedNamesOnlyForReturnTypes |
		                        IncludeHTMLMarkup |
		                        UseLinkArrayList |
		                        ShowGenericParameters,
	}
}
