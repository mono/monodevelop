// 
// UpdateResult.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using System.Net;
using MonoDevelop.Core.Gui;
using System.Collections.Generic;

namespace MonoDevelop.Platform.Updater
{
	public class Update
	{
		public string Name;
		public string Url;
		public string Version;
		public DateTime Date;
		public List<Release> Releases;
	}
	
	public class Release
	{
		public string Version;
		public bool IsUnstable;
		public DateTime Date;
		public string Notes;
	}
	
	public class UpdateInfo
	{
		UpdateInfo ()
		{
		}
		
		public UpdateInfo (Guid appId, long versionId)
		{
			this.AppId = appId;
			this.VersionId = versionId;
		}
		
		public readonly Guid AppId;
		public readonly long VersionId;
		
		public static UpdateInfo FromFile (string fileName)
		{
			using (var f = File.OpenText (fileName)) {
				var s = f.ReadLine ();
				var parts = s.Split (' ');
				return new UpdateInfo (new Guid (parts[0]), long.Parse (parts[1]));
			}
		}
	}
	
	public class UpdateResult
	{
		public UpdateResult (List<Update> updates, bool includesUnstable, string errorMessage, Exception errorDetail)
		{
			this.Updates = updates;
			this.IncludesUnstable = includesUnstable;
			this.ErrorMessage = errorMessage;
			this.ErrorDetail = errorDetail;
		}
		
		public List<Update> Updates { get; private set; }
		public bool IncludesUnstable { get; private set; }
		public string ErrorMessage { get; private set; }
		public Exception ErrorDetail { get; private set; }
		
		public bool HasError {
			get { return !string.IsNullOrEmpty (ErrorMessage); }
		}
		
		public bool HasUpdates {
			get { return Updates != null && Updates.Count > 0; }
		}
	}
}

