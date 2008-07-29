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
		EmitMarkup               = 0x0010,
		EmitKeywords             = 0x0020,
		IncludeModifiers         = 0x0040,
		IncludeBaseTypes         = 0x0080,
		IncludeGenerics          = 0x0100,
		UseIntrinsicTypeNames    = 0x0200,

		ClassBrowserEntries        = IncludeReturnType | IncludeParameters,
		AssemblyBrowserDescription = IncludeBaseTypes | IncludeReturnType | IncludeParameters | IncludeParameterName | EmitMarkup | EmitKeywords | IncludeModifiers
	}
}
