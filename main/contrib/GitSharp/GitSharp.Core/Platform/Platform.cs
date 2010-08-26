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
using System.Diagnostics;

namespace GitSharp.Core
{
	/// <summary>
	/// Base class for a singleton object that provides capabilities that
	/// require a different implementation per platform. 
	/// </summary>
	public abstract class Platform
	{
		/// <summary>
		/// Extension of System.PlatformID to add plaforms. 
		/// </summary>
		enum GitPlatformID
		{
			Win32S = PlatformID.Win32S,
			Win32Windows = PlatformID.Win32Windows,
			Win32NT = PlatformID.Win32NT,
			WinCE = PlatformID.WinCE,
			Unix = PlatformID.Unix,
			Xbox,
			MacOSX,
		}

		/// <summary>
		/// Enumeration of the known concrete implementation families. 
		/// </summary>
		public enum PlatformId
		{
			Windows = 1,
			Linux = 2,
			Mac = 3
		}

		/// <summary>
		/// Access to the singleton object.  Will create the object on first get. 
		/// </summary>
		public static Platform Instance
		{
			get
			{
				if (_instance == null)
				{
					System.OperatingSystem os = Environment.OSVersion;
					GitPlatformID pid = (GitPlatformID)os.Platform;
		
					switch (pid)
					{
						case GitPlatformID.Unix:
							_instance = new Linux();
							break;
						case GitPlatformID.MacOSX:
							_instance = new Mac();
							break;
						case GitPlatformID.Win32NT:
						case GitPlatformID.Win32S:
						case GitPlatformID.Win32Windows:
						case GitPlatformID.WinCE:
							_instance = new Win32();
							break;
						default:
							throw new NotSupportedException("Platform could not be detected!");
					}
				}

				return _instance;
			}
		}

		public abstract bool IsHardlinkSupported { get; }

		public abstract bool IsSymlinkSupported { get; }

		public abstract bool CreateSymlink(string symlinkFilename, string existingFilename, bool isSymlinkDirectory);

		public abstract bool CreateHardlink(string hardlinkFilename, string exisitingFilename);


		public abstract Process GetTextPager(string corePagerConfig);

		protected Platform()
		{
		}

		public string ClassName { get; protected set; }

		public PlatformId Id { get; protected set; }

		public string PlatformType { get; protected set; }

		public string PlatformSubType { get; protected set; }

		public string Edition { get; protected set; }

		public string Version { get; protected set; }

		public string VersionFile { get; protected set; }

		public string ProductName
		{
			get
			{
				return PlatformType + " " + PlatformSubType + " " + Edition + "(" + Version + ")";
			}
		}

		private static Platform _instance;
	}
}