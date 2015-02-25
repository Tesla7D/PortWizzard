using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace portwizzard
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = "";
            string inputName = "";
            string outputName = "";

            //
            // Extract command line parameters
            //
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i")
                {
                    if (i + 1 < args.Length)
                    {
                        inputName = args[i + 1];
                    }
                }
                else if (args[i] == "-o")
                {
                    if (i + 1 < args.Length)
                    {
                        outputName = args[i + 1];
                    }
                }
            }

            //
            // Make sure an input and output file was provided
            //
            if (inputName == "" || outputName == "")
            {
                Console.WriteLine("Usage error: -i <inputfile> -o <outputfile>");

                //
                // Returning to exit the function early for simple conditions is easier to read than deep nesting
                // This is called a gaurd statement in Fowler's refactoring
                // http://refactoring.com/catalog/replaceNestedConditionalWithGuardClauses.html
                //
                return;
            }

            //
            // Read input File
            //
            try
            {
                code = File.ReadAllText(inputName);
            }
            catch (System.IO.FileNotFoundException exception)
            {
                Console.WriteLine("Error: Input file not found");
                return;
            }

            //
            // Edit the code in various ways
            //
            code = ConvertStrings(code);

            Console.WriteLine(code);

            
            
        }

        static string ConvertStrings(string code)
        {
            return Regex.Replace(code, @"char\s+\*?\w+\[?\d*\]?", delegate(Match matchData)
            {
                string match = matchData.ToString();

                // Removes 'char' from match
                match = match.Substring(4);

                return "string " + Regex.Match(match,@"\w+").ToString();
            });
        }

        static string ConvertPrintf(string code)
        {
            // printf\s*\(\s*\".+?[^\\]\"\s*\)
            return Regex.Replace(code,"printf\\s*\\(\\s*\\\".+?[^\\\\]\\\"\\s*\\)", delegate(Match matchData)
            {
                string match = matchData.ToString();
                Regex.Replace(match,@"%\d*(\.\d+)?l?[a-z]",delegate(Match matchData){

                    return ":O";
                });
                return "Console.WriteLine()";
            });
        }
    }
}
