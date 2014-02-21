//
// Program.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using TypeScriptBinding.Hosting;

namespace TypeScriptBinding
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var engine = new Jurassic.ScriptEngine ();
			engine.EnableExposedClrTypes = true;

			var host = new IOImpl (engine, "/home/mkrueger");

			host.arguments = engine.Array.New (new [] {
				"test.ts"
			});

			engine.SetGlobalValue ("host", host); 
			using (var stream = new StreamReader (typeof(MainClass).Assembly.GetManifestResourceStream ("tsc.js"))) {
				engine.Evaluate (stream.ReadToEnd ());
			}

			engine.Execute (@"
Environment = host.Environment;
var batch = new TypeScript.BatchCompiler(host);
batch.batchCompile();
");
		}
	}
}
