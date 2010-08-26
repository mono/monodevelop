/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 * Copyrigth (C) 2010, Henon <meinrad.recheis@gmail.com>
 * Copyrigth (C) 2010, Andrew Cooper <andymancooper@gmail.com>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace GitSharp.Core
{

	public class Linux : Platform
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
			//Execute command
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
            var pager = new Process ();
            var pagerVar = System.Environment.GetEnvironmentVariable ("GIT_PAGER");
            if (pagerVar == null)
                pagerVar = corePagerConfig;
            if ((pagerVar == null) || (pagerVar.Length == 0))
                pagerVar = "less";
            var tokens = pagerVar.Split();
            pager.StartInfo.FileName = tokens[0];
            pager.StartInfo.UseShellExecute = false;
            pager.StartInfo.RedirectStandardInput = true;
            if (tokens.Length > 1)
                pager.StartInfo.Arguments = pagerVar.Substring(tokens[0].Length);

            // Apply LESS environment behavior
            var lessVar = System.Environment.GetEnvironmentVariable ("LESS");
            if (lessVar == null)
                lessVar = "FRSX";
            pager.StartInfo.EnvironmentVariables["LESS"] = lessVar;

            return pager;
        }

		public Linux()
		{
			System.IO.DirectoryInfo di = new System.IO.DirectoryInfo("/etc");
			System.IO.FileInfo[] release = di.GetFiles("*-release");
			System.IO.FileInfo[] debian = di.GetFiles("debian_version");
			System.IO.FileInfo[] slackware = di.GetFiles("slackware-version");
			
			ClassName = null;
			PlatformType = "Linux";
			PlatformSubType = "";
			Edition = "";
			Version = "";
			VersionFile  = "";
			
			if (release.Length > 0)
			{
				string str = release[0].ToString();
				string platformType = str.Substring(5,str.Length-13);
				VersionFile = str;
				
				switch (platformType)
				{
					case "arch":
						GetArchPlatform( this, null);
						break;
					case "fedora":
						GetFedoraPlatform( this, null);
						break;
					case "gentoo":
						GetGentooPlatform( this, null);
						break;
					case "mandriva":
						GetMandrivaPlatform( this, null);
						break;
					case "redhat": 	//RedHat variants
						GetRedHatPlatform( this, null);
						break;
					case "suse":
						GetSusePlatform( this, null);
						break;
					case "lsb": 	//Ubuntu variants
						GetUbuntuPlatform( this, null);
						break;
					default:
						GetDefaultLinuxPlatform( this, null);
						break;
				}
			}
			else if (slackware.Length > 0)
			{
				VersionFile = "/etc/" + slackware[0].ToString();
				GetSlackwarePlatform( this, null);
			}
			else if (debian.Length > 0)
			{
				VersionFile = "/etc/" + debian[0].ToString();
				GetDebianPlatform( this, null);
			}
			else
			{
				GetDefaultLinuxPlatform( this, null);
			}
			
			if (ClassName == null)
				throw new ArgumentNullException("ClassName was not defined. Please report this bug.");
			
		}
		
		public static void GetArchPlatform(Linux obj, string unitTestContent)
		{
			// Arch is not versioned. It is a single rolling version.
			// The existance of the arch-release file determines arch
			// is being used. The actual file is blank.
			obj.ClassName = "Linux.Arch";
			obj.PlatformSubType = "Arch";
			obj.Edition = "";
			obj.Version = "Current";
		}
		
		public static void GetDefaultLinuxPlatform(Linux obj, string unitTestContent)
		{
			obj.ClassName = "Linux.Default";
			obj.PlatformSubType = "Generic";
			obj.Edition = "";
			obj.Version = "";
		}
		
		public static void GetDebianPlatform(Linux obj, string unitTestContent)
		{
			//Version list available at: http://www.debian.org/releases/
			//There is no accurate way to determine the version information
			//in Debian. The community mixes the versions regularly and 
			//argues that programs should not rely on this information, 
			//instead using the dependencies to determine if a program will
			//work. Unfortunately, this viewpoint does little for generic
			//informational purposes, version based bug reporting, etc. They
			//have not standardized this process, even for the 
			//lsb_release package. The information provided in 
			// /etc/debian_version is often incorrect and should not be used.
			obj.ClassName = "Linux.Debian";
			obj.PlatformSubType = "Debian";
			obj.Edition = "";
			obj.Version = "";
		}
		
		public static void GetFedoraPlatform(Linux obj, string unitTestContent)
		{
			//Version list available at http://fedoraproject.org/wiki/Releases
			//Unique version variations for parsing include:
			//		Fedora release 8 (Werewolf)
			//		Fedora Core release 6 (Zod)

			List<string> lines;
			if (unitTestContent != null)
				lines = ParseString(unitTestContent);
			else
				lines = ParseFile("/etc/" + obj.VersionFile);

			int pt = lines[0].IndexOf("release");
			int pt2 = lines[0].IndexOf("(");
			int pt3 = lines[0].IndexOf(")");
			
			obj.ClassName = "Linux.Fedora";
			obj.PlatformSubType = lines[0].Substring(0,pt-1).Trim();
			obj.Version = lines[0].Substring(pt+8, pt2-1).Trim();
			obj.Edition = lines[0].Substring(pt2+1,pt3-1).Trim();
			
		}
		
		public static void GetGentooPlatform(Linux obj, string unitTestContent)
		{
			//Version list available at http://gentest.neysx.org/proj/en/releng/#doc_chap6
			//Versioning is primarily based on date.
			//Unique version variations for parsing include:
			//		Gentoo Base System release 1.12.11.1

			List<string> lines;
			if (unitTestContent != null)
				lines = ParseString(unitTestContent);
			else
				lines = ParseFile(obj.VersionFile);
			
			obj.ClassName = "Linux.Gentoo";
			obj.PlatformSubType = "Gentoo";
			obj.Edition = "";
			obj.Version = lines[0].Split().Last();
		}
		
		public static void GetMandrivaPlatform(Linux obj, string unitTestContent)
		{
			//Formerly known as Mandrake Linux
			//Version list is available at http://en.wikipedia.org/wiki/Mandriva_Linux
			//Unique version variations for parsing include:
			//		Mandriva Linux release 2010.0 (Official) for i586
			
			List<string> lines;
			if (unitTestContent != null)
				lines = ParseString(unitTestContent);
			else
				lines = ParseFile("/etc/" + obj.VersionFile);
			
			obj.ClassName = "Linux.Mandriva";
			obj.PlatformSubType = "Mandriva";
			obj.Edition = "";
			int pt = lines[0].IndexOf("release");
			int pt2 = lines[0].IndexOf("(");
			obj.Version = lines[0].Substring(pt+8, pt2-1).Trim();
			
			switch (obj.Version)
			{
				case "2010.0":
					obj.Edition = "Mandriva Linux 2010";
					break;
				case "2009.1":
					obj.Edition = "Mandriva Linux 2009 Spring";
					break;
				case "2009.0":
					obj.Edition = "Mandriva Linux 2009";
					break;
				case "2008.1":
					obj.Edition = "Mandriva Linux 2008 Spring";
					break;
				case "2008.0":
					obj.Edition = "Mandriva Linux 2008";
					break;
				case "2007.1":
					obj.Edition = "Mandriva Linux 2007 Spring";
					break;
				case "2007":
					obj.Edition = "Mandriva Linux 2007";
					break;
				case "2006.0":
					obj.Edition = "Mandriva Linux 2006";
					break;
				case "10.2":
					obj.Edition = "Limited Edition 2005";
					break;
				case "10.1":
					obj.Edition = "Community and Official";
					break;
				case "10.0":
					obj.Edition = "Community and Official";
					break;
				case "9.2":
					obj.Edition = "FiveStar";
					break;
				case "9.1":
					obj.Edition = "Bamboo";
					break;
				case "9.0":
					obj.Edition = "Dolphin";
					break;
				case "8.2":
					obj.Edition = "Bluebird";
					break;
				case "8.1":
					obj.Edition = "Vitamin";
					break;
				case "8.0":
					obj.Edition = "Traktopel";
					break;
				case "7.2":
					obj.Edition = "Odyssey";
					break;
				case "7.1":
					obj.Edition = "Helium";
					break;
				case "7.0":
					obj.Edition = "Air";
					break;
				case "6.1":
					obj.Edition = "Helios";
					break;
				case "6.0":
					obj.Edition = "Venus";
					break;
				case "5.3":
					obj.Edition = "Festen";
					break;
				case "5.2":
					obj.Edition = "Leeloo";
					break;
				case "5.1":
					obj.Edition = "Venice";
					break;
				default:
					obj.Edition = "Unknown";
					break;
			}
		}
		
		public static void GetRedHatPlatform(Linux obj, string unitTestContent)
		{
			//Version list is available at ...
			//Unique version variations for parsing include:
			//		Red Hat Enterprise Linux Server release 5 (Tikanga)
			//		Red Hat Enterprise Linux AS release 4 (Nahant Update 3)
			//		Red Hat Advanced Server Linux AS release 2.1 (Pensacola)
			//		Red Hat Enterprise Linux ES release 3 (Taroon Update 4)

			List<string> lines;
			if (unitTestContent != null)
				lines = ParseString(unitTestContent);
			else
				lines = ParseFile("/etc/" + obj.VersionFile);
			
			int pt = lines[0].IndexOf("release");
			int pt2 = lines[0].IndexOf("(");
			int pt3 = lines[0].IndexOf(")");
			int pt4 = lines[0].IndexOf("Linux");
			
			obj.ClassName = "Linux.RedHat";
			obj.PlatformSubType = lines[0].Substring(0, pt4-1);
			obj.Edition = lines[0].Substring(pt2+1, pt3-1-pt2);
			obj.Version = lines[0].Substring(pt+8, pt2 - 1 - pt + 8).Trim();
			
		}
		
		public static void GetSlackwarePlatform(Linux obj, string unitTestContent)
		{
			//Version list is available at ...
			//Unique version variations for parsing include:
			//		Slackware 13.0.0.0.0

			List<string> lines;
			if (unitTestContent != null)
				lines = ParseString(unitTestContent);
			else
				lines = ParseFile("/etc/" + obj.VersionFile);
			
			obj.ClassName = "Linux.Slackware";
			obj.PlatformSubType = "Slackware";
			obj.Edition = "";
			int pt = lines[0].IndexOf(" ");
			obj.Version = lines[0].Substring(pt+1, lines[0].Length - 1);
		}

		public static void GetSusePlatform(Linux obj)
		{
			GetSusePlatform( obj, null);
		}
		
		public static void GetSusePlatform(Linux obj, string unitTestContent)
		{
			//Version list is available at http://en.wikipedia.org/wiki/SUSE_Linux
			//Unique version variations for parsing include (multi-line):
			//		SUSE LINUX 10.0 (X86-64) OSS
			//		VERSION = 10.0

			List<string> lines;
			if (unitTestContent != null)
				lines = ParseString(unitTestContent);
			else
				lines = ParseFile("/etc/" + obj.VersionFile);
			
			int pt = lines[1].IndexOf(" ");
			obj.Version = lines[0].Substring(pt+1, lines[0].Length - 1);
			obj.ClassName = "Linux.Suse";
			obj.PlatformSubType = "Suse";
			obj.Edition = "";
			obj.Version = lines[1].Substring(11, lines[1].Length - 11);
		}
		
		public static void GetUbuntuPlatform(Linux obj)
		{
			GetUbuntuPlatform( obj, null);
		}
		
		public static void GetUbuntuPlatform(Linux obj, string unitTestContent)
		{
			//Version list is available at http://en.wikipedia.org/wiki/Ubuntu_(Linux_distribution)
			//Unique version variations for parsing include (multi-line):
			//		DISTRIB_ID=Ubuntu
			//		DISTRIB_RELEASE = 9.04
			//		DISTRIB_CODENAME=jaunty
			//		DISTRIB_DESCRIPTION = Ubuntu 9.04
			//Because Ubuntu can identify variants, we'll use the DISTRIB_ID 
			//to identify the variant (such as KUbuntu, XUbuntu) instead of 
			//a static string setting.

			List<string> lines;
			if (unitTestContent == null) 
				lines = ParseFile("/etc/" + obj.VersionFile);
			else
				lines = ParseString(unitTestContent);
			
			obj.ClassName = "Linux.Ubuntu";
			int pt = lines[0].IndexOf("=");
			obj.PlatformSubType = lines[0].Substring(pt+1).Trim();
			int pt1 = lines[2].IndexOf("=");
			obj.Edition = lines[2].Substring(pt1+1).Trim();
			int pt2 = lines[1].IndexOf("=");
			obj.Version = lines[1].Substring(pt2+1).Trim();
		}
		
		private static List<string> ParseFile(string f)
		{
			List<string> lines = new List<string>();

			using (StreamReader r = new StreamReader(f))
			{

				string line;
            	while ((line = r.ReadLine()) != null)
            	{
            		lines.Add(line.Trim());
    	        }
        	}
			
			return lines;
		}
		
		private static List<string> ParseString(string str)
		{
			string[] stringArray = str.Split('\n');
			return new List<string>(stringArray);
		}
	}
}