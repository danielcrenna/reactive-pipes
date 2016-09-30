using System;
using System.Security.Principal;

namespace reactive.pipes.scheduled
{
    public class LockedIdentity
    {
        public static string Get()
        {
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            return user.Name ?? Environment.UserName ?? Environment.MachineName;
        }
    }
}