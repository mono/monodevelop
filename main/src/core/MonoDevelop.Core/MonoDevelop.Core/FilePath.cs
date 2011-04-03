// 
// FilePath.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Mono.Addins;
using MonoDevelop.Core.FileSystem;

namespace MonoDevelop.Core
{
	[Serializable]
	public struct FilePath: IComparable<FilePath>, IComparable, IEquatable<FilePath>
	{
		string fileName;

		public static readonly FilePath Null = new FilePath (null);
		public static readonly FilePath Empty = new FilePath (string.Empty);

		public FilePath (string name)
		{
			fileName = name;
		}

		public bool IsNull {
			get { return fileName == null; }
		}

		public bool IsNullOrEmpty {
			get { return string.IsNullOrEmpty (fileName); }
		}
		
		public bool IsNotNull {
			get { return fileName != null; }
		}

		public bool IsEmpty {
			get { return fileName != null && fileName.Length == 0; }
		}

		public FilePath FullPath {
			get {
				return new FilePath (!string.IsNullOrEmpty (fileName) ? Path.GetFullPath (fileName) : "");
			}
		}
		
		/// <summary>
		/// Returns a path in standard form, which can be used to be compared
		/// for equality with other canonical paths. It is similar to FullPath,
		/// but unlike FullPath, the directory "/a/b" is considered equal to "/a/b/"
		/// </summary>
		public FilePath CanonicalPath {
			get {
				string fp = Path.GetFullPath (fileName);
				if (fp.Length > 0 && fp[fp.Length - 1] == Path.DirectorySeparatorChar)
					return fp.TrimEnd (Path.DirectorySeparatorChar);
				else
					return fp;
			}
		}

		public string FileName {
			get {
				return Path.GetFileName (fileName);
			}
		}

		public string Extension {
			get {
				return Path.GetExtension (fileName);
			}
		}
		
		public bool HasExtension (string extension)
		{
			return fileName.Length > extension.Length
				&& fileName.EndsWith (extension, StringComparison.OrdinalIgnoreCase)
				&& fileName[fileName.Length - extension.Length - 1] != Path.PathSeparator;
		}

		public string FileNameWithoutExtension {
			get {
				return Path.GetFileNameWithoutExtension (fileName);
			}
		}

		public FilePath ParentDirectory {
			get {
				return new FilePath (Path.GetDirectoryName (fileName));
			}
		}

		public bool IsAbsolute {
			get { return Path.IsPathRooted (fileName); }
		}

		public bool IsChildPathOf (FilePath basePath)
		{
			StringComparison sc = PropertyService.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if (basePath.fileName [basePath.fileName.Length - 1] != Path.DirectorySeparatorChar)
				return fileName.StartsWith (basePath.fileName + Path.DirectorySeparatorChar, sc);
			else
				return fileName.StartsWith (basePath.fileName, sc);
		}

		public FilePath ChangeExtension (string ext)
		{
			return Path.ChangeExtension (fileName, ext);
		}

		public FilePath Combine (params FilePath[] paths)
		{
			string path = fileName;
			foreach (FilePath p in paths)
				path = Path.Combine (path, p.fileName);
			return new FilePath (path);
		}

		public FilePath Combine (params string[] paths)
		{
			string path = fileName;
			foreach (string p in paths)
				path = Path.Combine (path, p);
			return new FilePath (path);
		}
		
		/// <summary>
		/// Builds a path by combining all provided path sections
		/// </summary>
		public static FilePath Build (params string[] paths)
		{
			return Empty.Combine (paths);
		}
		
		public static FilePath GetCommonRootPath (IEnumerable<FilePath> paths)
		{
			FilePath root = FilePath.Null;
			foreach (FilePath p in paths) {
				if (root.IsNull)
					root = p;
				else if (root == p)
					continue;
				else if (root.IsChildPathOf (p))
					root = p;
				else {
					while (!root.IsNullOrEmpty && !p.IsChildPathOf (root))
						root = root.ParentDirectory;
				}
			}
			return root;
		}

		public FilePath ToAbsolute (FilePath basePath)
		{
			if (IsAbsolute)
				return FullPath;
			else
				return Combine (basePath, this).FullPath;
		}

		public FilePath ToRelative (FilePath basePath)
		{
			return FileService.AbsoluteToRelativePath (basePath, fileName);
		}

		public static implicit operator FilePath (string name)
		{
			return new FilePath (name);
		}

		public static implicit operator string (FilePath filePath)
		{
			return filePath.fileName;
		}

		public static bool operator == (FilePath name1, FilePath name2)
		{
			if (PropertyService.IsWindows)
				return string.Equals (name1.fileName, name2.fileName, StringComparison.OrdinalIgnoreCase);
			else
				return string.Equals (name1.fileName, name2.fileName, StringComparison.Ordinal);
		}

		public static bool operator != (FilePath name1, FilePath name2)
		{
			return !(name1 == name2);
		}

		public override bool Equals (object obj)
		{
			if (!(obj is FilePath))
				return false;

			FilePath fn = (FilePath) obj;
			return this == fn;
		}

		public override int GetHashCode ( )
		{
			if (fileName == null)
				return 0;
			if (PropertyService.IsWindows)
				return fileName.ToLower ().GetHashCode ();
			else
				return fileName.GetHashCode ();
		}

		public override string ToString ( )
		{
			return fileName;
		}

		public int CompareTo (FilePath filePath)
		{
			return string.Compare (fileName, filePath.fileName, PropertyService.IsWindows);
		}

		int IComparable.CompareTo (object obj)
		{
			if (!(obj is FilePath))
				return -1;
			return CompareTo ((FilePath) obj);
		}

		#region IEquatable<FilePath> Members

		bool IEquatable<FilePath>.Equals (FilePath other)
		{
			return this == other;
		}

		#endregion
	}

	public static class FilePathUtil
	{
		public static string[] ToStringArray (this FilePath[] paths)
		{
			string[] array = new string[paths.Length];
			for (int n = 0; n < paths.Length; n++)
				array[n] = paths[n].ToString ();
			return array;
		}
		
		public static FilePath[] ToFilePathArray (this string[] paths)
		{
			var array = new FilePath[paths.Length];
			for (int n = 0; n < paths.Length; n++)
				array[n] = paths[n];
			return array;
		}
		
		public static IEnumerable<string> ToPathStrings (this IEnumerable<FilePath> paths)
		{
			foreach (FilePath p in paths)
				yield return p.ToString ();
		}
	}
}
