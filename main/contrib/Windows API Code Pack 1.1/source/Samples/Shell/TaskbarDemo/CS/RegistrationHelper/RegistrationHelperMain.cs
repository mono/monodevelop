//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace TaskbarDemo
{
    class RegistrationHelperMain
    {
        static void Main(string[] args)
        {
            if (args.Length < 6)
            {
                Console.WriteLine("Usage: <ProgId> <Register in HKCU: true|false> <AppId> <OpenWithSwitch> <Unregister: true|false> <Ext1> [Ext2 [Ext3] ...]");
                Console.ReadLine();
                return;
            }
            try
            {

                string progId = args[0];
                bool registerInHKCU = bool.Parse(args[1]);
                string appId = args[2];
                string openWith = args[3];
                bool unregister = bool.Parse(args[4]);

                string[] associationsToRegister = args.Skip(5).ToArray();

                if (registerInHKCU)
                    classesRoot = Registry.CurrentUser.OpenSubKey(@"Software\Classes");
                else
                    classesRoot = Registry.ClassesRoot;

                //First of all, unregister:
                Array.ForEach(associationsToRegister,
                    assoc => UnregisterFileAssociation(progId, assoc));
                UnregisterProgId(progId);

                if (!unregister)
                {
                    RegisterProgId(progId, appId, openWith);
                    Array.ForEach(associationsToRegister,
                        assoc => RegisterFileAssociation(progId, assoc));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }

        static RegistryKey classesRoot;

        private static void RegisterProgId(string progId, string appId,
            string openWith)
        {
            RegistryKey progIdKey = classesRoot.CreateSubKey(progId);
            progIdKey.SetValue("FriendlyTypeName", "@shell32.dll,-8975");
            progIdKey.SetValue("DefaultIcon", "@shell32.dll,-47");
            progIdKey.SetValue("CurVer", progId);
            progIdKey.SetValue("AppUserModelID", appId);
            RegistryKey shell = progIdKey.CreateSubKey("shell");
            shell.SetValue(String.Empty, "Open");
            shell = shell.CreateSubKey("Open");
            shell = shell.CreateSubKey("Command");
            shell.SetValue(String.Empty, openWith);
            
            shell.Close();
            progIdKey.Close();
        }
        private static void UnregisterProgId(string progId)
        {
            try
            {
                classesRoot.DeleteSubKeyTree(progId);
            }
            catch { }
        }
        private static void RegisterFileAssociation(string progId, string extension)
        {

            RegistryKey openWithKey = classesRoot.CreateSubKey(
                Path.Combine(extension, "OpenWithProgIds"));
            openWithKey.SetValue(progId, String.Empty);
            openWithKey.Close();
        }
        private static void UnregisterFileAssociation(string progId, string extension)
        {
            try
            {
                RegistryKey openWithKey = classesRoot.CreateSubKey(
                    Path.Combine(extension, "OpenWithProgIds"));
                openWithKey.DeleteValue(progId);
                openWithKey.Close();
            }
            catch(Exception e)
            {
                Debug.WriteLine("Error while unregistering file association: " + e.Message);
            }
        }
    }
}
