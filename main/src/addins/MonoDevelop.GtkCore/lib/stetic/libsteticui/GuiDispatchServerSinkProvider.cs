
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;

namespace Stetic
{
	public class GuiDispatchServerSinkProvider: IServerFormatterSinkProvider, IServerChannelSinkProvider
	{
		private IServerChannelSinkProvider next;
		
		public IServerChannelSinkProvider Next {
			get { return next;	}
			set { next = value; }
		}

		public IServerChannelSink CreateSink (IChannelReceiver channel)
		{
			IServerChannelSink chain = next.CreateSink (channel);
			GuiDispatchServerSink sink = new GuiDispatchServerSink (chain, channel);
			return sink;
		}

		public void GetChannelData (IChannelDataStore channelData)
		{
			if(next != null)
				next.GetChannelData(channelData);
		}
	}
}
