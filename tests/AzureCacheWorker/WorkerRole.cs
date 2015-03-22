using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace AzureCacheWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        // No further implementation is required to support a cache worker role. 
        // Additional functionality may affect the performance of the cache service. 
        // For information on the dedicated cache worker role and the cache service 
        // see the MSDN documentation at http://go.microsoft.com/fwlink/?LinkID=247285
    }
}
