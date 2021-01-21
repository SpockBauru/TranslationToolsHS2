﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;

namespace MachineTranslate
{
    class MachineTranslate
    {
        //Dictionary with all translations from both source and output folder
        private static Dictionary<string, string> allTranslated = new Dictionary<string, string>();
        //Dictionary with untranslated lines just from source folder
        private static Dictionary<string, string> sourceUntranslated = new Dictionary<string, string>();
        //Dictionary with lines that are in Untranslated but not in Translated
        private static Dictionary<string, string> toTranslate = new Dictionary<string, string>();
        //Machine Translations Dictinary - Translations for source untranslated
        private static Dictionary<string, string> machineTranslated = new Dictionary<string, string>();
        //Error Dictionary
        private static Dictionary<string, string> translationErrors = new Dictionary<string, string>();

        //Maintain the HTTP Client open for more speed
        private static readonly HttpClient httpClient = new HttpClient();

        static void Main(string[] args)
        {
            //==================== Defining Languages ===================
            string fromLanguage = "ja";
            string toLanguage = "en";

            //==================== Folder Management ====================
            //Read Folder with text to be translated
            string mainFolder;

            if (args.Length != 0) { mainFolder = args[0]; }
            else
            {
                Console.Write("Enter the source folder path: ");
                mainFolder = Console.ReadLine();
                Console.WriteLine();
                if (string.IsNullOrEmpty(mainFolder))
                {
                    Console.Write("Invalid");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //get data from main folder
            DirectoryInfo sourceDir = new DirectoryInfo(mainFolder);

            //check if folder exists
            if (!sourceDir.Exists)
            {
                Console.WriteLine("Folder Not Found.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            //Making the output path the same as the .exe
            string thisFolder = AppDomain.CurrentDomain.BaseDirectory;
            string outputFolder = Path.Combine(thisFolder, "MachineTranslation");
            Directory.CreateDirectory(outputFolder);
            DirectoryInfo outputDir = new DirectoryInfo(outputFolder);


            //==================== Populating Dictionaries ====================
            //Getting all files from folder and subfolders
            FileInfo[] filesInSourceFolder = sourceDir.GetFiles("*.txt", SearchOption.AllDirectories);

            //populating the Translated dictionary with unique translations from source folder
            Console.WriteLine("Searching for translated lines in source folder");
            for (int i = 0; i < filesInSourceFolder.Length; i++)
            {
                string fileName = filesInSourceFolder[i].FullName;
                UpdateTranslatedDictionary(fileName);

                string displayFileNumber = "\rFile " + (i + 1).ToString() + " of " + filesInSourceFolder.Length.ToString();
                Console.Write(displayFileNumber);
            }
            Console.WriteLine();

            //populating the Translated dictionary with translations from the output folder (in case of a second run)
            Console.WriteLine("Searching for translated lines in the output folder");
            FileInfo[] filesInOutputFolder = outputDir.GetFiles("*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < filesInOutputFolder.Length; i++)
            {
                string fileName = filesInOutputFolder[i].FullName;
                UpdateTranslatedDictionary(fileName);

                string displayFileNumber = "\rFile " + (i + 1).ToString() + " of " + filesInOutputFolder.Length.ToString();
                Console.Write(displayFileNumber);
            }
            Console.WriteLine();


            //populating the Untranslated dictionary with unique entries from source folder
            Console.WriteLine("Searching for untranslated lines in the source folder");
            for (int i = 0; i < filesInSourceFolder.Length; i++)
            {
                string fileName = filesInSourceFolder[i].FullName;
                UpdateUntranslatedDictionary(fileName);

                string displayFileNumber = "\rFile " + (i + 1).ToString() + " of " + filesInSourceFolder.Length.ToString();
                Console.Write(displayFileNumber);
            }
            Console.WriteLine();

            //pupulating toTranslate dictionary with entries that are in Untranslated but not in Translated
            for (int i = 0; i < sourceUntranslated.Count; i++)
            {
                string key = sourceUntranslated.ElementAt(i).Key;
                if (!allTranslated.ContainsKey(key)) 
                    toTranslate.Add(key, "");
            }

            //==================== Translating via GoogleTranslate ====================
            string googleTranslateFile = Path.Combine(outputFolder,"1-GoogleTranslateRAW.txt");

            Console.WriteLine("Translating lines with Google Translate:");
            int toTranslateSize = toTranslate.Count;
            int requestCount = 0;

            for (int i = 0; i < toTranslateSize; i++)
            {
                string line = toTranslate.ElementAt(i).Key;
                string translatedLine;

                //Translating text between parenthesis separately
                if (line.IndexOfAny("（(".ToCharArray()) != -1)
                {
                    int startindex = line.IndexOfAny("（(".ToCharArray());
                    int endindex = line.IndexOfAny("）)".ToCharArray());
                    int lineLenght = line.Length - 1;

                    string before = line.Substring(0, startindex);
                    string translatedBefore = GoogleTranslate.Translate(fromLanguage, toLanguage, before, httpClient);
                    requestCount++;

                    startindex++;
                    string between = line.Substring(startindex, endindex - startindex);
                    string translatedBetween = GoogleTranslate.Translate(fromLanguage, toLanguage, between, httpClient);
                    requestCount++;

                    string after = line.Substring(endindex + 1, lineLenght - endindex);
                    string translatedAfter = "";
                    if (after.Length > 0)
                    {
                        translatedAfter = GoogleTranslate.Translate(fromLanguage, toLanguage, after, httpClient);
                        requestCount++;
                    }

                    translatedLine = translatedBefore + " (" + translatedBetween + ") " + translatedAfter;
                }
                else
                {
                    translatedLine = GoogleTranslate.Translate(fromLanguage, toLanguage, line, httpClient);
                    requestCount++;
                }

                translatedLine = line + "=" + translatedLine;

                File.AppendAllText(googleTranslateFile, translatedLine + Environment.NewLine);

                string displayCount = "\rLine " + (i + 1) + " of " + toTranslateSize;
                Console.Write(displayCount);

                //Google translate limit of 5 translations per second
                Thread.Sleep(200);

                //Sleep after 100 translations so your ip is not banned
                if (requestCount >= 100)
                {
                    Console.Write(" Sleeping for 20 seconds so Google Translate don't ban your IP");
                    requestCount = 0;
                    Thread.Sleep(20000);
                    Console.Write("\r                                                                               ");
                }
            }
            Console.WriteLine();


            //==================== Checking for Errors ====================
            Console.WriteLine("Checking for errors from CommonErrors.txt");

            //Updating translated dictionary with new translations
            filesInOutputFolder = outputDir.GetFiles("*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < filesInOutputFolder.Length; i++)
            {
                string fileName = filesInOutputFolder[i].FullName;
                UpdateTranslatedDictionary(fileName);
            }

            //populating machine dictionary with translations just for the untranslated text
            for (int i = 0; i < sourceUntranslated.Count; i++)
            {
                string key = sourceUntranslated.ElementAt(i).Key;
                string value = allTranslated[key];

                if (!machineTranslated.ContainsKey(key))
                    machineTranslated.Add(key, value);
            }

            //populating error dictionary with lines that contains at least one error
            string[] errorList = File.ReadAllLines(Path.Combine(thisFolder, "CommonErrors.txt"));
           
            for (int i = 0; i < machineTranslated.Count; i++)
            {
                for (int j = 0; j < errorList.Length; j++)
                {
                    string key = machineTranslated.ElementAt(i).Key;
                    string value = machineTranslated.ElementAt(i).Value;
                    string error = errorList[j];
                    if ((!string.IsNullOrEmpty(error)) && !(error.Substring(0, 2) == "//"))
                    {
                        if (value.Contains(error) && !translationErrors.ContainsKey(key))
                        {
                            translationErrors.Add(key, value);
                        }
                    }
                }
            }


            //==================== Translating errors with Bing Translator ====================
            string bingTranslationsFile = Path.Combine(outputFolder,"2-BingTranslateRAW.txt");
            int errorSize = translationErrors.Count;
            Console.WriteLine("Trying to translate errors with Bing translator");

            //Bing like this index for some reason...
            int bingIndex = 0;
            // Bing needs ID and IIG before start translating
            string[] bingSetup = new string[2];

            for (int i = 0; i < errorSize; i++)
            {
                if (i==0)
                {
                    //Getting BingTranslator's ID and IIG before start translating
                    bingSetup = BingTranslator.Setup(httpClient);
                }
                string line = translationErrors.ElementAt(i).Key;
                string translatedLine;

                //Translating text between parenthesis separately
                if (line.IndexOfAny("（(".ToCharArray()) != -1)
                {
                    int startindex = line.IndexOfAny("（(".ToCharArray());
                    int endindex = line.IndexOfAny("）)".ToCharArray());
                    int lineLenght = line.Length-1;

                    string before = line.Substring(0, startindex);
                    string translatedBefore= BingTranslator.Translate(fromLanguage, toLanguage, before, httpClient, bingSetup, bingIndex);
                    bingIndex++;

                    startindex++;
                    string between = line.Substring(startindex, endindex - startindex);
                    string translatedBetween= BingTranslator.Translate(fromLanguage, toLanguage, between, httpClient, bingSetup, bingIndex);
                    bingIndex++;

                    string after = line.Substring(endindex + 1, lineLenght - endindex);
                    string translatedAfter = "";
                    if (after.Length > 0)
                    {
                        translatedAfter = BingTranslator.Translate(fromLanguage, toLanguage, after, httpClient, bingSetup, bingIndex);
                        bingIndex++;
                    }

                    translatedLine = translatedBefore + " (" + translatedBetween + ") " + translatedAfter;
                }
                else
                {
                    translatedLine = BingTranslator.Translate(fromLanguage, toLanguage, line, httpClient, bingSetup, bingIndex);
                    bingIndex++;
                }                
                
                translatedLine = line + "=" + translatedLine;

                File.AppendAllText(bingTranslationsFile, translatedLine + Environment.NewLine);

                string displayCount = "\rLine " + (i + 1) + " of " + errorSize;
                Console.Write(displayCount);

                //Bing translate limit of translations per second
                Thread.Sleep(100);
            }
            Console.WriteLine();


            //Updating machine dictionary with Bing translations
            if (File.Exists(bingTranslationsFile))
             {
                string[] bingTranslationsString = File.ReadAllLines(bingTranslationsFile);
                for (int i = 0; i < bingTranslationsString.Length; i++)
                {
                    string line = bingTranslationsString[i];
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        machineTranslated[key] = value;
                    }
                }
            }


            //==================== Fixing Style Errors ====================
            Dictionary<string, string> substitutions = new Dictionary<string, string>();

            Console.WriteLine("Fixing Style errors from Substitutions.txt");

            //Populating substitution dictionary
            string subtitutionsFile = Path.Combine(thisFolder, "Substitutions.txt");
            string[] substitutionsString = File.ReadAllLines(subtitutionsFile);
            for (int i = 0; i < substitutionsString.Length; i++)
            {
                string line = substitutionsString[i];
                if (!string.IsNullOrEmpty(line) && !(line.Substring(0, 2) == "//"))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        substitutions.Add(key, value);
                    }
                }
            }

            //Fixing machineTranslated dictionary
            for (int i= 0; i < machineTranslated.Count; i++)
            {
                string key= machineTranslated.ElementAt(i).Key;
                string text = machineTranslated.ElementAt(i).Value;

                for (int j = 0; j < substitutions.Count; j++)
                {
                    string from = substitutions.ElementAt(j).Key;
                    string to = substitutions.ElementAt(j).Value;
                    text= text.Replace(from, to);
                }

                machineTranslated[key] = text;
            }

            //Writing final file
            string machineTranslationsFinalFile = Path.Combine(outputFolder, "MachineTranslationsFinal.txt");
            string[] machineTranslationsFinalString = new string[machineTranslated.Count];
            for (int i = 0; i < machineTranslated.Count; i++)
            {
                machineTranslationsFinalString[i] = machineTranslated.ElementAt(i).Key + "=" + machineTranslated.ElementAt(i).Value;
            }
            File.WriteAllLines(machineTranslationsFinalFile, machineTranslationsFinalString);


            //==================== Ending Console Dialogues ====================
            stopWatch.Stop();

            string display = "Elapsed time " + stopWatch.Elapsed;
            Console.WriteLine(display);
            Console.WriteLine("Press any key to exit");
            Console.WriteLine();
            Console.ReadKey();
        }


        static void UpdateTranslatedDictionary(string fileName)
        {
            //Read Current File
            string[] currentFile = File.ReadAllLines(fileName);

            //seek all lines of the current file
            for (int i = 0; i < currentFile.Length; i++)
            {
                string line = currentFile[i];

                //null check and add Uncommented lines to Translated dictionary
                if ((!string.IsNullOrEmpty(line)) && (!line[0].Equals('/')))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        if (!allTranslated.ContainsKey(key))
                        {
                            allTranslated.Add(key, value);
                        }
                        else if (allTranslated.ContainsKey(key) && (value != ""))
                        {
                            allTranslated[key] = value;
                        }
                    }
                }
            }
        }

        static void UpdateUntranslatedDictionary(string fileName)
        {
            //Read Current File
            string[] currentFile = File.ReadAllLines(fileName);

            //seek all lines of the current file
            for (int i = 0; i < currentFile.Length; i++)
            {
                string line = currentFile[i];

                //null check and add Commented lines to Untranslated dictionary
                if ((!string.IsNullOrEmpty(line)) && (line[0].Equals('/')))
                {
                    line = line.Replace("/", "");
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        if (!sourceUntranslated.ContainsKey(key))
                        {
                            sourceUntranslated.Add(key, value);
                        }
                    }
                }
            }
        }
    }
}
