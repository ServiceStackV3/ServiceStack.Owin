//
// https://github.com/mythz/ServiceStack.Owin
// ServiceStack.Owin: ECMA CLI utils for Owin (http://owin.github.com)
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2011 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Threading;

namespace ServiceStack.Owin.IO
{
	public class PooledOwinStreamManager : IOwinStreamManager
	{
		private readonly object readLock = new object();
		const int DefaultPoolSize = 100;
		private int poolIndex;
		private readonly IOwinStream[] owinStreams;

		public PooledOwinStreamManager() : this(DefaultPoolSize) {}

		public PooledOwinStreamManager(int poolSize)
		{
			if (poolSize < 1)
				throw new ArgumentException("poolSize");

			owinStreams = new IOwinStream[poolSize];
		}

		public IOwinStream GetStream()
		{
			lock (readLock)
			{
				IOwinStream inActiveClient;
				while ((inActiveClient = GetInActiveStream()) == null)
				{
					Monitor.Wait(owinStreams);
				}

				poolIndex++;
				inActiveClient.Active = true;

				return inActiveClient;
			}
		}

		/// <summary>
		/// Called within a lock
		/// </summary>
		private IOwinStream GetInActiveStream()
		{
			for (var i = 0; i < owinStreams.Length; i++)
			{
				var nextIndex = (poolIndex + i) % owinStreams.Length;

				//Initialize if not exists or existing client had errors
				var existingClient = owinStreams[nextIndex];
				if (existingClient == null
				    || existingClient.HadExceptions)
				{
					if (existingClient != null)
					{
						try
						{
							existingClient.Dispose();
						}
						catch (Exception ignore) {}
					}

					var client = CreateOwinStream();

					client.Manager = this;

					owinStreams[nextIndex] = client;

					return client;
				}

				//look for free one
				if (!owinStreams[nextIndex].Active)
				{
					return owinStreams[nextIndex];
				}
			}
			return null;
		}

		public virtual IOwinStream CreateOwinStream()
		{
			return new NonBlockingReadBufferedStream();
		}

		public void DisposeStream(IOwinStream stream)
		{
			lock (readLock)
			{
				for (var i = 0; i < owinStreams.Length; i++)
				{
					var readClient = owinStreams[i];
					if (stream != readClient) continue;
					stream.Active = false;
					Monitor.PulseAll(owinStreams);
					return;
				}
			}

			if (stream.IsDisposed) return;

			throw new NotSupportedException("Cannot add unknown IOwinStream back to the pool");
		}

		~PooledOwinStreamManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// get rid of managed resources
				for (var i = 0; i < owinStreams.Length; i++)
				{
					try
					{
						owinStreams[i].Dispose();
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine(string.Format(
						                        "Error when trying to dispose of RedisClient #{0}",i), ex);
					}
				}
			}
		}
	}

}