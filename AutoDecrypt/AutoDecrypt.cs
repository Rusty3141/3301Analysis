using AutoDecrypt.modules;
using AutoDecrypt.modules.data;
using AutoDecrypt.modules.language;
using CommandLine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoDecrypt
{
    /// <summary>
    /// Class <c>AutoDecrypt</c> is the entry-point for the program and handles class setup and decryption control.
    /// </summary>
    internal static class AutoDecrypt
    {
        private static readonly string[] acceptableBoolStrings = { "yes", "no", "y", "n", "true", "false", "t", "f" };

        private static readonly string JOB_DIRECTORY = IOTools.PersistentPath("_data/jobs/");

        /// <summary>
        /// Class <c>CommandLineOptions</c> defines command-line parameters that can be passed to the program.
        /// </summary>
        internal sealed class CommandLineOptions
        {
            [Option('m', "make", Default = false, HelpText = "Begin interactively making a job settings file. Accepts no additional data and is used by itself to enter the mode.", SetName = "Make operation")]
            public bool MakeSettingsMode { get; private set; }

            [Option('d', "decrypt", Default = "SolvedPagesDecryptions.json", HelpText = "Begin decryption with the specified job settings JSON file. Accepts a path (relative to _data/jobs/) to the job settings file. The path is mandatory if this argument is passed.", SetName = "Decrypt operation")]
            public string JobSettingsPath { get; private set; }
        }

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            IOTools.AttemptToSetCursorVisibility(true);

            Directory.CreateDirectory(JOB_DIRECTORY);

            _ = Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(options =>
            {
                if (options.MakeSettingsMode)
                {
                    MakeSettings();
                }
                else
                {
                    RunDecryptionJob(options.JobSettingsPath);
                }
            })
            .WithNotParsed(errors =>
            {
                Console.WriteLine("The provided command line arguments are invalid.");
            });
        }

        private static void WriteOperationsString(this List<OperationReference> list)
        {
            Console.WriteLine(string.Join(Environment.NewLine, list.Select(x => $"{x.Index}. {x.Operation} ({x.Category}) — {(x.Selected ? "SELECTED" : "not selected")}").ToArray()));
        }

        private static void WriteDataFilesString(this List<DatafileReference> list)
        {
            Console.WriteLine(string.Join(Environment.NewLine, list.Select(x => $"{x.Index}. {x.Name} — {(x.Selected ? "SELECTED" : "not selected")}").ToArray()));
        }

        private static void GatherValidSettings(ref JobSettings settings)
        {
            DefaultJobSettings defaultSettingsReference = new();

            Console.WriteLine($"All paths are relative to \"_data{IOTools.PathSeparator}\".");

            Console.WriteLine($"{Environment.NewLine}Prompt format: Property [Default Value] (Acceptable Formats)?: Input");
            IOTools.PlaceDivisionBar();

            Console.WriteLine($"{Environment.NewLine}Please enter the requested information.");

            PropertyInfo[] properties = settings.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                if (!properties[i].PropertyType.IsArray)
                {
                    object defaultResult = defaultSettingsReference.GetType().GetProperties()[i].GetValue(defaultSettingsReference);

                    string input;
                    do
                    {
                        Console.Write($" \u2022 {IOTools.CamelToSpaced(properties[i].Name)} [{defaultResult}]");
                        if (properties[i].PropertyType == typeof(bool))
                        {
                            Console.Write($" ({string.Join('/', acceptableBoolStrings.Select(x => x.ToLower()).ToArray())})");
                        }
                        Console.Write(": ");

                        input = Console.ReadLine().ToLower();
                    } while (!(input == string.Empty || TypeDescriptor.GetConverter(properties[i].PropertyType).IsValid(input) || acceptableBoolStrings.Contains(input)));

                    if (properties[i].PropertyType == typeof(bool))
                    {
                        // Even indices correspond to a "true" input.
                        properties[i].SetValue(settings, Array.IndexOf(acceptableBoolStrings, input) % 2 == 0);
                    }
                    else
                    {
                        object conversion = null;
                        if (input != string.Empty)
                        {
                            conversion = TypeDescriptor.GetConverter(properties[i].PropertyType).ConvertFrom(input);
                        }

                        properties[i].SetValue(settings, conversion != null && conversion.ToString() != string.Empty ? conversion : defaultResult);
                    }
                }
            }
        }

        private static void MakeSettings()
        {
            JobSettings settings = new();

            Console.WriteLine("*** 3301 Analysis — Job Settings File Creation Mode ***");

            GatherValidSettings(ref settings);

            PropertyInfo[] operationDictionaries = typeof(Grammar).GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)).ToArray();

            List<OperationReference> operations = new();
            int operationReferenceIndex = 0;

            for (int i = 0; i < operationDictionaries.Length; i++)
            {
                object dict = operationDictionaries[i].GetValue(null);
                string category = IOTools.CamelToSpaced(operationDictionaries[i].Name);

                if (char.ToLower(category[^1]) == 's')
                {
                    category = category[0..^1]; // Make non-plural.
                }

                foreach (string k in ((IDictionary)dict).Keys)
                {
                    operations.Add(new OperationReference(category, k.ToString(), operationReferenceIndex++));
                }
            }

            // Gather selected attempts.
            string input = string.Empty;
            do
            {
                IOTools.Clear();

                Console.WriteLine("*** 3301 Analysis — Job Settings File Creation Mode ***");
                Console.WriteLine($"Decryption attempt generation step.");

                Console.WriteLine($"The currently supported grammar tokens are:{Environment.NewLine}");

                operations.WriteOperationsString();

                Console.WriteLine($"{Environment.NewLine}Writing \"s\" or \"d\" followed by numbers separated by spaces or commas will select or deselect the grammar item at the position indicated in the list.");
                Console.WriteLine("Write \"s\" to select all, \"d\" to deselect all, and \"quit\" to quit and start generating the decryption attempts.");

                Console.Write($"{Environment.NewLine}Input: ");

                input = Console.ReadLine().ToLower();

                IOTools.SelectionUpdate(input, operations);
            } while (input.Trim() != "quit");

            // Gather selected pages/sections to run the attempts on.
            List<DatafileReference> datafiles = new();
            string[] fullPaths = Directory.GetFiles(IOTools.PersistentPath($"_data{IOTools.PathSeparator}{settings.DatafileDirectory}")).Where(x => x.Split('.')[^1].ToLower() == "txt").OrderBy(f => f).ToArray();
            for (int i = 0; i < fullPaths.Length; i++)
            {
                datafiles.Add(new DatafileReference(fullPaths[i].Split(IOTools.PathSeparator)[^1], i));
            }
            do
            {
                IOTools.Clear();

                Console.WriteLine("*** 3301 Analysis — Job Settings File Creation Mode ***");

                Console.WriteLine($"The rune files in the provided directory are:{Environment.NewLine}");

                datafiles.WriteDataFilesString();

                Console.WriteLine($"{Environment.NewLine}Writing \"s\" or \"d\" followed by numbers separated by spaces or commas will select or deselect the grammar item at the position indicated in the list.");
                Console.WriteLine("Write \"s\" to select all, \"d\" to deselect all, and \"quit\" to quit and start generating the decryption attempts.");

                Console.Write($"{Environment.NewLine}Input: ");

                input = Console.ReadLine().ToLower();

                IOTools.SelectionUpdate(input, datafiles);
            } while (input.Trim() != "quit");

            settings.FileNamesToDecrypt = datafiles.Where(x => x.Selected).Select(x => x.Name).ToArray();

            IOTools.Clear();

            Console.WriteLine("*** 3301 Analysis — Job Settings File Creation Mode ***");

            do
            {
                Console.Write($"Remove suspected equivalent decryption attempts");
                Console.Write($" ({string.Join('/', acceptableBoolStrings.Select(x => x.ToLower()).ToArray())})? ");

                input = Console.ReadLine().ToLower();
            } while (!(input == string.Empty || acceptableBoolStrings.Contains(input)));

            // Even indices correspond to a "true" input.
            bool eliminateEquivalent = Array.IndexOf(acceptableBoolStrings, input) % 2 == 0;
            if (eliminateEquivalent) Console.WriteLine("Warning: eliminating equivalent results may miss a small percentage of unique decryption attempts erroneously.");
            settings.GenerateAttempts(operations, eliminateEquivalent);

            Console.Write($"Enter name for job settings file (to be saved relative to {JOB_DIRECTORY}): ");

            string fileName = Regex.Replace(Console.ReadLine().Trim(), $@"{Environment.NewLine}", "");
            if (fileName.Split('.')[^1].ToLower() != "json")
            {
                fileName += ".json";
            }

            settings.Serialise(JOB_DIRECTORY, fileName);
        }

        private static void RunDecryptionJob(string pathToSettings)
        {
            int totalJobProgress = 0;
            int totalJobLength = 0;

            // If the job is projected to take longer than this, display the estimated time remaining.
            const int verboseTimeLogThresholdSeconds = 15;

            pathToSettings = IOTools.PersistentPath(Path.Combine(JOB_DIRECTORY, pathToSettings));

            JobSettings settings;
            string[] datafilePaths;
            Console.Write($"Attempting to read job settings from {pathToSettings}. ");
            try
            {
                settings = JsonSerializer.Deserialize<JobSettings>(File.ReadAllText(pathToSettings));
                datafilePaths = IOTools.GetSelectedDirectoryContents(IOTools.PersistentPath($"_data{IOTools.PathSeparator}{settings.DatafileDirectory}"), settings.FileNamesToDecrypt);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Failed.{Environment.NewLine}");
                Console.WriteLine(e);
                return;
            }
            Console.WriteLine($"Done.{Environment.NewLine}");

            Decryptor decryptor = new(settings);

            Console.Write($"Attempting to decrypt {datafilePaths.Length} rune file{(datafilePaths.Length == 1 ? "" : "s")}.{Environment.NewLine}");

            IOTools.PlaceDivisionBar();

            Stopwatch timer = new();

            int[] fileRunicLengths = new int[datafilePaths.Length];

            for (int i = 0; i < datafilePaths.Length; i++)
            {
                fileRunicLengths[i] = GetFileRunicLength(datafilePaths[i]);
                totalJobLength += fileRunicLengths[i];
            }

            bool verboseTimeLog = false;

            timer.Start();
            for (int i = 0; i < datafilePaths.Length; i++)
            {
                Console.WriteLine(Environment.NewLine + $"Started decryption of \"{datafilePaths[i]}\".");

                decryptor.Attempt(datafilePaths[i]);

                totalJobProgress += fileRunicLengths[i];

                Console.Write($"File finished in {TimeSpanToString(timer.Elapsed)}");
                Console.WriteLine(datafilePaths.Length > 1 ? $" ({Math.Round((float)totalJobProgress / totalJobLength * 100)}% of the whole job has been finished)." : ".");

                TimeSpan projectedTotalTime = TimeSpan.FromMilliseconds(timer.Elapsed.TotalMilliseconds * totalJobLength / totalJobProgress);

                if (projectedTotalTime.TotalSeconds > verboseTimeLogThresholdSeconds)
                {
                    verboseTimeLog = true;
                }

                if (verboseTimeLog && i < datafilePaths.Length - 1)
                {
                    Console.WriteLine($"It will take an estimated total of {TimeSpanToString(projectedTotalTime)} to finish the job, which is another {TimeSpanToString(TimeSpan.FromMilliseconds(projectedTotalTime.TotalMilliseconds - timer.Elapsed.TotalMilliseconds))}.");
                }

                if (i < datafilePaths.Length - 1)
                {
                    IOTools.PlaceDivisionBar();
                }
            }
            timer.Stop();

            static string TimeSpanToString(TimeSpan time)
            {
                int hours = (int)Math.Floor(time.TotalHours);
                int minutes = ((int)Math.Floor(time.TotalMinutes) - hours * 60) % 60;
                double seconds = (double)Math.Round((time.TotalSeconds - minutes * 60 - hours * 3600), minutes > 0 || hours > 0 ? 0 : 3) % 60;
                if (minutes < 0) minutes += 60;
                if (seconds < 0) seconds += 60;

                return $"{(hours > 0 ? $"{hours}h " : "")}{(minutes > 0 || hours > 0 ? $"{minutes}m " : "")}{seconds}s";
            }
        }

        // How many lines in the provided file contain only runes (that is, characters that are non letters, digits or whitespace)?
        private static int GetFileRunicLength(string path) => File.ReadAllLines(path).Where(x => Regex.Replace(x, @"[a-z]|[A-Z]|\d|\s|" + IOTools.CommentDeliminator, "").Length > 0).Count();
    }
}