using NtpClient;
using System;
using System.Threading;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    /// <summary>
    ///     This class is used to synchronize system time with an NTP server.
    /// </summary>
    static class TimeSync
    {
        /// <summary>
        ///     Amount of milliseconds from ntptime - servertime
        ///     Please use NtpToSysTime() or SysToNtpTime() to be sure of correctness!
        /// </summary>
        public static double delta { get; private set; } = 0.0;

        /// <summary>
        ///     Requests the system time from the ntp server (nl.pool.ntp.org) and calculate/store the difference between system time and ntp time.
        /// </summary>
        public static void SyncTime() {
            Console.WriteLine($"Syncing time with ntp server. This will take about 2 seconds.");
            NtpConnection.Utc("nl.pool.ntp.org"); // Making first request and disposing result. For some reason the first request is not accurate enough!

            Thread.Sleep(2000); // Wait a second to not overload the NTP server.

            // Make actual request and calculate difference with system time.
            DateTime sysTime = DateTime.UtcNow;
            DateTime servTime = NtpConnection.Utc("nl.pool.ntp.org"); 
            delta = (servTime - sysTime).TotalMilliseconds;
            Console.WriteLine($"Time synced with ntp server. Difference is: {delta} ms\r\n\r\n");
        }

        /// <summary>
        ///     Method to calculate the system time, given the ntp time. If SyncTime has not ran before this method, will return ntp time.
        /// </summary>
        /// <param name="ntp">The ntp time that needs to be converted to system time</param>
        /// <returns>Returns calculated system time</returns>
        public static DateTime NtpToSysTime(DateTime ntp) {
            return ntp.AddMilliseconds(-1 * delta);
        }

        /// <summary>
        ///     Method to calculate the ntp time, given the sysetm time. If SyncTime has not ran before this method, will return system time.
        /// </summary>
        /// <param name="ntp">The system time that needs to be converted to ntp time</param>
        /// <returns>Returns calculated ntp time</returns>
        public static DateTime SysToNtpTime(DateTime sys) {
            return sys.AddMilliseconds(delta);
        }
    }
}
