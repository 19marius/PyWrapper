using f = System.Windows.Forms;

using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System;

namespace Python.Wrapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        public static RoutedCommand BuildCmd = new RoutedCommand();

        Process lastProc;
        BitmapSource defIcon;
        internal static string initPath = null;

        public MainWindow()
        {
            // Preventively delete an existing leftover temp folder both on open & close.

            InitializeComponent();
            Helpers.DeleteTempFiles();
            Closing += (s, e) => Helpers.DeleteTempFiles();

            // Get a reference to the defualt icon to not have to call the helper method every time.
            // Also set the icon image.

            defIcon = Helpers.DefaultIcon();
            iconImg.Source = string.IsNullOrEmpty(Properties.Settings.Default.LastIcon) ? defIcon : Helpers.GetIcon(Regex.Replace(Properties.Settings.Default.LastIcon, @"[0-9]+$", ""), 
                                                                                                    int.Parse(Regex.Match(Properties.Settings.Default.LastIcon, @"[0-9]+$").Value));
            // initPath is set if the app is run with an argument.

            if (initPath != null)
            {
                Properties.Settings.Default.LastModulePath = initPath;
                Properties.Settings.Default.Save();
            }

            // If the default installation of Python is found, disable the ability to change it.

            ToolTipService.SetShowOnDisabled(pythonPathBtn, true);
            if (Helpers.GetPythonPath() != null) TogglePythonPathBtn(false);

            // Set all the text fields and check boxes.

            modulePathBox.Text = Properties.Settings.Default.LastModulePath;
            outputFolderBox.Text = Properties.Settings.Default.LastOutputPath;
            noConsoleCheck.IsChecked = Properties.Settings.Default.DoNoConsole;
            releaseCheck.IsChecked = Properties.Settings.Default.Release;
            contextCheck.IsChecked = Properties.Settings.Default.Context;
            shortcutCheck.IsChecked = Properties.Settings.Default.Shortcut;
            killCheck.IsChecked = Properties.Settings.Default.KillLastProc;
            runCheck.IsChecked = Properties.Settings.Default.RunImd;

            // Additional kill button checks

            if (!runCheck.IsChecked ?? false)
            {
                killCheck.Opacity = 1;
                killCheck.IsEnabled = true;
            }
            else
            {
                killCheck.Opacity = 0.5;
                killCheck.IsChecked = true;
                killCheck.IsEnabled = false;

                Properties.Settings.Default.KillLastProc = true;
                Properties.Settings.Default.Save();
            }

            // If the context menu entry already exists but control isn't checked, reset the context and check the control.

            if ((!contextCheck.IsChecked ?? false) && Helpers.ContextExists())
            {
                Helpers.RemoveFromContext();
                Helpers.AddToContext();

                contextCheck.IsChecked = true;
                Properties.Settings.Default.Context = true;
                Properties.Settings.Default.Save();
            }

            if ((shortcutCheck.IsChecked ?? false) && !Helpers.ShortcutExists())
            {
                shortcutCheck.IsChecked = false;
                Properties.Settings.Default.Shortcut = false;
                Properties.Settings.Default.Save();
            }

            if ((!shortcutCheck.IsChecked ?? false) && Helpers.ShortcutExists())
            {
                Helpers.RemoveShortcut();
                Helpers.AddShortcut();

                shortcutCheck.IsChecked = true;
                Properties.Settings.Default.Shortcut = true;
                Properties.Settings.Default.Save();
            }

            // Add hotkeys

            BuildCmd.InputGestures.Add(new KeyGesture(Key.F5));

            // Standard build/utility button checks.

            UpdateUtilityButtons();
            CheckBuildValidity();
        }


        private void UpdateUtilityButtons()
        {
            clearModuleBtn.IsEnabled = modulePathBox.Text != "...";
            copyModuleBtn.IsEnabled = modulePathBox.Text != "...";

            clearOutputBtn.IsEnabled = outputFolderBox.Text != "...";
        }

        private void CheckBuildValidity()
        {
            var unready = modulePathBox.Text == "..." || outputFolderBox.Text == "...";
            statusText.Text = unready ? "Waiting..." : "Ready";
            buildBtn.IsEnabled = !unready;
        }

        private void TogglePythonPathBtn(bool value)
        {
            pythonPathBtn.ToolTip = value ? "The executable uses the Python program found at this path.\nIf it is modified or removed, the executable won't run anymore, and you'll need to reselect Python's location" :
                                            "The default Python installation was found";

            pythonPathBtn.IsEnabled = value;
        }


        private void OnWindowFocus(object sender, EventArgs e)
        {
            modulePathBox.Text = File.Exists(modulePathBox.Text) ? modulePathBox.Text : "...";
            outputFolderBox.Text = Directory.Exists(outputFolderBox.Text) ? outputFolderBox.Text : "...";
            CheckBuildValidity();

            if (!pythonPathBtn.IsEnabled && Helpers.GetPythonPath() == null)
            {
                MessageBox.Show("The default installation of Python could no longer be found.\nSwitching to the manual installation.",
                                "PyWrapper",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                TogglePythonPathBtn(true);
            }
            else if (Helpers.GetPythonPath() != null) TogglePythonPathBtn(false);
        }


        private void OnBuildBtnClick(object sender, RoutedEventArgs e)
        {
            // Reset the status controls.
            
            progressBar.Value = 0;
            buildBtn.IsEnabled = false;
            statusText.Text = "Working...";

            // Replace all spaces in the selected module file and replace them with underscores.

            if (Regex.IsMatch(modulePathBox.Text, @"(?<=\\[^ ]*) +(?=[^ ]+)"))
            {
                modulePathBox.Text = Path.Combine(Path.GetDirectoryName(modulePathBox.Text), Path.GetFileName(modulePathBox.Text).Replace(' ', '_'));
                File.Move(Properties.Settings.Default.LastModulePath, modulePathBox.Text);

                Properties.Settings.Default.LastModulePath = modulePathBox.Text;
                Properties.Settings.Default.Save();
            }

            // Optionally kill the last process.

            if ((killCheck.IsChecked ?? false) && (!lastProc?.HasExited ?? false))
            {
                lastProc?.Kill();
                lastProc = null;
            }

            // Build the executable.

            Helpers.BuildPythonExe(progressBar,
                                   iconImg.Source == null || iconImg.Source == defIcon ? null : iconImg,
                                   modulePathBox.Text,
                                   outputFolderBox.Text,
                                   noConsoleCheck.IsChecked ?? false,
                                   releaseCheck.IsChecked ?? false);

            // Optionally run the executable after it was built.

            var out_path = Path.Combine(outputFolderBox.Text, Path.GetFileNameWithoutExtension(modulePathBox.Text) + ".exe");
            if (runCheck.IsChecked ?? false)
            {
                var startInfo = new ProcessStartInfo(out_path);
                
                startInfo.UseShellExecute = true;
                lastProc = Process.Start(startInfo);
            }

            // Reset the status controls (except the progress bar).

            statusText.Text = $"Done. Ready for new build.";
            buildBtn.IsEnabled = true;
        }


        private void OnModuleBtnClick(object sender, RoutedEventArgs e)
        {
            // Show the file dialog.

            var file = new f.OpenFileDialog()
            {
                Title = "Select Script",
                Filter = "Python source files|*.py",
                InitialDirectory = modulePathBox.Text == "..." ? App.Location : modulePathBox.Text,
                CheckPathExists = true,
                CheckFileExists = true,
                Multiselect = false
            };

            if (file.ShowDialog() != f.DialogResult.OK) return;

            // If the dialog wasn't canceled, set and save the path.

            modulePathBox.Text = string.IsNullOrEmpty(file.FileName) ? "..." : file.FileName;
            if (modulePathBox.Text != "...") Properties.Settings.Default.LastModulePath = modulePathBox.Text;
            Properties.Settings.Default.Save();

            UpdateUtilityButtons();
            CheckBuildValidity();
        }

        private void OnOutputBtnClick(object sender, RoutedEventArgs e)
        {
            // Show the dialog.

            var folder = new f.FolderBrowserDialog() 
            { 
                ShowNewFolderButton = true,
                Description = "Select the folder where you wish for the executable to be placed in."
            };
            var result = folder.ShowDialog();
            if (result != f.DialogResult.OK) return; 
                
            // If the dialog wasn't canceled, set and save the path.

            outputFolderBox.Text = string.IsNullOrEmpty(folder.SelectedPath) ? "..." : folder.SelectedPath;

            if (outputFolderBox.Text != "...") Properties.Settings.Default.LastOutputPath = outputFolderBox.Text;
            Properties.Settings.Default.Save();

            UpdateUtilityButtons();
            CheckBuildValidity();
        }


        private void OnClearModuleClick(object sender, RoutedEventArgs e)
        {
            modulePathBox.Text = "...";
            UpdateUtilityButtons();
            CheckBuildValidity();
        }

        private void OnClearOutputClick(object sender, RoutedEventArgs e)
        {
            outputFolderBox.Text = "...";
            UpdateUtilityButtons();
            CheckBuildValidity();
        }

        private void OnCopyModuleClick(object sender, RoutedEventArgs e)
        {
            outputFolderBox.Text = Path.GetDirectoryName(modulePathBox.Text);
            Properties.Settings.Default.LastOutputPath = outputFolderBox.Text;
            Properties.Settings.Default.Save();

            UpdateUtilityButtons();
            CheckBuildValidity();
        }


        private void OnSetIconBtnClick(object sender, RoutedEventArgs e)
        {
            // Show the icon dialog starting from the path of the last picked dialog.

            int index = 0;
            string dir = Regex.Replace(Properties.Settings.Default.LastIcon ?? "", @"[0-9]+$", "");
            var src = Helpers.PickIconDialog(ref dir, out index);

            if (src == null) return;

            // Set and save the icon data (path, index) and show the icon.

            Properties.Settings.Default.LastIcon = dir + " " + index;
            Properties.Settings.Default.Save();

            iconImg.Source = src;
        }

        private void OnClearIcon(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LastIcon = null;
            Properties.Settings.Default.Save();

            iconImg.Source = defIcon;
        }


        private void OnReleaseClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Release = releaseCheck.IsChecked ?? false;
            Properties.Settings.Default.Save();
        }

        private void OnNoConsoleClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DoNoConsole = noConsoleCheck.IsChecked ?? false;
            Properties.Settings.Default.Save();
        }

        private void OnRunImdClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RunImd = runCheck.IsChecked ?? false;
            Properties.Settings.Default.Save();

            if (!runCheck.IsChecked ?? false)
            {
                killCheck.Opacity = 1;
                killCheck.IsEnabled = true;
            }
            else
            {
                killCheck.Opacity = 0.5;
                killCheck.IsChecked = true;
                killCheck.IsEnabled = false;

                Properties.Settings.Default.KillLastProc = true;
                Properties.Settings.Default.Save();
            }
        }

        private void OnKillClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.KillLastProc = killCheck.IsChecked ?? false;
            Properties.Settings.Default.Save();
        }


        private void OnContextClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Context = contextCheck.IsChecked ?? false;
            Properties.Settings.Default.Save();

            if (contextCheck.IsChecked ?? false) Helpers.AddToContext();
            else Helpers.RemoveFromContext();
        }

        private void OnShortcutClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Shortcut = shortcutCheck.IsChecked ?? false;
            Properties.Settings.Default.Save();

            if (shortcutCheck.IsChecked ?? false) Helpers.AddShortcut();
            else Helpers.RemoveShortcut();
        }

        private void OnPythonPathClick(object sender, RoutedEventArgs e)
        {
            var path = Helpers.SelectPythonPath();
            if (path == null) return;

            Properties.Settings.Default.PythonPath = path;
            Properties.Settings.Default.Save();
        }


        private void OnBuildHotkey(object sender, RoutedEventArgs e)
        {
            if (!buildBtn.IsEnabled) return;

            OnBuildBtnClick(null, null);
        }
    }
}
