// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.Services
{
	/// <summary>
	/// This class does basic service handling for you.
	/// </summary>
	public class ServiceManager
	{
		ArrayList serviceList       = new ArrayList();
		Hashtable servicesHashtable = new Hashtable();
		
		static ServiceManager defaultServiceManager = new ServiceManager();

		protected ServiceManager ()
		{
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
			AddService(new PropertyService());
			AddService(new StringParserService());
			AddService(new FileUtilityService());
		}
		
		/// <remarks>
		/// This method initializes the service system to a path inside the add-in tree.
		/// This method must be called ONCE.
		/// </remarks>
		public static void InitializeServicesSubsystem(string servicesPath)
		{
			// add add-in tree services
			AddServices((IService[])AddInTreeSingleton.AddInTree.GetTreeNode(servicesPath).BuildChildItems(defaultServiceManager).ToArray(typeof(IService)));
			
			// initialize all services
			foreach (IService service in defaultServiceManager.serviceList) {
				service.InitializeService();
			}
		}
		
		/// <remarks>
		/// Calls UnloadService on all services. This method must be called ONCE.
		/// </remarks>
		public static void UnloadAllServices()
		{
			foreach (IService service in defaultServiceManager.serviceList) {
				service.UnloadService();
			}
		}
		
		public static void AddService(IService service)
		{
			defaultServiceManager.serviceList.Add(service);
		}
		
		public static void AddServices(IService[] services)
		{
			foreach (IService service in services) {
				AddService(service);
			}
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
			IService s = (IService)servicesHashtable[serviceType];
			if (s != null) {
				return s;
			}
			
			foreach (IService service in serviceList) {
				if (IsInstanceOfType(serviceType, service)) {
					servicesHashtable[serviceType] = service;
					return service;
				}
			}
			
			return null;
		}
	}
}
