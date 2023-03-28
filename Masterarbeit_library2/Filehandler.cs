using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Formats.Asn1;
using System.Text.RegularExpressions;

namespace Masterarbeit_library2;

public class Filehandler
{
    public int NumberOfFiles { get; set; } = 1;
    public string? Input { get; set; }
    public string? Output { get; set; }
    public Package2D Depot { get; set; }
    public List<Package2D> Packagelist { get; set; } = new List<Package2D>();
    public List<Package2D> Loadorder { get; set; } = new List<Package2D>();
    public Filehandler(string input)
    {
        Input = input;
        bool canberead = Readtest();
        if ((Input.Length > 0) && (canberead))
        {
            ReadLoad2();

        }
    }

    public string GetUntilOrEmpty(string text, string stopAt = "-")
    {
        if (!String.IsNullOrWhiteSpace(text))
        {
            int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

            if (charLocation > 0)
            {
                return text.Substring(0, charLocation);
            }
        }

        return String.Empty;
    }

    public void Createfiles() // csv helper josh close
    {
        string input = Regex.Replace(Input, @"\s\s+", " ").Trim();
        string[] parts = input.Split("\\");
        string namepart = GetUntilOrEmpty(parts[parts.Length - 1], ".");
        string filename = $"{namepart}.csv";
        //string filename = $"testload{4}.{namepart}.csv";
        string path = Path.Combine(Output, filename);
        using (var writer = new StreamWriter(path))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            //csv.Context.RegisterClassMap<Infoclassmap2>();
            //string[] header = new string[] { "ID", "P1", "P2", "P3", "P4" };
            //csv.WriteField(header);
            //csv.NextRecord();
            foreach (var p in Loadorder)
            {
                string[] output = new string[5];
                if (p.Indexes.Count > 0) { output[0] = p.Indexes["Instance"].ToString(); }
                else { output[0] = "0"; }
                for (int i = 0; i < p.Pointslist.Count; i++) { output[i + 1] = (p.Pointslist[i].CSVFormat()).ToString(); }

                csv.WriteField(output);
                csv.NextRecord();
            }
        }

        return;
    }
    public bool Readtest()
    {
        try
        {
            File.Open(this.Input, FileMode.Open, FileAccess.Read).Dispose();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
    public void ReadLoad1() //for instances with id 
    {
        List<Point> points = new List<Point>();
        if (Input.Length > 0)
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(Input))
            {


                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Insert logic here.
                    // ... The "line" variable is a line in the file.
                    // ... Add it to our List.
                    lines.Add(line);
                }
            }

            int startindex = 2;
            for (int i = 2; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("1") | lines[i].StartsWith("01"))
                {
                    startindex = i;
                    break;
                }
            }
            for (int i = startindex; i < lines.Count; i++)
            {

                string input = Regex.Replace(lines[i], @"\s\s+", " ").Trim();

                //string[] parts = lines[i].Split(" ");
                string[] parts = input.Split(" ");
                int x = Convert.ToInt32(parts[1]);
                int y = Convert.ToInt32(parts[2]);

                Package2D p = new Package2D(x, y);
                p.Indexes.Add("Instance", Convert.ToInt32(parts[0]));
                Packagelist.Add(p);

            }
            //string[] parts2 = lines[startindex - 2].Split(" ");
            //string[] parts3 = lines[startindex - 1].Split(" ");
            //int xdepot = Convert.ToInt32(parts2[0]);
            //int ydepot = Convert.ToInt32(parts3[0]);

            //Package2D depot = new Package2D(xdepot, ydepot);
            //depot.Indexes.Add("Depot", 0);
            //Depot = depot;


        }
        return;
    }
    public void ReadLoad2() //for instances with id 
    {
        List<Point> points = new List<Point>();
        if (Input.Length > 0)
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(Input))
            {


                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Insert logic here.
                    // ... The "line" variable is a line in the file.
                    // ... Add it to our List.
                    lines.Add(line);
                }
            }

            int startindex = 2;
            //for (int i = 2; i < lines.Count; i++)
            //{
            //    if (lines[i].StartsWith("1") | lines[i].StartsWith("01"))
            //    {
            //        startindex = i;
            //        break;
            //    }
            //}
            for (int i = startindex; i < lines.Count; i++)
            {

                string input = Regex.Replace(lines[i], @"\s\s+", " ").Trim();

                //string[] parts = lines[i].Split(" ");
                string[] parts = input.Split(" ");
                int x = Convert.ToInt32(parts[0]);
                int y = Convert.ToInt32(parts[1]);

                Package2D p = new Package2D(x, y);
                p.Indexes.Add("Instance", Convert.ToInt32(i - startindex + 1));
                Packagelist.Add(p);

            }
            //string[] parts2 = lines[startindex - 2].Split(" ");
            //string[] parts3 = lines[startindex - 1].Split(" ");
            //int xdepot = Convert.ToInt32(parts2[0]);
            //int ydepot = Convert.ToInt32(parts3[0]);

            //Package2D depot = new Package2D(xdepot, ydepot);
            //depot.Indexes.Add("Depot", 0);
            //Depot = depot;


        }
        return;


    }
}



