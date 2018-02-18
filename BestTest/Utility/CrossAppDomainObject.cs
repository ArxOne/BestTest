// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Utility
{
    using System;
    using System.Runtime.Remoting.Lifetime;

    public class CrossAppDomainObject : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            lease.InitialLeaseTime = TimeSpan.Zero;
            return lease;
        }
    }
}