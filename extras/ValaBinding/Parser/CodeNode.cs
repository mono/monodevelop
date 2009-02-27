//
// CodeNode.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2009 Levi Bard
//
// This source code is licenced under The MIT License:
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
using System.Collections.Generic;

using MonoDevelop.Core.Gui;

namespace MonoDevelop.ValaBinding.Parser
{
	public enum AccessModifier {
		Private,
		Protected,
		Public,
		Internal
	}

	/// <summary>
	/// Representation of a Vala code symbol
	/// </summary>
	public class CodeNode
	{
		private static Dictionary<string,string> publicIcons = new Dictionary<string, string> () {
			{ "namespaces", Stock.NameSpace },
			{ "class", Stock.Class },
			{ "struct", Stock.Struct },
			{ "enums", Stock.Enum },
			{ "field", Stock.Field },
			{ "method", Stock.Method },
			{ "property", Stock.Property },
			{ "constants", Stock.Literal },
			{ "signal", Stock.Event },
			{ "other", Stock.Delegate }
		};

		private static Dictionary<string,string> privateIcons = new Dictionary<string, string> () {
			{ "namespaces", Stock.NameSpace },
			{ "class", Stock.PrivateClass },
			{ "struct", Stock.PrivateStruct },
			{ "enums", Stock.PrivateEnum },
			{ "field", Stock.PrivateField },
			{ "method", Stock.PrivateMethod },
			{ "property", Stock.PrivateProperty },
			{ "constants", Stock.Literal },
			{ "signal", Stock.PrivateEvent },
			{ "other", Stock.PrivateDelegate }
		};

		private static Dictionary<string,string> protectedIcons = new Dictionary<string, string> () {
			{ "namespaces", Stock.NameSpace },
			{ "class", Stock.ProtectedClass },
			{ "struct", Stock.ProtectedStruct },
			{ "enums", Stock.ProtectedEnum },
			{ "field", Stock.ProtectedField },
			{ "method", Stock.ProtectedMethod },
			{ "property", Stock.ProtectedProperty },
			{ "constants", Stock.Literal },
			{ "signal", Stock.ProtectedEvent },
			{ "other", Stock.ProtectedDelegate }
		};

		private static Dictionary<AccessModifier,Dictionary<string,string>> iconTable = new Dictionary<AccessModifier, Dictionary<string, string>> () {
			{ AccessModifier.Public, publicIcons },
			{ AccessModifier.Internal, publicIcons },
			{ AccessModifier.Private, privateIcons },
			{ AccessModifier.Protected, protectedIcons }
		};

		public string Name{ get; set; }
		public string FullName{ get; set; }
		public AccessModifier Access{ get; set; }
		public string NodeType{ get; set; }
		public string Icon {
			get{ return GetIconForType (NodeType, Access); }
		}
		public virtual string Description {
			get{ return string.Format("{0} {1}", NodeType, Name); }
		}

		public CodeNode () {}

		public CodeNode (string type, string name, string parentname)
		{
			Name = name;
			NodeType = type;
			FullName = (string.IsNullOrEmpty (parentname))? Name: string.Format ("{0}.{1}", parentname, name);
		}
		
		public CodeNode (string type, string name, string parentname, AccessModifier access): this (type, name, parentname)
		{
			Access = access;
		}

		public static string GetIconForType (string nodeType, AccessModifier visibility)
		{
			string icon = null;
			iconTable[visibility].TryGetValue (nodeType, out icon);
			return icon;
		}

	}
}
