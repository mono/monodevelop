// 
// MonoMacPackagingTool.cs
//  
// Author:
//       David Siegel <djsiegel@gmail.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Mono.Options;

using MonoDevelop.Core;
using MonoDevelop.MacDev;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;

using MonoDevelop.MonoMac.Gui;

namespace MonoDevelop.MonoMac
{
    public class MonoMacPackagingTool : IApplication
    {
        MonoMacPackagingSettings GetDefaultSettings ()
        {
            return new MonoMacPackagingSettings {
                IncludeMono   = true,
                LinkerMode    = MonoMacLinkerMode.LinkAll,
                SignBundle    = false,
                SignPackage   = false,
                CreatePackage = false
            };
        }
        
        public int Run (string [] arguments)
        {
            bool showHelp = false;
            string configName = "Release";
            var settings = GetDefaultSettings ();
            var linkerModes = string.Join (", ", Enum.GetNames (typeof (MonoMacLinkerMode)));
            
            var options = new OptionSet {
                { "i|include-mono", "Include Mono in the bundle.", v => {
                    settings.IncludeMono = v != null;
                }},
                { "k|create-package", "Create bundle package (installer).", v => {
                    settings.CreatePackage = v != null;
                }},
                { "l|linker-mode=", "Linker mode ("+linkerModes+").", v => {
                    MonoMacLinkerMode mode;
                    if (Enum.TryParse<MonoMacLinkerMode> (v, out mode))
                        settings.LinkerMode = mode;
                }},
                { "b|sign-bundle=", "Sign bundle with specified key.", v => {
                    settings.SignBundle = v != null;
                    settings.BundleSigningKey = v;
                }},
                { "p|sign-package=", "Sign package with specified key.", v => {
                    settings.SignPackage = v != null;
                    settings.PackageSigningKey = v;
                }},
                { "c|configuration=", "Project configuration to bundle (Release).", v => {
                    if (v != null)
                        configName = v;
                }},
                { "h|help", "Show bundle tool help.", v => {
                    showHelp = v != null;
                }}
            };
            
            try {
                options.Parse (arguments);
            } catch (OptionException e) {
                Console.WriteLine ("bundle: {0}", e.Message);
                Console.WriteLine ("Try `bundle --help' for more information.");
                return 1;
            }
            
            if (showHelp) {
                ShowHelp (options);
                return 0;
            }
                
            var monitor = new ConsoleProgressMonitor ();
            var project = FindMonoMacProject (monitor);
            
            if (project == null) {
                Console.WriteLine (GettextCatalog.GetString ("Error: Could not find a MonoMac project to bundle."));
                return 1;
            }
            
            var config = project.Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == configName);
            
            if (config == null) {
                Console.WriteLine (GettextCatalog.GetString ("Error: Could not find configuration: ") + configName);
                return 1;
            }
            
            MonoMacPackaging.BuildPackage (monitor, project, config.Selector, settings, project.Name + ".app");
    
            return 0;
        }
        
        void ShowHelp (OptionSet options)
        {
            Console.WriteLine ("Usage: bundle [options]");
            Console.WriteLine ("Builds an application bundle from the MonoMac project under the current directory.");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            options.WriteOptionDescriptions (Console.Out);
        }
        
        MonoMacProject FindMonoMacProject (IProgressMonitor monitor)
        {
            var projects =
                from solutionFile in Directory.GetFiles (".", "*.sln")
                let solution = Services.ProjectService.ReadWorkspaceItem (monitor, solutionFile)
                from project in solution.GetAllProjects ().OfType<MonoMacProject> ()
                select project;
            
            return projects.FirstOrDefault ();
        }
    }
}
