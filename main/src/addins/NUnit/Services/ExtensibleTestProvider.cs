//
// ExtensibleTestProvider.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using System.Reflection;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Debugger;

namespace MonoDevelop.NUnit
{
	public abstract class ExtensibleTestProvider<T>: IExtensibleTestProvider
		where T: IWorkspaceObject
	{
		public string Id { get; set; }

		public ExtensionRegistry Registry { get; set; }

		protected abstract UnitTest CreateUnitTest (T entry, string discovererId,
			ITestDiscoverer discoverer, ITestExecutionDispatcher dispatcher);

		public IEnumerable<UnitTest> CreateUnitTests (IWorkspaceObject entry)
		{
			if (entry is T) {
				foreach (var tuple in Registry.GetDiscoverers(Id)) {
					var discovererId = tuple.Item1;
					var discovererType = tuple.Item2;
					yield return CreateUnitTest ((T)entry, discovererId, new LazyDiscoverer (discovererType),
						new ExecutionDispatcher (Registry, Id, discovererId));
				}
			}
		}

		class ExecutionDispatcher: ITestExecutionDispatcher
		{
			readonly string providerId;
			readonly string discovererId;
			ExtensionRegistry registry;

			public ExecutionDispatcher (ExtensionRegistry registry, string providerId, string discovererId)
			{
				this.providerId = providerId;
				this.discovererId = discovererId;
				this.registry = registry;
			}

			public void DispatchExecution (IEnumerable<TestCase> testCases, TestContext context,
				ITestExecutionHandler handler)
			{
				var executorType = registry.GetDefaultExecutor(providerId, discovererId);

				ExternalExecutor externalExecutor = (ExternalExecutor)Runtime.ProcessService.CreateExternalProcessObject (typeof(ExternalExecutor),
                	context.ExecutionContext);

				try {
					externalExecutor.Preload(executorType.Assembly.Location, executorType.FullName);
					var externalHandler = new ExternalExecutionHandler(handler);
					externalExecutor.Execute (testCases.ToList(), new ExternalExecutionContext(context), externalHandler);
				} catch (Exception e) {
					throw e;
				} finally {
					externalExecutor.Dispose ();
				}
			}
		}

		class LazyDiscoverer: ITestDiscoverer
		{
			Type discovererType;

			public LazyDiscoverer (Type discovererType)
			{
				this.discovererType = discovererType;
			}

			public void Discover (IWorkspaceObject entry, ITestDiscoveryContext context, ITestDiscoverySink sink)
			{
				// TODO: do we need to instantiate it as an external process object? (like executor)
				// TODO: is there need for caching discoverer?
				var discoverer = (ITestDiscoverer) Activator.CreateInstance (discovererType);
				discoverer.Discover (entry, context, sink);
			}
		}

		public virtual Type[] GetOptionTypes ()
		{
			return null;
		}
	}

	public interface IExtensibleTestProvider: ITestProvider
	{
		string Id { get; set; }
		ExtensionRegistry Registry { get; set; }
	}

	public class ExternalExecutor: RemoteProcessObject
	{
		Assembly assembly;
		Type type;

		public ExternalExecutor ()
		{
			// In some cases MS.NET can't properly resolve assemblies even if they
			// are already loaded. For example, when deserializing objects from remoting.
			/*AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs args) {
				foreach (Assembly am in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (am.GetName ().FullName == args.Name)
						return am;
				}
				return null;
			};*/
		}

		public void Preload (string assembly, string type)
		{
			this.assembly = Assembly.LoadFile (assembly);
			this.type = this.assembly.GetType (type);
		}

		public void Execute (List<TestCase> testCases, ITestExecutionContext context, ITestExecutionHandler handler)
		{
			var executor = Activator.CreateInstance (type) as ITestExecutor;
			executor.Execute (testCases, context, handler);
		}
	}

	public class ExternalExecutionHandler: MarshalByRefObject, ITestExecutionHandler
	{
		ITestExecutionHandler handler;

		public ExternalExecutionHandler (ITestExecutionHandler handler)
		{
			this.handler = handler;
		}

		public void RecordStart (TestCase testCase)
		{
			handler.RecordStart (testCase);
		}

		public void RecordResult (TestCaseResult testCaseResult)
		{
			handler.RecordResult (testCaseResult);
		}

		public void RecordEnd (TestCase testCase)
		{
			handler.RecordEnd (testCase);
		}
	}

	public class ExternalExecutionContext: MarshalByRefObject, ITestExecutionContext
	{
		TestContext context;

		public ExternalExecutionContext (TestContext context)
		{
			this.context = context;
		}

		public bool IsCancelRequested {
			get {
				return context.Monitor.IsCancelRequested;
			}
		}
	}
}

