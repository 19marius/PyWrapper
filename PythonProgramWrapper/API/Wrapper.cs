using System.Windows.Controls;
using System.IO;

namespace Python.Wrapper
{
    public static class Wrapper
    {
        public static string Build(BuildConfiguration config)
        {
            if (!File.Exists(config.ModulePath)) throw new FileNotFoundException($"Module file '{config.ModulePath}' was not found.");
            if (!Directory.Exists(config.OutputPath)) throw new DirectoryNotFoundException($"Output folder '{config.OutputPath}' was not found.");

            Helpers.BuildPythonExe(null, 
                                   new Image() { Source = config.Icon }, 
                                   config.ModulePath, 
                                   config.OutputPath,
                                   config.NoConsole, 
                                   config.IsRelease);

            return Path.Combine(Path.GetDirectoryName(config.OutputPath), Path.GetFileNameWithoutExtension(config.ModulePath) + ".exe");
        }

        public static string QuickDebugBuild(string modulePath)
        {
            if (!File.Exists(modulePath)) throw new FileNotFoundException($"Module file '{modulePath} was not found.");

            return Build(new BuildConfiguration(modulePath, Path.GetDirectoryName(modulePath)));
        }

        public static string QuickReleaseBuild(string modulePath)
        {
            if (!File.Exists(modulePath)) throw new FileNotFoundException($"Module file '{modulePath} was not found.");

            return Build(new BuildConfiguration(modulePath, Path.GetDirectoryName(modulePath), release: true));
        }
    }
}