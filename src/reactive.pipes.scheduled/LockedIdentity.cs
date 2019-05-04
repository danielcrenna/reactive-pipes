// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Principal;

namespace reactive.pipes.scheduled
{
	public class LockedIdentity
	{
		public static string Get()
		{
			var user = WindowsIdentity.GetCurrent();
			return user.Name ?? Environment.UserName ?? Environment.MachineName;
		}
	}
}