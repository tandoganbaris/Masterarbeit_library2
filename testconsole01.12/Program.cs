// Seeusing System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Masterarbeit_library2;


namespace Masterarbeit_library2;

class Program
{
    public static void Main(string[] args)
    {
        string filepath = @"C:\classes\masterarbeit\instances\N_T\N_T\N1c.ins2D";
        Filehandler fhandler = new Filehandler(filepath);
        //foreach(Package2D p2 in fhandler.Packagelist)
        //{
        //    Console.WriteLine(p2);
        //}

        Extreme_Algorithms algos = new Extreme_Algorithms();
        algos.Input_packages = fhandler.Packagelist.ToList();
        algos.Main_SU();











    }
}

