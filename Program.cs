using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LaboratorSistemeRecunoastereaFormelor
{
    class DocumentData
    {
        public string FileName { get; set; }
        public string FolderType { get; set; } 
        public List<string> Topics { get; set; } = new List<string>();
        public List<string> StemmedWords { get; set; } = new List<string>();

        public string RawContent { get; set; }  
    }


    class Program
    {
        private static double CalculeazaEntropie(List<DocumentData> documenteSubset)
        {
            if (documenteSubset.Count == 0) return 0.0;
            var frecventeClase = documenteSubset
                .Select(d => d.Topics.FirstOrDefault() ?? "UNK")
                .GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count());

            double entropie = 0.0;
            double total = documenteSubset.Count;

            foreach (var kvp in frecventeClase)
            {
                double p = kvp.Value / total;
                if (p > 0)
                {
                    entropie -= p * (Math.Log(p) / Math.Log(2));
                }
            }

            return entropie;
        }
        static void Main(string[] args)
        {
            string stopwordsPath = @"C:\Users\sbirc\Desktop\INGINERIE\An III Facultate\An 3 sem 1\LaboratorSistemeRecunoastereaFormelor\stopwords.txt";

            HashSet<string> stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(stopwordsPath))
            {
                foreach (string line in File.ReadAllLines(stopwordsPath))
                {
                    string word = line.Trim().ToLower();
                    if (!string.IsNullOrEmpty(word))
                    {
                        stopwords.Add(word);
                    }
                }
            }

            Console.WriteLine("Stopwords incarcate: " + stopwords.Count);

            string trainPath = @"C:\Users\sbirc\Desktop\INGINERIE\An III Facultate\An 3 sem 1\LaboratorSistemeRecunoastereaFormelor\Training";
            string testPath = @"C:\Users\sbirc\Desktop\INGINERIE\An III Facultate\An 3 sem 1\LaboratorSistemeRecunoastereaFormelor\Testing";

            string[] trainFiles = Directory.GetFiles(trainPath, "*.xml");
            string[] testFiles = Directory.GetFiles(testPath, "*.xml");

            Console.WriteLine("Fisiere gasite in Training: " + trainFiles.Length);
            Console.WriteLine("Fisiere gasite in Testing: " + testFiles.Length);

            List<DocumentData> documente = new List<DocumentData>();
            SortedSet<string> globalVocabulary = new SortedSet<string>();
            PorterStemmer stemmer = new PorterStemmer();

            void ProceseazaArticol(string filePath, string folderType)
            {
                XDocument doc = XDocument.Load(filePath);

                string titlu = doc.Root?.Element("title")?.Value ?? "";
                string corpText = string.Join(" ", doc.Root?.Element("text")?.Elements("p").Select(p => p.Value) ?? new string[0]);
                string textComplet = titlu + " " + corpText;

                var topics = doc.Descendants("codes")
                    .Where(c => (string)c.Attribute("class") == "bip:topics:1.0")
                    .Elements("code")
                    .Select(c => (string)c.Attribute("code") ?? (string)c.Attribute("value"))
                    .Where(val => !string.IsNullOrEmpty(val))
                    .Distinct()
                    .ToList();

                var docData = new DocumentData
                {
                    FileName = Path.GetFileName(filePath),
                    FolderType = folderType,
                    Topics = topics,
                    RawContent = textComplet
                };

                string textCuratat = Regex.Replace(textComplet.ToLower(), @"[^a-z\s]", " ");
                string[] tokens = textCuratat.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string token in tokens)
                {
                    if (!stopwords.Contains(token) && token.Length >=1)
                    {
                        string stem = stemmer.StemWord(token);

                        docData.StemmedWords.Add(stem);
                        globalVocabulary.Add(stem);
                    }
                }

                documente.Add(docData);
            }

            Console.WriteLine("Se citesc fisierele din Training.");
            foreach (string file in trainFiles)
            {
                ProceseazaArticol(file, "Training");
            }

            Console.WriteLine("Se citesc fisierele din Testing.");
            foreach (string file in testFiles)
            {
                ProceseazaArticol(file, "Testing");
            }

            Console.WriteLine($"\nAm incarcat continutul a {documente.Count} documente in memorie.");
            Console.WriteLine($"Dimensiune dictionar global (cuvinte unice): {globalVocabulary.Count}");

            if (documente.Count > 0)
            {
                Console.WriteLine($"\nExemplu de radacini extrase pentru {documente[0].FileName}:");
                Console.WriteLine(string.Join(", ", documente[0].StemmedWords.Take(15)) + " ...");
            }

            var wordToIndex = globalVocabulary
                .Select((word, index) => new { word, index })
                .ToDictionary(index=> index.word, index => index.index);

            string outputPath = @"C:\Users\sbirc\Desktop\INGINERIE\An III Facultate\An 3 sem 1\LaboratorSistemeRecunoastereaFormelor\dataset_procesat.txt";
            Console.WriteLine("\nSe scrie fisierul 'dataset_procesat.txt.");
            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                foreach (string word in globalVocabulary)
                {
                    writer.WriteLine($"@attribute {word}");
                }

                writer.WriteLine("@data");

                foreach (var doc in documente)
                {
                    var frequencies = new SortedDictionary<int, int>();

                    foreach (var stem in doc.StemmedWords)
                    {
                        if (wordToIndex.TryGetValue(stem, out int idx))
                        {
                            frequencies[idx] = frequencies.ContainsKey(idx) ? frequencies[idx] + 1 : 1;
                        }
                    }

                    string sparseVector = string.Join(" ", frequencies.Select(f => $"{f.Key}:{f.Value}"));
                    string topicsStr = string.Join(" ", doc.Topics);

                    writer.WriteLine($"{doc.FileName} # {sparseVector} # {topicsStr} # {doc.FolderType}");
                }
            }

            Console.WriteLine("Fisierul a fost generat cu succes.");


            //Lab 3 - Calculare castig informational
            Console.WriteLine("\n--- Calculare Castig Informational (Etapa 1 - Lab 3) ---");

            double entropieTotala = CalculeazaEntropie(documente);
            Console.WriteLine($"Entropia totala S: {entropieTotala:F4}");

            List<string> vocabularList = globalVocabulary.ToList();
            Dictionary<string, double> gains = new Dictionary<string, double>();

            for (int i = 0; i < vocabularList.Count; i++)
            {
                string cuvant = vocabularList[i];

                var sPrezent = documente.Where(d => d.StemmedWords.Contains(cuvant)).ToList();

                var sAbsent = documente.Where(d => !d.StemmedWords.Contains(cuvant)).ToList();

                double pPrezent = (double)sPrezent.Count / documente.Count;
                double pAbsent = (double)sAbsent.Count / documente.Count;

                double ePrezent = CalculeazaEntropie(sPrezent);
                double eAbsent = CalculeazaEntropie(sAbsent);

                double gain = entropieTotala - (pAbsent * eAbsent) - (pPrezent * ePrezent);
                gains[cuvant] = gain < 0 ? 0 : gain;
            }

            string outputPathGain = @"C:\Users\sbirc\Desktop\INGINERIE\An III Facultate\An 3 sem 1\LaboratorSistemeRecunoastereaFormelor\dataset_cu_gain.txt";
            Console.WriteLine("Se scrie fisierul 'dataset_cu_gain.txt'...");

            using (StreamWriter writer = new StreamWriter(outputPathGain, false, Encoding.UTF8))
            {
                foreach (string word in globalVocabulary)
                {
                    writer.WriteLine($"@attribute {word} {gains[word]:F6}");
                }

                writer.WriteLine("@data");

                foreach (var doc in documente)
                {
                    var frequencies = new SortedDictionary<int, int>();

                    foreach (var stem in doc.StemmedWords)
                    {
                        if (wordToIndex.TryGetValue(stem, out int idx))
                        {
                            frequencies[idx] = frequencies.ContainsKey(idx) ? frequencies[idx] + 1 : 1;
                        }
                    }

                    string sparseVector = string.Join(" ", frequencies.Select(f => $"{f.Key}:{f.Value}"));
                    string topicsStr = string.Join(" ", doc.Topics);

                    writer.WriteLine($"{doc.FileName} # {sparseVector} # {topicsStr} # {doc.FolderType}");
                }
            }

            Console.WriteLine("Fisierul 'dataset_cu_gain.txt' a fost generat cu succes!");


        }
    }
}