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
using System.Collections.Generic;

namespace ServiceStack.Owin
{
	public interface IOwinStream
	: IEnumerable<Action<Action<object>, Action<Exception>>>, IDisposable
	{
		IOwinStreamManager Manager { set; }
		bool Active { get; set; }
		bool HadExceptions { get; }
		bool IsDisposed { get; }
	}
}