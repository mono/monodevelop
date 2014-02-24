//
// IOImpl.cs
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
using Mono.TextEditor.Utils;
using System.Text;
using System.Linq;
using Jurassic.Library;
using Jurassic;

namespace TypeScriptBinding.Hosting
{
	class ScriptingHost : ObjectInstance
	{
		string executingFilePath;

		public string ExecutingFilePath {
			get {
				return executingFilePath;
			}
			set {
				executingFilePath = value;
			}
		}

		public StringWriter ErrorWriter {
			get;
			private set;
		}

		public StringWriter OutWriter {
			get;
			private set;
		}

		public void ResetIO ()
		{
			Stderr = new TextWriterWrapper (Engine, ErrorWriter = new StringWriter ());
			Stdout = new TextWriterWrapper (Engine, OutWriter = new StringWriter ());
		}

		public ScriptingHost (ScriptEngine engine, string executingFilePath) : base (engine)
		{
			this.executingFilePath = executingFilePath;
			ResetIO ();
			this.PopulateFunctions ();
		}

		[JSProperty]
		public ObjectInstance Environment {
			get {
				return this;
			}
		}

		#region IIO implementation

		[JSFunction(Name="readFile")]
		public FileInformation ReadFile (string path, int codepage)
		{
			return FileInformation.Read (Engine, path, codepage);
		}

		[JSFunction(Name="writeFile")]
		public void WriteFile (string path, string contents, bool writeByteOrderMark)
		{
			TextFileUtility.WriteText (path, contents, Encoding.UTF8, writeByteOrderMark); 
		}

		[JSFunction(Name="deleteFile")]
		public void DeleteFile (string path)
		{
			File.Delete (path); 
		}

		[JSFunction(Name="dir")]
		public string[] Dir (string path, object re, object options)
		{
			return Directory.GetFiles (path);
		}

		[JSFunction(Name="fileExists")]
		public bool FileExists (string path)
		{
			return File.Exists (path);
		}

		[JSFunction(Name="directoryExists")]
		public bool DirectoryExists (string path)
		{
			return Directory.Exists (path);
		}

		[JSFunction(Name="createDirectory")]
		public void CreateDirectory (string path)
		{
			Directory.CreateDirectory (path);
		}

		[JSFunction(Name="resolvePath")]
		public string ResolvePath (string path)
		{
			return System.IO.Path.IsPathRooted (path) ? path : Path.Combine (GetExecutingFilePath (), path);
		}

		[JSFunction(Name="dirName")]
		public string DirName (string path)
		{
			return Path.GetDirectoryName(path);
		}

		[JSFunction(Name="findFile")]
		public ResolvedFile FindFile (string rootPath, string partialFilePath)
		{
			Console.WriteLine ("FIND: " + partialFilePath);
			var file = Directory.GetFiles (rootPath, partialFilePath, SearchOption.AllDirectories).FirstOrDefault (); 
			if (file == null)
				return null;
			return new ResolvedFile (Engine, file);
		}

		[JSFunction(Name="print")]
		public void Print (string str)
		{
			Console.Write (str);
		}

		[JSFunction(Name="printLine")]
		public void PrintLine (string str)
		{
			Console.WriteLine (str);
		}

		//[JSFunction(Name="watchFile")]
		public IFileWatcher WatchFile (string fileName, Action<string> callback)
		{
			return null;
		}

		[JSFunction(Name="run")]
		public void Run (string source, string filename)
		{
			Console.WriteLine ("RUN");
		}

		[JSFunction(Name="getExecutingFilePath")]
		public string GetExecutingFilePath ()
		{
			return executingFilePath;
		}

		[JSFunction(Name="quit")]
		public void Quit (int exitCode)
		{
			// TODO
		}

		[JSProperty(Name="arguments")]
		public ArrayInstance Arguments {
			get;
			set;
		}

		[JSProperty(Name="stderr")]
		public ObjectInstance Stderr {
			get;
			set;
		}

		[JSProperty(Name="stdout")]
		public ObjectInstance Stdout {
			get;
			set;
		}
		#endregion

		#region IEnvironment implementation
		[JSFunction(Name="supportsCodePage")]
		public bool SupportsCodePage ()
		{
			return true;
		}

		[JSFunction(Name="listFiles")]
		public string[] ListFiles (string path, object re, object options)
		{
			return null;
		}

		[JSFunction(Name="currentDirectory")]
		public string CurrentDirectory ()
		{
			return this.executingFilePath;

		}

		[JSProperty(Name="standardOut")]
		public ObjectInstance StandardOut {
			get {
				return Stdout;
			}
		}

		[JSProperty(Name="newLine")]
		public string NewLine {
			get {
				return System.Environment.NewLine;
			}
		}
		#endregion
	}
}