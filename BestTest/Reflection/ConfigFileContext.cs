// BestTest: test better than using MSTest
// https://github.com/ArxOne/BestTest
// MIT license, blah blah blah.

namespace BestTest.Reflection
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Allows to use a specific context
    /// https://stackoverflow.com/questions/6150644/change-default-app-config-at-runtime/6151688#6151688
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class ConfigFileContext : IDisposable
    {
        private readonly string _previousConfigFile;
        private readonly bool _set;
        private string _currentConfigFile;

        public ConfigFileContext(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                return;

            var currentConfigFile = assemblyPath + ".config";
            _currentConfigFile = currentConfigFile;
            if (!File.Exists(_currentConfigFile))
                return;

            _previousConfigFile = (string)AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE");

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", _currentConfigFile);
            _set = true;
            ResetContext(_currentConfigFile);
        }

        public ConfigFileContext(Assembly assembly) : this(assembly.Location)
        {
        }

        public void Dispose()
        {
            if (!_set)
                return;

            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", _previousConfigFile);
            ResetContext(_previousConfigFile);
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var directory = Path.GetDirectoryName(_currentConfigFile);
            return TryLoad(directory, args.Name + ".dll") ?? TryLoad(directory, args.Name + ".exe");
        }

        private static Assembly TryLoad(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            if (!File.Exists(path))
                return null;
            return Assembly.LoadFrom(path);
        }

        private void ResetContext(string configFilePath)
        {
            if (Type.GetType("Mono.Runtime") != null)
                ResetMonoContext(configFilePath);
            else
                ResetWindowsContext();
        }

        private static void ResetWindowsContext()
        {
            var configurationManagerType = typeof(ConfigurationManager);
            var initStateField = configurationManagerType.GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static);
            initStateField?.SetValue(null, 0);

            var configSystemField = configurationManagerType.GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static);
            configSystemField?.SetValue(null, null);

            var clientConfigPathsType = configurationManagerType.Assembly.GetTypes().FirstOrDefault(t => t.FullName == "System.Configuration.ClientConfigPaths");
            var currentField = clientConfigPathsType?.GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static);
            currentField?.SetValue(null, null);

            // because for the XML serializer, the settings must be loaded (WTF, right dude, WTF)
            var settings = ConfigurationManager.AppSettings.Count;
        }

        private void ResetMonoContext(string configFilePath)
        {
            // totally untested
            var newConfiguration = ConfigurationManager.OpenExeConfiguration(configFilePath);
            var configSystemField = typeof(ConfigurationManager).GetField("configSystem", BindingFlags.NonPublic | BindingFlags.Static);
            var configSystem = configSystemField?.GetValue(null);
            var cfgField = configSystem?.GetType().GetField("cfg", BindingFlags.Instance | BindingFlags.NonPublic);
            cfgField?.SetValue(configSystem, newConfiguration);
        }
    }
}
