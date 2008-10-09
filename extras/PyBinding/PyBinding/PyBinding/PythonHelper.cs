// PythonHelper.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.IO;

namespace PyBinding
{
	public class PythonHelper
	{
		static readonly char[] s_PathSeparators = new char[] {';', ':'};
		static Dictionary<string,string> m_ModuleCache = new Dictionary<string, string> ();

		public static string Which (string commandName)
		{
			List<string> paths = new List<string> ();

			foreach (string dirName in Environment.GetEnvironmentVariable ("PATH").Split (s_PathSeparators))
				paths.Add (dirName);

			foreach (string dirName in paths) {
				string absPath = System.IO.Path.Combine (dirName, commandName);

				if (System.IO.File.Exists (absPath))
					return absPath;
			}

			throw new FileNotFoundException ("Could not locate executable");
		}

		public static string ModuleFromFilename (string fileName)
		{
			if (String.IsNullOrEmpty (fileName))
				return String.Empty;
			else if (!fileName.ToLower ().EndsWith (".py"))
				return String.Empty;

			if (!m_ModuleCache.ContainsKey (fileName))
			{
				string[] parts = fileName.Split (Path.DirectorySeparatorChar);
				string module = parts[parts.Length - 1];
				module = module.Substring (0, module.Length - 3);
				string dirname = Path.GetDirectoryName (fileName);
				if (!String.IsNullOrEmpty (dirname)) {
					DirectoryInfo dirInfo = new DirectoryInfo (Path.GetDirectoryName (fileName));
					m_ModuleCache[fileName] = RecursiveModuleFromFile (dirInfo, module);
				}
				else {
					m_ModuleCache[fileName] = module;
				}
			}

			return m_ModuleCache[fileName];
		}

		static string RecursiveModuleFromFile (DirectoryInfo dirInfo, string modName)
		{
			bool matched = false;
			
			foreach (FileInfo fileInfo in dirInfo.GetFiles ("*.py"))
			{
				if (fileInfo.Name.Equals ("__init__.py")) {
					modName = modName.Insert (0, dirInfo.Name + ".");
					matched = true;
					break;
				}
			}

			if (!matched)
				return modName;

			return RecursiveModuleFromFile (dirInfo.Parent, modName);
		}
	}
}