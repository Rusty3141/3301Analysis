using AutoDecrypt.modules.data;
using AutoDecrypt.modules.maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoDecrypt.modules.language
{
    /// <summary>
    /// Class <c>AttemptBuilder</c> provides the ability to generate decryption attempts by parsing postfix grammar production rules.
    /// </summary>
    internal class AttemptBuilder
    {
        private static readonly string GRAMMAR_PRODUCTION_RULES = IOTools.PersistentPath("_data/grammar/ProductionRules.txt");
        private static readonly string GRAMMAR_LOGICAL_REPLACEMENTS = IOTools.PersistentPath("_data/grammar/LogicalReplacements.txt");

        private static readonly Dictionary<string, List<string>> parseRules = new();

        public static string[] Attempts
        {
            get
            {
                if (parseRules.ContainsKey("decryption"))
                {
                    return parseRules["decryption"].ToArray();
                }

                return null;
            }
        }

        public AttemptBuilder(List<OperationReference> operations, string gematriaPath, bool eliminateEquivalent)
        {
            Maths.Generate();
            Gematria _gematria = new(IOTools.PersistentPath(Path.Join("_data", gematriaPath))); // Required for removing equivalent attempts.

            Console.Write("Parsing production rules and generating all decryption attempts. ");

            parseRules["operation"] = operations.Where(x => x.Category.ToLower() == "decryption operation").Select(x => x.Operation).ToList();
            parseRules["single"] = operations.Where(x => x.Category.ToLower() == "decryption single").Select(x => x.Operation).ToList();
            parseRules["special"] = operations.Where(x => x.Category.ToLower() == "decryption special").Select(x => x.Operation).ToList();

            foreach (string rule in File.ReadAllLines(GRAMMAR_PRODUCTION_RULES))
            {
                if (!rule.StartsWith(IOTools.CommentDeliminator))
                {
                    ParseRule(new string(rule.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray())); // Remove whitespace.
                }
            }

            StringAttemptOptimiser optimiser = new(GRAMMAR_LOGICAL_REPLACEMENTS);
            for (int i = 0; i < parseRules["decryption"].Count; i++)
            {
                parseRules["decryption"][i] = optimiser.Optimise(parseRules["decryption"][i]);
            }

            Console.WriteLine("Done.");

            if (eliminateEquivalent)
            {
                Console.Write("Detecting equivalent results. ");

                const int numberOfDuplicateChecksPerAttempt = 40;
                HashSet<string> duplicateChecker = new();
                RuneDefinition[] duplicateCheckSeed = new RuneDefinition[numberOfDuplicateChecksPerAttempt];

                Random random = new();
                int[] seedIndex = new int[numberOfDuplicateChecksPerAttempt];

                for (int i = 0; i < numberOfDuplicateChecksPerAttempt; i++)
                {
                    duplicateCheckSeed[i] = _gematria.IndexLookup[random.Next(29)];
                    seedIndex[i] = random.Next(1000);
                }

                List<int> invalidIndices = new();
                using (ProgressBar progress = new())
                {
                    for (int i = 0; i < parseRules["decryption"].Count; i++)
                    {
                        int[] result = new int[numberOfDuplicateChecksPerAttempt];

                        for (int j = 0; j < result.Length; j++)
                        {
                            result[j] = Grammar.GetNewIndex(duplicateCheckSeed[j], duplicateCheckSeed[j - 1 > 0 ? j - 1 : 0], parseRules["decryption"][i], seedIndex[j]);
                        }

                        if (duplicateChecker.Contains(string.Join(',', result)))
                        {
                            invalidIndices.Add(i);
                        }
                        else
                        {
                            duplicateChecker.Add(string.Join(',', result));
                        }

                        progress.Report((float)(i + 1) / parseRules["decryption"].Count);
                    }
                }

                Console.WriteLine("Done.");
                Console.Write("Eliminating equivalent results. ");

                using (ProgressBar progress = new())
                {
                    invalidIndices.Sort();
                    invalidIndices.Reverse();
                    for (int i = 0; i < invalidIndices.Count; i++)
                    {
                        parseRules["decryption"].RemoveAt(invalidIndices[i]);
                        progress.Report((float)(i + 1) / invalidIndices.Count);
                    }
                }
            }

            Console.WriteLine($"Done (generated {parseRules["decryption"].Count} attempts).");
        }

        private static void ParseRule(string rule)
        {
            string[] temp = rule.Split("::=");

            BuildProduction(temp[0][1..^1], temp[1]);
        }

        private static void BuildProduction(string definitionOf, string inserts)
        {
            if (!parseRules.ContainsKey(definitionOf))
            {
                foreach (string option in inserts.Split('|'))
                {
                    string[] partialParse = Regex.Replace(option, @">", "").Split('<', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    List<List<string>> toCross = new();
                    foreach (string j in partialParse)
                    {
                        toCross.Add(parseRules[j]);
                    }

                    if (!parseRules.ContainsKey(definitionOf))
                    {
                        parseRules[definitionOf] = IterativeProduct(toCross).ToList();
                    }
                    else
                    {
                        parseRules[definitionOf] = parseRules[definitionOf].Concat(IterativeProduct(toCross)).ToList();
                    }
                }
            }
        }

        private static IEnumerable<string> IterativeProduct(List<List<string>> productOver)
        {
            string[][] pools = new string[productOver.Count][];

            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = productOver[i].ToArray();
            }

            List<List<string>> result = new();

            int redundantFirstItems = 1;
            for (int i = 0; i < pools.Length; i++)
            {
                string[] pool = pools[i];

                if (result.Count > 0)
                {
                    foreach (List<string> x in result.ToArray())
                    {
                        foreach (string y in pool)
                        {
                            List<string> newInsertion = new(x) { y };

                            result.Add(newInsertion);
                        }
                    }
                }
                else
                {
                    foreach (string y in pool)
                    {
                        result.Add(new List<string> { y });
                    }
                }

                if (i > 0)
                {
                    redundantFirstItems *= pools[i - 1].Length;
                    result.RemoveRange(0, redundantFirstItems);
                }
            }

            foreach (List<string> production in result)
            {
                yield return string.Join(' ', production);
            }
        }
    }
}