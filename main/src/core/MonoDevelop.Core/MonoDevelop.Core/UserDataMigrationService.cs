// 
// UserDataMigrationService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Addins;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core
{
	static class UserDataMigrationService
	{
		static UserProfile profile;
		static string version;
		static bool handlerAdded;
		
		//TODO: it would be nice to migrate custom addins that are installed after first run
		//maybe we would maintain a property with IDs of migrated addins, and check that on addin load
		//and if an unknown addin comes along, re-migrate just nodes of that addin
		public static void SetMigrationSource (UserProfile profile, string version)
		{
			if (UserDataMigrationService.profile != null)
				throw new InvalidOperationException ("Already set");
			if (profile == null)
				throw new ArgumentNullException ("profile");
			if (version == null)
				throw new ArgumentNullException ("version");
			
			UserDataMigrationService.profile = profile;
			UserDataMigrationService.version = version;
		}
		
		public static string SourceVersion {
			get { return version; }
		}
		
		public static bool HasSource {
			get { return version != null; }
		}
		
		public static void StartMigration ()
		{
			if (profile != null && !handlerAdded) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/UserDataMigration", HandleUserDataMigration);
				handlerAdded = true;
			}
		}
		
		static void HandleUserDataMigration (object sender, ExtensionNodeEventArgs args)
		{
			if (args.Change != ExtensionChange.Add)
				return;
			
			var node = (UserDataMigrationNode) args.ExtensionNode;
			if (node.SourceVersion != version)
				return;
			
			FilePath source = FilePath.Null;
			FilePath target = FilePath.Null;

			try {
				source = profile.GetLocation (node.Kind).Combine (node.SourcePath);
				target = UserProfile.Current.GetLocation (node.Kind).Combine (node.TargetPath);

				bool sourceIsDirectory = Directory.Exists (source);

				if (sourceIsDirectory) {
					if (Directory.Exists (target))
						return;
				} else {
					if (File.Exists (target) || Directory.Exists (target) || !File.Exists (source))
						return;
				}
			
				LoggingService.LogInfo ("Migrating '{0}' to '{1}'", source, target);
				if (!sourceIsDirectory)
					FileService.EnsureDirectoryExists (target.ParentDirectory);
				
				var handler = node.GetHandler ();
				if (handler != null) {
					handler.Migrate (source, target);
					return;
				}
				
				if (sourceIsDirectory) {
					DirectoryCopy (source, target);
				} else {
					File.Copy (source, target);
				}
			} catch (Exception ex) {
				string message = string.Format ("{0}: Failed to migrate '{1}' to '{2}'",
					node.Addin.Id, source.ToString () ?? "", target.ToString () ?? "");
				LoggingService.LogError (message, ex);
			}
		}
		
		static void DirectoryCopy (FilePath source, FilePath target)
		{
			foreach (FilePath f in Directory.EnumerateFiles (source, "*", SearchOption.AllDirectories)) {
				var rel = FileService.AbsoluteToRelativePath (source, f);
				var t = target.Combine (rel);
				var dir = t.ParentDirectory;
				if (!Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				File.Copy (f, t);
			}
		}
	}
	
	public interface IUserDataMigrationHandler
	{
		void Migrate (FilePath source, FilePath target);
	}
}
