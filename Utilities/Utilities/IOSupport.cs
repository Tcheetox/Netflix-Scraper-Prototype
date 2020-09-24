﻿using System;
using System.IO;
using System.Text;
using Utilities.Logger;

namespace Utilities
{
    public static class IOSupport
    {
        public static bool IsDirectoryExisting(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                return true;
            else return false;
        }

        /// <summary>
        /// Check if directory exists and is R&W accessible, otherwise try to create the directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static bool IsDirectoryWriteable(string directoryPath)
        {
            try
            {
                if (IsDirectoryExisting(directoryPath))
                {
                    string testFile = Path.Combine(directoryPath, "temporaryTestFile");
                    using (FileStream fs = File.Create(testFile))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes("This is some text in the file.");
                        fs.Write(info, 0, info.Length);
                    }
                    File.Delete(testFile);
                }
                else
                    Directory.CreateDirectory(directoryPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write(ex, $"An issue occured while assessing R&W permission in {directoryPath}", LogEntry.SeverityType.Medium);
                return false;
            }
        }
    }
}
