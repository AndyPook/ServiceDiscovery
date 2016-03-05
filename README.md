# ServiceDiscovery
A server-less service discovery mechanism

# Building
Simply open in VS2015 and build

# Usage
Running ServiceDiscoveryConsole "registers" a dummy service with a made up ID
 * run another instance of ServiceDiscoveryConsole
 * The first instance will report the appearance of this instance
 * stop either instance
 * after approximately 5 seconds the other instance will report the disappearance
 
# Theory
The ServiceMonitor listens on a multicast UDP port for ServiceBeacons.
A beacon is a small structure that represents simple information about a service: ID; Name; Version; Uri

When a service is started a monitor is created and one or several beacons are registered to announce the details of the service.

Other services or tools can query the monitor to list services of a type, or find a specific service.

 * By default the monitor sends the locally registered beacons once even 2 seconds
 * If a monitor is disposed gracefully it will send a deregister message for each local service
 * A monitor will "time-out" a service if it does not see a beacon for 5 seconds (by default).
 This means that even if a service crashes other services will see the dissappearance fairly quickly.
