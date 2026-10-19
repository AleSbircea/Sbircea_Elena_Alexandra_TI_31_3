using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
                    .Select(c => (string)c.Attribute("value"))
                    .Where(val => !string.IsNullOrEmpty(val))
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
                    if (!stopwords.Contains(token) && token.Length > 2)
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
        }
    }
}