using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace ResXManager.View.CustomActions
{
    public static class ResXRootProjectHelper
    {
        public const string ResXRootName = "_resxmanager";

        public static string? ResXManagerRootFile(string currentPath, int maxDepth = 5)
        {
            var dir = new DirectoryInfo(currentPath);

            while (maxDepth > 0 && dir.Parent != null)
            {
                var file = Path.Combine(dir.FullName, ResXRootName);
                if (File.Exists(file))
                    return file;
                dir = dir.Parent;
                maxDepth--;
            }
            return null;
        }

        public static bool HasResXManagerRoot(string currentPath, int maxDepth = 5)
        {
            return ResXManagerRootFile(currentPath, maxDepth) != null;
        }

        public static string? ResXManagerRootDir(string currentPath, int maxDepth = 5)
        {
            return Path.GetDirectoryName(ResXManagerRootFile(currentPath, maxDepth));
        }

        internal static bool CreateResxManagerRootFile()
        {
            if (MessageBox.Show($"At root of your project repo please create a \"{ResXRootName}\"" +
                                " for proper functioning of app", "Warning"
                                , MessageBoxButton.OKCancel, MessageBoxImage.Stop) == MessageBoxResult.OK)
            {
                SaveFileDialog openFileDialog = new()
                {
                    Filter = "ResXManager Project File | *.*",
                    FileName = ResXRootName
                };

                if (openFileDialog.ShowDialog().Value)
                {
                    File.WriteAllText(
                        Path.Combine(Path.GetDirectoryName(openFileDialog.FileName),
                        ResXRootName), string.Empty);
                    return true;
                }
            }

            return false;
        }
    }
}
