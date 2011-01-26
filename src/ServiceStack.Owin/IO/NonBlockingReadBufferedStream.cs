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
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Owin.IO
{
	public class NonBlockingReadBufferedStream : Stream, IOwinStream
	{
		private const int MtuAppSize = 1450;
		private const int BufferAllocationSize = 32 * 1024;

		internal int ResetClearsBufferOfMaxSize = 4 * 1024 * 1024; //4MB

		internal byte[] Buffer = new byte[BufferAllocationSize];
		internal int WriteIndex = 0;

		public override void Write(byte[] srcBytes, int srcOffset, int srcCount)
		{
			if ((WriteIndex + srcCount) > Buffer.Length)
			{
				const int breathingSpaceToReduceReallocations = BufferAllocationSize;
				var newLargerBuffer = new byte[WriteIndex + srcCount + breathingSpaceToReduceReallocations];
				System.Buffer.BlockCopy(Buffer, 0, newLargerBuffer, 0, Buffer.Length);
				Buffer = newLargerBuffer;
			}

			System.Buffer.BlockCopy(srcBytes, srcOffset, Buffer, WriteIndex, srcCount);
			WriteIndex += srcBytes.Length;
		}

		public override void Flush() { }

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException("Use the IEnumerator to read");
		}

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override long Length
		{
			get { return this.WriteIndex; }
		}

		public override long Position
		{
			get
			{
				return WriteIndex;
			}
			set
			{
				WriteIndex = (int)value;
			}
		}

		public void Reset()
		{
			//These buffers are expected to be pooled but remove large writes to save memory
			if (Buffer.Length > ResetClearsBufferOfMaxSize)
			{
				Buffer = new byte[BufferAllocationSize];
			}
			WriteIndex = 0;
			IsDisposed = false;
		}

		//Called from IEnumerator
		internal void Release()
		{
			Reset();
		}

		public IOwinStreamManager Manager { set; private get; }
		public bool Active { get; set; }
		public bool HadExceptions { get; private set; }

		public bool IsDisposed { get; private set; }
		void IDisposable.Dispose()
		{
			IsDisposed = true;
		}

		public class Enumerator 
			: IEnumerator<Action<Action<object>, Action<Exception>>>
		{
			private readonly NonBlockingReadBufferedStream stream;
			private int readIndex = 0;
			private int emptyResponsesCount = 0;
			private ArraySegment<byte> currentSegment;

			public Enumerator(NonBlockingReadBufferedStream stream)
			{
				this.stream = stream;
			}

			public bool MoveNext()
			{
				if (readIndex >= stream.WriteIndex)
				{
					currentSegment = new ArraySegment<byte>(
					stream.Buffer, stream.WriteIndex, 0);

					emptyResponsesCount++;
					return !stream.IsDisposed; 
				}

				var newReadIndex = readIndex + MtuAppSize;
				if (newReadIndex <= stream.WriteIndex)
				{
					currentSegment = new ArraySegment<byte>(
						stream.Buffer, readIndex, MtuAppSize);

					readIndex = newReadIndex;
					return newReadIndex != stream.WriteIndex;
				}

				//Partial chunk size left
				newReadIndex = stream.WriteIndex;
				currentSegment = new ArraySegment<byte>(
					stream.Buffer, readIndex, newReadIndex);
				readIndex = newReadIndex;

				return true; //or false?
			}

			public void Reset()
			{
				readIndex = 0;
			}

			public void CallBack(Action<object> readCallback, Action<Exception> ex)
			{
				readCallback(currentSegment);
			}

			public Action<Action<object>, Action<Exception>> Current
			{
				get
				{
					return CallBack;
				}
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

			public void Dispose()
			{
				stream.Release();
			}
		}

		public IEnumerator<Action<Action<object>, Action<Exception>>> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}