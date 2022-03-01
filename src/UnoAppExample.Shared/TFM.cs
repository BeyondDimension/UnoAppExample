using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace UnoAppExample
{
    /// <summary>
    /// .NET目标框架的属性。
    /// <para>https://docs.microsoft.com/zh-cn/dotnet/standard/frameworks</para>
    /// </summary>
    public struct TFM
    {
        static TFM()
        {
            NameDictionary = new Dictionary<Name, string>
            {
                { Name.Universal, ".NETCore" },
                { Name.Xamarin_iOS, "Xamarin.iOS" },
                { Name.MonoAndroid, "MonoAndroid" },
                { Name.NETFramework, ".NETFramework" },
                { Name.NETCoreApp, ".NETCoreApp" },
                { Name.NETStandard, ".NETStandard" },
            };
            Current = new TFM(Assembly.GetCallingAssembly());
            if (Current.FrameworkName == Name.NETFramework ||
                Current.FrameworkName == Name.MonoAndroid ||
                Current.FrameworkName == Name.Xamarin_iOS)
                IsRunningOnMono = Type.GetType("Mono.Runtime") != null;
        }

        /// <summary>
        /// 当前主进程的 .NET目标框架 。
        /// </summary>
        public static readonly TFM Current;

        /// <summary>
        /// 当前是否使用Mono运行时。
        /// </summary>
        public static readonly bool IsRunningOnMono;

        /// <summary>
        /// 当前主进程 是否运行在 .NET Framework 上。
        /// </summary>
        public static bool IsNETFramework => Current.FrameworkName == Name.NETFramework;

        /// <summary>
        /// 当前主进程 是否运行在 .NET Core 上。
        /// </summary>
        public static bool IsNETCore => Current.FrameworkName == Name.NETCoreApp;

        /// <summary>
        /// 当前主进程 是否运行在 Xamarin.Android 上。
        /// </summary>
        public static bool IsMonoAndroid => Current.FrameworkName == Name.MonoAndroid;

        /// <summary>
        /// 当前主进程 是否运行在 Xamarin.iOS 上。
        /// </summary>
        public static bool IsXamarin_iOS => Current.FrameworkName == Name.Xamarin_iOS;

        /// <summary>
        /// 当前主进程 是否运行在 .NET for Windows Universal / .NETCore,Version=v5.0 / UWP 上。
        /// </summary>
        public static bool IsUniversal => Current.FrameworkName == Name.Universal;

        /// <summary>
        /// .NET目标框架名称
        /// </summary>
        public Name FrameworkName { get; }

        /// <summary>
        /// .NET目标框架版本。
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// .NET目标框架显示名称，例如 ( .NET Framework 4.7.1 / Xamarin.Android v8.0 Support )。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// .NET目标框架显示名称是否有值。
        /// </summary>
        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        /// <summary>
        /// .NET目标框架的版本属性构造函数。
        /// </summary>
        /// <param name="frameworkName"></param>
        public TFM(string frameworkName) : this(frameworkName, null)
        {
        }

        /// <summary>
        /// .NET目标框架的版本属性构造函数。
        /// </summary>
        /// <param name="assembly"></param>
        public TFM(Assembly assembly) : this(assembly.GetCustomAttribute<TargetFrameworkAttribute>())
        {
        }

        /// <summary>
        /// .NET目标框架的版本属性构造函数。
        /// </summary>
        /// <param name="type"></param>
        public TFM(Type type) : this(type.Assembly)
        {
        }

        /// <summary>
        /// .NET目标框架的版本属性构造函数。
        /// </summary>
        /// <param name="attribute"></param>
        public TFM(TargetFrameworkAttribute attribute) : this(attribute.FrameworkName, attribute.FrameworkDisplayName)
        {
        }

        /// <summary>
        /// .NET目标框架的版本属性构造函数。
        /// </summary>
        /// <param name="frameworkName"></param>
        /// <param name="displayName"></param>
        public TFM(string frameworkName, string displayName)
        {
            const string vStartsWith = "Version=v";
            Version version = null;
            var array = Split(frameworkName);
            if (array?.Length >= 2)
            {
                FrameworkName = NameDictionary.FirstOrDefault(x => x.Value == array[0]).Key;
                if (FrameworkName == Name.Xamarin_iOS) // 未测试
                    version = SetVersionByFindSdkAssemblyVersionAttribute("Xamarin.iOS");
                if (FrameworkName == Name.MonoAndroid) // 未测试
                    version = SetVersionByFindSdkAssemblyVersionAttribute("Mono.Android");
                if (version == null)
                {
                    var versionString = array[1];
                    if (versionString.StartsWith(vStartsWith))
                    {
                        version = GetVersion(versionString.Substring(vStartsWith.Length, versionString.Length - vStartsWith.Length));
                    }
                }
            }
            else
            {
                FrameworkName = 0;
            }
            Version = version;
            DisplayName = displayName;
        }

        static string[] Split(string s, string separator = ",") => s?.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        static Version GetVersion(string s) => Version.TryParse(s, out var v) ? v : null;
        static Version SetVersionByFindSdkAssemblyVersionAttribute(string sdkAssemblyName)
        {
            var sdkAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == sdkAssemblyName);
            if (sdkAssembly == null) return default;
            var attr = sdkAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attr != null)
                return GetVersion(Split(attr.InformationalVersion).FirstOrDefault());
            return default;
        }

        ///// <summary>
        ///// $"[assembly: TargetFramework(\"{Name.GetString()},Version=v{Version}\", FrameworkDisplayName = \"{DisplayName}\")]"
        ///// </summary>
        ///// <returns></returns>
        //public string ToAssemblyAttributeString() =>
        //    $"[assembly: TargetFramework(\"{(NameDictionary.TryGetValue(FrameworkName, out var value) ? value : null)},Version=v{Version}\", FrameworkDisplayName = \"{DisplayName}\")]";

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string name;
            switch (FrameworkName)
            {
                case Name.NETFramework:
                    name = ".NET Framework";
                    break;

                case Name.NETCoreApp:
                    name = ".NET Core";
                    break;

                case Name.NETStandard:
                    name = ".NET Standard";
                    break;

                case Name.MonoAndroid:
                    name = "Xamarin.Android";
                    break;

                case Name.Xamarin_iOS:
                    name = "Xamarin.iOS";
                    break;

                case Name.Universal:
                    name = "Windows Universal";
                    break;

                default:
                    name = "Unknown";
                    break;
            }
            return name + " " + Version;
        }

        /// <summary>
        /// .NET目标框架名称。
        /// </summary>
        public enum Name : byte
        {
            /// <summary>
            /// .NET Framework / Mono
            /// </summary>
            NETFramework = 1,

            /// <summary>
            /// .NET Core / Asp.Net Core
            /// </summary>
            NETCoreApp = 2,

            /// <summary>
            /// .NET Standard
            /// </summary>
            NETStandard = 3,

            /// <summary>
            /// Xamarin.Android
            /// <para>https://developer.xamarin.com/releases/android</para>
            /// </summary>
            MonoAndroid = 4,

            /// <summary>
            /// Xamarin.iOS
            /// <para>https://developer.xamarin.com/releases/ios</para>
            /// </summary>
            Xamarin_iOS = 5,

            /// <summary>
            /// .NET for Windows Universal / .NETCore,Version=v5.0 / UWP
            /// </summary>
            Universal = 6,
        }

        internal static readonly Dictionary<Name, string> NameDictionary;
    }
}