/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 * Copyrigth (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Diagnostics;
using System.IO;

namespace GitSharp.Core
{
	public class Mac : Platform
	{
		public override bool IsSymlinkSupported
		{
			get { return true; }
		}

		public override bool IsHardlinkSupported
		{
			get { return true; }
		}

		public override bool CreateSymlink(string symlinkFilename, string existingFilename, bool isSymlinkDirectory)
		{
			ProcessStartInfo info = new ProcessStartInfo();
			info.FileName = "ln";
			info.Arguments = (isSymlinkDirectory ? "-d " : "") +"-s " + existingFilename+" "+symlinkFilename;
			info.UseShellExecute = false;
			info.RedirectStandardOutput = true;
			
			try {
					Process.Start(info);
				} 
				catch (Exception) 
				{
					return false; 
				}

			return true;
		}

		public override bool CreateHardlink(string hardlinkFilename, string existingFilename)
		{
			ProcessStartInfo info = new ProcessStartInfo();
			info.FileName = "ln";
			info.Arguments = existingFilename+" "+hardlinkFilename;
			info.UseShellExecute = false;
			info.RedirectStandardOutput = true;
			
			try {
					Process.Start(info);
				} 
				catch (Exception) 
				{
					return false; 
				}

			return true;
		}

		public override Process GetTextPager(string corePagerConfig)
		{
			// TODO: instantiate "more" or "less"
			return null;
		}
		
		public Mac()
		{
			//Version list available at http://fedoraproject.org/wiki/Releases
			//Unique version variations for parsing include:
			//		Darwin 9.8.0 Power Macintosh
			
			ProcessStartInfo info = new ProcessStartInfo();
			info.FileName = "uname";
			info.Arguments = "-mrs";
			info.UseShellExecute = false;
			info.RedirectStandardOutput = true;
			
			using (Process process = Process.Start(info))
			{
				using (StreamReader reader = process.StandardOutput)
				{
					string result = reader.ReadToEnd();
					
					int pt = result.IndexOf(" ");
					int pt2 = result.IndexOf(" ",pt+1);
					int pt3 = pt2+1; 
					
					ClassName = "Macintosh.Macosx";
					PlatformSubType = "";
					Version = result.Substring(pt2, pt3).Trim();
					Edition = result.Substring(0,pt).Trim();
				}
			}
		}
	}
}
