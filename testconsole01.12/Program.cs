
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
using Google.OrTools.ConstraintSolver;

namespace Masterarbeit_library2;

class Program
{
    public static void Main(string[] args)
    {
         //testing vstudio gitcommit

        string filepath = @"C:\classes\masterarbeit\instances\daniel\Burke\n10-formatted.txt";
        //"C:\classes\masterarbeit\instances\daniel\cgcut\cgcut1.txt"
        //"C:\classes\masterarbeit\instances\c\c\C1\C1_1.txt"
        //"C:\classes\masterarbeit\instances\daniel\Nice\nice1.txt"
        //"C:\classes\masterarbeit\instances\c\c\C7\C7_3.txt"
        //"C:\classes\masterarbeit\instances\daniel\babu\babu.txt"
        //"C:\classes\masterarbeit\instances\daniel\Burke\n12 -formatted.txt"
        //@"C:\classes\masterarbeit\instances\daniel\Burke\n11.txt";
        //"C:\classes\masterarbeit\instances\N_T\N_T\T7e.ins2D"
        //"C:\classes\masterarbeit\instances\daniel\Burke\n13.txt"
        //@"C:\classes\masterarbeit\instances\N_T\N_T\N1c.ins2D"
        //"C:\classes\masterarbeit\instances\daniel\Path\path6.txt"
        //@"C:\classes\masterarbeit\instances\daniel\Nice\nice6.txt";
        string outputpath = "C:\\classes\\masterarbeit\\tests";
        Filehandler fhandler = new Filehandler(filepath);
        fhandler.Output = outputpath;
        Extreme_Algorithms algomain = new Extreme_Algorithms();
        algomain.StripHeight = 1100;

        algomain.Input_packages = fhandler.Packagelist.ToList();
        //algomain.Input_Analysis();

        List<Extreme_Algorithms> solvers = new List<Extreme_Algorithms>() { };
        for (int i = 0; i < 5; i++)
        {
            solvers.Add((Extreme_Algorithms)algomain.Clone());
        }


        ParameterSA sa1 = new ParameterSA(solvers[0].Extract_Parameters(), solvers[0]);
        ParameterSA sa2 = new ParameterSA(solvers[1].Extract_Parameters(), solvers[1]);
        ParameterSA sa3 = new ParameterSA(solvers[2].Extract_Parameters(), solvers[2]);
        ParameterSA sa4 = new ParameterSA(solvers[3].Extract_Parameters(), solvers[3]);
        ParameterSA sa5 = new ParameterSA(solvers[4].Extract_Parameters(), solvers[4]);
        List<ParameterSA> anneilings = new List<ParameterSA> { sa1, sa2, sa3, sa4, sa5 };


        Runonce(fhandler, out List<string> list);

        //anneilings[0].SA();

        //string parstring = String.Join(",", sa1.Par_asKey(sa1.bestparameters).Select(p => p.ToString()).ToArray());

        //Console.WriteLine("Parameters: " + parstring.PadRight(16) + "Obj val: ".PadLeft(14) + sa1.Bestval.ToString());





        int timelimit = 480; //in minutes
        timelimit *= 60000;

        Parallel.For(0, 5, i =>
        {
            anneilings[i].SA(timelimit);

        });
        //anneilings[1].SA(timelimit);



        double averagetime = 0;
        foreach (ParameterSA p in anneilings)
        {


            Console.WriteLine("Parameters: " + p.bestparameters + "Obj val: ".PadLeft(14) + p.Bestval.ToString());


        }
        Console.WriteLine("Entire Neighborhood \n");
        foreach (ParameterSA p in anneilings)
        {
            foreach (var item in p.Neighborhood_sofar)
            {
                string parstring = String.Join(",", (item.Key).Select(p => p.ToString()).ToArray());
                Console.WriteLine(parstring + " : " + item.Value.ToString());


            }

            Console.WriteLine("Scaling:");
            foreach (List<int> l in p.Scalelog)
            {
                foreach (int i in l)
                {
                    Console.Write(i + ";");
                }
                Console.WriteLine();

            }
            Console.WriteLine("Elapsed time per individual run: " + p.Timeperit);
            averagetime += p.Timeperit / anneilings.Count;
            //foreach (var item in p.Neighborhood_withLoad)
            //{
            //    List<Package2D> outputorder = item.Value.Item2;
            //    fhandler.Loadorder.Clear();
            //    fhandler.Loadorder.AddRange(outputorder);
            //    fhandler.Stripheight = item.Value.Item1;
            //    try { fhandler.Createfiles(); }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e.Message);
            //    }


            //}

        }
        Console.WriteLine("On avg: " + averagetime);















    }
    public static void Runonce(Filehandler fhandler, out List<string> outputlist)
    {

        Extreme_Algorithms algos = new Extreme_Algorithms();
        algos.Input_packages = fhandler.Packagelist.ToList();
        algos.Input_Analysis();
        Stopwatch timer = new Stopwatch();
        timer.Start();
        algos.Main_OffURPrep2();
        //algos.Large_OffURPrep();
        //algos.Large_OffURPrep2();
        //algos.Main_OffUR2();
        timer.Stop();
        Console.WriteLine("Took this much time: " + timer.ElapsedMilliseconds);
        fhandler.Loadorder.Clear();
        fhandler.Loadorder.AddRange(algos.Load_order.ToList());
        fhandler.Stripheight = algos.StripHeight;

        fhandler.Createfiles();
        Console.WriteLine($"The Height is: {algos.StripHeight}");

        //ParameterSA anneiling = new ParameterSA(algos.Extract_Parameters(), algos);
        //anneiling.SA();

        //outputlist = (anneiling.Output);
        outputlist = new List<string>();
    }
}


