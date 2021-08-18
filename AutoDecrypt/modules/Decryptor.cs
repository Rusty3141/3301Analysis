using AutoDecrypt.modules.data;
using AutoDecrypt.modules.language;
using AutoDecrypt.modules.maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoDecrypt.modules
{
    /// <summary>
    /// Class <c>Decryptor</c> processes automated decryption attempts for any Liber Primus page or section
    /// using postfix expressions for index-based translation.
    /// </summary>
    internal class Decryptor
    {
        public JobSettings Settings;

        private readonly string[] _attempts;
        private readonly Gematria _gematria;

        private readonly FitnessWorker _fitnessWorker;

        public Decryptor(JobSettings settings)
        {
            Settings = settings;

            DisplaySettings();

            Console.Write($"{Environment.NewLine}Loading attempts. ");
            _attempts = settings.Attempts;
            Console.WriteLine($"Done (loaded {_attempts.Length} attempts).");

            Console.Write("Generating mathematical data. ");
            Maths.Generate();
            Console.WriteLine("Done.");

            Console.Write("Initialising text fitness worker from provided frequencies. ");
            _fitnessWorker = new FitnessWorker(Settings.NgramWidth, Settings.NgramStatisticsPath);
            Console.WriteLine("Done.");

            _gematria = new Gematria(Settings.GematriaPath);
        }

        private void DisplaySettings()
        {
            Console.WriteLine("Initialised a new decryption worker with the following options:");

            bool invalidPathProvided = false;
            PropertyInfo[] properties = Settings.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                if (!properties[i].PropertyType.IsArray)
                {
                    string printableProperty = properties[i].GetValue(Settings).ToString();
                    if (properties[i].PropertyType == typeof(string))
                    {
                        printableProperty = IOTools.PersistentPath($"_data{IOTools.PathSeparator}{printableProperty}");
                        properties[i].SetValue(Settings, printableProperty);

                        // Path property.
                        if (File.Exists(printableProperty) || Directory.Exists(printableProperty))
                        {
                            printableProperty += " (valid path)";
                        }
                        else
                        {
                            invalidPathProvided = true;
                            printableProperty += " (invalid path)";
                        }
                    }

                    Console.Write($" \u2022 {IOTools.CamelToSpaced(properties[i].Name)}: {printableProperty}");

                    Console.WriteLine(".");
                }
            }

            if (invalidPathProvided) throw new FileNotFoundException();
        }

        public void Attempt(string path)
        {
            string[] readFile = File.ReadAllLines(path);

            HashSet<int> skips = ExtractSkips(readFile);

            float bestScore = float.NegativeInfinity;
            string bestAttempt = string.Empty;
            int bestShift = 0;
            string bestResult = string.Empty;

            static HashSet<int> ExtractSkips(string[] file)
            {
                for (int i = 0; i < file.Length; i++)
                {
                    if (file[i].StartsWith("skips"))
                    {
                        return new HashSet<int>(file[i].Replace("skips", string.Empty).Replace(" ", string.Empty).Split(",").Select(x => int.Parse(x)));
                    }
                }

                return new HashSet<int>();
            }

            using (ProgressBar progress = new())
            {
                for (int i = 0; i < _attempts.Length; i++)
                {
                    List<PartialTranslation> intermediate = Grammar.MakeTranslation(_attempts[i], readFile, skips, _gematria);

                    if (intermediate != null)
                    {
                        for (int shift = 0; shift < (Settings.IsShiftMode ? 29 : 1); shift++)
                        {
                            string output = string.Empty;

                            for (int j = 0; j < intermediate.Count; j++)
                            {
                                if (intermediate[j].Rune == null)
                                {
                                    output += intermediate[j].Plain;
                                }
                                else
                                {
                                    output += _gematria.IndexLookup[(intermediate[j].Rune.Index + shift) % 29].Plaintext;
                                }
                            }

                            string trimmedOutput = output.SanitiseAsScorerInput();

                            float score = _fitnessWorker.Evaluate(trimmedOutput);

                            if (score > bestScore && new HashSet<char>(trimmedOutput).Count >= (trimmedOutput.Length > 20 ? 10 : 5)) // Enforce at least 10 unique characters, or 5 unique characters for very short files.
                            {
                                bestScore = score;
                                bestAttempt = _attempts[i];
                                bestShift = shift;
                                bestResult = output;
                            }
                        }
                    }

                    progress.Report((float)(i + 1) / _attempts.Length);
                }
            }

            if (bestAttempt != string.Empty)
            {
                Console.Write($"The optimum result is using attempt \"{bestAttempt}\"");
                Console.Write($"{(skips.Count > 0 ? $" (and skipping rune {(skips.Count > 1 ? "indices" : "index")} {string.Join(", ", skips)})" : string.Empty)},");
                Console.Write(" with ");
                if (bestShift != 0)
                {
                    Console.Write($"a shift of {bestShift} and ");
                }
                Console.Write("score ");
                IOTools.WriteColouredTextScore(bestScore);
                Console.WriteLine(':');
                Console.WriteLine(bestResult.TrimEnd(Environment.NewLine.ToCharArray()));
            }
            else
            {
                Console.WriteLine("Unable to find an acceptable result.");
            }
        }
    }
}