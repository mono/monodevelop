// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core
{
	/// <summary>
	/// This class does basic service handling for you.
	/// </summary>
	public class ServiceManager
	{
		List<IService>             serviceList         = new List<IService> ();
		Dictionary<Type, IService> servicesHashtable   = new Dictionary<Type, IService> ();
		List<IService>             initializedServices = new List<IService> ();
		
		static ServiceManager defaultServiceManager = new ServiceManager ();

		protected ServiceManager ()
		{
		}
		
		public static void Initialize ()
		{
			// Ensure service extension nodes are created
			AddinManager.GetExtensionNodes ("/MonoDevelop/Core/Services");
		}
		
		public static IService GetService (Type serviceType) {
			return defaultServiceManager.FetchService (serviceType);
		}
			

		public IService this [Type index] {
			get {
				return FetchService (index);
			}
		}		
		
		/// <summary>
		/// Don't create ServiceManager objects, only have ONE per application.
		/// </summary>
		static ServiceManager()
		{
			// add 'core' services
			AddService (new PropertyService());
			AddService (new StringParserService());
		}
		
		/// <remarks>
		/// Calls UnloadService on all services. This method must be called ONCE.
		/// </remarks>
		public static void UnloadAllServices()
		{
			foreach (IService service in defaultServiceManager.serviceList) 
				service.UnloadService();
		}
		
		public static void AddService(IService service)
		{
			defaultServiceManager.serviceList.Add(service);
		}
		
		public static void AddServices(IService[] services)
		{
			foreach (IService service in services) 
				AddService(service);
		}

		// HACK: MONO BUGFIX
		// this doesn't work on mono:serviceType.IsInstanceOfType(service)
		bool IsInstanceOfType(Type type, IService service)
		{
			Type serviceType = service.GetType();

			foreach (Type iface in serviceType.GetInterfaces()) {
				if (iface == type) {
					return true;
				}
			}
			
			while (serviceType != typeof(System.Object)) {
				if (type == serviceType) {
					return true;
				}
				serviceType = serviceType.BaseType;
			}
			return false;
		}
		
		/// <remarks>
		/// Requestes a specific service, may return null if this service is not found.
		/// </remarks>
		IService FetchService(Type serviceType)
		{
			if (servicesHashtable.ContainsKey (serviceType)) 
				return servicesHashtable[serviceType];
			
			foreach (IService service in serviceList) {
				if (IsInstanceOfType(serviceType, service)) {
					servicesHashtable[serviceType] = service;
					if (!initializedServices.Contains (service)) {
						Runtime.LoggingService.Info (GettextCatalog.GetString ("Initializing service: ") + serviceType);
						service.InitializeService();
						initializedServices.Add (service);
					}
					return service;
				}
			}
			
			return null;
		}
	}
}
