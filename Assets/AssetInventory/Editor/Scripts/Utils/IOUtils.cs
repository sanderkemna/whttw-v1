﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_2021_2_OR_NEWER
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using CompressionType = SharpCompress.Common.CompressionType;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace AssetInventory
{
    public static class IOUtils
    {
        private const string LONG_PATH_PREFIX = @"\\?\";
        private const string LONG_PATH_UNC_PREFIX = @"\\?\UNC\";

        public static DriveInfo GetDriveInfoForPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return null;

            folderPath = Path.GetFullPath(folderPath);
            DriveInfo bestMatch = null;

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string root = drive.RootDirectory.FullName;
                if (folderPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    if (bestMatch == null || root.Length > bestMatch.RootDirectory.FullName.Length)
                    {
                        bestMatch = drive;
                    }
                }
            }
            if (bestMatch == null) Debug.LogError($"No drive found for the given path: {folderPath}");

            return bestMatch;
        }

        public static bool IsSameDrive(string path1, string path2)
        {
            DriveInfo drive1 = GetDriveInfoForPath(path1);
            DriveInfo drive2 = GetDriveInfoForPath(path2);
            return string.Equals(drive1.Name, drive2.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static long GetFreeSpace(string folderPath)
        {
            DriveInfo drive = GetDriveInfoForPath(folderPath);
            return drive.AvailableFreeSpace;
        }

        public static string NormalizeRelative(string path)
        {
            string[] parts = path.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            Stack<string> stack = new Stack<string>();
            foreach (string part in parts)
            {
                if (part == "..")
                {
                    stack.Pop();
                }
                else if (part != ".")
                {
                    stack.Push(part);
                }
            }
            return string.Join("/", stack.Reverse());
        }

        public static string ToLongPath(string path)
        {
            if (path == null) return null;

#if UNITY_EDITOR_WIN && UNITY_2020_2_OR_NEWER // support was only added in that Mono version
            // see https://learn.microsoft.com/en-us/answers/questions/240603/c-app-long-path-support-on-windows-10-post-1607-ne
            path = path.Replace("/", "\\"); // in case later concatenations added /
            if (path.StartsWith(LONG_PATH_PREFIX, StringComparison.Ordinal)) return path;
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                string withoutSlashes = path.Substring(2);
                return $"{LONG_PATH_UNC_PREFIX}{withoutSlashes}";
            }
            return $"{LONG_PATH_PREFIX}{path}";
#else
            return path;
#endif
        }

        public static string ToShortPath(string path)
        {
#if UNITY_EDITOR_WIN && UNITY_2020_2_OR_NEWER
            if (path == null) return null;

            // handle UNC long-path prefix \\?\UNC\server\share\…
            if (path.StartsWith(LONG_PATH_UNC_PREFIX, StringComparison.Ordinal))
            {
                string withoutUncPrefix = path.Substring(LONG_PATH_UNC_PREFIX.Length);
                string uncPath = @"\\" + withoutUncPrefix;
                return uncPath; // UNC paths must use backslashes
            }
            return path.Replace(LONG_PATH_PREFIX, string.Empty).Replace("\\", "/");
#else
            return path;
#endif
        }

        public static bool PathContainsInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }

        public static string RemoveInvalidChars(string path)
        {
            return string.Concat(path.Split(Path.GetInvalidFileNameChars()));
        }

        public static string MakeProjectRelative(string path)
        {
            if (path.Replace("\\", "/").StartsWith(Application.dataPath.Replace("\\", "/")))
            {
                return "Assets" + path.Substring(Application.dataPath.Length);
            }
            return path;
        }

        public static string CreateTempFolder()
        {
            string tempDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectoryPath);

            return tempDirectoryPath;
        }

        public static string CreateTempFolder(string name, bool deleteIfExists = false)
        {
            string tempDirectoryPath = Path.Combine(Path.GetTempPath(), name);
            if (deleteIfExists && Directory.Exists(tempDirectoryPath)) Directory.Delete(tempDirectoryPath, true);
            if (!Directory.Exists(tempDirectoryPath)) Directory.CreateDirectory(tempDirectoryPath);

            return tempDirectoryPath;
        }

        public static async Task<List<string>> FindMatchesInBinaryFile(string filePath, List<string> searchStrings, int bufferSize = 1048576)
        {
            HashSet<string> foundMatches = new HashSet<string>();
            byte[] buffer = new byte[bufferSize];

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
            {
                List<Task> searchTasks = new List<Task>();
                StringBuilder chunk = new StringBuilder();

                try
                {
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        chunk.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                        string chunkContent = chunk.ToString();

                        searchTasks.Add(Task.Run(() =>
                        {
                            Parallel.ForEach(searchStrings, searchString =>
                            {
                                if (chunkContent.IndexOf(searchString, StringComparison.Ordinal) >= 0)
                                {
                                    lock (foundMatches)
                                    {
                                        foundMatches.Add(searchString);
                                    }
                                }
                            });
                        }));

                        if (chunk.Length > bufferSize * 2)
                        {
                            chunk.Remove(0, chunk.Length - bufferSize);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error reading binary file '{filePath}': {e.Message}");
                }

                await Task.WhenAll(searchTasks);
            }

            return foundMatches.ToList();
        }

        public static string GetExtensionWithoutDot(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            string ext = Path.GetExtension(path);
            return string.IsNullOrEmpty(ext) ? string.Empty : ext.TrimStart('.');
        }

        public static string GetFileName(string path, bool returnOriginalOnError = true, bool quiet = true)
        {
            try
            {
                return Path.GetFileName(path);
            }
            catch (Exception e)
            {
                if (!quiet) Debug.LogError($"Illegal characters in path '{path}': {e}");
                return returnOriginalOnError ? path : null;
            }
        }

        public static string ReadFirstLine(string path)
        {
            string result = null;
            try
            {
                using (StreamReader reader = new StreamReader(ToLongPath(path)))
                {
                    result = reader.ReadLine();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading file '{path}': {e.Message}");
            }

            return result;
        }

        public static bool TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> DeleteFileOrDirectory(string path, int retries = 3)
        {
            bool success = false;
            while (retries >= 0)
            {
                try
                {
                    success = FileUtil.DeleteFileOrDirectory(path); // use Unity method to circumvent unauthorized access that can happen every now and then
                    break;
                }
                catch
                {
                    retries--;
                    if (retries >= 0) await Task.Delay(200);
                }
            }

            return success;
        }

        // Regex version
        public static IEnumerable<string> GetFiles(string path, string searchPatternExpression = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            return Directory.EnumerateFiles(path, "*", searchOption)
                .Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        // Takes multiple patterns and executes in parallel
        public static IEnumerable<string> GetFiles(string path, IEnumerable<string> searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (path == null) return Enumerable.Empty<string>();

            return searchPatterns.AsParallel()
                .SelectMany(searchPattern => Directory.EnumerateFiles(path, searchPattern, searchOption));
        }

        public static IEnumerable<string> GetFilesSafe(string rootPath, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories)
        {
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(rootPath);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Dequeue();
                string[] subDirs;
                string[] files;

                // Try to get files in the current directory
                try
                {
                    files = Directory.GetFiles(currentDir, searchPattern);
                }
                catch (Exception)
                {
                    // Skip this directory if access is denied
                    // Skip if the directory is not found
                    // Skip if timeout happens
                    continue;
                }

                foreach (string file in files)
                {
                    yield return file;
                }

                if (searchOption == SearchOption.TopDirectoryOnly) continue;

                // Try to get subdirectories
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string subDir in subDirs)
                {
                    dirs.Enqueue(subDir);
                }
            }
        }

        public static bool IsDirectoryEmpty(string path)
        {
            if (path == null) return true;
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public static bool IsSameDirectory(string path1, string path2)
        {
            DirectoryInfo di1 = new DirectoryInfo(path1);
            DirectoryInfo di2 = new DirectoryInfo(path2);

            return string.Equals(di1.FullName, di2.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public static void CopyDirectory(string sourceDir, string destDir, bool includeSubDirs = true)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDir);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDir, file.Name);
                file.CopyTo(tempPath, true);
            }

            if (includeSubDirs)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string tempPath = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, tempPath, includeSubDirs);
                }
            }
        }

        public static async Task<long> GetFolderSize(string folder, bool async = true)
        {
            if (!Directory.Exists(folder)) return 0;
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            try
            {
                if (async)
                {
                    // FIXME: this can crash Unity
                    return await Task.Run(() => dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length));
                }
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns a combined path with unified slashes
        /// </summary>
        /// <returns></returns>
        public static string PathCombine(params string[] path)
        {
            return Path.GetFullPath(Path.Combine(path));
        }

        public static string ExecuteCommand(string command, string arguments, string workingDirectory = "", bool waitForExit = true, bool createWindow = false)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, arguments)
            {
                RedirectStandardOutput = !createWindow,
                UseShellExecute = createWindow,
                CreateNoWindow = !createWindow,
                WorkingDirectory = workingDirectory
            };

            try
            {
                using (Process process = new Process {StartInfo = processStartInfo})
                {
                    process.Start();
                    string result = null;
                    if (!createWindow) result = process.StandardOutput.ReadToEnd();
                    if (waitForExit) process.WaitForExit();
                    return result;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing command '{command}': {e.Message}");
                return null;
            }
        }

#if UNITY_2021_2_OR_NEWER
        public static async Task<bool> DownloadFile(Uri uri, string targetFile)
        {
            UnityWebRequest request = UnityWebRequest.Get(uri);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
                return false;
            }

            byte[] data = request.downloadHandler.data;
            await File.WriteAllBytesAsync(targetFile, data);

            return true;
        }
#endif

        public static bool IsFirstArchiveVolume(string file)
        {
            string fileName = Path.GetFileName(file).ToLowerInvariant();
            if (fileName.EndsWith(".rar"))
            {
                Match match = Regex.Match(fileName, @"\.part(\d+)\.rar$");
                if (match.Success)
                {
                    int partNumber = int.Parse(match.Groups[1].Value);
                    return partNumber == 1;
                }
                return true;
            }
            return true;
        }

#if UNITY_2021_2_OR_NEWER
        public static void CompressFolder(string source, string target)
        {
            using FileStream zipStream = File.Create(target);
            WriterOptions options = new WriterOptions(CompressionType.Deflate);
            using IWriter writer = WriterFactory.Open(zipStream, ArchiveType.Zip, options);
            writer.WriteAll(source, "*", SearchOption.AllDirectories);
        }

        public static void CreateEmptyZip(string zipPath)
        {
            using FileStream zipStream = File.Create(zipPath);
            using IWriter writer = WriterFactory.Open(zipStream, ArchiveType.Zip, new WriterOptions(CompressionType.Deflate));
            // No entries added: creates an empty zip.
        }

        public static bool ExtractArchive(string archiveFile, string targetFolder, CancellationToken ct = default(CancellationToken))
        {
            Directory.CreateDirectory(targetFolder);

            try
            {
                using (IArchive archive = ArchiveFactory.Open(archiveFile))
                {
                    foreach (IArchiveEntry entry in archive.Entries)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            _ = DeleteFileOrDirectory(targetFolder);
                            return false;
                        }
                        if (string.IsNullOrEmpty(entry.Key)) continue;

                        if (!entry.IsDirectory)
                        {
                            try
                            {
                                string fullOutputPath = Path.Combine(targetFolder, entry.Key);
                                string directoryName = Path.GetDirectoryName(fullOutputPath);
                                Directory.CreateDirectory(directoryName);

                                entry.WriteToDirectory(targetFolder, new ExtractionOptions
                                {
                                    Overwrite = true,
                                    ExtractFullPath = true
                                });
                            }
                            catch (Exception e)
                            {
                                if (e is ArgumentException || e is IOException)
                                {
                                    // can happen for paths containing : and other illegal characters
                                    Debug.LogWarning($"Could not extract file '{entry.Key}' from archive '{archiveFile}': {e.Message}");
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not extract archive '{archiveFile}'. The process was potentially interrupted, the file is corrupted or the path too long: {e.Message}");
                return false;
            }

            return true;
        }
#endif
    }
}