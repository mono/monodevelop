//
// ConfigurationMerger.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using System.Linq;

namespace MonoDevelop.Components.MainToolbar
{
	/// <summary>
	/// This class is used to generate a list of configurations to show in the configuration
	/// selector of the MonoDevelop toolbar. The class tries to reduce the number of configurations
	/// by merging those which have the same prefix and build the same project configurations
	/// for the current startup project. It also can be used to get a list of execution targets.
	/// </summary>
	class ConfigurationMerger
	{
		List<TargetPartition> currentTargetPartitions = new List<TargetPartition> ();
		List<string> currentSolutionConfigurations = new List<string> ();
		HashSet<string> reducedConfigurations = new HashSet<string> ();
		DummyExecutionTarget dummyExecutionTarget = new DummyExecutionTarget ();

		/// <summary>
		/// Resulting list of configurations. Some of them may be merged.
		/// </summary>
		public List<string> SolutionConfigurations {
			get { return currentSolutionConfigurations; }
		}

		/// <summary>
		/// Load configuration information for a solution
		/// </summary>
		public void Load (Solution sol)
		{
			currentSolutionConfigurations.Clear ();
			currentTargetPartitions.Clear ();
			reducedConfigurations.Clear ();

			if (sol == null)
				return;

			var project = sol.StartupItem;

			// Create a set of configuration partitions. Each partition will contain configurations
			// which are implicitly selected when selecting an execution target. For example, in
			// an iOS project we would have two partitions: 
			//   1) Debug|IPhoneSimulator, Release|IPhoneSimulator
			//      targets: iPhone, iPad
			//   2) Debug|IPhone, Release|IPhone
			//      targets: device

			List<TargetPartition> partitions = new List<TargetPartition> ();
			if (project != null) {
				foreach (var conf in project.Configurations) {
					var targets = project.GetExecutionTargets (conf.Selector);
					if (!targets.Any ()) {
						targets = new ExecutionTarget[] { dummyExecutionTarget };
					}
					var parts = partitions.Where (p => targets.Any (t => p.Targets.Contains (t))).ToArray();
					if (parts.Length == 0) {
						// Create a new partition for this configuration
						var p = new TargetPartition ();
						p.Configurations.Add (conf.Id);
						p.Targets.UnionWith (targets);
						partitions.Add (p);
					}
					else if (parts.Length == 1) {
						// Register the configuration into an existing partition
						parts[0].Configurations.Add (conf.Id);
						parts[0].Targets.UnionWith (targets);
					}
					else {
						// The partitions have to be merged into a single one
						for (int n=1; n<parts.Length; n++) {
							parts[0].Configurations.UnionWith (parts[n].Configurations);
							parts[0].Targets.UnionWith (parts[n].Targets);
							partitions.Remove (parts[n]);
						}
					}
				}

				// The startup project configuration partitions are used to create solution configuration partitions

				foreach (var solConf in sol.Configurations) {
					var pconf = solConf.GetEntryForItem (project);
					if (pconf != null && pconf.Build) {
						var part = partitions.FirstOrDefault (p => p.Configurations.Contains (pconf.ItemConfiguration));
						if (part != null) {
							part.SolutionConfigurations.Add (solConf.Id);
							continue;
						}
					}
					// The solution configuration is not bound to the startup project
					// Add it to all partitions so that it can still take part of
					// the solution configuration simplification process
					foreach (var p in partitions)
						p.SolutionConfigurations.Add (solConf.Id);
				}
			}

			if (partitions.Count == 0) {
				// There is no startup project, just use all solution configurations in this case
				var p = new TargetPartition ();
				p.SolutionConfigurations.AddRange (sol.GetConfigurations ());
			}

			// There can be several configurations with the same prefix and different platform but which build the same projects.
			// If all configurations with the same prefix are identical, all of them can be reduced into a single configuration
			// with no platform name. This loop detects such configurations

			var notReducibleConfigurations = new HashSet<string> ();

			foreach (var p in partitions) {
				var groupedConfigs = p.SolutionConfigurations.GroupBy (sc => {
					string name, plat;
					ItemConfiguration.ParseConfigurationId (sc, out name, out plat);
					return name;
				}).ToArray ();
				foreach (var confGroup in groupedConfigs) {
					var configs = confGroup.ToArray ();
					var baseConf = sol.Configurations[configs[0]];
					if (configs.Skip (1).All (c => ConfigurationEquals (sol, baseConf, sol.Configurations[c])))
						p.ReducedConfigurations.Add (confGroup.Key);
					else
						notReducibleConfigurations.Add (confGroup.Key);
				}
			}

			// To really be able to use reduced configuration names, all partitions must have that reduced configuration
			// Find the configurations that have been reduced in all partitions

			reducedConfigurations = new HashSet<string> (partitions.SelectMany (p => p.ReducedConfigurations));
			reducedConfigurations.ExceptWith (notReducibleConfigurations);

			// Final merge of configurations

			var result = new HashSet<string> ();
			foreach (var p in partitions)
				result.UnionWith (p.SolutionConfigurations);

			// Replace reduced configurations

			foreach (var reducedConf in reducedConfigurations) {
				result.RemoveWhere (c => {
					string name, plat;
					ItemConfiguration.ParseConfigurationId (c, out name, out plat);
					return name == reducedConf;
				});
				result.Add (reducedConf);
			}
			currentTargetPartitions = partitions;
			currentSolutionConfigurations.AddRange (result);
			currentSolutionConfigurations.Sort ();
		}

		/// <summary>
		/// Gets the full configuration name given a possibly merged configuration name and execution target
		/// </summary>
		/// <param name='currentConfig'>
		/// A configuration name (can be a merged configuration name)
		/// </param>
		/// <param name='currentTarget'>
		/// Selected execution target
		/// </param>
		/// <param name='resolvedConfig'>
		/// Resolved configuration
		/// </param>
		/// <param name='resolvedTarget'>
		/// If the provided target is not valid for the provided configuration, this returns a valid target
		/// </param>
		public void ResolveConfiguration (string currentConfig, ExecutionTarget currentTarget, out string resolvedConfig, out ExecutionTarget resolvedTarget)
		{
			resolvedConfig = null;
			resolvedTarget = currentTarget;

			if (!reducedConfigurations.Contains (currentConfig)) {
				// The selected configuration is not reduced, just use it as full config name
				resolvedConfig = currentConfig;
				var part = currentTargetPartitions.FirstOrDefault (p => p.SolutionConfigurations.Contains (currentConfig));
				if (part == null)
					resolvedTarget = null;
				else if (!part.Targets.Contains (resolvedTarget))
					resolvedTarget = part.Targets.FirstOrDefault (t => !(t is DummyExecutionTarget));
			} else {
				// Reduced configuration. Find the partition and guess the implicit project configuration

				var part = currentTargetPartitions.FirstOrDefault (p => p.Targets.Contains (currentTarget ?? dummyExecutionTarget));
				if (part != null) {
					resolvedConfig = part.SolutionConfigurations.FirstOrDefault (c => {
						string name, plat;
						ItemConfiguration.ParseConfigurationId (c, out name, out plat);
						return name == currentConfig;
					});
				}
				if (resolvedConfig == null) {
					part = currentTargetPartitions.FirstOrDefault (p => p.ReducedConfigurations.Contains (currentConfig));
					if (part == null)
						part = currentTargetPartitions.FirstOrDefault (p => p.SolutionConfigurations.Contains (currentConfig));
					if (part != null) {
						resolvedTarget = part.Targets.FirstOrDefault (t => !(t is DummyExecutionTarget));
						resolvedConfig = part.SolutionConfigurations.FirstOrDefault (c => {
							string name, plat;
							ItemConfiguration.ParseConfigurationId (c, out name, out plat);
							return name == currentConfig;
						});
						if (resolvedConfig == null)
							resolvedConfig = currentConfig;
					} else {
						resolvedTarget = null;
						resolvedConfig = currentConfig;
					}
				}
			}
			if (resolvedTarget == dummyExecutionTarget)
				resolvedTarget = null;
		}

		/// <summary>
		/// Gets the targets which are valid for a configuration
		/// </summary>
		public IEnumerable<ExecutionTarget> GetTargetsForConfiguration (string fullConfigurationId, bool ignorePlatform)
		{
			string conf,plat;
			ItemConfiguration.ParseConfigurationId (fullConfigurationId, out conf, out plat);

			if (ignorePlatform && reducedConfigurations.Contains (conf)) {
				// Reduced configuration. Show all targets since they will be used to guess the implicit platform
				return currentTargetPartitions.Where (p => p.ReducedConfigurations.Contains (conf)).SelectMany (p => p.Targets);
			} else {
				// Show targets for the configuration
				var part = currentTargetPartitions.FirstOrDefault (p => p.SolutionConfigurations.Contains (fullConfigurationId));
				if (part != null)
					return part.Targets;
				else
					return new ExecutionTarget[0];
			}
		}

		/// <summary>
		/// Given a full configuration id, returns the merged configuration
		/// </summary>
		public string GetUnresolvedConfiguration (string fullConfigurationId)
		{
			string conf,plat;
			ItemConfiguration.ParseConfigurationId (fullConfigurationId, out conf, out plat);

			if (reducedConfigurations.Contains (conf))
				return conf;
			else
				return fullConfigurationId;
		}

		bool ConfigurationEquals (Solution sol, SolutionConfiguration s1, SolutionConfiguration s2)
		{
			foreach (var p in sol.GetAllSolutionItems<SolutionEntityItem> ()) {
				var c1 = s1.GetEntryForItem (p);
				var c2 = s2.GetEntryForItem (p);
				if (c1 == null && c2 == null)
					continue;
				if (c1 == null || c2 == null)
					return false;
				if (c1.Build != c2.Build || c1.ItemConfiguration != c2.ItemConfiguration)
					return false;
			}
			return true;
		}

		class DummyExecutionTarget: ExecutionTarget
		{
			public override string Name {
				get { return "Default"; }
			}

			public override string Id {
				get {
					return "MonoDevelop.Default";
				}
			}
		}

		class TargetPartition
		{
			/// <summary>
			/// Targets included in this partition
			/// </summary>
			public HashSet<ExecutionTarget> Targets = new HashSet<ExecutionTarget> ();

			/// <summary>
			/// Project configurations included in this partition
			/// </summary>
			public HashSet<string> Configurations = new HashSet<string> ();

			/// <summary>
			/// Solution configurations included in this partition (configurations which are bound to the project configurations)
			/// </summary>
			public List<string> SolutionConfigurations = new List<string> ();

			/// <summary>
			/// Configurations (without platform) that have been reduced
			/// </summary>
			public HashSet<string> ReducedConfigurations = new HashSet<string> ();
		}
	}
}

