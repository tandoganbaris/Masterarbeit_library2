
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Masterarbeit_library2;
using System.Security.Cryptography.X509Certificates;

namespace Masterarbeit_library2;

class Program
{
    public static void Main(string[] args)
    {

        string filepath = @"C:\classes\masterarbeit\instances\daniel\Burke\n6-formatted.txt";
        ////"C:\classes\masterarbeit\instances\c\c\C7\C7_3.txt"
        ////"C:\classes\masterarbeit\instances\daniel\babu\babu.txt"
        ////"C:\classes\masterarbeit\instances\daniel\Burke\n12 -formatted.txt"
        ////@"C:\classes\masterarbeit\instances\daniel\Burke\n11.txt";
        ////"C:\classes\masterarbeit\instances\N_T\N_T\T7e.ins2D"
        ////"C:\classes\masterarbeit\instances\daniel\Burke\n13.txt"
        ////@"C:\classes\masterarbeit\instances\N_T\N_T\N1c.ins2D"
        string outputpath = @"C:\Users\tando\Desktop\tests\";








        //anneiling.SA();


        Runonce(filepath, outputpath);











    }
    public static void Runonce(string filepath, string outputpath)
    {
        Filehandler fhandler = new Filehandler(filepath);
        fhandler.Output = outputpath;

        Extreme_Algorithms algos = new Extreme_Algorithms();
        algos.Input_packages = fhandler.Packagelist.ToList();
        algos.Input_Analysis();
        Stopwatch timer = new Stopwatch();
        timer.Start();
        algos.Main_OffURPrep();
        timer.Stop();
        Console.WriteLine("Took this much time: " + timer.ElapsedMilliseconds);
        fhandler.Loadorder.Clear();
        fhandler.Loadorder.AddRange(algos.Load_order.ToList());

        fhandler.Createfiles();
        Console.WriteLine($"The Height is: {algos.StripHeight}");

        ParameterSA anneiling = new ParameterSA(algos.Extract_Parameters(), algos);
        anneiling.SA();
    }
}


