using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using mccBuild;

namespace mccCli
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            List<Function> functions = new List<Function>();
            foreach (string s in args)
            {
                functions.Add(new Function(File.ReadAllLines(s.Split(';')[0]), s.Split(';')[1]=="true", s.Split(new []{'/','\\'}).Last().Replace(".mcc",""),Convert.ToInt32(s.Split(';')[2])));
            }
            foreach (string s in Build.GetOneCommand(functions))
            {
                Console.WriteLine();
                Console.WriteLine(s);
            }
        }
    }
}
