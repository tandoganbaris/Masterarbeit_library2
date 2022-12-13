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
        //Package p = new Package(10, 10, 10);

        //Console.WriteLine("length"+p.LengthOG.ToString()+"widht"+p.WidthOG.ToString()+"height"+p.HeightOG.ToString());

        //p.OverwritePosition(10, 10, 10, 7);
        //foreach (Point v in p.Pointslist)
        //{ Console.WriteLine(v); }
        //Console.WriteLine("    " + p.Vertixes.Count);
        //foreach (Vertex v in p.Vertixes)
        //{
        //    Console.WriteLine(v.Length);
        //}
        //p.OverwritePosition(1111, 1111, 1111, 1);
        //foreach (Point v in p.Pointslist)
        //{ Console.WriteLine(v); }
        //Console.WriteLine("    " + p.Vertixes.Count);
        //foreach (Vertex v in p.Vertixes)
        //{
        //    Console.WriteLine(v.Length);
        //}
        DataGenerator dataGenerator = new DataGenerator();
        dataGenerator.Standardparameters();

        dataGenerator.FilePath = @"C:\Users\baris\Desktop\test\";
        dataGenerator.NumberOfFiles = 20;
        //for (int i = 0; i < 100; i++)
        //{ dataGenerator.FillDataList(); }
        //foreach (var item in dataGenerator.Packages)
        //{ Console.WriteLine(item); }
        //int test;

        //Console.WriteLine(dataGenerator.RandomPackage("small", 5997, out test));
        dataGenerator.Createfiles();
    }
}

