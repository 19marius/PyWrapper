using System.Windows.Media.Imaging;
using System.IO;

namespace Python.Wrapper
{
    public struct BuildConfiguration
    {
        public string ModulePath { get; }

        public string OutputPath { get; }

        public string IconPath { get; }

        public bool NoConsole { get; }

        public bool IsRelease { get; }

        public BitmapSource Icon { get; }

        public BuildConfiguration(string module,
                                  string output,
                                  string iconPath = null,
                                  bool noConsole = false, 
                                  bool release = false)
        {
            ModulePath = module;
            OutputPath = output;
            IconPath = iconPath;
            NoConsole = noConsole;
            IsRelease = release;
            Icon = null;

            if (IconPath != null && (Icon = Helpers.GetIcon(iconPath, 0)) == null)
                throw new FileNotFoundException($"Icon file '{iconPath}' was not found.");
        }
    }
}
