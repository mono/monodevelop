// IncludeExtensionNode.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using MonoDevelop.Gettext;
using Mono.Addins;

namespace MonoDevelop.Gettext.ExtensionNodes
{
	[ExtensionNode ("Include", Description="Specifies a regular expression to use to extract translatable strings.")]
	class IncludeExtensionNode: ExtensionNode
	{
		[NodeAttribute ("regex", Required=true, Description="Regular expression")]
		string regexValue = null;
		
		[NodeAttribute ("group", Description="Index of the group that matches the string to be translated")]
		int group = 1;
		
		[NodeAttribute ("pluralGroup", Description="Index of the group that matches the plural number to be translated (valid only for plural regexes)")]
		int pluralGroup = 2;
		
		[NodeAttribute ("plural", Description="Set to 'true' if this regex includes a plural group.")]
		bool plural = false;
		
		[NodeAttribute ("regexOptions", Description="RegexOptions flags separated by '|'")]
		string regexOptions = null;
		
		[NodeAttribute ("escapeMode", Description="If the string is escaped, this can be used to unescape it using a mode defined in MonoDevelop.Gettext.StringEscaping.EscapeMode. If more flexibility is needed, define a Transform regex instead.")]
		StringEscaping.EscapeMode escapeMode = StringEscaping.EscapeMode.None;
		
		public string RegexValue {
			get {
				return regexValue;
			}
		}

		public int Group {
			get {
				return group;
			}
		}

		public int PluralGroup {
			get {
				return plural ? pluralGroup : -1;
			}
		}
		
		public string RegexOptions {
			get {
				return regexOptions;
			}
		}
		
		public StringEscaping.EscapeMode EscapeMode {
			get {
				return escapeMode;
			}
		}
	}
}
