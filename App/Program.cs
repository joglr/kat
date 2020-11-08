﻿using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.IO.Compression;
using System.Json;
using System.Net.Http;

namespace Tester
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (args.Length == 0) {
                //TODO print commands and options
            } else if (args.Length >= 1) {
                switch (args[0]){
                    case "submit":
                        if (args.Length == 2){
                            await runSubmitAsync(args[1]);
                        } else {
                            Console.WriteLine("submit takes 1 more argument, <filename>, e.g.: hello.java");
                        }
                        break;
                    case "test":
                        runTest();
                        break;
                    case "fetch":
                        if (args.Length == 2){
                            runFetch(args[1]);
                        } else {
                            Console.WriteLine("fetch takes 1 argument, <kattis-problem-shortname> e.g.: hello.java");
                        }
                        break;
                    case "template":
                        if (args.Length == 2) {
                            runTemplate(args[1]);
                        } else {
                            Console.WriteLine("template takes 1 more argument, <filename>, e.g.: hello.java");
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown first argument" + args[0]);
                        break;
                }
            }
        }

        private static async System.Threading.Tasks.Task runSubmitAsync(string filename)
        {
            string configPath = ".kattisrc"; //TODO this path points to dir where you call katpis, change this to some absolute path
            Dictionary<string, Dictionary<string, string>> configObject = ParseConfigFile(configPath);

            string loginurl = configObject["kattis"]["loginurl"];
            string submissionurl = configObject["kattis"]["submissionurl"];
            string submissionsurl = configObject["kattis"]["submissionsurl"];
            var contentObject = new Dictionary<string,string>
            {
                { "user", configObject["user"]["username"]},
                { "script", "true"},
                { "token", configObject["user"]["token"]},
                { "User-Agent", "kattis-cli-submit" },
            };
            var content = new FormUrlEncodedContent(contentObject);

            HttpClient client = new HttpClient();

            var loginResponse = await client.PostAsync(loginurl, content);
            Console.WriteLine(loginResponse.StatusCode);
            // var sessionCookie = loginResponse.Headers.GetValues("Set-Cookie").Where(x => x.Contains("EduSiteCookie")).First().Split(";").First();

            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();

            Console.WriteLine(loginResponseString);

            // Status
            contentObject = new Dictionary<string,string>
            {
                { "user", configObject["user"]["username"]},
                { "script", "true"},
                { "token", configObject["user"]["token"]},
                { "User-Agent", "kattis-cli-submit" },
            };
            content = new FormUrlEncodedContent(contentObject);


            // TODO submit file to kattis

            // string data = @"{
            //     'submit': 'true',
            //     'submit_ctr': 2,
            //     'language': 'Java,
            //     'mainclass': 'Tetris',
            //     'problem': 'tetris',
            //     'script': 'true',
            // }";

            // string submissionFiles = "{('sub_file[]', ('Tetris.java', '// some file content', 'application/octet-stream'))}";

            // submitReply = post(submitUrl, data=data, files=submissionFiles, cookies=loginReply.cookies, headers=headers);


            var submissionid = "6372563"; // TODO get from submission response
            var statusurl = $"{submissionsurl}/{submissionid}?json";
            var statusResponse = await client.PostAsync(statusurl, content);
            var statusResponseString = await statusResponse.Content.ReadAsStringAsync();
            JsonValue status = JsonObject.Parse(statusResponseString);

            // Console.WriteLine(statusResponseString);

            MatchCollection mc = Regex.Matches(statusResponseString, @"Test case 1\\/(\d+): ");
            string numOfTestcases = "Unknown";
            if (mc.Count > 0) {
                numOfTestcases = mc[0].Groups[1].ToString();
            }

            Console.WriteLine(status["status_id"].ToString());
            Console.WriteLine(status["testcase_index"].ToString() + " of " + numOfTestcases);
        }

        private static Dictionary<string, Dictionary<string, string>> ParseConfigFile(string configPath)
        {
            var sr = new StreamReader(configPath);
            var configObject = new Dictionary<string, Dictionary<string, string>>();
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                MatchCollection mc = Regex.Matches(line, @"\[(.+)\]", RegexOptions.Multiline);
                if (mc.Count >= 1)
                {
                    Match m = mc[0];
                    string sectionName = m.Groups[1].ToString();
                    var section = new Dictionary<string, string>();
                    configObject.Add(sectionName, section);

                    while (sr.Peek() >= 0)
                    {
                        line = sr.ReadLine();
                        mc = Regex.Matches(line, @"^(.+): (.+)$", RegexOptions.Multiline);
                        if (mc.Count <= 0) break;
                        m = mc[0];
                        string key = m.Groups[1].ToString();
                        string val = m.Groups[2].ToString();
                        configObject[sectionName].Add(key, val);
                    }
                }
            }

            return configObject;
        }

        public static void runTemplate(string filename)
        {
            string pattern = @".java$";
            string input = filename;
            if (Regex.Matches(input, pattern).Count() == 1) {
                string className = filename.Split(".").First();
                string javaPattern = $@"import java.util.Scanner;
import java.io.*;
import java.util.StringTokenizer;

public class {className} {{
    public static void main(String[] args) throws IOException {{
        var reader = new BufferedReader(new InputStreamReader(System.in));
        StringTokenizer st = new StringTokenizer(reader.readLine());
        int N = Integer.parseInt(st.nextToken());
        for (int i = 0; i < N; i++) {{
            String[] line = reader.readLine().split("" "");
        }}
        System.out.println(""Output"");
    }}
}}";
                File.WriteAllText(filename, javaPattern);
            } else {
                Console.WriteLine($"filename: {filename} doesn't end with any of the supported file extensions: [ .java ]");
            }
        }

        public static void runFetch(string problemName)
        {

            Console.WriteLine("Fetching...");
            
            string cd = System.Environment.CurrentDirectory;

            string url = $"https://open.kattis.com/problems/{problemName}/file/statement/samples.zip";

            string zipPath = $@"{cd}\samples.zip";

            using (var client = new WebClient())
            {
                client.DownloadFile(url, zipPath);
            }

            try {
                ZipFile.ExtractToDirectory(zipPath, cd);
            } catch (IOException err) {
                Console.WriteLine(
                    $"Filename conflict with one or more of the sample files, '{err.Message.Split(@"\").Last()} Please remove existing files with this name from the current directory."
                    // Todo display all conflicts, and give option to override
                );
            }
            File.Delete(zipPath);

        }

        public static void runTest()
        {
            Console.WriteLine("Test...");
            string cd = System.Environment.CurrentDirectory;

            // 1. Reads .in file(s) from tests
            string[] files = Directory.GetFiles(cd);
            List<string> inFiles = new List<string>();
            List<string> ansFiles = new List<string>();
            foreach (string file in files)
            {
                if (file.EndsWith(".in")) inFiles.Add(file);
                if (file.EndsWith(".ans")) ansFiles.Add(file);
            }
            Console.WriteLine("Found " + inFiles.Count + " pairs of sample test files");

            // 2. Determine program file extension

            string programPath = null;
            foreach (string file in files)
            {
                if (file.EndsWith(".java")) programPath = file;
            }
            if (programPath == null) {
                Console.WriteLine("No program was found, see supported file extensions"); //TODO create list of supported file extensions
                return;
            }

            Console.WriteLine(
                "Found the program file " + programPath.Split(@"\").Last()
            );

            // 3. Run program with in files
            
            foreach (string inFile in inFiles)
            {
                string command = "java " + programPath + " < " + inFile;

                // 4. Get result
                // 5. Read output of program
                List<string> actualOutput = RunTestCommand(command);        

                string ansFile = inFile.Replace(".in", ".ans");
                if (!ansFiles.Contains(ansFile)) throw new FileNotFoundException(
                    "Could not find " + ansFile.Split(@"\").Last() + " in the current directory"
                );

                string inFileName = inFile.Split(@"\").Last();
                string ansFileName = ansFile.Split(@"\").Last();

                // 6. Compare to .ans file(s)
                List<string> expectedOutput = File.ReadAllLines(ansFile).ToList();

                // 7. Output differences or accepted

                actualOutput = TrimTrailingWhiteSpace(actualOutput);
                expectedOutput = TrimTrailingWhiteSpace(expectedOutput);

                string testName = inFileName.Replace(".in", "");
                if (IsEqual(actualOutput, expectedOutput)) {
                    Console.WriteLine("Test " + testName + " Passed");
                } else {
                    Console.WriteLine("Test " + testName + " Failed");
                    Console.WriteLine("Expected output for " + ansFileName + " :");
                    foreach (string line in expectedOutput) Console.WriteLine(line);
                    Console.WriteLine("Program output for " + inFileName + " :");
                    foreach (string line in actualOutput) Console.WriteLine(line);
                }

            }

        }

        private static bool IsEqual(List<string> actualOutput, List<string> expectedOutput)
        {
            // if (actualOutput.Count != expectedOutput.Count) return false;
            for (int i = 0; i < expectedOutput.Count - 1; i++)
            {
                if (actualOutput[i] != expectedOutput[i]) return false;
            }
            return true;
        }

        private static List<string> TrimTrailingWhiteSpace(List<string> outputLines)
        {
            for (int i = outputLines.Count - 1; i >= 0; i--)
            {
                if (outputLines[i] == null) {
                    outputLines.RemoveRange(i,1);
                } else {
                    string pattern = @"^\s+$";
                    string input = outputLines[i];
                    RegexOptions options = RegexOptions.Multiline;
                    if (Regex.Matches(input, pattern, options).Count() > 0) outputLines.RemoveRange(i,1);
                }
            }
            return outputLines;
        }

        static List<string> RunTestCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            var process = Process.Start(processInfo);

            List<string> outputLines = new List<string>();
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                // Console.WriteLine("output>>" + e.Data);
                outputLines.Add(e.Data);
            };
            process.BeginOutputReadLine();

            // process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            //     Console.WriteLine("error>>" + e.Data);
            process.BeginErrorReadLine();

            process.WaitForExit();

            // Console.WriteLine("ExitCode: {0}", process.ExitCode);
            process.Close();
            return outputLines;
        }

    }
}
