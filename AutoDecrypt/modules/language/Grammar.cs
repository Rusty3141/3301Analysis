using AutoDecrypt.modules.data;
using AutoDecrypt.modules.maths;
using System;
using System.Collections.Generic;

namespace AutoDecrypt.modules.language
{
    /// <summary>
    /// Class <c>Grammar</c> handles translation of text using the postfix grammar.
    /// </summary>
    internal static class Grammar
    {
        public static bool ErrorFlag { get; private set; } = false;

        public static Dictionary<string, Func<int, int, int>> DecryptionOperations { get; } = new()
        {
            { "+", (a, b) => a + b },
            { "-", (a, b) => a - b },
            { "*", (a, b) => a * b },
            { "^", (a, b) => a ^ b }
        };

        public static Dictionary<string, Func<RuneDefinition, RuneDefinition, int, int>> DecryptionSingles { get; } = new()
        {
            { "p", (ciphertextRune, previousCiphertextRune, runeIndex) => ciphertextRune.Index },
            { "pr", (ciphertextRune, previousCiphertextRune, runeIndex) => ciphertextRune.Prime },
            { "prevp", (ciphertextRune, previousCiphertextRune, runeIndex) => previousCiphertextRune.Index },
            { "prevpr", (ciphertextRune, previousCiphertextRune, runeIndex) => previousCiphertextRune.Prime },
            { "i", (ciphertextRune, previousCiphertextRune, runeIndex) => runeIndex }
        };

        public static Dictionary<string, Func<int, int>> DecryptionSpecials { get; } = new()
        {
            { "prime", (ciphertextIndex) => Maths.Prime(ciphertextIndex) },
            { "φ", (ciphertextIndex) => Maths.Totient(ciphertextIndex) },
            { "π", (ciphertextIndex) => Maths.Pi(ciphertextIndex) }
        };

        public static List<PartialTranslation> MakeTranslation(string attempt, string[] file, HashSet<int> skips, Gematria gematria)
        {
            int _runeIndex = 0;
            HashSet<int> _skipQueue = new(skips);

            List<PartialTranslation> intermediate = new();
            RuneDefinition previousRune = gematria.IndexLookup[0];
            bool previousRuneSetToFirstOccurrence = false;

            for (int i = 0; i < file.Length; i++)
            {
                if (!file[i].StartsWith("skips"))
                {
                    if (!previousRuneSetToFirstOccurrence && gematria.RuneLookup.ContainsKey(file[i].SanitiseAsScorerInput().Trim()[0]))
                    {
                        previousRune = gematria.RuneLookup[file[i].SanitiseAsScorerInput().Trim()[0]];
                        previousRuneSetToFirstOccurrence = true;
                    }

                    for (int j = 0; j < file[i].Length; j++)
                    {
                        if (gematria.RuneExists(file[i][j]))
                        {
                            if (_skipQueue.Contains(_runeIndex))
                            {
                                _skipQueue.Remove(_runeIndex);
                                intermediate.Add(new PartialTranslation(gematria.RuneLookup[file[i][j]]));
                            }
                            else
                            {
                                RuneDefinition currentRune = gematria.RuneLookup[file[i][j]];
                                intermediate.Add(new PartialTranslation(gematria.IndexLookup[Maths.Mod(GetNewIndex(currentRune, previousRune, attempt, _runeIndex), 29)]));
                                previousRune = currentRune;
                                if (ErrorFlag)
                                {
                                    ErrorFlag = false;
                                    return null;
                                }
                                _runeIndex++;
                            }
                        }
                        else
                        {
                            intermediate.Add(new PartialTranslation(file[i][j].ToString()));
                        }
                    }

                    intermediate.Add(new PartialTranslation(Environment.NewLine));
                }
            }

            return intermediate;
        }

        public static int GetNewIndex(RuneDefinition ciphertextRune, RuneDefinition previousCiphertextRune, string attempt, int runeIndex)
        {
            Stack<int> interpretationStack = new();

            string[] attemptTokens = attempt.Split(' ');

            for (int i = 0; i < attemptTokens.Length; i++)
            {
                if (DecryptionSingles.ContainsKey(attemptTokens[i]))
                {
                    attemptTokens[i] = DecryptionSingles[attemptTokens[i]](ciphertextRune, previousCiphertextRune, runeIndex).ToString();
                }
            }

            for (int i = 0; i < attemptTokens.Length; i++)
            {
                if (DecryptionSpecials.ContainsKey(attemptTokens[i]))
                {
                    interpretationStack.Push(DecryptionSpecials[attemptTokens[i]](interpretationStack.Pop()));
                }
                else if (DecryptionOperations.ContainsKey(attemptTokens[i]))
                {
                    int arg2 = interpretationStack.Pop();
                    int arg1 = interpretationStack.Pop();

                    try
                    {
                        interpretationStack.Push(DecryptionOperations[attemptTokens[i]](arg1, arg2));
                    }
                    catch (DivideByZeroException)
                    {
                        ErrorFlag = true;
                        return 1;
                    }
                }
                else
                {
                    interpretationStack.Push(int.Parse(attemptTokens[i]));
                }
            }

            return interpretationStack.Pop();
        }
    }
}