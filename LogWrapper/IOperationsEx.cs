using System.IO;

namespace LogWrapper
{
    /// <summary>
    /// <c>IOperationsEx</c>
    /// </summary>
    internal static class IOperationsEx
    {
        /// <summary>
        /// Copy file from provided <paramref name="pathToFile"/> to temporary location and name and return absolute path to it
        /// </summary>
        /// <param name="pathToFile">The path to file.</param>
        /// <returns>Absolute path to temp file</returns>
        internal static string BackupCopy(this string pathToFile)
        {
            var targetPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            File.Copy(pathToFile, targetPath);

            return targetPath;
        }
    }
}
