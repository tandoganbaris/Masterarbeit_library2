using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Masterarbeit_library2;

public class Filehandler
{
    public string? Input { get; set; }
    public string? Output { get; set; }

    public List<Package> Packagelist { get; set; } = new List<Package>();
    public Filehandler(string input)
    {
        Input = input;
        bool canberead = Readtest();
        if ((Input.Length > 0) && (canberead))
        {
            ReadLoad();

        }
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
    public void ReadLoad()
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

            int startindex = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("1") | lines[i].StartsWith("01"))
                {
                    startindex = i;
                    break;
                }
            }
            for (int i = startindex; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(';');
                int x = Convert.ToInt32(parts[1]);
                int y = Convert.ToInt32(parts[2]);
                int z = Convert.ToInt32(parts[3]);
                Package p = new Package(x, y, z);
                p.Indexes.Add("Loadorder", i);
                Packagelist.Add(p);

            }


        }
        return;

    }
}
