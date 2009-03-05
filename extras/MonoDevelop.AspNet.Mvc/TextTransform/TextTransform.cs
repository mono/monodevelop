// 
// Main.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System;
using System.Collections.Generic;
using Mono.Options;

namespace Mono.TextTemplating
{
	class TextTransform
	{
		static OptionSet optionSet;
		const string name ="TextTransform.exe";
		
		public static int Main (string[] args)
		{
			if (args.Length == 0) {
				ShowHelp (true);
				return -1;
			}
			
			string output = null;
			List<string> refs = new List<string> ();
			List<string> imports = new List<string> ();
			List<string> includePaths = new List<string> ();
			List<string> referencePaths = new List<string> ();
			List<string> directiveProcessors = new List<string> ();
			Dictionary<string, string> values = new Dictionary<string, string> ();
			
			optionSet = new OptionSet () {
				{ "o=|out=", "The name of the output {file}", s => output = s },
				{ "r=", "Assemblies to reference", s => refs.Add (s) },
				{ "u=", "Namespaces to import <{0:namespace}>", s => imports.Add (s) },
				{ "I=", "Paths to search for included files", s => includePaths.Add (s) },
				{ "P=", "Paths to search for referenced assemblies", s => referencePaths.Add (s) },
				{ "dp=", "Directive processor name!class!assembly", s => directiveProcessors.Add (s) },
				{ "a=", "Key value pairs for directive processors", (s, p) => values[s] = p },
				{ "h|?|help", "Show help", s => ShowHelp (false) }
			};
			
			optionSet.Parse (args);
			
			return 0;
		}
		
		static void ShowHelp (bool concise)
		{
			Console.WriteLine ("TextTransform command line T4 processor");
			Console.WriteLine ("Usage: {0} [options] input-files", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ("Options:");
				optionSet.WriteOptionDescriptions (System.Console.Out);
			}
			Console.WriteLine ();
		}
	}
}