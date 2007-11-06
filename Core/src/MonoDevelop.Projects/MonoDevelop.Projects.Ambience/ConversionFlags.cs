//  ConversionFlags.cs
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
		ShowReturnType         = 1<<13,
		ShowParameters         = 1<<14,
		
		StandardConversionFlags = ShowParameterNames | 
		                          UseFullyQualifiedNames | 
		                          ShowMemberModifiers |
		                          ShowClassModifiers |
		                          ShowGenericParameters |
		                          ShowReturnType |
		                          ShowParameters |
		                          UseIntrinsicTypeNames,
		                          
		All = ShowParameterNames | 
		      ShowAccessibility | 
		      UseFullyQualifiedNames |
		      ShowMemberModifiers |
		      ShowClassModifiers |
		      ShowInheritanceList |
              ShowGenericParameters |
              ShowReturnType |
              ShowParameters |
              UseIntrinsicTypeNames,

		      
		AssemblyScoutDefaults = StandardConversionFlags |
		                        ShowAccessibility |	
		                        QualifiedNamesOnlyForReturnTypes |
		                        IncludeHTMLMarkup |
		                        UseLinkArrayList |
		                        ShowReturnType |
  	                            ShowParameters |
		                        ShowGenericParameters,
	}
}
