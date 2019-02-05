//
// ServiceProvider.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Core
{
	/// <summary>
	/// A basic implementation of a service provider
	/// </summary>
	public class BasicServiceProvider : ServiceProvider
	{
		List<object> services = new List<object> ();
		Dictionary<Type, Type> serviceTypes = new Dictionary<Type, Type> ();
		Dictionary<object, TaskCompletionSource<object>> initializationTasks = new Dictionary<object, TaskCompletionSource<object>> ();
		Dictionary<Type, object> servicesByType = new Dictionary<Type, object> ();
		Dictionary<Type, List<object>> initializationCallbacks = new Dictionary<Type, List<object>> ();
		bool disposing;

		public override async Task<T> GetService<T> ()
		{
			CheckValid ();
			Task<object> currentInitTask = null;

			lock (services) {
				// Fast path, try to get a service for this specific type
				if (!servicesByType.TryGetValue (typeof (T), out var service)) {
					// Look in all registered services
					service = services.OfType<T> ().FirstOrDefault ();
					if (service == null) {
						// Create a new service instance
						LoggingService.LogInfo ("Creating service: " + typeof (T));
						service = (T)Activator.CreateInstance (GetImplementationType (typeof (T)), true);
						services.Add (service);
						servicesByType [typeof (T)] = service;
						if (service is IService serviceInstance) {
							var completionTask = new TaskCompletionSource<object> ();
							initializationTasks [service] = completionTask;
							serviceInstance.Initialize (this).ContinueWith (t => {
								if (t.IsFaulted)
									completionTask.SetException (t.Exception);
								else
									OnServiceInitialized (completionTask, service);
							});
						}
					}
				}

				// If the service is being initialized, wait for it to be done, but the wait has to be done outside the lock
				if (initializationTasks.TryGetValue (service, out var initTask))
					currentInitTask = initTask.Task;
				else
					return (T)service;
			}

			return (T)await currentInitTask.ConfigureAwait (false);
		}

		/// <summary>
		/// Executes an action when a service is initialized
		/// </summary>
		/// <param name="action">Action to run</param>
		/// <typeparam name="T">Service type</typeparam>
		/// <remarks>This method does not cause the initialization of the service.</remarks>
		public override void WhenServiceInitialized<T> (Action<T> action)
		{
			lock (services) {
				if (servicesByType.TryGetValue (typeof (T), out var service)) {
					// Service already requested
					if (initializationTasks.TryGetValue (service, out var initTask))
						initTask.Task.ContinueWith (t => action ((T)service), TaskScheduler.Current);
					else
						action ((T)service);
				} else {
					// Service not yet requested, register the callback
					if (!initializationCallbacks.TryGetValue (typeof (T), out var list))
						initializationCallbacks [typeof (T)] = list = new List<object> ();
					list.Add (action);
				}
			}
		}

		void OnServiceInitialized<T> (TaskCompletionSource<object> completionTask, T service)
		{
			List<object> callbacks = null;

			lock (services) {
				completionTask.SetResult (service);
				initializationTasks.Remove (service);
				if (initializationCallbacks.TryGetValue (typeof (T), out callbacks))
					initializationCallbacks.Remove (typeof (T));
			}

			if (callbacks != null) {
				foreach (Action<T> action in callbacks) {
					try {
						action (service);
					} catch (Exception ex) {
						LoggingService.LogInternalError ("Service initialization callback failed", ex);
					}
				}
			}
		}

		Type GetImplementationType (Type serviceType)
		{
			lock (serviceTypes) {
				if (serviceTypes.TryGetValue (serviceType, out var implType))
					return implType;
			}
			var attr = (DefaultServiceImplementationAttribute)Attribute.GetCustomAttribute (serviceType, typeof (DefaultServiceImplementationAttribute));
			if (attr != null) {
				return attr.ImplementationType ?? serviceType;
			}

			throw new InvalidOperationException ("Unknown service type: " + serviceType);
		}

		public void RegisterServiceType (Type serviceType, Type serviceImplementationType)
		{
			lock (serviceTypes)
				serviceTypes [serviceType] = serviceImplementationType;
		}

		public void RegisterServiceType<T, TImpl> () where TImpl : T
		{
			lock (serviceTypes)
				serviceTypes [typeof (T)] = typeof (TImpl);
		}

		public void RegisterService (IService service)
		{
			lock (services) {
				CheckValid ();
				services.Add (service);
			}
		}

		public void UnregisterService (IService service)
		{
			lock (services) {
				CheckValid ();
				services.Remove (service);
			}
		}

		public async Task Dispose ()
		{
			List<object> list;
			lock (services) {
				if (disposing)
					return;
				list = services.ToList ();
				services.Clear ();
				disposing = true;
			}

			await Task.WhenAll (list.OfType<IService> ().Select (s => s.Dispose ()));
		}

		void CheckValid ()
		{
			if (disposing) throw new ObjectDisposedException ("BasicServiceProvider");
		}
	}
}
