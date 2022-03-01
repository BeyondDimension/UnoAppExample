using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Linq;
using System.Net.Sockets;
#if WINDOWS_UWP || WINDOWS
using Microsoft.Win32;
#endif
#if WINDOWS
using System.Management;
#endif

namespace UnoAppExample.ViewModels
{
    public sealed class MainPageViewModel : BaseViewModel
    {
        const string Loading = "Loading...";

        string mOSVersion = Loading;
        public string OSVersion
        {
            get => mOSVersion;
            set
            {
                if (mOSVersion == value) return;
                mOSVersion = value;
                OnPropertyChanged();
            }
        }

        string mMachineName = Loading;
        public string MachineName
        {
            get => mMachineName;
            set
            {
                if (mMachineName == value) return;
                mMachineName = value;
                OnPropertyChanged();
            }
        }

        string mRuntime = Loading;
        public string Runtime
        {
            get => mRuntime;
            set
            {
                if (mRuntime == value) return;
                mRuntime = value;
                OnPropertyChanged();
            }
        }

        string mFramework = Loading;
        public string Framework
        {
            get => mFramework;
            set
            {
                if (mFramework == value) return;
                mFramework = value;
                OnPropertyChanged();
            }
        }

        string mIsWindowsServer = Loading;
        public string IsWindowsServer
        {
            get => mIsWindowsServer;
            set
            {
                if (mIsWindowsServer == value) return;
                mIsWindowsServer = value;
                OnPropertyChanged();
            }
        }

        string mIPAddresses = Loading;
        public string IPAddresses
        {
            get => mIPAddresses;
            set
            {
                if (mIPAddresses == value) return;
                mIPAddresses = value;
                OnPropertyChanged();
            }
        }

        string mCPU = Loading;
        public string CPU
        {
            get => mCPU;
            set
            {
                if (mCPU == value) return;
                mCPU = value;
                OnPropertyChanged();
            }
        }

        string mGPU = Loading;
        public string GPU
        {
            get => mGPU;
            set
            {
                if (mGPU == value) return;
                mGPU = value;
                OnPropertyChanged();
            }
        }

        string mTargetFramework = Loading;
        public string TargetFramework
        {
            get => mTargetFramework;
            set
            {
                if (mTargetFramework == value) return;
                mTargetFramework = value;
                OnPropertyChanged();
            }
        }

        public MainPageViewModel()
        {
            Task.Run(() =>
            {
                MachineName = Environment.MachineName;
                Runtime = RuntimeInformation.FrameworkDescription;

                var s = new[] { NetworkInterfaceType.Ethernet, NetworkInterfaceType.Wireless80211 };
                var allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var query = (from m in allNetworkInterfaces
                             let type_filter = s.Contains(m.NetworkInterfaceType)
                             let ips = m.GetIPProperties()?.UnicastAddresses
                             where type_filter && m.OperationalStatus == OperationalStatus.Up && (ips?.Any() ?? false)
                             select ips).SelectMany(m => m).Where(m => m.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(m => m.Address.ToString());

                IPAddresses = string.Join(";", query.Where(x => !string.IsNullOrWhiteSpace(x)));

#if WINDOWS_UWP || WINDOWS
                IsWindowsServer = IsOS(OS_ANYSERVER).ToString().ToLowerInvariant();
#else
                IsWindowsServer = "false";
#endif
                OSVersion = GetOSVersion();
                Framework = GetFramework();
                TargetFramework = TFM.Current.ToString();
                CPU = GetCPUName();
                GPU = GetGPUName();
            });
        }

#if WINDOWS
        static string GetValueByManagementClass(string managementClassPath, string propertiesName)
        {
            using (var managementClass = new ManagementClass(managementClassPath))
            using (var instances = managementClass.GetInstances())
            {
                string cache = null;
                foreach (var instance in instances)
                {
#if DEBUG
                    var debug = instance.Properties.OfType<PropertyData>().ToDictionary(k => k.Name, v => v.Value);
#endif
                    var temp = instance.Properties[propertiesName].Value?.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(temp)) continue;
                    if (temp.Length < cache?.Length)
                    {
                        continue;
                    }
                    cache = temp;
                }
                return cache;
            }
        }
#endif

#if WINDOWS_UWP || WINDOWS
        const int OS_ANYSERVER = 29;

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/bb773795%28v=VS.85%29.aspx?f=255&MSPPError=-2147217396
        /// </summary>
        /// <param name="os"></param>
        /// <returns></returns>
        [DllImport("shlwapi.dll", SetLastError = true, EntryPoint = "#437")]
        static extern bool IsOS(int os);

        // Checking the version using >= enables forward compatibility.
        static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
                return "4.8 or later";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }
#endif

        static string GetOSVersion()
        {
#if WINDOWS_UWP || WINDOWS
            try
            {
                const string subkey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
                using (var ndpKey = Registry.LocalMachine.OpenSubKey(subkey))
                {
                    var productName = ndpKey?.GetValue("ProductName")?.ToString();
                    var major = Environment.OSVersion.Version.Major.ToString();
                    var minor = Environment.OSVersion.Version.Minor.ToString();
                    var build = Environment.OSVersion.Version.Build.ToString();
                    var revision = ndpKey?.GetValue("UBR")?.ToString();
                    if (string.IsNullOrEmpty(revision))
                    {
                        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
                            "kernel32.dll");
                        revision = File.Exists(path)
                            ? FileVersionInfo.GetVersionInfo(path).ProductPrivatePart.ToString()
                            : null;
                    }
                    var releaseId = ndpKey?.GetValue("ReleaseId")?.ToString();
                    var buildBranch = ndpKey?.GetValue("BuildBranch")?.ToString().Replace("_release", string.Empty).ToUpperInvariant();
                    var servicePack = Environment.OSVersion.ServicePack;
                    string additional = string.IsNullOrEmpty(servicePack) ? null : " " + servicePack;
                    if (releaseId != null || buildBranch != null)
                        additional += " (" + string.Join(" ", new[] { releaseId, buildBranch }.Where(x => x != null)) + ")";
                    return $"{productName} {major}.{minor}.{build}{(string.IsNullOrEmpty(revision) ? null : "." + revision)}{additional}";
                }
            }
            catch
            {
            }
#endif
            return Environment.OSVersion.VersionString;
        }

        static string GetFramework()
        {
#if WINDOWS_UWP || WINDOWS
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return $".NET Framework {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}";
                }
            }
#endif
            return string.Empty;
        }

        static string GetCPUName()
        {
#if WINDOWS
            return GetValueByManagementClass("Win32_Processor", "Name");
#else
            return string.Empty;
#endif
        }

        static string GetGPUName()
        {
#if WINDOWS
            return GetValueByManagementClass("Win32_VideoController", "Name");
#else
            return string.Empty;
#endif
        }
    }
}
