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
using System.IO;
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
			}
			
			TemplateGenerator generator = new TemplateGenerator ();
			string outputFile = null, inputFile = null;
			
			optionSet = new OptionSet () {
				{ "o=|out=", "The name of the output {file}", s => outputFile = s },
				{ "r=", "Assemblies to reference", s => generator.Refs.Add (s) },
				{ "u=", "Namespaces to import <{0:namespace}>", s => generator.Imports.Add (s) },
				{ "I=", "Paths to search for included files", s => generator.IncludePaths.Add (s) },
				{ "P=", "Paths to search for referenced assemblies", s => generator.ReferencePaths.Add (s) },
				{ "dp=", "Directive processor name!class!assembly", s => generator.DirectiveProcessors.Add (s) },
				{ "a=", "Key value pairs for directive processors", (s, p) => generator.ProcessorValues[s] = p },
				{ "h|?|help", "Show help", s => ShowHelp (false) }
			};
			
			List<string> remainingArgs = optionSet.Parse (args);
			
			if (String.IsNullOrEmpty (outputFile)) {
				Console.WriteLine ("No output file specified.");
				return -1;
			}
			
			if (remainingArgs.Count != 1) {
				Console.WriteLine ("No input file specified.");
				return -1;
			}
			inputFile = remainingArgs [0];
			
			if (!File.Exists (inputFile)) {
				Console.WriteLine ("Input file '{0}' does not exist.");
				return -1;
			}
			
			System.Console.Write("Processing '{0}'... ", inputFile);
			generator.ProcessTemplate (inputFile, outputFile);
			
			if (generator.Errors.HasErrors) {
				System.Console.WriteLine("failed.");
			} else {
				System.Console.WriteLine("completed successfully.");
			}
			
			foreach (System.CodeDom.Compiler.CompilerError err in generator.Errors)
				Console.WriteLine ("{0}({1},{2}): {3} {4}", err.FileName, err.Line, err.Column,
				                   err.IsWarning? "WARNING" : "ERROR", err.ErrorText);
			
			return generator.Errors.HasErrors? -1 : 0;
		}
		
		static void ShowHelp (bool concise)
		{
			Console.WriteLine ("TextTransform command line T4 processor");
			Console.WriteLine ("Usage: {0} [options] input-file", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ("Options:");
				optionSet.WriteOptionDescriptions (System.Console.Out);
			}
			Console.WriteLine ();
			Environment.Exit (0);
		}
	}
}