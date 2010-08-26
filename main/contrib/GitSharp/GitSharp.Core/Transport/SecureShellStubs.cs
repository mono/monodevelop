using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace GitSharp.Core.Transport
{
	// This file holds the stubs needed since we removed the dependency on SharpSsh.  This will definitely change once 
	// we find a suitable replacement.
	public interface ISshSession
	{
		bool IsConnected { get; }
		
		void SetPassword(string password);
		
		void SetConfig(Hashtable config);
		
		void Connect(int timeout);
		
		SecureChannel OpenChannel(string type);
		
		void Disconnect();
	}
	
	public class SecureChannel
	{
		public bool IsConnected { get { throw new NotImplementedException(); } }
		
		public void Connect()
		{
			throw new NotImplementedException();
		}
		
		public void Disconnect()
		{
			throw new NotImplementedException();
		}
	}
	
	public class SecureShell
	{
		public SecureShell()
		{
		}
		
		public ISshSession GetSession(string user, string host, int port)
		{
			throw new NotImplementedException();
		}
		
		public IHostKeyRepository GetHostKeyRepository()
		{
			throw new NotImplementedException();
		}
		
		public void SetHostKeyRepository(IHostKeyRepository hostKeyRepository)
		{
			throw new NotImplementedException();
		}
		
		public void AddIdentity(string identityKey)
		{
			throw new NotImplementedException();
		}
		
		public void SetKnownHosts(StreamReader hostsFile)
		{
			throw new NotImplementedException();
		}
	}
	
	public interface IHostKeyRepository
	{
	}
	
	public class SshChannel : SecureChannel
	{
		public SshChannel()
		{
		}
		
		public void SetCommand(string command)
		{
			throw new NotImplementedException();
		}
		
		public void SetErrStream(Stream outStream)
		{
			throw new NotImplementedException();
		}
		
		public Stream GetInputStream()
		{
			throw new NotImplementedException();
		}
		
		public Stream GetOutputStream()
		{
			throw new NotImplementedException();
		}
		
		public int GetExitStatus()
		{
			throw new NotImplementedException();
		}
	}
	
	public class SecureFtpChannel : SecureChannel
	{
		public static  int SSH_FX_NO_SUCH_FILE = 2;
		
		public SecureFtpChannel()
		{
		}
		
		public void ChangeDirectory(string path)
		{
			throw new NotImplementedException();
		}
		
		public string CurrentDirectory()
		{
			throw new NotImplementedException();
		}
		
		public IEnumerable<object> ListDirectory(string path)
		{
			throw new NotImplementedException();
		}
		
		public Stream Get(string source)
		{
			throw new NotImplementedException();
		}
		
		public Stream Put(string destination)
		{
			throw new NotImplementedException();
		}
		
		public void Rename(string oldpath, string newpath)
		{
			throw new NotImplementedException();
		}
		
		public void Remove(string path)
		{
			throw new NotImplementedException();
		}
		
		public void RemoveDirectory(string path)
		{
			throw new NotImplementedException();
		}
		
		public void MakeDirectory(string path)
		{
			throw new NotImplementedException();
		}
		
		public class LsEntry
		{
			public string GetFilename()
			{
				throw new NotImplementedException();
			}
			
			public ItemAttributes GetAttrs()
			{
				throw new NotImplementedException();
			}
		}
		
		public class ItemAttributes
		{
			public bool IsDirectory { get { throw new NotImplementedException(); } }
			
			public int GetMTime()
			{
				throw new NotImplementedException();
			}
		}
	}
}
