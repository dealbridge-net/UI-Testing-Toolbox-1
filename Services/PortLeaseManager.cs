using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services
{
    /// <summary>
    /// Service for acquiring a lease on a given network port number between concurrent processes.
    /// </summary>
    /// <remarks>
    /// You may think it's about managing the rent of a sea harbor but rest assured it isn't.
    /// </remarks>
    public class PortLeaseManager
    {
        private readonly IEnumerable<int> _availablePortsRange;
        private readonly HashSet<int> _usedPorts = new HashSet<int>();
        private readonly object _portAcquisitionLock = new object();


        public PortLeaseManager(int lowerBound, int upperBound) =>
            _availablePortsRange = Enumerable.Range(lowerBound, upperBound - lowerBound);


        public int LeaseAvailableRandomPort()
        {
            lock (_portAcquisitionLock)
            {
                var availablePorts = _availablePortsRange.Except(_usedPorts).ToList();
                var port = availablePorts[new Random().Next(availablePorts.Count)];
                _usedPorts.Add(port);
                return port;
            }
        }

        public void StopLease(int port)
        {
            lock (_portAcquisitionLock)
            {
                _usedPorts.Remove(port);
            }
        }
    }
}
