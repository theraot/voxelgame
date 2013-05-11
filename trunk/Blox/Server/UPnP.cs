//using System;
//using System.Runtime.InteropServices;

//namespace Hexpoint.Blox.Server
//{
//    internal class UPnP : IDisposable
//    {
//        private readonly NATUPNPLib.UPnPNAT _upnpnat;
//        private readonly NATUPNPLib.IStaticPortMappingCollection _staticMapping;

//        internal enum Protocol
//        {
//            TCP,
//            UDP
//        }

//// ReSharper disable InconsistentNaming
//        public bool UPnPEnabled { get; private set; }
//// ReSharper restore InconsistentNaming

//        internal UPnP()
//        {
//            _upnpnat = new NATUPNPLib.UPnPNAT();

//            try
//            {
//                _staticMapping = _upnpnat.StaticPortMappingCollection;
//                if (_staticMapping != null) UPnPEnabled = true;
//            }
//            catch (NotImplementedException)
//            {
//                UPnPEnabled = false;
//            }
//        }

//        internal void Add(string localIp, int port, Protocol prot, string desc)
//        {
//            if (!IsPrivateIp(localIp)) throw new ArgumentException("This is not a local IP");

//            _staticMapping.Add(port, prot.ToString(), port, localIp, true, desc);
//        }

//        internal bool Exists(int port, Protocol prot)
//        {
//            if (!UPnPEnabled) throw new ApplicationException("UPnP is not enabled, or there was an error with UPnP Initialization.");

//            if (_staticMapping.Count == 0) return false;

//            foreach (NATUPNPLib.IStaticPortMapping mapping in _staticMapping)
//            {
//                if (mapping.ExternalPort.Equals(port) && mapping.Protocol.Equals(prot.ToString())) return true;
//            }

//            return false;
//        }

//        internal static string LocalIp()
//        {
//            System.Net.IPHostEntry ipList = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
//            foreach (var ipAddress in ipList.AddressList)
//            {
//                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && IsPrivateIp(ipAddress.ToString()))
//                    return ipAddress.ToString();
//            }
//            return string.Empty;
//        }

//        private static bool IsPrivateIp(string checkIp)
//        {
//            var quad1 = int.Parse(checkIp.Substring(0, checkIp.IndexOf('.')));
//            var quad2 = int.Parse(checkIp.Substring(checkIp.IndexOf('.') + 1).Substring(0, checkIp.IndexOf('.')));

//            switch (quad1)
//            {
//                case 10:
//                    return true;
//                case 172:
//                    if (quad2 >= 16 && quad2 <= 31) return true;
//                    break;
//                case 192:
//                    if (quad2 == 168) return true;
//                    break;
//            }

//            return false;
//        }

//// ReSharper disable UnusedParameter.Local
//        private void Dispose(bool disposing)
//// ReSharper restore UnusedParameter.Local
//        {
//            Marshal.ReleaseComObject(_staticMapping);
//            Marshal.ReleaseComObject(_upnpnat);
//        }

//        void IDisposable.Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }
//    }
//}