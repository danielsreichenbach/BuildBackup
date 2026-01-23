using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BuildBackup
{
    /// <summary>
    /// Validates and sanitizes file paths to prevent path traversal attacks.
    /// </summary>
    public static class PathValidator
    {
        private static readonly Regex HexHashPattern = new Regex(@"^[a-fA-F0-9]+$", RegexOptions.Compiled);

        /// <summary>
        /// Validates that a path does not contain path traversal sequences.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when path contains traversal sequences.</exception>
        public static void ValidateNoTraversal(string path, string paramName)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", paramName);

            // Check for path traversal patterns
            if (path.Contains("..") ||
                path.Contains("~") ||
                path.StartsWith("/") ||
                (path.Length >= 2 && path[1] == ':')) // Windows absolute path
            {
                // Get the normalized path and check if it's trying to escape
                var normalized = Path.GetFullPath(path);
                var current = Path.GetFullPath(".");

                // Allow absolute paths only if they don't contain traversal
                if (path.Contains(".."))
                    throw new ArgumentException($"Path contains invalid traversal sequence: {path}", paramName);
            }
        }

        /// <summary>
        /// Validates that an output path is safe and within an allowed base directory.
        /// </summary>
        /// <param name="outputPath">The output path to validate.</param>
        /// <param name="baseDirectory">The base directory that output must be within.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <returns>The validated absolute path.</returns>
        /// <exception cref="ArgumentException">Thrown when path would escape the base directory.</exception>
        public static string ValidateOutputPath(string outputPath, string baseDirectory, string paramName)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty.", paramName);

            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory cannot be null or empty.", nameof(baseDirectory));

            var fullBase = Path.GetFullPath(baseDirectory);
            var fullOutput = Path.GetFullPath(Path.Combine(baseDirectory, outputPath));

            if (!fullOutput.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Output path '{outputPath}' would escape base directory '{baseDirectory}'.", paramName);

            return fullOutput;
        }

        /// <summary>
        /// Validates that a string is a valid hexadecimal hash of expected length.
        /// </summary>
        /// <param name="hash">The hash string to validate.</param>
        /// <param name="expectedLength">Expected length (0 for any length).</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when hash format is invalid.</exception>
        public static void ValidateHash(string hash, int expectedLength, string paramName)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be null or empty.", paramName);

            if (!HexHashPattern.IsMatch(hash))
                throw new ArgumentException($"Hash contains invalid characters (expected hexadecimal): {hash}", paramName);

            if (expectedLength > 0 && hash.Length != expectedLength)
                throw new ArgumentException($"Hash has invalid length {hash.Length} (expected {expectedLength}): {hash}", paramName);
        }

        /// <summary>
        /// Validates that an input file exists and is accessible.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <returns>The validated absolute path.</returns>
        /// <exception cref="ArgumentException">Thrown when path is invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when file does not exist.</exception>
        public static string ValidateInputFile(string filePath, string paramName)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", paramName);

            var fullPath = Path.GetFullPath(filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Input file not found: {fullPath}", fullPath);

            return fullPath;
        }

        /// <summary>
        /// Validates that an input directory exists and is accessible.
        /// </summary>
        /// <param name="dirPath">The directory path to validate.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <returns>The validated absolute path.</returns>
        /// <exception cref="ArgumentException">Thrown when path is invalid.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when directory does not exist.</exception>
        public static string ValidateInputDirectory(string dirPath, string paramName)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
                throw new ArgumentException("Directory path cannot be null or empty.", paramName);

            var fullPath = Path.GetFullPath(dirPath);

            if (!Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"Input directory not found: {fullPath}");

            return fullPath;
        }
    }
}
