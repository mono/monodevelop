/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Core;

namespace GitSharp.Core.Util
{

    /* Abstraction to support various file system operations not in Java. */
    public static class FS
    {
        /* The implementation selected for this operating system and JRE. */
        //public static FS INSTANCE = new FS_Win32;

        //static {
        //    if (FS_Win32.detect()) {
        //        if (FS_Win32_Cygwin.detect())
        //            INSTANCE = new FS_Win32_Cygwin();
        //        else
        //            INSTANCE = new FS_Win32();
        //    } else if (FS_POSIX_Java6.detect())
        //        INSTANCE = new FS_POSIX_Java6();
        //    else
        //        INSTANCE = new FS_POSIX_Java5();
        //}

        /**
         * Does this operating system and JRE support the execute flag on files?
         * 
         * @return true if this implementation can provide reasonably accurate
         *         executable bit information; false otherwise.
         */
        public static bool supportsExecute()
        {
            return false; // windows does not support executable file flag
        }

        /**
         * Determine if the file is executable (or not).
         * <para />
         * Not all platforms and JREs support executable flags on files. If the
         * feature is unsupported this method will always return false.
         * 
         * @param f
         *            abstract path to test.
         * @return true if the file is believed to be executable by the user.
         */
        public static bool canExecute(FileSystemInfo f)
        {
            return false; // windows does not support executable file flag
        }

        /**
         * Set a file to be executable by the user.
         * <para />
         * Not all platforms and JREs support executable flags on files. If the
         * feature is unsupported this method will always return false and no
         * changes will be made to the file specified.
         * 
         * @param f
         *            path to modify the executable status of.
         * @param canExec
         *            true to enable execution; false to disable it.
         * @return true if the change succeeded; false otherwise.
         */
        public static bool setExecute(FileInfo f, bool canExec)
        {
            return false; // windows does not support executable file flag
        }

        /**
         * Resolve this file to its actual path name that the JRE can use.
         * <para />
         * This method can be relatively expensive. Computing a translation may
         * require forking an external process per path name translated. Callers
         * should try to minimize the number of translations necessary by caching
         * the results.
         * <para />
         * Not all platforms and JREs require path name translation. Currently only
         * Cygwin on Win32 require translation for Cygwin based paths.
         * 
         * @param dir
         *            directory relative to which the path name is.
         * @param name
         *            path name to translate.
         * @return the translated path. <code>new File(dir,name)</code> if this
         *         platform does not require path name translation.
         */
        public static FileSystemInfo resolve(DirectoryInfo dir, string name)
        {
            return resolveImpl(dir, name);
        }

        /**
         * Resolve this file to its actual path name that the JRE can use.
         * <para />
         * This method can be relatively expensive. Computing a translation may
         * require forking an external process per path name translated. Callers
         * should try to minimize the number of translations necessary by caching
         * the results.
         * <para />
         * Not all platforms and JREs require path name translation. Currently only
         * Cygwin on Win32 require translation for Cygwin based paths.
         * 
         * @param dir
         *            directory relative to which the path name is.
         * @param name
         *            path name to translate.
         * @return the translated path. <code>new File(dir,name)</code> if this
         *         platform does not require path name translation.
         */
        private static FileSystemInfo resolveImpl(DirectoryInfo dir, string name)
        {
            string fullname = Path.Combine(dir.FullName, name);
            if (Directory.Exists(fullname))
            {
                return new DirectoryInfo(fullname);
            }
            
            return new FileInfo(fullname);
        }

        /**
         * Determine the user's home directory (location where preferences are).
         * <para />
         * This method can be expensive on the first invocation if path name
         * translation is required. Subsequent invocations return a cached result.
         * <para />
         * Not all platforms and JREs require path name translation. Currently only
         * Cygwin on Win32 requires translation of the Cygwin HOME directory.
         * 
         * @return the user's home directory; null if the user does not have one.
         */
        public static DirectoryInfo userHome()
        {
            return USER_HOME.home;
        }

        private static class USER_HOME
        {
            public static DirectoryInfo home = userHomeImpl();
        }

        /**
         * Determine the user's home directory (location where preferences are).
         * 
         * @return the user's home directory; null if the user does not have one.
         */
        private static DirectoryInfo userHomeImpl()
        {
            string userHomeFolderPath;

            var platform = (int)Environment.OSVersion.Platform;

            if (platform == (int)PlatformID.Unix || platform  == 6 /* (int)PlatformID.MacOSX */
                || platform == (int)PlatformType.UnixMono)
            {
                userHomeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }
            else
            {
                userHomeFolderPath = Environment.GetEnvironmentVariable("USERPROFILE");
            }

            return new DirectoryInfo(userHomeFolderPath);
        }

        /**
         * Determine the global application directory (location where preferences are).
         * Also known as the "all users" directory.
         * <para />
         * This method can be expensive on the first invocation if path name
         * translation is required. Subsequent invocations return a cached result.
         * <para />
         * 
         * @return the user's home directory; null if the user does not have one.
         */
        public static DirectoryInfo globalHome()
        {
            return GLOBAL_HOME.home;
        }

        private static class GLOBAL_HOME
        {
            public static DirectoryInfo home = globalHomeImpl();
        }
        
        /// <summary>
        /// Returns the global (user-specific) path for application settings based on OS
        /// </summary>
        /// <returns>Value of the global path</returns>
        public static DirectoryInfo globalHomeImpl()
        {
            string path = string.Empty;
            PlatformType ptype = SystemReader.getInstance().getOperatingSystem();
            
            switch (ptype)
            {
                case PlatformType.Windows:
                    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    break;
                case PlatformType.Unix:
                    path = "~/";
                    break;
                case PlatformType.MacOSX:
                    path = "~/";
                    break;
                case PlatformType.Xbox:
                default:
                    throw new ArgumentException("GlobalHomeImpl support for '" + Environment.OSVersion.VersionString + " ' is not implemented.");
            }

            return new DirectoryInfo(path);
        }

        /**
         * Determine the system-wide application directory (location where preferences are).
         * Also known as the "all users" directory.
         * <para />
         * This method can be expensive on the first invocation if path name
         * translation is required. Subsequent invocations return a cached result.
         * <para />
         * 
         * @return the user's home directory; null if the user does not have one.
         */
        public static DirectoryInfo systemHome()
        {
            return SYSTEM_HOME.home;
        }

        private static class SYSTEM_HOME
        {
            public static DirectoryInfo home = systemHomeImpl();
        }
        
        /// <summary>
        /// Returns the system-wide path for application settings based on OS
        /// </summary>
        /// <returns></returns>
        public static DirectoryInfo systemHomeImpl()
        {
            string path = string.Empty;
            PlatformType ptype = SystemReader.getInstance().getOperatingSystem();

            switch (ptype)
            {
                case PlatformType.Windows:
            		path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GitSharp");
                    break;
                case PlatformType.Unix:
                    path = "~(prefix)/etc";
                    break;
                case PlatformType.MacOSX:
                    path = "~(prefix)/etc";
                    break;
                case PlatformType.Xbox:
                default:
                    throw new ArgumentException("SystemHomeImpl support for '" + Environment.OSVersion.VersionString + " ' is not implemented.");
            }

            return new DirectoryInfo(path);
        }
    }

}
