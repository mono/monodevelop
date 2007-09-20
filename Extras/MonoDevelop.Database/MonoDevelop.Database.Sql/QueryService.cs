//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Christian Hergert
// Copyright (c) 2007 Ben Motmans
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
using System.Data;
using System.Threading;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Database.Sql
{
	public delegate void DatabaseConnectionContextCallback (DatabaseConnectionContext context, bool connected, object state);

	public static class QueryService
	{
		public static void RaiseException (Exception exception)
		{
			Services.MessageService.ShowError (exception);
			Runtime.LoggingService.Error ((object)"Database Exception", exception);
		}
		
		public static void RaiseException (string message, Exception exception)
		{
			Services.MessageService.ShowError (exception, message);
			Runtime.LoggingService.Error ((object)"Database Exception", exception);
		}
		
		//TODO: show errors
		public static void EnsureConnection (DatabaseConnectionContext context, DatabaseConnectionContextCallback callback, object state)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			
			IConnectionPool pool = context.ConnectionPool;
			if (pool.IsInitialized) {
				callback (context, true, state);
				return;
			}
			
			IDbFactory fac = DbFactoryService.GetDbFactory (context.ConnectionSettings);
			bool requiresPassword = fac.GetCapabilities ("ConnectionSettings", SchemaActions.Schema) == (int)ConnectionSettingsCapabilities.Password;
			
			if (!context.ConnectionSettings.SavePassword && String.IsNullOrEmpty (context.ConnectionSettings.Password) && requiresPassword) {
				string password = Services.MessageService.GetPassword (
					GettextCatalog.GetString ("Please enter the password for connection '{0}'",
					context.ConnectionSettings.Name),
					GettextCatalog.GetString ("Enter Password")
				);
				
				if (String.IsNullOrEmpty (password)) {
					callback (context, false, state);
					return;
				} else {
					context.ConnectionSettings.Password = password;
				}
			}
			
			EnsureConnectionState internalState = new EnsureConnectionState (context, callback, state);
			ThreadPool.QueueUserWorkItem (new WaitCallback (EnsureConnectionThreaded), internalState);
		}
				                   
		private static void EnsureConnectionThreaded (object obj)
		{
			EnsureConnectionState internalState = obj as EnsureConnectionState;
			IConnectionPool pool = internalState.ConnectionContext.ConnectionPool;

			try {
				bool connected = pool.Initialize ();
				DispatchService.GuiDispatch (delegate () {
					internalState.Callback (internalState.ConnectionContext, connected, internalState.State);
				});
			} catch (Exception e) {
				Runtime.LoggingService.Debug (e);
				
				DispatchService.GuiDispatch (delegate () {
					internalState.Callback (internalState.ConnectionContext, false, internalState.State);
				});
			}
		}
	}
					
	internal class EnsureConnectionState
	{
		public object State;
		public DatabaseConnectionContext ConnectionContext;
		public DatabaseConnectionContextCallback Callback;
		
		public EnsureConnectionState (DatabaseConnectionContext context, DatabaseConnectionContextCallback callback, object state)
		{
			ConnectionContext = context;
			Callback = callback;
			State = state;
		}
	}
}