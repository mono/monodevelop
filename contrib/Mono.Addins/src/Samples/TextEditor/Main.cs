using System;
using Gtk;
using Mono.Addins;

namespace TextEditor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			
			AddinManager.AddinLoadError += OnLoadError;
			AddinManager.AddinLoaded += OnLoad;
			AddinManager.AddinUnloaded += OnUnload;
			
			AddinManager.Initialize ();
			AddinManager.Registry.Update (null);
			AddinManager.ExtensionChanged += OnExtensionChange;
			
			
			MainWindow win = new MainWindow ();
			
			foreach (ICommand cmd in AddinManager.GetExtensionObjects ("/TextEditor/StartupCommands"))
				cmd.Run ();
			
			win.Show ();
			Application.Run ();
		}
		
		static void OnLoadError (object s, AddinErrorEventArgs args)
		{
			Console.WriteLine ("Add-in error: " + args.Message);
			Console.WriteLine (args.AddinId);
			Console.WriteLine (args.Exception);
		}
		
		static void OnLoad (object s, AddinEventArgs args)
		{
			Console.WriteLine ("Add-in loaded: " + args.AddinId);
		}
		
		static void OnUnload (object s, AddinEventArgs args)
		{
			Console.WriteLine ("Add-in unloaded: " + args.AddinId);
		}
		
		static void OnExtensionChange (object s, ExtensionEventArgs args)
		{
			Console.WriteLine ("Extension changed: " + args.Path);
		}
	}
}