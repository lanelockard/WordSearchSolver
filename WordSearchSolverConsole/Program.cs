﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using CommandLine;
using WordSearchSolver;

namespace WordSearchSolverConsole
{
    public class Program
    {
        public class Options
        {
            [Option('f', "word-search-file", HelpText = "A text file from which to read the word search grid. " +
                                                        "Takes precedence over '--word-search'.")]
            public string WordSearchFile { get; set; }

            [Option("words-file", HelpText = "A text file from which to read the search words. Takes precedence over" +
                                             "'--words'.")]
            public string WordsFile { get; set; }

            [Option("word-search",
                HelpText = "A string containing the word search. Rows are delimited with a semicolon.")]
            public string WordSearch { get; set; }

            [Option('w', "words", HelpText = "A string containing the search words, delimited with semicolons.")]
            public string Words { get; set; }

            [Option('o', "allow-overlapping", HelpText =
                "Searches for overlapping words (words that originate from the" +
                "same location and point in the same direction).")]
            public bool AllowOverlapping { get; set; }
            
            [Option("solutions-format", HelpText = "The method to use to display solutions on the word search. " +
                                                     "Available options are 'asterisk', 'parentheses', 'color', " +
                                                     "'single-color' or 'none'. Defaults to 'color'.")]
            public string SolutionsFormat { get; set; }


            [Option('h', "hspace", HelpText = "The amount of space to put between the columns of a formatted word" +
                                              " search.")]
            public int HSpace { get; set; } = 1;

            [Option('v', "vspace", HelpText = "The amount of space to put between the rows of a formatted word" +
                                              " search.")]
            public int VSpace { get; set; } = 0;

            public ISolutionFormatter GetSolutionFormatter()
            {
                if (string.IsNullOrEmpty(SolutionsFormat))
                    return new ColorSolutionFormatter();
                
                switch (SolutionsFormat.ToLower())
                {
                    case "asterisk":
                    case "a":
                        return new AsteriskSolutionFormatter();
                    case "parentheses":
                    case "p":
                        return new ParenthesesSolutionFormatter();
                    case "color":
                    case "c":
                        return new ColorSolutionFormatter();
                    case "single-color":
                    case "singlecolor":
                    case "sc":
                        return new SingleColorSolutionFormatter();
                    case "none":
                    case "n":
                        return new DummySolutionFormatter();
                    default:
                        Console.WriteLine("The provided solution format was invalid!");
                        return null;
                }
            }

            public IEnumerable<string> GetWordSearch()
            {
                if (WordSearchFile != null)
                    return ReadWordSearchFile();

                if (WordSearch != null)
                    return ParseWordSearch();
                
                Console.WriteLine("The word search must be provided in some way, rather directly or by a word " +
                                  "search file!");
                return null;
            }

            public IEnumerable<string> GetWords()
            {
                if (WordsFile != null)
                    return ReadWordsFile();

                if (Words != null)
                    return ParseWords();
                
                Console.WriteLine("The search words list must be provided in some way, rather directly or by a " +
                                  "words list file!");
                return null;
            }

            private IEnumerable<string> ReadWordSearchFile()
            {
                return File.ReadAllLines(WordSearchFile);
            }

            private IEnumerable<string> ParseWordSearch()
            {
                return WordSearch.Split(';').Select(s => s.Trim());
            }

            private IEnumerable<string> ReadWordsFile()
            {
                return File.ReadAllLines(WordsFile);
            }

            private IEnumerable<string> ParseWords()
            {
                return Words.Split(';').Select(s => s.Trim());
            }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Launch);
        }

        private static void Launch(Options options)
        {
            var wordSearch = GetWordSearchSafe(options);
            if (wordSearch == null) return;
            
            var words = GetWordsSafe(options);
            if (words == null) return;

            var solutionFormatter = options.GetSolutionFormatter();
            if (solutionFormatter == null) return;

            try
            {
                var f = new WordSearchFormatter(solutionFormatter, options.HSpace, options.VSpace);
                var w = new WordSearchSolverConsole(wordSearch, words, options.AllowOverlapping, f);
                if (w.Confirm())
                {
                    Console.WriteLine();
                    w.Launch();
                }
            }
            catch (Exception e)
            {
                ErrorHandler.HandleUnknownException(e);
            }
        }

        private static WordSearch GetWordSearchSafe(Options options)
        {
            try
            {
                var input = options.GetWordSearch();
                if (input == null) return null;
                
                var wordSearch = WordSearch.Parse(input);
                
                return wordSearch;
            }
            catch (IOException io)
            {
                ErrorHandler.HandleIOExeption("while trying to read the word search file", io);
            }
            catch (Exception e) when (e is FormatException || e is ArgumentException)
            {
                Console.WriteLine($"The word search couldn't be parsed: {e.Message}");
            }
            catch (Exception e)
            {
                ErrorHandler.HandleUnknownException("while trying to parse the word search", e);
            }

            return null;
        }

        private static IEnumerable<string> GetWordsSafe(Options options)
        {
            try
            {
                return options.GetWords();
            }
            catch (IOException io)
            {
                ErrorHandler.HandleIOExeption("while trying to read the words file", io);
            }
            catch (Exception e)
            {
                ErrorHandler.HandleUnknownException("while trying to parse the words list", e);
            }

            return null;
        }
    }
}