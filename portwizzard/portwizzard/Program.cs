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
            string inputPath = "";
            string outputPath = "";

            //
            // Extract command line parameters
            //
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i")
                {
                    if (i + 1 < args.Length)
                    {
                        inputPath = args[i + 1];
                    }
                }
                else if (args[i] == "-o")
                {
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[i + 1];
                    }
                }
            }

            //
            // Make sure an input and output file was provided
            //
            if (inputPath == "")
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
            // Default output path to input path .cs if not output path was provided
            //
            if (outputPath == "")
            {
                int index = inputPath.LastIndexOf(".");
                if (index != -1 && index > inputPath.LastIndexOfAny(new char[] { '\\', '/' }))
                {
                    outputPath = inputPath.Remove(inputPath.LastIndexOf(".")) + ".cs";
                }
                else
                {
                    outputPath = inputPath + ".cs";
                }
                
            }

            //
            // Read input File
            //
            try
            {
                code = File.ReadAllText(inputPath);
            }
            catch (System.IO.IOException exception)
            {
                Console.WriteLine("Error: Could not read input file");
                return;
            }

            //
            // Edit the code in various ways
            //
            code = ConvertIncludes(code);
            code = ConvertMain(code,inputPath);
            code = ConvertStrings(code);
            code = ConvertPrintf(code);
            code = ConvertGets(code);
            code = ConvertAtoi(code);
            code = RemoveMainReturn(code);
            code = UpdateHeaderComments(code,inputPath,outputPath);

            //
            // Write to output file
            //
            try
            {
                File.WriteAllText(outputPath, code);
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("Error: Could not write to output file");
            }
        }

        static string ConvertIncludes(string code)
        {
            int includeSpot = code.IndexOf("#include");
            if (includeSpot == -1)
            {
                includeSpot = 0;
            }

            code = code.Insert(includeSpot, "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Threading.Tasks;\nusing System.IO;\n");
            return Regex.Replace(code, @"#include.+?\n", "");
        }

        static string ConvertMain(string code, string outputPath)
        {
            string theNamespace = ExtractFileName(outputPath, false);

            int mainIndex = -1;
            code = Regex.Replace(code, @"int\s+main\s*\(.*?\)", delegate(Match matchData){
                string newMainHeader = "namespace " + theNamespace + "\n{\n\tclass Program\n\t{\n\t\tstatic void Main(string[] args)";
                mainIndex = matchData.Index + newMainHeader.Length;
                return newMainHeader;
            });

            //
            // Tab in rest of file
            //
            code = code.Substring(0,mainIndex) + code.Substring(mainIndex).Replace("\n","\n\t\t");

            code += "\n\t}\n}";

            return code;
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
            //
            // Finds all printf statements with this regular expression:
            // printf\s*\(\s*\".+?[^\\]\"
            // Can't use @ syntax here because the regular expression involves quotes which need to be escaped
            //
            return Regex.Replace(code,"printf\\s*\\(\\s*\\\".+?[^\\\\]\"", delegate(Match matchData)
            {
                int i = 0;
                string str = Regex.Match(matchData.ToString(), "\\s*\\(\\s*\\\".+?[^\\\\]\"").ToString();
                str = Regex.Replace(str, @"%\d*(\.\d+)?l?[a-z]", delegate(Match formatCodeMatch)
                {
                    //
                    // Add variable index
                    //
                    string oldFormatCode = formatCodeMatch.ToString();
                    string newFormatCode = "{" + i;
                    i++;

                    //
                    // Add padding to field
                    //
                    string padding = Regex.Match(oldFormatCode, @"\d+").ToString();
                    if (padding != "")
                    {
                        newFormatCode += "," + padding;
                    }
                    
                    newFormatCode += "}";

                    return newFormatCode;
                });

                //
                // printf behaves closer to Console.Write rather than Console.WriteLine
                // This is because WriteLine adds a newline to the end while printf and Write do not
                //
                return "Console.Write" + str;
            });
        }

        static string ConvertGets(string code)
        {
            return Regex.Replace(code, @"gets\s*\(.*?\)", delegate(Match matchData)
            {
                string variable = matchData.ToString().Replace("gets", "").Replace("(", "").Replace(")", "").Trim();
                return variable + " = Console.ReadLine()";
            });
        }

        static string ConvertAtoi(string code)
        {
            return code.Replace("atoi", "Convert.ToInt32");
        }

        static string UpdateHeaderComments(string code, string inputPath, string outputPath)
        {
            string inputFileName = ExtractFileName(inputPath,true);
            string outputFileName = ExtractFileName(outputPath,true);

            return code.Replace(inputFileName, outputFileName);
        }

        static string ExtractFileName(string path, bool includeExtension)
        {
            int fileNameStart = path.LastIndexOfAny(new char[] { '\\', '/' });
            if (fileNameStart == -1)
            {
                fileNameStart = 0;
            }
            else
            {
                fileNameStart++;
            }

            string fileName = path.Substring(fileNameStart);

            if (!includeExtension)
            {
                int extensionIndex = fileName.LastIndexOf('.');
                fileName = fileName.Remove(extensionIndex);
            }
            return fileName;
        }

        static string RemoveMainReturn(string code)
        {
            return Regex.Replace(code, @"return.*;\n?", "");
        }
    }
}
