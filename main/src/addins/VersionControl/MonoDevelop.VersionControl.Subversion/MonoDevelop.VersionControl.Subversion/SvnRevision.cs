// 
// ISvnClient.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion
{
	public class SvnRevision : Revision
	{
		public readonly int Rev;
		public readonly int Kind;
		
		public SvnRevision (Repository repo, int rev): base (repo) 
		{
			Rev = rev;
			Kind = 1;
		}
		
		SvnRevision (int kind): base (null) 
		{
			Kind = kind;
		}
		
		public SvnRevision (Repository repo, int rev, DateTime time, string author, string message, RevisionPath[] changedFiles)
			: base (repo, time, author, message, changedFiles)
		{
			Rev = rev;
			Kind = 1;
		}
		
		public override string ToString()
		{
			return Rev.ToString();
		}
		
		public override Revision GetPrevious()
		{
			return new SvnRevision (Repository, Rev-1);
		}
		
		public readonly static SvnRevision Blank = new SvnRevision(0);
		public readonly static SvnRevision First = new SvnRevision(1);
		public readonly static SvnRevision Committed = new SvnRevision(3);
		public readonly static SvnRevision Previous = new SvnRevision(4);
		public readonly static SvnRevision Base = new SvnRevision(5);
		public readonly static SvnRevision Working = new SvnRevision(6);
		public readonly static SvnRevision Head = new SvnRevision(7);
	}

	public class CertficateInfo
	{
		public string HostName { get; set; }
		public string Fingerprint { get; set; }
		public string ValidFrom { get; set; }
		public string ValidUntil { get; set; }
		public string IssuerName { get; set; }
		public string AsciiCert { get; set; }
	}

	[Flags]
	public enum SslFailure: uint
	{
		None = 0,
		// Certificate is not yet valid.
		NotYetValid = 0x00000001,
		// Certificate has expired.
		Expired = 0x00000002,
		// Certificate's CN (hostname) does not match the remote hostname.
		CNMismatch = 0x00000004,
		// Certificate authority is unknown (i.e. not trusted)
		UnknownCA = 0x00000008,
		// Other failure. This can happen if neon has introduced a new failure bit that we do not handle yet. */
		Other = 0x40000000
	}
	
	public class DirectoryEntry
	{
		public string Name { get; set; }
		public bool IsDirectory { get; set; }
		public long Size { get; set; }
		public bool HasProps { get; set; }
		public int CreatedRevision { get; set; }
		public DateTime Time { get; set; }
		public string LastAuthor { get; set; }
	}

	
}
