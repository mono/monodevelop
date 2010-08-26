using System;
using System.IO;
using System.Net;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
	[Serializable]
    public enum PlatformType
    {
        
        Windows = 1,
        Unix = 4,
        Xbox = 5,
        MacOSX = 6,
        Suse = 20,
        Ubuntu = 21,
        RedHat = 22,
        Unknown = 127,
        UnixMono = 128
    }

	[Serializable]
    public enum ConfigFileType
    {

        System = 1,
        Global = 2,
        Repo = 3
    }

    public abstract class SystemReader
    {
        
        private static SystemReader _instance = new InternalSystemReader();

        /** @return the live instance to read system properties. */
        public static SystemReader getInstance()
        {
            return _instance;
        }

        /**
	     * @param newReader
	     *            the new instance to use when accessing properties.
	     */
        public static void setInstance(SystemReader newReader)
        {
            _instance = newReader;
        }

        /**
	     * Gets the hostname of the local host. If no hostname can be found, the
	     * hostname is set to the default value "localhost".
	     *
	     * @return the canonical hostname
	     */
        public abstract string getHostname();

        /**
         * @param variable system variable to read
         * @return value of the system variable
         */
        public abstract string getenv(string variable);
        
        /**
	     * @param key of the system property to read
    	 * @return value of the system property
	     */
        public abstract string getProperty(string key);

        /**
    	 * @return the git configuration found in the user home
    	 */
        public abstract FileBasedConfig openUserConfig();

        /**
        * @return the current system time
        */
        public abstract long getCurrentTime();

        /**
        * @param when TODO
        * @return the local time zone
        */
        public abstract int getTimezone(long when);

        /// <summary>
        /// Returns Windows, Linux or Mac for identification of the OS in use
        /// </summary>
        /// <returns>Operating System name</returns>
        public abstract PlatformType getOperatingSystem();

        /// <summary>
        /// Returns the GitSharp configuration file from the OS-dependant location.
        /// </summary>
        /// <returns></returns>
        public abstract FileBasedConfig getConfigFile(ConfigFileType configType);
        
        /// <summary>
        /// Returns the GitSharp configuration file based on a user-specified location.
        /// </summary>
        /// <returns></returns>
        public abstract FileBasedConfig getConfigFile(string fileLocation);

        private class InternalSystemReader : SystemReader
        {
            public override string getenv(string variable)
            {
                return Environment.GetEnvironmentVariable(variable);
            }
            public override string getProperty(string key)
            {
                //[java] return  System.getProperty(key);
                string result = string.Empty;

                switch (key)
                {
                    case Constants.OS_USER_NAME_KEY:
                        using (System.Security.Principal.WindowsIdentity ident = System.Security.Principal.WindowsIdentity.GetCurrent())
					    {
						    result = ident.Name;
                            int index = result.LastIndexOf('\\');
                            if (result.Length >= index+1)
                                result = result.Substring(index+1);
                            break;
				        }
                    default:
                        throw new NotImplementedException("The " + key + " property has not been implemented. This was a Java feature.");        
                }
                
                return result;
            }

            public override FileBasedConfig openUserConfig()
            {
                DirectoryInfo home = FS.userHome();
                return new FileBasedConfig(new FileInfo(Path.Combine(home.FullName, ".gitconfig")));
            }

            public override string getHostname()
            {
                return Dns.GetHostName();
            }

            public override long getCurrentTime()
            {
                return DateTime.UtcNow.ToMillisecondsSinceEpoch();
            }

            public override int getTimezone(long when)
            {
                return (int)TimeZone.CurrentTimeZone.GetUtcOffset(when.MillisToUtcDateTime()).TotalMinutes;
            }

            public override PlatformType getOperatingSystem()
            {
                OperatingSystem os = Environment.OSVersion;
                PlatformID pid = os.Platform;
                PlatformType pType = PlatformType.Unknown;

                switch (pid)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                        pType = PlatformType.Windows;
                        break;
                    case PlatformID.Unix:
                        pType = PlatformType.Unix;
                        break;
                    //case PlatformID.MacOSX:
                    //    pType = PlatformType.MacOSX;
                    //    break;
                    //case PlatformID.Xbox:
                    //    pType = PlatformType.Xbox;
                    //    break;
                    default:
                        // Mono used 128 as its internal Unix identifier before it was added by MS.
                        if ((int)pid == (int)PlatformType.UnixMono)
                        {
                            pType = PlatformType.Unix;
                            break;
                        }
                        throw new ArgumentException("OS support for '" + os.VersionString + " ' is not implemented.");
                }

                //Testing should be added here to identify the flavor of *nix in use.
                //This is primarily useful because *nix does not have a standardized install location
                //or function call to identify "special folders".
                if (pType == PlatformType.Unix)
                {
                }

                return pType;
            }

            public override FileBasedConfig getConfigFile(ConfigFileType configType)
            {
            	string filename = string.Empty;
            	
               	switch (configType)
            	{
            		case ConfigFileType.Global:
               			filename = Path.Combine(FS.globalHome().FullName, ".gitconfig");
            			break;
	           		case ConfigFileType.System:
            			filename = Path.Combine(FS.systemHome().FullName, "gitconfig");
           				break;	
            		case ConfigFileType.Repo:
           				filename = Path.Combine(FS.userHome().FullName, Constants.DOT_GIT);
           				filename = Path.Combine(filename, "config");
            			break;
            		default:
            			throw new ArgumentException("getConfigFile used unknown Config filetype.");
                }
               	
               	FileInfo info = new FileInfo(filename);
               	if (info != null)
            		return (new FileBasedConfig(info));
               	else
               		throw new FileNotFoundException();
            }
            
            public override FileBasedConfig getConfigFile(string fileLocation)
            {
            	FileInfo info = new FileInfo(fileLocation);
            	if (info != null)
            		return (new FileBasedConfig(info));
               	else
               		throw new FileNotFoundException();
            }
        }

    }
}