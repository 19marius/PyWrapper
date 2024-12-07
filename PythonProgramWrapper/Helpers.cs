using static Python.Wrapper.PInvoke;

using i = IWshRuntimeLibrary;
using d = System.Drawing;

using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Drawing.Imaging;
using System.Windows.Interop;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace Python.Wrapper
{
    /// <summary>
    /// Archive for core utility methods.
    /// </summary>
    internal static class Helpers
    {
        public static string ReadTextResourceFile(string path)
        {
            using (var sr = new StreamReader(Application.GetResourceStream(new Uri($"pack://application:,,,/{path}")).Stream)) 
                return sr.ReadToEnd();
        }

        public static void SaveImage(Image img, string path)
        {
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(img.Source as BitmapSource));
            using (var s = File.Create(path)) png.Save(s);
        }


        public static void BuildPythonExe(ProgressBar bar, Image icon, string module, string output, bool noConsole, bool release)
        {
            // Set up the .cs and the .csproj files.

            var source = ReadTextResourceFile("templates/pytemplate.txt").Replace("<MODULE_PATH>", module)
                                                           .Replace("<IS_CONSOLE>", (!noConsole).ToString().ToLower())
                                                           .Replace("<DEBUG_AFTER>", !release && !noConsole ? "Console.WriteLine(\"Press any key to continue . . .\"); Console.ReadKey(true);" : "");
            var proj = ReadTextResourceFile("templates/" + (icon == null || icon.Source == null ? "projtemplate.txt" : "projtemplate_icon.txt")).Replace("<DO_NO_CONSOLE>", noConsole ? "WinExe" : "Exe");

            if (bar != null) bar.Value = 20;

            // Write them to the temp folder to prepare for building.

            DeleteTempFiles();

            var cscpath = App.Location + @"\msbuild";
            File.WriteAllText(cscpath + @"\Program.cs", source);
            File.WriteAllText(cscpath + @"\temp.csproj", proj);

            // Additionally, save the icon to disk.

            if (icon != null && icon.Source != null)
            {
                SaveImage(icon, cscpath + @"\icon.png");
                ConvertToIcon(cscpath + @"\icon.png", cscpath + @"\icon.ico");
                File.Delete(cscpath + @"\icon.png");
            }

            if (bar != null) bar.Value = 30;

            // Prepare to run cmd to build the project.

            var cmdInfo = new ProcessStartInfo()
            {
                WorkingDirectory = cscpath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                FileName = cscpath + @"\MSBuild.exe",
            };

            // Run "dotnet build (--configuration Release)".

            using (var p = Process.Start(cmdInfo)) p.WaitForExit();

            if (bar != null) bar.Value = 80;

            // Obtain the path where to move the executable and move it from the temp folder.

            var out_file = Path.Combine(output, Path.GetFileNameWithoutExtension(module) + ".exe");

            if (File.Exists(out_file)) File.Delete(out_file);
            File.Move(cscpath + $@"\bin\{(release ? "Release" : "Debug")}\PythonRunner.exe", out_file);

            if (bar != null) bar.Value = 95;

            // Clean up by deleting the temp folder.

            DeleteTempFiles();

            if (bar != null) bar.Value = 100;
        }

        public static void DeleteTempFiles()
        {
            var cscpath = App.Location + @"\msbuild";

            if (Directory.Exists(cscpath + @"\bin")) Directory.Delete(cscpath + @"\bin", true);
            if (File.Exists(cscpath + @"\Program.cs")) File.Delete(cscpath + @"Program.cs");
            if (File.Exists(cscpath + @"\temp.csproj")) File.Delete(cscpath + @"\temp.csproj");
            if (File.Exists(cscpath + @"\icon.ico")) File.Delete(cscpath + @"\icon.ico");
        }


        public static BitmapSource PickIconDialog(ref string initialDir, out int index)
        {
            int retval;
            index = -1;

            var iconfile = string.IsNullOrEmpty(initialDir) ? Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\shell32.dll" : initialDir;
            var sb = new StringBuilder(iconfile, 0x00000104);

            retval = PickIconDlg(IntPtr.Zero, sb, sb.MaxCapacity, ref index);

            if (retval == 0) return null;

            // Extract the icon

            var largeIcons = new IntPtr[1];
            var smallIcons = new IntPtr[1];

            ExtractIconEx(initialDir = sb.ToString(), index, largeIcons, smallIcons, 1);

            // Convert icon handle to ImageSource

            var src = Imaging.CreateBitmapSourceFromHIcon(largeIcons[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // clean up
            DestroyIcon(largeIcons[0]);
            DestroyIcon(smallIcons[0]);

            return src;
        }

        public static BitmapSource GetIcon(string path, int index)
        {
            var largeIcons = new IntPtr[1];
            var smallIcons = new IntPtr[1];

            ExtractIconEx(path, index, largeIcons, smallIcons, 1);
            if (largeIcons[0] == IntPtr.Zero || smallIcons[0] == IntPtr.Zero) return null;

            var src = Imaging.CreateBitmapSourceFromHIcon(largeIcons[0] == IntPtr.Zero ? smallIcons[0] : largeIcons[0], 
                                                          Int32Rect.Empty, 
                                                          BitmapSizeOptions.FromEmptyOptions());

            DestroyIcon(largeIcons[0]);
            DestroyIcon(smallIcons[0]);

            return src;
        }

        public static BitmapSource DefaultIcon()
        {
            return GetIcon(@"C:\Windows\System32\Shell32.dll", 2);
        }


        public static bool ConvertToIcon(string inputPath, string outputPath, int size = 16, bool preserveAspectRatio = false)
        {
            using (FileStream inputStream = new FileStream(inputPath, FileMode.Open))
            using (FileStream outputStream = new FileStream(outputPath, FileMode.OpenOrCreate))

                return ConvertToIcon(inputStream, outputStream, size, preserveAspectRatio);
        }

        public static bool ConvertToIcon(Stream input, Stream output, int size = 16, bool preserveAspectRatio = false)
        {
            var inputBitmap = (d.Bitmap) d.Bitmap.FromStream(input);
            if (inputBitmap == null) return false;

            int width, height;
            if (preserveAspectRatio)
            {
                width = size;
                height = inputBitmap.Height / inputBitmap.Width * size;
            }
            else width = height = size;

            var newBitmap = new d.Bitmap(inputBitmap, new d.Size(width, height));
            if (newBitmap == null) return false;

            using (var memoryStream = new MemoryStream())
            {
                newBitmap.Save(memoryStream, ImageFormat.Png);

                var iconWriter = new BinaryWriter(output);
                if (output == null || iconWriter == null) return false;

                    // 0-1 reserved, 0
                iconWriter.Write((byte)0);
                iconWriter.Write((byte)0);

                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((short)1);

                // 4-5 number of images
                iconWriter.Write((short)1);

                // image entry 1
                // 0 image width
                iconWriter.Write((byte)width);
                // 1 image height
                iconWriter.Write((byte)height);

                // 2 number of colors
                iconWriter.Write((byte)0);

                // 3 reserved
                iconWriter.Write((byte)0);

                // 4-5 color planes
                iconWriter.Write((short)0);

                // 6-7 bits per pixel
                iconWriter.Write((short)32);

                // 8-11 size of image data
                iconWriter.Write((int)memoryStream.Length);

                // 12-15 offset of image data
                iconWriter.Write((int)(6 + 16));

                // write image data
                // png data must contain the whole png data file
                iconWriter.Write(memoryStream.ToArray());
                iconWriter.Flush();

                return true;
            }
        }


        public static void AddToContext()
        {
            // Add the "Quick debug build" option.

            var reg = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", true);
            reg = reg.CreateSubKey("q.pywrapper");
            reg.SetValue("", "Quick debug build", RegistryValueKind.String);
            reg.SetValue("Icon", Assembly.GetExecutingAssembly().Location + ",0", RegistryValueKind.String);
            reg.CreateSubKey("command").SetValue("", $"\"{Assembly.GetExecutingAssembly().Location}\" \"q %1");
            reg.Close();

            // Add the "Build" option.

            reg = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", true);
            reg = reg.CreateSubKey("pywrapper");
            reg.SetValue("", "Build", RegistryValueKind.String);
            reg.SetValue("Icon", Assembly.GetExecutingAssembly().Location + ",0", RegistryValueKind.String);
            reg.CreateSubKey("command").SetValue("", $"\"{Assembly.GetExecutingAssembly().Location}\" \"%1");
            reg.Close();
        }
    
        public static void RemoveFromContext()
        {
            // The program only uses the keys "q.pywrapper" and "pywrapper".

            var reg = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", true);
            reg.DeleteSubKeyTree("q.pywrapper");
            reg.DeleteSubKeyTree("pywrapper");
            reg.Close();
        }

        public static bool ContextExists()
        {
            using (var r = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell"))
                return r.GetSubKeyNames().FirstOrDefault(x => x == "pywrapper")?.Any() ?? false;
        }


        public static void AddShortcut()
        {
            var exepath = App.Location + @"\pywrapper.exe";
            var programsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                                                "pywrapper");

            if (!Directory.Exists(programsDir)) Directory.CreateDirectory(programsDir);

            var shortcutLocation = Path.Combine(programsDir, "PyWrapper" + ".lnk");

            var shortcut = (i.IWshShortcut) new i.WshShell().CreateShortcut(shortcutLocation);
            shortcut.TargetPath = exepath;
            shortcut.Save();
        }

        public static void RemoveShortcut()
        {
            var programsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "pywrapper");
            if (Directory.Exists(programsDir)) Directory.Delete(programsDir, true);
        }

        public static bool ShortcutExists()
        {
            return Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "pywrapper"));
        }


        public static string GetPythonPath(bool system = false)
        {
            var folder = Environment.GetEnvironmentVariable("Path", system ? EnvironmentVariableTarget.Machine :
                                                                              EnvironmentVariableTarget.User)
                                     .Split(';')
                                     .FirstOrDefault(x => Regex.IsMatch(x, @"^.+\\Python\\Python[0-9]+\\$"));

            return string.IsNullOrEmpty(folder) ? null : folder + "python.exe";
        }
    }

    /// <summary>
    /// Archive for Win32 functions.
    /// </summary>
    public static class PInvoke
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern int PickIconDlg(IntPtr hwndOwner, System.Text.StringBuilder lpstrFile, int nMaxFile, ref int lpdwIconIndex);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}