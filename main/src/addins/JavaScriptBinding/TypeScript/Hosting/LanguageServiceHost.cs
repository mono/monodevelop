//
// LanguageServiceHost.cs
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
using Jurassic.Library;
using Jurassic;

namespace Hosting
{
	public class LanguageServiceHost : ObjectInstance
	{
		public LanguageServiceHost (ScriptEngine engine) : base (engine)
		{
			this.PopulateFunctions ();
		}

		#region ILanguageServiceHost implementation
		[JSFunction(Name="getCompilationSettings")]
		public string GetCompilationSettings()
		{
			return null;
		}

		[JSFunction(Name="getScriptFileNames")]
		public ArrayInstance GetScriptFileNames()
		{
			return null;
		}

		[JSFunction(Name="getScriptVersion")]
		public int GetScriptVersion(string fileName)
		{
		}

		[JSFunction(Name="getScriptIsOpen")]
		public bool GetScriptIsOpen(string fileName)
		{
		}

		[JSFunction(Name="getScriptByteOrderMark")]
		public int GetScriptByteOrderMark(string fileName)
		{
			return (int)TypeScriptBinding.Hosting.ByteOrderMark.None;
		}

		[JSFunction(Name="getScriptSnapshot")]
		public ObjectInstance GetScriptSnapshot(string fileName)
		{
			return null;
		}

		[JSFunction(Name="getDiagnosticsObject")]
		public ObjectInstance GetDiagnosticsObject()
		{
			return null;
		}

		[JSFunction(Name="getLocalizedDiagnosticMessages")]
		public string GetLocalizedDiagnosticMessages()
		{
			return "";
		}
		#endregion

	}
}

