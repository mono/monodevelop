// ProjectExtensionUtil.cs
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

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	public static class ProjectExtensionUtil
	{
		static int loading;
		static LocalDataStoreSlot loadControlSlot;
		
		static ProjectExtensionUtil ()
		{
			loadControlSlot = Thread.AllocateDataSlot ();
		}
		
		public static ISolutionItemHandler GetItemHandler (SolutionItem item)
		{
			return item.GetItemHandler ();
		}
		
		public static void InstallHandler (ISolutionItemHandler handler, SolutionItem item)
		{
			item.SetItemHandler (handler);
		}
		
		public static SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName, ItemLoadCallback callback)
		{
			return Services.ProjectService.ExtensionChain.LoadSolutionItem (monitor, fileName, callback);
		}
		
		public static void BeginLoadOperation ()
		{
			Interlocked.Increment (ref loading);
			LoadOperation op = (LoadOperation) Thread.GetData (loadControlSlot);
			if (op == null) {
				op = new LoadOperation ();
				Thread.SetData (loadControlSlot, op);
			}
			op.LoadingCount++;
		}
		
		public static void EndLoadOperation ()
		{
			Interlocked.Decrement (ref loading);
			LoadOperation op = (LoadOperation) Thread.GetData (loadControlSlot);
			if (op != null)
				return;
			if (--op.LoadingCount == 0) {
				op.End ();
				Thread.SetData (loadControlSlot, null);
			}
		}
		
		public static void LoadControl (ILoadController rc)
		{
			if (loading == 0)
				return;
			LoadOperation op = (LoadOperation) Thread.GetData (loadControlSlot);
			if (op != null)
				op.Add (rc);
		}
		
		public static string EncodePath (SolutionEntityItem item, string path, string oldPath)
		{
			IPathHandler ph = item.GetItemHandler () as IPathHandler;
			if (ph != null)
				return ph.EncodePath (path, oldPath);
			else {
				string basePath = Path.GetDirectoryName (item.FileName);
				return FileService.RelativeToAbsolutePath (basePath, path);
			}
		}
		
		public static string DecodePath (SolutionEntityItem item, string path)
		{
			IPathHandler ph = item.GetItemHandler () as IPathHandler;
			if (ph != null)
				return ph.DecodePath (path);
			else {
				string basePath = Path.GetDirectoryName (item.FileName);
				return FileService.AbsoluteToRelativePath (basePath, path);
			}
		}
	}
	
	class LoadOperation
	{
		List<ILoadController> objects = new List<ILoadController> ();
		public int LoadingCount;
		
		public void Add (object ob)
		{
			ILoadController lc = ob as ILoadController;
			if (lc != null) {
				objects.Add (lc);
				lc.BeginLoad ();
			}
		}
		
		public void End ()
		{
			foreach (ILoadController ob in objects)
				ob.EndLoad ();
		}
	}
	

	
	public delegate SolutionEntityItem ItemLoadCallback (IProgressMonitor monitor, string fileName);
}
