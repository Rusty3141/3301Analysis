using AutoDecrypt.modules.language;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoDecrypt.modules.data
{
    /// <summary>
    /// Class <c>JobSettings</c> provides processing and serialisation for decryption attempt settings and allows job settings
    /// to be loaded from a JSON configuration file.
    /// </summary>
    internal class JobSettings
    {
        [JsonInclude] public virtual string GematriaPath { get; protected set; }
        [JsonInclude] public virtual int NgramWidth { get; protected set; }
        [JsonInclude] public virtual string NgramStatisticsPath { get; protected set; }
        [JsonInclude] public virtual bool IsShiftMode { get; protected set; }
        [JsonInclude] public string DatafileDirectory { get; protected set; }
        [JsonInclude] public string[] FileNamesToDecrypt { get; set; }
        [JsonInclude] public string[] Attempts { get; protected set; }

        public JobSettings()
        {
        }

        public void Serialise(string directory, string fileName)
        {
            try
            {
                string fullPath = Path.Combine(directory, fileName);

                Console.Write("Writing job settings. ");
                File.WriteAllText(fullPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true }), Encoding.UTF8);
                Console.WriteLine($"Done (saved into {fullPath}).");
            }
            catch (IOException)
            {
                Console.WriteLine("Error when trying to write to the provided directory. Aborting.");
                throw;
            }
        }

        public virtual void GenerateAttempts(List<OperationReference> operations, bool eliminateEquivalent = false)
        {
            AttemptBuilder builder = new(operations.Where(x => x.Selected).ToList(), GematriaPath, eliminateEquivalent);

            Attempts = AttemptBuilder.Attempts;
        }
    }
}