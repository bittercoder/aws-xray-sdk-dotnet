//-----------------------------------------------------------------------------
// <copyright file="IPEndPointExtension.cs" company="Amazon.com">
//      Copyright 2017 Amazon.com, Inc. or its affiliates. All Rights Reserved.
//
//      Licensed under the Apache License, Version 2.0 (the "License").
//      You may not use this file except in compliance with the License.
//      A copy of the License is located at
//
//      http://aws.amazon.com/apache2.0
//
//      or in the "license" file accompanying this file. This file is distributed
//      on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
//      express or implied. See the License for the specific language governing
//      permissions and limitations under the License.
// </copyright>
//-----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Util;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// Provides extension function to <see cref="System.Net.IPEndPoint"/>.
    /// </summary>
    public static class IPEndPointExtension
    {
        private const string Ipv4Address = @"^\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}:\d{1,5}$";
        private static readonly Logger _logger = Logger.GetLogger(typeof(IPEndPointExtension));

        /// <summary>
        /// Tries to parse a string to <see cref="System.Net.IPEndPoint"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="endPoint">The parsed IPEndPoint</param>
        /// <returns>true if <paramref name="input"/> converted successfully; otherwise, false.</returns>
        public static bool TryParse(string input, out IPEndPoint endPoint)
        {
            endPoint = null;

            try
            {
                // Validate basic format of IPv4 address
                if (!Regex.IsMatch(input, Ipv4Address, RegexOptions.None, TimeSpan.FromMinutes(1)))
                {
                    _logger.InfoFormat("Failed to parse IPEndPoint because input is invalid. ({0})", input);
                    return false;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.Error(e, "Failed to parse IPEndPoint because of matach timeout. ({0})", input);
                return false;
            }

            string[] ep = input.Split(':');
            if (ep.Length != 2)
            {
                _logger.InfoFormat("Failed to parse IPEndpoint because input has not exactly two parts splitting by ':'. ({0})", input);
                return false;
            }

            // Validate IP address is in valid range
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                _logger.InfoFormat("Failed to parse IPEndPoint because ip address is invalid. ({0})", input);
                return false;
            }

            int port;
            if (!int.TryParse(ep[1], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out port))
            {
                _logger.InfoFormat("Failed to parse IPEndPoint because port is invalid. ({0})", input);
                return false;
            }

            try
            {
                // Validate port number is in valid range
                endPoint = new IPEndPoint(ip, port);
                _logger.InfoFormat("Using custom daemon address: {0}:{1}", endPoint.Address.ToString(), endPoint.Port);
                return true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                _logger.Error(e, "Failed to parse IPEndPoint because argument to IPEndPoint is invalid. ({0}", input);
                return false;
            }
        }
        
        public static bool TryParse(string endpoint, out IPEndPoint endPoint)
		{
			if (string.IsNullOrEmpty(endpoint) || endpoint.Trim().Length == 0) {
				_logger.InfoFormat("Failed to parse IPEndPoint because input is invalid. ({0})", endpoint);
			}

			if (defaultport != -1 && (defaultport < IPEndPoint.MinPort || defaultport > IPEndPoint.MaxPort)) {
				throw new ArgumentException(string.Format("Invalid default port '{0}'", defaultport));
			}

			var values = endpoint.Split(new[] {':'});
			IPAddress ipaddy;
			int port = -1;

			//check if we have an IPv6 or ports
			if (values.Length <= 2) { // ipv4 or hostname
				port = values.Length == 1 ? defaultport : GetPort(values[1]);
				//try to use the address as IPv4, otherwise get hostname
				if (!IPAddress.TryParse(values[0], out ipaddy)) {
					ipaddy = GetIPfromHost(values[0]);
				}
			} else if (values.Length > 2) { //ipv6			
				//could [a:b:c]:d
				if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]")) {
					string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
					ipaddy = IPAddress.Parse(ipaddressstring);
					port = GetPort(values[values.Length - 1]);
				} else { //[a:b:c] or a:b:c
					ipaddy = IPAddress.Parse(endpoint);
					port = defaultport;
				}
			} else {
				throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", endpoint));
			}

			if (port == -1) {
				throw new ArgumentException(string.Format("No port specified: '{0}'", endpoint));
			}

			return new IPEndPoint(ipaddy, port);
		}

		static int GetPort(string p)
		{
			if (!int.TryParse(p, out var port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
				throw new FormatException(string.Format("Invalid end point port '{0}'", p));
			}

			return port;
		}

		static IPAddress GetIPfromHost(string p)
		{
			var hosts = Dns.GetHostAddresses(p);

			if (hosts == null || hosts.Length == 0) {
				throw new ArgumentException(string.Format("Host not found: {0}", p));
			}

			return hosts[0];
		}

    }
}
