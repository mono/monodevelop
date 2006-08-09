// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Ambience
{
	[Flags]
	public enum ConversionFlags {
		None                   = 0,
		ShowParameterNames     = 1,
		ShowAccessibility      = 16,
		UseFullyQualifiedNames = 2,
		ShowMemberModifiers          = 4,
		ShowInheritanceList    = 8,
		IncludeHTMLMarkup      = 32,
		UseLinkArrayList       = 64,
		QualifiedNamesOnlyForReturnTypes = 128,
		IncludeBodies          = 256,
		IncludePangoMarkup     = 512,
		ShowClassModifiers     = 1024,
		ShowGenericParameters  = 2048,
		
		StandardConversionFlags = ShowParameterNames | 
		                          UseFullyQualifiedNames | 
		                          ShowMemberModifiers |
		                          ShowClassModifiers |
		                          ShowGenericParameters,
		                          
		All = ShowParameterNames | 
		      ShowAccessibility | 
		      UseFullyQualifiedNames |
		      ShowMemberModifiers |
		      ShowClassModifiers |
		      ShowInheritanceList |
		      ShowGenericParameters,

		      
		AssemblyScoutDefaults = StandardConversionFlags |
		                        ShowAccessibility |	
		                        QualifiedNamesOnlyForReturnTypes |
		                        IncludeHTMLMarkup |
		                        UseLinkArrayList |
		                        ShowGenericParameters,
	}
}
