//  ServiceManager.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core;

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
						LoggingService.LogInfo (GettextCatalog.GetString ("Initializing service: ") + serviceType);
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
