//
// OutputFlags.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Projects.Dom.Output
{
	[Flags]
	public enum OutputFlags {
		None = 0,
		
		// Flags
		UseFullName              = 0x0001,
		IncludeReturnType        = 0x0002,
		IncludeParameters        = 0x0004,
		IncludeParameterName     = 0x0008,
		IncludeMarkup            = 0x0010,
		IncludeKeywords          = 0x0020,
		IncludeModifiers         = 0x0040,
		IncludeBaseTypes         = 0x0080,
		IncludeGenerics          = 0x0100,
		UseIntrinsicTypeNames    = 0x0200,
		HighlightName            = 0x0400,
		HideExtensionsParameter  = 0x0800,
		HideGenericParameterNames= 0x1000,
		HideArrayBrackets        = 0x2000,
		UseNETTypeNames          = 0x4000, // print 'System.Int32' intead of 'int'
		
		ClassBrowserEntries        = IncludeReturnType | IncludeParameters | IncludeGenerics,
		AssemblyBrowserDescription = IncludeGenerics | IncludeBaseTypes | IncludeReturnType | IncludeParameters | IncludeParameterName | IncludeMarkup | IncludeKeywords | IncludeModifiers
	}
}
