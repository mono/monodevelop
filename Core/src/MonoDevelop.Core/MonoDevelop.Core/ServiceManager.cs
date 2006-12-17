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

namespace MonoDevelop.Core
{
	public delegate void ServiceLoadCallback (object sender, ServiceLoadArgs args);

	public enum ServiceLoadType
	{
		LoadStarted,
		LoadCompleted
	}

	public class ServiceLoadArgs : EventArgs
	{
		private IService        service       = null;
		private ServiceLoadType loadType      = ServiceLoadType.LoadStarted;
		private int             totalServices = 0;

		public ServiceLoadArgs (IService service, ServiceLoadType loadType, int totalServices)
		{
			this.service       = service;
			this.loadType      = loadType;
			this.totalServices = totalServices;
		}

		public IService Service {
			get {
				return service;
			}
		}

		public ServiceLoadType LoadType {
			get {
				return loadType;
			}
		}

		public int TotalServices {
			get {
				return totalServices;
			}
		}
	}

	/// <summary>
	/// This class does basic service handling for you.
	/// </summary>
	public class ServiceManager
	{
		ArrayList serviceList       = new ArrayList();
		Hashtable servicesHashtable = new Hashtable();
		Hashtable initializedServices = new Hashtable ();
		
		static Queue serviceAddinQueue = new Queue ();
		static ServiceManager      defaultServiceManager = new ServiceManager();
		static ServiceLoadCallback serviceLoadCallback   = null;

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
			int totalServices = defaultServiceManager.serviceList.Count;
			int i = 0;
			foreach (IService service in defaultServiceManager.serviceList) {
				i++;
				SendCallback (service, ServiceLoadType.LoadStarted, totalServices);
				service.InitializeService();
				SendCallback (service, ServiceLoadType.LoadCompleted, totalServices);
			}
		}
		
		public static void InitializeServices (string servicesPath, AddIn addin)
		{
			if (serviceAddinQueue.Count != 0) {
				serviceAddinQueue.Enqueue (addin);
				return;
			}
			
			serviceAddinQueue.Enqueue (addin);
			
			while (serviceAddinQueue.Count > 0) {
				AddIn qaddin = (AddIn) serviceAddinQueue.Peek ();
				IAddInTreeNode rootNode = AddInTreeSingleton.AddInTree.GetTreeNode (servicesPath);
				Hashtable services = (Hashtable) rootNode.ChildNodes.Clone ();
				foreach (DictionaryEntry de in services) {
					IAddInTreeNode node = (IAddInTreeNode) de.Value;
					if (node.Codon.AddIn == qaddin) {
						IService service = (IService) rootNode.BuildChildItem ((string)de.Key, defaultServiceManager);
						if (service == null)
							throw new InvalidOperationException ("Could not create service: " + de.Key);
//						Console.WriteLine ("     Added service " + service.GetType ());
						AddService (service);
					}
				}
				serviceAddinQueue.Dequeue ();
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

		public static ServiceLoadCallback ServiceLoadCallback {
			get {
				return serviceLoadCallback;
			}
			set {
				serviceLoadCallback = value;
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
					if (!initializedServices.Contains (service)) {
						Runtime.LoggingService.Info (GettextCatalog.GetString ("Initializing service: ") + serviceType);
						service.InitializeService();
						initializedServices [service] = service;
					}
					return service;
				}
			}
			
			return null;
		}

		static void SendCallback (IService service, ServiceLoadType loadType, int totalServices)
		{
			if (serviceLoadCallback != null)
				serviceLoadCallback (typeof (ServiceManager), new ServiceLoadArgs (service, loadType, totalServices));
		}
	}
}
