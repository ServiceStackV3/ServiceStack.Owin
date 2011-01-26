using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Owin.IO;
using ServiceStack.Text;

namespace ServiceStack.Owin.Tests
{
	[TestFixture]
	public class NonBlockingReadBufferedStreamTests
	{
		private const string TestString = "test";
		private readonly byte[] testStringBytes = TestString.ToUtf8Bytes();

		public NonBlockingReadBufferedStream Create()
		{
			return new NonBlockingReadBufferedStream();
		}

		static void FailOnError(Exception ex)
		{
			Assert.Fail(ex.Message);
		}

		static void AssertArraySegmentsEquals(IList<ArraySegment<byte>> arraySegs, byte[] equalToBytes)
		{
			Assert.That(arraySegs.Count, Is.EqualTo(1));
			AssertArraySegmentEquals(arraySegs[0], equalToBytes);
		}

		static void AssertArraySegmentEquals(ArraySegment<byte> arraySeg, byte[] equalToBytes)
		{
			Assert.That(arraySeg.Count, Is.EqualTo(equalToBytes.Length));
			for (var i = 0; i < equalToBytes.Length; i++)
			{
				Assert.That(arraySeg.Array[arraySeg.Offset + i], Is.EqualTo(equalToBytes[i]));
			}
		}

		[Test]
		public void Can_write_and_read_string_on_single_thread()
		{
			var stream = Create();
			using (stream)
			{
				stream.Write(testStringBytes, 0, testStringBytes.Length);
			}

			var arraySegs = new List<ArraySegment<byte>>();
			foreach (var bufferedStream in stream)
			{
				bufferedStream(r => arraySegs.Add((ArraySegment<byte>)r), FailOnError);
			}

			AssertArraySegmentsEquals(arraySegs, testStringBytes);
		}
	}

}
