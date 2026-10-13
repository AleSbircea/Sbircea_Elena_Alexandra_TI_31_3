using System;
using System.Collections.Generic;
using System.IO;
namespace LaboratorSistemeRecunoastereaFormelor
{
    class DocumentData
    {
        public string FileName { get; set; }
        public string FolderType { get; set; } 
        public List<string> Topics { get; set; } = new List<string>();
        public List<string> StemmedWords { get; set; } = new List<string>();
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

            foreach (string file in trainFiles)
            {
            }

            foreach (string file in testFiles)
            {
            }
        }
    }
}