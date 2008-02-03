//
// NetAmbience.cs
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

namespace MonoDevelop.Dom.Output
{
	public class NetAmbience : Ambience
	{
		public NetAmbience () : base ("NET")
		{
			classTypes[ClassType.Class]     = "Class";
			classTypes[ClassType.Enum]      = "Enumeration";
			classTypes[ClassType.Interface] = "Interface";
			classTypes[ClassType.Struct]    = "Structure";
			classTypes[ClassType.Delegate]  = "Delegate";
			
			parameterModifiers[ParameterModifiers.In]       = "In";
			parameterModifiers[ParameterModifiers.Out]      = "Out";
			parameterModifiers[ParameterModifiers.Ref]      = "Ref";
			parameterModifiers[ParameterModifiers.Params]   = "Params";
			parameterModifiers[ParameterModifiers.Optional] = "Optional";
			
			modifiers[Modifiers.Private]              = "Private";
			modifiers[Modifiers.Internal]             = "Internal";
			modifiers[Modifiers.Protected]            = "Protected";
			modifiers[Modifiers.Public]               = "Public";
			modifiers[Modifiers.Abstract]             = "Abstract";
			modifiers[Modifiers.Virtual]              = "Virtual";
			modifiers[Modifiers.Sealed]               = "Sealed";
			modifiers[Modifiers.Static]               = "Static";
			modifiers[Modifiers.Override]             = "Override";
			modifiers[Modifiers.Readonly]             = "Readonly";
			modifiers[Modifiers.Const]                = "Const";
			modifiers[Modifiers.Partial]              = "Partial";
			modifiers[Modifiers.Extern]               = "Extern";
			modifiers[Modifiers.Volatile]             = "Volatile";
			modifiers[Modifiers.Unsafe]               = "Unsafe";
			modifiers[Modifiers.Overloads]            = "Overloads";
			modifiers[Modifiers.WithEvents]           = "WithEvents";
			modifiers[Modifiers.Default]              = "Default";
			modifiers[Modifiers.Fixed]                = "Fixed";
			modifiers[Modifiers.ProtectedAndInternal] = "Protected Internal";
			modifiers[Modifiers.ProtectedOrInternal]  = "Internal Protected";
		}
	}
}
