using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace Python.Wrapper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    internal partial class App : Application
    {
        public static string Location { get; private set; }

        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            Location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if ((Helpers.GetPythonPath() ?? Helpers.GetPythonPath(true)) == null)
            {
                MessageBox.Show("No Python installation was not found on this system.",
                                "PyWrapper",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                Environment.Exit(0);
            }

            using (var r = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*", true))
                if (!r.GetSubKeyNames().Contains("shell")) r.CreateSubKey("shell");

            if (e.Args.Length == 0) return;

            if (!e.Args[0].EndsWith(".py")) Environment.Exit(0);

            if (e.Args[0].StartsWith("q "))
            {
                try
                {
                    Wrapper.QuickDebugBuild(Regex.Replace(e.Args[0], @"^q ", ""));
                    MessageBox.Show("The build was successful.", "PyWrapper", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The build was cancelled becuase of an internal error.\n\n" +
                                    string.Join("", Enumerable.Repeat('-', 70)) + "\n\n" +
                                    $"The exception is of type {ex.GetType().Name}.\n" +
                                    $"Error message: \"{ex.Message}\"\n\n" +
                                    $"Stack Trace:\n\n{ex.StackTrace}",
                                    "PyWrapper",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }

                Environment.Exit(0);
            }

            Python.Wrapper.MainWindow.initPath = e.Args[0];
        }
    }
}
