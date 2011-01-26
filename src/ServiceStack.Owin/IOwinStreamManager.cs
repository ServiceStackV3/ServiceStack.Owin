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

namespace ServiceStack.Owin
{
	public interface IOwinStreamManager : IDisposable
	{
		IOwinStream GetStream();

		void DisposeStream(IOwinStream stream);
	}
}