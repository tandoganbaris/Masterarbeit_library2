using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;


namespace Masterarbeit_library2;

public class DataGenerator
{
    public int NumberOfFiles { get; set; }
    //public sealed class FolderBrowserDialog : System.Windows.Forms.CommonDialog
    public string FilePath { get; set; }
    public List<Package> Vehicles { get; set; }
    Dictionary<string, (Package, Package)> Data { get; set; }
    public double Largepack_min_percentage { get; set; }
    public double Largepack_max_percentage { get; set; }
    public double Mediumpack_min_percentage { get; set; }
    public double Mediumpack_max_percentage { get; set; }
    public List<Package> Packages { get; set; }
    public DataGenerator() { }
    public DataGenerator(List<Package> vehicles, string filepath, int numfiles, Dictionary<string, (Package, Package)> data)
    {
        Vehicles = vehicles;
        FilePath = filepath;
        NumberOfFiles = numfiles;
        Data = data;

    }
    public DataGenerator(string filepath, int numfiles) // default constructor
    {

        FilePath = filepath;
        NumberOfFiles = numfiles;
        Standardparameters();

    }


    public void Createfiles() // csv helper josh close
    {   for(int i = 0; i < NumberOfFiles; i++)
        {
            string filename = $"testload{i+1}.csv";
            string path = Path.Combine(FilePath,filename);
            using(var streamwriter = new StreamWriter(path))
            {
                using(var csvwriter = new CsvWriter(streamwriter, CultureInfo.InvariantCulture))
                {   FillDataList();
                    var input = Packages;
                    csvwriter.Context.RegisterClassMap<Infoclassmap>();
                    csvwriter.WriteRecords(input);

                }
            }
        }
        return;
    }
    public void CreateEmptyFile(string filename)
    {
        File.Create(filename).Dispose();
    }
    public static double GetSecureDouble() //https://code-maze.com/csharp-random-double-range/ 
    {
        RandomNumberGenerator.Create();
        var denominator = RandomNumberGenerator.GetInt32(2, int.MaxValue);
        double sDouble = (double)1 / denominator;
        return sDouble;
    }
    public static double GetSecureDoubleWithinRange(double lowerBound, double upperBound)
    {
        var rDouble = GetSecureDouble();
        var rRangeDouble = (double)rDouble * (upperBound - lowerBound) + lowerBound;
        return rRangeDouble;
    }
    public Package RandomPackage(string size, int volume, out int realvolume) //note that size needs to be defined with a min and max package
    {
        Random rnd = new Random();
        int length = 0;
        int width = 0;
        int height = 0;
        double tolerance = 0.2;
        Package rando = new Package(length, width, height);
        double optimallength = 0, optimalwidth = 0, optimalheight = 0;
        double volumeposition = Convert.ToDouble(volume - Data[size].Item1.Volume); //remove the small package volume from the existing volume.
        double positioninrange = volumeposition /(Convert.ToDouble(Data[size].Item2.Volume) - Convert.ToDouble(Data[size].Item1.Volume)); //where does it lie within the two allowed sizes of packages
        optimallength = (Data[size].Item1.Length + ((Data[size].Item2.Length - Data[size].Item1.Length)) * positioninrange);
        optimalwidth = (Data[size].Item1.Width + ((Data[size].Item2.Width - Data[size].Item1.Width)) * positioninrange);
        optimalheight = (Data[size].Item1.Height + ((Data[size].Item2.Height - Data[size].Item1.Height)) * positioninrange);
        int lrangemin = 0, lrangemax = 0, wrangemin = 0, wrangemax = 0, hrangemin = 0, hrangemax = 0;
        if(optimalheight*(1+tolerance) >= Data[size].Item2.Height)
        {
            
            hrangemax = Data[size].Item2.Height;
            hrangemin = Convert.ToInt32(optimalheight * (1 - tolerance));
            lrangemax= Data[size].Item2.Length;
            lrangemin= Convert.ToInt32(optimallength *(1-tolerance));
            wrangemax = Data[size].Item2.Width;
            wrangemin = Convert.ToInt32(optimalwidth *(1-tolerance));
        }
        else if(optimalheight * (1 + tolerance) < Data[size].Item2.Height && (optimalheight*(1- tolerance))> Data[size].Item1.Height)
        {
            hrangemax = Convert.ToInt32(optimalheight *(1+ tolerance));
            hrangemin = Convert.ToInt32(optimalheight *(1- tolerance));
            lrangemax = Convert.ToInt32(optimallength *(1+ tolerance));
            lrangemin = Convert.ToInt32(optimallength *(1- tolerance));
            wrangemax = Convert.ToInt32(optimalwidth *(1+ tolerance));
            wrangemin = Convert.ToInt32(optimalwidth *(1- tolerance));

        }
        else if((optimalheight * (1 - tolerance)) <= Data[size].Item1.Height)
        {
            hrangemax = Convert.ToInt32(optimalheight + tolerance * optimalheight);
            hrangemin = Data[size].Item1.Height;
            lrangemax = Convert.ToInt32(optimallength + tolerance * optimallength);
            lrangemin = Data[size].Item1.Length;
            wrangemax = Convert.ToInt32(optimalwidth + tolerance * optimalwidth);
            wrangemin = Data[size].Item1.Width;

        }

        if (Data.ContainsKey(size))

        {
            
            List<string> dimension = new List<string>();
            dimension.AddRange(new string[] { "Length", "Width", "Height" });
            string d1 = dimension[rnd.Next(dimension.Count)]; dimension.Remove(d1);
            string d2 = dimension[rnd.Next(dimension.Count)]; dimension.Remove(d2); //random choice of dimensions
            if ((d1.Contains("Length") && d2.Contains("Width")) || (d2.Contains("Length") && d1.Contains("Width")))
            {
                length = rnd.Next(lrangemin, lrangemax + 1); // where the multiplier would land us in range +- tolerance
                width = rnd.Next(wrangemin, wrangemax + 1);
                height = volume / (length * width); //will round down
                if (height > Data[size].Item2.Height) { height = Data[size].Item2.Height; } //if it overreaches then modify
                else if(height < Data[size].Item1.Height) { height = Data[size].Item1.Height; }

            }
            else if ((d1.Contains("Height") && d2.Contains("Width")) || (d2.Contains("Height") && d1.Contains("Width")))
            {

                height = rnd.Next(hrangemin, hrangemax + 1); // where the multiplier would land us in range +- tolerance
                width = rnd.Next(wrangemin, wrangemax + 1);
                length = volume / (height * width); //will round down
                if (length > Data[size].Item2.Length) { length = Data[size].Item2.Length; } //if it overreaches then modify
                else if(length < Data[size].Item1.Length) { length = Data[size].Item1.Length;}
            }
            else if ((d1.Contains("Height") && d2.Contains("Length")) || (d2.Contains("Height") && d1.Contains("Length"))) //OR
            {

                height = rnd.Next(hrangemin, hrangemax + 1); // where the multiplier would land us in range +- tolerance
                length = rnd.Next(lrangemin, lrangemax + 1);
                width = volume / (height * length); //will round down
                if (width > Data[size].Item2.Width) { length = Data[size].Item2.Width; } //if it overreaches then modify
                else if(width < Data[size].Item1.Width) { width = Data[size].Item1.Width;}
            }




        }
        else
        {
            rando.Error = "There is no info on package size in the dictionary";
        }


       
        Package rando2 = new Package(length,width,height); 
        rando = rando2;
        realvolume = length * width * height;
        return rando;
    }
    //public Package2D RandomPackage2D(string size, int volume, out int realvolume) //note that size needs to be defined with a min and max package
    //{
    //    Random rnd = new Random();
    //    int length = 0;
    //    int width = 0;
       
    //    double tolerance = 0.2;
    //    Package2D rando = new Package2D(length, width);
    //    double optimallength = 0, optimalwidth = 0;
    //    double volumeposition = Convert.ToDouble(volume - Data[size].Item1.Volume); //remove the small package volume from the existing volume.
    //    double positioninrange = volumeposition / (Convert.ToDouble(Data[size].Item2.Volume) - Convert.ToDouble(Data[size].Item1.Volume)); //where does it lie within the two allowed sizes of packages
    //    optimallength = (Data[size].Item1.Length + ((Data[size].Item2.Length - Data[size].Item1.Length)) * positioninrange);
    //    optimalwidth = (Data[size].Item1.Width + ((Data[size].Item2.Width - Data[size].Item1.Width)) * positioninrange);

    //    int lrangemin = 0, lrangemax = 0, wrangemin = 0, wrangemax = 0;
    //    if (optimalwidth * (1 + tolerance) >= Data[size].Item2.Width)
    //    {

            
    //        lrangemax = Data[size].Item2.Length;
    //        lrangemin = Convert.ToInt32(optimallength * (1 - tolerance));
    //        wrangemax = Data[size].Item2.Width;
    //        wrangemin = Convert.ToInt32(optimalwidth * (1 - tolerance));
    //    }
    //    else if (optimalwidth * (1 + tolerance) < Data[size].Item2.Width && (optimalwidth * (1 - tolerance)) > Data[size].Item1.Width)
    //    {
         
    //        lrangemax = Convert.ToInt32(optimallength * (1 + tolerance));
    //        lrangemin = Convert.ToInt32(optimallength * (1 - tolerance));
    //        wrangemax = Convert.ToInt32(optimalwidth * (1 + tolerance));
    //        wrangemin = Convert.ToInt32(optimalwidth * (1 - tolerance));

    //    }
    //    else if ((optimalwidth * (1 - tolerance)) <= Data[size].Item1.Width)
    //    {
          
    //        lrangemax = Convert.ToInt32(optimallength + tolerance * optimallength);
    //        lrangemin = Data[size].Item1.Length;
    //        wrangemax = Convert.ToInt32(optimalwidth + tolerance * optimalwidth);
    //        wrangemin = Data[size].Item1.Width;

    //    }

    //    if (Data.ContainsKey(size))

    //    {

    //        List<string> dimension = new List<string>();
    //        dimension.AddRange(new string[] { "Length", "Width"});
    //        string d1 = dimension[rnd.Next(dimension.Count)]; dimension.Remove(d1);
    //        string d2 = dimension[rnd.Next(dimension.Count)]; dimension.Remove(d2); //random choice of dimensions
    //        if ((d1.Contains("Length") && d2.Contains("Width")) || (d2.Contains("Length") && d1.Contains("Width")))
    //        {
    //            length = rnd.Next(lrangemin, lrangemax + 1); // where the multiplier would land us in range +- tolerance
               
    //            width = volume / (length); //will round down
    //            if (width > Data[size].Item2.Width) { width = Data[size].Item2.Width; } //if it overreaches then modify
    //            else if (width < Data[size].Item1.Height) { width = Data[size].Item1.Height; }

    //        }
    //        else if ((d2.Contains("Length") && d1.Contains("Width")) || (d1.Contains("Length") && d2.Contains("Width")))
    //        {

               
    //            width = rnd.Next(wrangemin, wrangemax + 1);
    //            length = volume / (width); //will round down
    //            if (length > Data[size].Item2.Length) { length = Data[size].Item2.Length; } //if it overreaches then modify
    //            else if (length < Data[size].Item1.Length) { length = Data[size].Item1.Length; }
    //        }
          

    //    }
    //    else
    //    {
    //        rando.Error = "There is no info on package size in the dictionary";
    //    }



    //    //int realvolumetest = length * width * height;

    //    //while ((volume - realvolumetest) > (volume * 0.3)) //how much it is allowed to deviate from the volume input
    //    //{
    //    //    if (realvolumetest == 0 || realvolumetest > volume) { break; }

    //    //    int caser = 0;
    //    //    if (length / optimallength < width / optimalwidth && length / optimallength < height / optimalheight) { caser = 1; } //length is too far off            
    //    //    else if (width / optimalwidth < length / optimallength && width / optimalwidth < height / optimalheight) { caser = 2; }//width is too far off
    //    //    else if (height / optimalheight < length / optimallength && height / optimalheight < width / optimalwidth) { caser = 3; }
    //    //    switch (caser)
    //    //    {
    //    //        case 1: 
    //    //            if ((optimallength - length) > 1.5) { length += Convert.ToInt32(rnd.NextDouble() * (optimallength - length)); realvolumetest = length * width * height; break; } //imagine 8 reaching 10, if opt is 9 it wouldnt increase, if 9,1 then the random double would still stall
    //    //            else if ((optimallength - length) < 1.5 && Data[size].Item2.Length - optimallength > 1) { length = Convert.ToInt32(optimallength); realvolumetest = length * width * height; break; } // imagine 6 to 7.1, no need to calculate
    //    //            else if ((optimallength - length) < 1.5 && Data[size].Item2.Length - optimallength < 1) { length = Data[size].Item2.Length; realvolumetest = length * width * height; break; } //imagine 8.8 to 9,4 (max 10), we just go max. 
    //    //            else break;

    //    //        case 2:
    //    //            if ((optimalwidth - width) > 1.5) { width += Convert.ToInt32(rnd.NextDouble() * (optimalwidth - width)); realvolumetest = length * width * height; break; }
    //    //            else if ((optimalwidth - width) < 1.5 && Data[size].Item2.Width - optimalwidth > 1) { width = Convert.ToInt32(optimalwidth); realvolumetest = length * width * height; break; }
    //    //            else if ((optimalwidth - width) < 1.5 && Data[size].Item2.Width - optimalwidth < 1) { width = Data[size].Item2.Width; realvolumetest = length * width * height; break; }
    //    //            else break;
    //    //        case 3:
    //    //            if ((optimalheight - height) > 1.5) { height += Convert.ToInt32(rnd.NextDouble() * (optimalheight - height)); realvolumetest = length * width * height; break; }
    //    //            else if ((optimalheight - height) < 1.5 && Data[size].Item2.Height - optimalheight > 1) { height = Convert.ToInt32(optimalheight); realvolumetest = length * width * height; break; }
    //    //            else if ((optimalheight - height) < 1.5 && Data[size].Item2.Height - optimalheight < 1) { height = Data[size].Item2.Height; realvolumetest = length * width * height; break; }
    //    //            else break;


    //    //    }
    //    //}
    //    Package2D rando2 = new Package2D(length, width);
    //    rando = rando2;
    //    realvolume = length * width;
    //    return rando;
    //}
    public void FillDataList()
    {
        List<Package> filllist = new List<Package>();
        if (Vehicles.Count > 0 && NumberOfFiles > 0 && Data.Keys.Count > 0)
        {
            List<Package> vehiclescopy = Vehicles.ToList();
            Random rnd = new Random();
            int r = rnd.Next(Vehicles.Count);
            Package chosenvehicle = vehiclescopy[r];
            int totalvolume = chosenvehicle.Volume;

            Dictionary<string, (Package, Package)> data_copy = new Dictionary<string, (Package, Package)>();
            foreach (var v in Data)
            { data_copy.Add(v.Key, v.Value); }

            while (data_copy.Keys.Count > 0)
            {
                double touse_vol = totalvolume * (Largepack_min_percentage + (Largepack_max_percentage - Largepack_min_percentage) * rnd.NextDouble());
                if (data_copy.Keys.Count == 1)
                {
                    touse_vol = totalvolume;
                    int no_of_packs = rnd.Next(Convert.ToInt32(touse_vol / data_copy.Last().Value.Item2.Volume), Convert.ToInt32(touse_vol / data_copy.Last().Value.Item1.Volume)); //at the random size (s,m,l) we can have max vol/minsize packages and min vol/maxsize
                    //int lastpacksvol = Convert.ToInt32(touse_vol / no_of_packs);
                    List<int> packagedimensions = new List<int>();
                    for (int i = 0; i < no_of_packs; i++)
                    {
                        packagedimensions.Add(data_copy.Last().Value.Item1.Volume); //add min size packages

                    }
                    touse_vol -= (data_copy.Last().Value.Item1.Volume * no_of_packs);
                    while (touse_vol > 0) //if theres remaining volume then distribute it. this will go below 0 but we will see how this changes with realvols
                    {
                        for (int i = 0; i < packagedimensions.Count; i++)
                        {
                            int increase = rnd.Next(0, data_copy.Last().Value.Item2.Volume - data_copy.Last().Value.Item1.Volume);//better would be a distribution with the "lastpacksvol" above as mean
                            if (packagedimensions[i] + increase < data_copy.Last().Value.Item2.Volume) //if vol can be increased then do it
                            {
                                packagedimensions[i] += increase;
                                touse_vol -= increase;
                            }
                            else { continue; }

                            if (i + 1 == packagedimensions.Count) //go back to the beginning
                            {
                                i = 0;
                            }
                        }
                    }
                    for (int i = 0; i < packagedimensions.Count; i++) //for all the volumes we create packages, the difference is then added to the remaining value. 
                    {
                        int realvol = 0;
                        int volume_input = packagedimensions[i];
                        Package p = RandomPackage(data_copy.Last().Key, volume_input, out realvol);
                        p.Size = data_copy.Last().Key;
                        filllist.Add(p);
                        touse_vol += volume_input - realvol;

                    }
                    while (touse_vol > data_copy.Last().Value.Item2.Volume/2) //we modify the remaining value so we check if more packs can be made
                    {
                        int i = 0;
                        int realvol = 0;
                        int volume_input = packagedimensions[rnd.Next(0, packagedimensions.Count)];
                        if (touse_vol - volume_input < 0) { i++; if (i > 100) { break; } continue; } //if after 100 iterations we cannot make a package we stop
                        Package p = RandomPackage(data_copy.Last().Key, volume_input, out realvol);
                        p.Size = data_copy.Last().Key;
                        filllist.Add(p);
                        touse_vol -= realvol;
                    }
                    data_copy.Clear();


                }
                else if (data_copy.Keys.Count == 3)
                {
                    int d = data_copy.Keys.Count - 1; //rnd.Next(data_copy.Keys.ToList().Count); //random key 
                    int no_of_max_packs = Convert.ToInt32(touse_vol / data_copy.ElementAt(d).Value.Item1.Volume); //at the random size (s,m,l) we can have max vol/minsize packages)
                    int sum = 0;
                    for (int i = 0; i < no_of_max_packs; i++) //create random volumes for that number of packages. Splt volume into sections. Soft boundry random division
                    {
                        while (sum < touse_vol + data_copy.ElementAt(d).Value.Item2.Volume)
                        {
                            int volume_input = rnd.Next(data_copy.ElementAt(d).Value.Item1.Volume, data_copy.ElementAt(d).Value.Item2.Volume);
                            int realvol = 0;
                            Package p = RandomPackage(data_copy.ElementAt(d).Key, volume_input, out realvol);
                            p.Size = data_copy.ElementAt(d).Key;
                            filllist.Add(p);
                            sum += realvol;
                            if (sum > touse_vol - data_copy.ElementAt(d).Value.Item2.Volume) //reached the range
                            {
                                i = no_of_max_packs;
                                break;
                            }
                        }
                    }
                    totalvolume -= sum;
                    data_copy.Remove(data_copy.ElementAt(d).Key);
                }
                else if (data_copy.Keys.Count == 2)
                {
                    touse_vol = totalvolume * (Mediumpack_min_percentage + (Mediumpack_max_percentage - Mediumpack_min_percentage) * rnd.NextDouble());
                    int d = data_copy.Keys.Count - 1; //rnd.Next(data_copy.Keys.ToList().Count); //random key 
                    int no_of_max_packs = Convert.ToInt32(touse_vol / data_copy.ElementAt(d).Value.Item1.Volume); //at the random size (s,m,l) we can have max vol/minsize packages)
                    int sum = 0;
                    for (int i = 0; i < no_of_max_packs; i++) //create random volumes for that number of packages. Splt volume into sections. Soft boundry random division
                    {
                        while (sum < touse_vol + data_copy.ElementAt(d).Value.Item2.Volume)
                        {
                            int volume_input = rnd.Next(data_copy.ElementAt(d).Value.Item1.Volume, data_copy.ElementAt(d).Value.Item2.Volume);
                            int realvol = 0;
                            Package p = RandomPackage(data_copy.ElementAt(d).Key, volume_input, out realvol);
                            p.Size = data_copy.ElementAt(d).Key;
                            filllist.Add(p);
                            sum += realvol;
                            if (sum > touse_vol - data_copy.ElementAt(d).Value.Item2.Volume) //reached the range
                            {
                                i = no_of_max_packs;
                                break;
                            }
                        }
                    }
                    totalvolume -= sum;
                    data_copy.Remove(data_copy.ElementAt(d).Key);
                }
            }

            Packages = filllist.OrderBy(i => Guid.NewGuid()).ToList();
            //Packages = filllist.ToList(); // LARGE MEDIUM SMALL ORDER




        }
        else
        {
            Package errorpack = new Package(0, 0, 0);
            errorpack.Error = "input parameters missing or faulty";
            filllist.Add(errorpack);
            Packages = filllist;



            return;


        }
    }
    public void Standardparameters()
    {
        Dictionary<string, (Package, Package)> Datastandard = new Dictionary<string, (Package, Package)>();
        Package small1 = new Package(15, 11, 1); //DHL STANDARD SMALL(15,11,1)/LARGE(60,30,15)-(120,60,60) 
        Package small2 = new Package(30, 20, 10);
        Package large1 = new Package(60, 30, 15);
        Package large2 = new Package(120, 60, 60);
        Package medium1 = new Package(30, 20, 10);
        Package medium2 = new Package(60, 30, 15);
        Datastandard.Add("small", (small1, small2));
        Datastandard.Add("medium", (medium1, medium2));
        Datastandard.Add("large", (large1, large2));
        Data = Datastandard;
        Package Sprinter_standard_internal = new Package(327, 178, 181); //Standard mit Hochdach, 3500 kg, Hinterradantrieb, https://www.mercedes-benz.at/vans/de/sprinter/panel-van/technical-data
        List<Package> vehiclesstandard = new List<Package>();
        vehiclesstandard.Add(Sprinter_standard_internal);
        Vehicles = vehiclesstandard.ToList();
        Largepack_max_percentage = 0.8;
        Largepack_min_percentage = 0.5;
        Mediumpack_max_percentage = 0.99; //of the remaining vol after large
        Mediumpack_min_percentage = 0.95;
    }

    static public bool IsValidPath(string path)
    {
        Regex r = new Regex(@"^(([a-zA-Z]:)|(\))(\{1}|((\{1})[^\]([^/:*?<>""|]*))+)$");
        return r.IsMatch(path);
    }




}

