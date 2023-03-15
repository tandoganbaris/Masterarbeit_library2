
using System;
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
        string filepath = @"C:\classes\masterarbeit\instances\N_T\N_T\T7e.ins2D";
    
        //@"C:\classes\masterarbeit\instances\N_T\N_T\N1c.ins2D"
        string outputpath = @"C:\Users\tando\Desktop\tests\";
        Filehandler fhandler = new Filehandler(filepath);
        fhandler.Output = outputpath;
       
        Extreme_Algorithms algos = new Extreme_Algorithms();
        algos.Input_packages = fhandler.Packagelist.ToList();
        algos.Main_OffURPrep();
        Console.WriteLine("WHERE THE MAGIC HAPPENS");
        fhandler.Loadorder.Clear();
        fhandler.Loadorder.AddRange(algos.Load_order.ToList());
      
        fhandler.Createfiles();

       








    }
}

