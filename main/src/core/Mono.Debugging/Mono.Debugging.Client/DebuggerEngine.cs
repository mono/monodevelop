using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Mono.Debugging.Backend;
using Mono.Addins;

namespace Mono.Debugging.Client
{
	public static class DebuggerEngine
	{
		const string FactoriesPath = "/Mono/Debugging/DebuggerFactories";
		
		public static bool CanDebugPlatform (string platform)
		{
			return GetFactoryForPlatform (platform) != null;
		}
		
		public static bool CanDebugFile (string file)
		{
			return GetFactoryForFile (file) != null;
		}
		
		public static DebuggerSession CreateDebugSessionForPlatform (string platform)
		{
			IDebuggerSessionFactory factory = GetFactoryForPlatform (platform);
			if (factory != null) {
				DebuggerSession ds = factory.CreateSession ();
				ds.Initialize ();
				return ds;
			} else
				throw new InvalidOperationException ("Unsupported platform: " + platform);
		}
		
		public static DebuggerSession CreateDebugSessionForFile (string file)
		{
			IDebuggerSessionFactory factory = GetFactoryForFile (file);
			if (factory != null) {
				DebuggerSession ds = factory.CreateSession ();
				ds.Initialize ();
				return ds;
			} else
				throw new InvalidOperationException ("Unsupported file: " + file);
		}
		
		static IDebuggerSessionFactory GetFactoryForPlatform (string platform)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath)) {
				IDebuggerSessionFactory factory = (IDebuggerSessionFactory) node.GetInstance ();
				if (factory.CanDebugPlatform (platform))
					return factory;
			}
			return null;
		}
		
		static IDebuggerSessionFactory GetFactoryForFile (string file)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (FactoriesPath)) {
				IDebuggerSessionFactory factory = (IDebuggerSessionFactory) node.GetInstance ();
				if (factory.CanDebugFile (file))
					return factory;
			}
			return null;
		}
	}
	
}
