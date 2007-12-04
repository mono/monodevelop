//
// FileDatabase.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;
using Mono.Addins.Serialization;

namespace Mono.Addins.Database
{
	internal class FileDatabase
	{
		Stream updatingLock;
		
		bool inTransaction;
		string rootDirectory;
		Hashtable foldersToUpdate;
		Hashtable deletedFiles;
		Hashtable deletedDirs;
		IDisposable transactionLock;
		bool ignoreDesc;
		
		public FileDatabase (string rootDirectory)
		{
			this.rootDirectory = rootDirectory;
		}
		
		string DatabaseLockFile {
			get { return Path.Combine (rootDirectory, "fdb-lock"); }
		}
		
		string UpdateDatabaseLockFile {
			get { return Path.Combine (rootDirectory, "fdb-update-lock"); }
		}
		
		// Returns 'true' if description data must be ignored when reading the contents of a file
		public bool IgnoreDescriptionData {
			get { return ignoreDesc; }
			set { ignoreDesc = value; }
		}
		
		public bool BeginTransaction ()
		{
			if (inTransaction)
				throw new InvalidOperationException ("Already in a transaction");
			
			transactionLock = LockWrite ();
			try {
				updatingLock = new FileStream (UpdateDatabaseLockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			} catch (IOException) {
				// The database is already being updated. Can't do anything for now.
				return false;
			} finally {
				transactionLock.Dispose ();
			}

			// Delete .new files that could have been left by an aborted database update
			
			transactionLock = LockRead ();
			CleanDirectory (rootDirectory);
			
			inTransaction = true;
			foldersToUpdate = new Hashtable ();
			deletedFiles = new Hashtable ();
			deletedDirs = new Hashtable ();
			return true;
		}
		
		void CleanDirectory (string dir)
		{
			foreach (string file in Directory.GetFiles (dir, "*.new"))
				File.Delete (file);
		
			foreach (string sdir in Directory.GetDirectories (dir))
				CleanDirectory (sdir);
		}
		
		public IDisposable LockRead ()
		{
			return FileLock (FileAccess.Read, -1);
		}
		
		public IDisposable LockWrite ()
		{
			return FileLock (FileAccess.Write, -1);
		}
		
		IDisposable FileLock (FileAccess access, int timeout)
		{
			DateTime tim = DateTime.Now;
			DateTime wt = tim;

			FileShare share = access == FileAccess.Read ? FileShare.Read : FileShare.None;
			string path = Path.GetDirectoryName (DatabaseLockFile);
			
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);
			
			do {
				try {
					return new FileStream (DatabaseLockFile, FileMode.OpenOrCreate, access, share);
				}
				catch (IOException) {
					// Wait and try again
					if ((DateTime.Now - wt).TotalSeconds >= 4) {
						Console.WriteLine ("Waiting for " + access + " add-in database lock");
						wt = DateTime.Now;
					}

				}
				System.Threading.Thread.Sleep (100);
			}
			while (timeout <= 0 || (DateTime.Now - tim).TotalMilliseconds < timeout);
			
			throw new Exception ("Lock timed out");
		}
		
		public Stream Create (string fileName)
		{
			if (inTransaction) {
				deletedFiles.Remove (fileName);
				foldersToUpdate [Path.GetDirectoryName (fileName)] = null;
				return File.Create (fileName + ".new");
			}
			else
				return File.Create (fileName);
		}
		
		public Stream OpenRead (string fileName)
		{
			if (inTransaction) {
				if (deletedFiles.Contains (fileName))
					throw new FileNotFoundException ();
				if (File.Exists (fileName + ".new"))
					return File.OpenRead (fileName + ".new");
			}
			return File.OpenRead (fileName);
		}
		
		public void Delete (string fileName)
		{
			if (inTransaction) {
				if (deletedFiles.Contains (fileName))
					return;
				if (File.Exists (fileName + ".new"))
					File.Delete (fileName + ".new");
				if (File.Exists (fileName))
					deletedFiles [fileName] = null;
			}
			else {
				File.Delete (fileName);
			}
		}

		public void DeleteDir (string dirName)
		{
			if (inTransaction) {
				if (deletedDirs.Contains (dirName))
					return;
				if (Directory.Exists (dirName + ".new"))
					Directory.Delete (dirName + ".new", true);
				if (Directory.Exists (dirName))
					deletedDirs [dirName] = null;
			}
			else {
				Directory.Delete (dirName, true);
			}
		}

		
		public bool Exists (string fileName)
		{
			if (inTransaction) {
				if (deletedFiles.Contains (fileName))
					return false;
				if (File.Exists (fileName + ".new"))
					return true;
			}
			return File.Exists (fileName);
		}
		
		public bool DirExists (string dir)
		{
			return Directory.Exists (dir);
		}
		
		public void CreateDir (string dir)
		{
			Directory.CreateDirectory (dir);
		}
		
		public string[] GetDirectories (string dir)
		{
			return Directory.GetDirectories (dir);
		}
		
		public bool DirectoryIsEmpty (string dir)
		{
			foreach (string f in Directory.GetFiles (dir)) {
				if (!inTransaction || !deletedFiles.Contains (f))
					return false;
			}
			return true;
		}
		
		public string[] GetDirectoryFiles (string dir, string pattern)
		{
			if (pattern == null || pattern.Length == 0 || pattern.EndsWith ("*"))
				throw new NotSupportedException ();
			
			if (inTransaction) {
				Hashtable files = new Hashtable ();
				foreach (string f in Directory.GetFiles (dir, pattern)) {
					if (!deletedFiles.Contains (f))
						files [f] = f;
				}
				foreach (string f in Directory.GetFiles (dir, pattern + ".new")) {
					string ofile = f.Substring (0, f.Length - 4);
					files [ofile] = ofile;
				}
				string[] res = new string [files.Count];
				int n = 0;
				foreach (string s in files.Keys)
					res [n++] = s;
				return res;
			}
			else
				return Directory.GetFiles (dir, pattern);
		}
		
		public void CommitTransaction ()
		{
			if (!inTransaction)
				return;
			
			try {
				transactionLock.Dispose ();
				transactionLock = LockWrite ();
				foreach (string dir in foldersToUpdate.Keys) {
					foreach (string file in Directory.GetFiles (dir, "*.new")) {
						string dst = file.Substring (0, file.Length - 4);
						File.Delete (dst);
						File.Move (file, dst);
					}
				}
				foreach (string file in deletedFiles.Keys)
					File.Delete (file);
				foreach (string dir in deletedDirs.Keys)
					Directory.Delete (dir, true);
			}
			finally {
				transactionLock.Dispose ();
				EndTransaction ();
			}
		}
		
		public void RollbackTransaction ()
		{
			if (!inTransaction)
				return;
			
			try {
				// There is no need for write lock since existing files won't be updated.
				
				foreach (string dir in foldersToUpdate.Keys) {
					foreach (string file in Directory.GetFiles (dir, "*.new"))
						File.Delete (file);
				}
			}
			finally {
				transactionLock.Dispose ();
				EndTransaction ();
			}
		}
		
		void EndTransaction ()
		{
			inTransaction = false;
			deletedFiles = null;
			foldersToUpdate = null;
			updatingLock.Close ();
			updatingLock = null;
			transactionLock = null;
		}


		
		// The ReadSharedObject and WriteSharedObject methods can be used to read/write objects from/to files.
		// What's special about those methods is that they handle file name colisions.
		
		public string[] GetObjectSharedFiles (string directory, string sharedFileName, string extension)
		{
			return GetDirectoryFiles (directory, sharedFileName + "*" + extension);
		}
		
		public object ReadSharedObject (string fullFileName, BinaryXmlTypeMap typeMap)
		{
			object result;
			OpenFileForPath (fullFileName, null, typeMap, false, out result);
			return result;
		}
		
		public bool SharedObjectExists (string directory, string sharedFileName, string extension, string objectId)
		{
			return null != GetSharedObjectFile (directory, sharedFileName, extension, objectId);
		}
		
		public string GetSharedObjectFile (string directory, string sharedFileName, string extension, string objectId)
		{
			string fileName;
			ReadSharedObject (directory, sharedFileName, extension, objectId, null, true, out fileName);
			return fileName;
		}
		
		public object ReadSharedObject (string directory, string sharedFileName, string extension, string objectId, BinaryXmlTypeMap typeMap, out string fileName)
		{
			return ReadSharedObject (directory, sharedFileName, extension, objectId, typeMap, false, out fileName);
		}
		
		object ReadSharedObject (string directory, string sharedFileName, string extension, string objectId, BinaryXmlTypeMap typeMap, bool checkOnly, out string fileName)
		{
			string name = sharedFileName + "_" + Util.GetStringHashCode (objectId).ToString ("x");
			string file = Path.Combine (directory, name + extension);

			object result;
			if (OpenFileForPath (file, objectId, typeMap, checkOnly, out result)) {
				fileName = file;
				return result;
			}
		
			// The file is not the one we expected. There has been a name colision
			
			foreach (string f in GetDirectoryFiles (directory, name + "*" + extension)) {
				if (f != file && OpenFileForPath (f, objectId, typeMap, checkOnly, out result)) {
					fileName = f;
					return result;
				}
			}
			
			// File not found
			fileName = null;
			return null;
		}
		
		bool OpenFileForPath (string f, string objectId, BinaryXmlTypeMap typeMap, bool checkOnly, out object result)
		{
			result = null;
			
			if (!Exists (f)) {
				return false;
			}
			using (Stream s = OpenRead (f)) {
				BinaryXmlReader reader = new BinaryXmlReader (s, typeMap);
				reader.ReadBeginElement ();
				string id = reader.ReadStringValue ("id");
				if (objectId == null || objectId == id) {
					if (!checkOnly)
						result = reader.ReadValue ("data");
					return true;
				}
			}
			return false;
		}
		
		public void WriteSharedObject (string objectId, string targetFile, BinaryXmlTypeMap typeMap, IBinaryXmlElement obj)
		{
			WriteSharedObject (null, null, null, objectId, targetFile, typeMap, obj);
		}
		
		public string WriteSharedObject (string directory, string sharedFileName, string extension, string objectId, string readFileName, BinaryXmlTypeMap typeMap, IBinaryXmlElement obj)
		{
			string file = readFileName;
			
			if (file == null) {
				int count = 1;
				string name = sharedFileName + "_" + Util.GetStringHashCode (objectId).ToString ("x");
				file = Path.Combine (directory, name + extension);
				
				while (Exists (file)) {
					count++;
					file = Path.Combine (directory, name + "_" + count + extension);
				}
			}
			
			using (Stream s = Create (file)) {
				BinaryXmlWriter writer = new BinaryXmlWriter (s, typeMap);
				writer.WriteBeginElement ("File");
				writer.WriteValue ("id", objectId);
				writer.WriteValue ("data", obj);
				writer.WriteEndElement ();
			}
			return file;
		}
		
		public object ReadObject (string file, BinaryXmlTypeMap typeMap)
		{
			using (Stream s = OpenRead (file)) {
				BinaryXmlReader reader = new BinaryXmlReader (s, typeMap);
				return reader.ReadValue ("data");
			}
		}
		
		public void WriteObject (string file, object obj, BinaryXmlTypeMap typeMap)
		{
			using (Stream s = Create (file)) {
				BinaryXmlWriter writer = new BinaryXmlWriter (s, typeMap);
				writer.WriteValue ("data", obj);
			}
		}	
	}
}
