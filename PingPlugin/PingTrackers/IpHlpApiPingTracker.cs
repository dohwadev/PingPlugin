﻿using Dalamud.Logging;
using PingPlugin.GameAddressDetectors;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PingPlugin.PingTrackers
{
    public class IpHlpApiPingTracker : PingTracker
    {
        public IpHlpApiPingTracker(PingConfiguration config, GameAddressDetector addressDetector) : base(config, addressDetector, PingTrackerKind.IpHlpApi)
        {
        }

        protected override async Task PingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (SeAddress != null)
                {
                    try
                    {
                        var rtt = GetAddressLastRTT(SeAddress);
                        var error = (WinError)Marshal.GetLastWin32Error();

                        Errored = error != WinError.NO_ERROR;

                        if (!Errored)
                        {
                            NextRTTCalculation(rtt);
                        }
                        else
                        {
                            PluginLog.LogWarning($"Got Win32 error {error} when executing ping - this may be temporary and acceptable.");
                        }
                    }
                    catch (Exception e)
                    {
                        Errored = true;
                        PluginLog.LogError(e, "Error occurred when executing ping.");
                    }
                }

                await Task.Delay(3000, token);
            }
        }

        private static ulong GetAddressLastRTT(IPAddress address)
        {
            var addressBytes = address.GetAddressBytes();
            var addressRaw = BitConverter.ToUInt32(addressBytes);

            var hopCount = 0U;
            var rtt = 0U;

            return GetRTTAndHopCount(addressRaw, ref hopCount, 51, ref rtt) == 1 ? rtt : 0;
        }

        [DllImport("Iphlpapi.dll", EntryPoint = "GetRTTAndHopCount", SetLastError = true)]
        private static extern int GetRTTAndHopCount(uint address, ref uint hopCount, uint maxHops, ref uint rtt);
    }
}