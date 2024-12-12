using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.Win32;
using System.Windows;
using System.Linq;
using System.IO;
using System;

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
            Current.DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show("The build was cancelled becuase of an internal error.\n\n" +
                                 string.Join("", Enumerable.Repeat('-', 70)) + "\n\n" +
                                 $"The exception is of type {ex.Exception.GetType().Name}.\n" +
                                 $"Error message: \"{ex.Exception.Message}\"\n\n" +
                                 $"Stack Trace:\n\n{ex.Exception.StackTrace}",
                                 "PyWrapper",
                                 MessageBoxButton.OK,
                                 MessageBoxImage.Error);

                Helpers.DeleteTempFiles();
                ex.Handled = true;
            };

            if (Helpers.GetPythonPath() == null && 
                string.IsNullOrEmpty(Python.Wrapper.Properties.Settings.Default.PythonPath))
            {
                var result = MessageBox.Show("PyWrapper was not able to find a Python installation on this device.\n\nIf you have Python installed, do you wish to select Python's location manually?",
                                             "PyWrapper",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Error);

                if (result != MessageBoxResult.Yes) Environment.Exit(0);

                if ((Python.Wrapper.Properties.Settings.Default.PythonPath = Helpers.SelectPythonPath()) == null) Environment.Exit(0);
                Python.Wrapper.Properties.Settings.Default.Save();
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
