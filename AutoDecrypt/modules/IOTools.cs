using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutoDecrypt.modules
{
    /// <summary>
    /// Class <c>IOTools</c> provides output formatting functions, file management and text highlighting in some supporting terminals.
    /// </summary>
    internal static class IOTools
    {
        public static string CommentDeliminator { get; } = "//";

        public static char PathSeparator { get; } = Path.DirectorySeparatorChar;

        public static void PlaceDivisionBar()
        {
            int barWidth = 65;
            Console.WriteLine(Environment.NewLine + new string('─', barWidth));
        }

        public static void WriteColouredTextScore(float score)
        {
            if (score > -3.5)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            }
            else if (score > -4)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.Write(Math.Round(score, 3));

            Console.ResetColor();
        }

        public static string PersistentPath(string joiner)
        {
            string crossPlatformJoiner = @joiner.Replace('\\', PathSeparator).Replace('/', PathSeparator);
            string workingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.GetFullPath(Path.Combine(workingPath, "../../../../", crossPlatformJoiner));
        }

        public static string CamelToSpaced(string camel)
        {
            string output = camel[0].ToString();

            for (int i = 1; i < camel.Length; i++)
            {
                output += $"{(Char.IsUpper(camel[i]) ? ' ' : String.Empty)}{camel[i]}";
            }

            return output;
        }

        public static void AttemptToSetCursorVisibility(bool isCursorTargetVisible)
        {
            try
            {
                Console.CursorVisible = isCursorTargetVisible;
            }
            catch (IOException)
            {
                // Likely using a Linux terminal that does not support this operation, so continue without affecting the cursor visibility.
            }
        }

        public static void SelectionUpdate(string input, IEnumerable<ISelectable> selectables)
        {
            input = Regex.Replace(input.ToLower(), @"\s+", " "); // Case insensitive and replace multiple spaces with a single space.

            string commandWord = Regex.Replace(input, @"[\d\s,]", "").Trim(); // Just the command word.
            string strippedInput = Regex.Replace(input, @"[^\d\s,]", ""); // Only digits and separators.

            int[] selection = Array.ConvertAll(strippedInput.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), x => int.Parse(x));

            if (commandWord == "s" || commandWord == "d")
            {
                int i = 0;
                foreach (ISelectable selectable in selectables)
                {
                    if (selection.Length == 0 || (selection.Length > 0 && selection.Contains(i++)))
                    {
                        selectable.Selected = input[0] == 's';
                    }
                }
            }
        }

        public static void Clear()
        {
            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                // Clearing unsupported in terminal environment.

                if (!Console.IsOutputRedirected)
                {
                    int clearLength = 40;

                    for (int i = 0; i < clearLength; i++)
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    PlaceDivisionBar();
                    Console.WriteLine();
                }
            }
        }

        public static string[] GetSelectedDirectoryContents(string directoryPath, string[] selectedNames)
        {
            return Directory.GetFiles(directoryPath).Where(x => selectedNames.Contains(x.Split(PathSeparator)[^1])).OrderBy(f => f).ToArray();
        }
    }
}