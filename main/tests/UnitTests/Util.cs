// Util.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using System.IO;
using System.Xml;
using System.Collections;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace UnitTests
{
	public static class Util
	{
		static string rootDir;
		static int projectId = 1;
		
		public static string TestsRootDir {
			get {
				if (rootDir == null) {
					rootDir = Path.GetDirectoryName (typeof(Util).Assembly.Location);
					rootDir = Path.Combine (Path.Combine (rootDir, ".."), "..");
					rootDir = Path.GetFullPath (Path.Combine (rootDir, "tests"));
				}
				return rootDir;
			}
		}
		
		public static string TmpDir {
			get { return Path.Combine (TestsRootDir, "tmp"); }
		}
		
		public static ProgressMonitor GetMonitor ()
		{
			return GetMonitor (true);
		}
		
		public static ProgressMonitor GetMonitor (bool ignoreLogMessages)
		{
			ConsoleProgressMonitor m = new ConsoleProgressMonitor ();
			m.IgnoreLogMessages = ignoreLogMessages;
			return m;
		}
		
		public static string GetSampleProject (params string[] projectName)
		{
			string srcDir = Path.Combine (Path.Combine (TestsRootDir, "test-projects"), Combine (projectName));
			string projDir = srcDir;
			srcDir = Path.GetDirectoryName (srcDir);
			string tmpDir = CreateTmpDir (Path.GetFileName (projDir));
			CopyDir (srcDir, tmpDir);
			return Path.Combine (tmpDir, Path.GetFileName (projDir));
		}
		
		public static string GetSampleProjectPath (params string[] projectName)
		{
			return Path.Combine (Path.Combine (TestsRootDir, "test-projects"), Combine (projectName));
		}
		
		public static string CreateTmpDir (string hint)
		{
			string tmpDir = Path.Combine (TmpDir, hint + "-" + projectId.ToString ());
			projectId++;
			
			if (!Directory.Exists (tmpDir))
				Directory.CreateDirectory (tmpDir);
			return tmpDir;
		}
		
		public static void ClearTmpDir ()
		{
			if (Directory.Exists (TmpDir))
				Directory.Delete (TmpDir, true);
			projectId = 1;
		}
		
		public static string GetXmlFileInfoset (params string[] path)
		{
			string file = Combine (path);
			XmlDocument doc = new XmlDocument ();
			doc.Load (file);
			return Infoset (doc);
		}

		public static string ToWindowsEndings (string s)
		{
			return s.Replace ("\r\n", "\n").Replace ("\n", "\r\n");
		}

		public static string ToSystemEndings (string s)
		{
			if (!Platform.IsWindows)
				return s.Replace ("\r\n", "\n");
			else
				return s;
		}

		public static string ReadAllWithWindowsEndings (string fileName)
		{
			return File.ReadAllText (fileName).Replace ("\r\n", "\n").Replace ("\n", "\r\n");
		}
		
		static void CopyDir (string src, string dst)
		{
			if (Path.GetFileName (src) == ".svn")
				return;
			
			if (!Directory.Exists (dst))
				Directory.CreateDirectory (dst);
			
			foreach (string file in Directory.GetFiles (src))
				File.Copy (file, Path.Combine (dst, Path.GetFileName (file)), overwrite: true);
			
			foreach (string dir in Directory.GetDirectories (src))
				CopyDir (dir, Path.Combine (dst, Path.GetFileName (dir)));
		}
		

		public static string Infoset (XmlNode nod)
		{
			StringBuilder sb = new StringBuilder ();
			GetInfoset (nod, sb);
			return sb.ToString ();
		}

		static void GetInfoset (XmlNode nod, StringBuilder sb)
		{
			switch (nod.NodeType) {
			case XmlNodeType.Document:
				GetInfoset (((XmlDocument)nod).DocumentElement, sb);
				break;
			case XmlNodeType.Attribute:
				if (nod.LocalName == "xmlns" && nod.NamespaceURI == "http://www.w3.org/2000/xmlns/") return;
				sb.Append (" " + nod.NamespaceURI + ":" + nod.LocalName + "='" + nod.Value + "'");
				break;

			case XmlNodeType.Element:
				XmlElement elem = (XmlElement) nod;
				sb.Append ("<" + elem.NamespaceURI + ":" + elem.LocalName);

				ArrayList ats = new ArrayList ();
				foreach (XmlAttribute at in elem.Attributes)
					ats.Add (at.LocalName + " " + at.NamespaceURI);

				ats.Sort ();

				foreach (string name in ats) {
					string[] nn = name.Split (' ');
					GetInfoset (elem.Attributes[nn[0], nn[1]], sb);
				}

				sb.Append (">");
				foreach (XmlNode cn in elem.ChildNodes)
					GetInfoset (cn, sb);
				sb.Append ("</>");
				break;

			default:
				sb.Append (nod.OuterXml);
				break;
			}
		}
		
		public static string Combine (params string[] paths)
		{
			string p = paths [0];
			for (int n=1; n<paths.Length; n++)
				p = Path.Combine (p, paths [n]);
			return p;
		}
	}
}
