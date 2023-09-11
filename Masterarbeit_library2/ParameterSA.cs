using Google.OrTools.ConstraintSolver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Masterarbeit_library2;

public class ParameterSA : ICloneable
{
    public ParameterSA() { }

    public List<Package2D> Load_order { get; set; } = new List<Package2D>();
    internal Parameter[] initialparameters { get; set; } = new Parameter[] { };
    public int Bestval { get; set; } = int.MaxValue;
    public string bestparameters { get; set; } = string.Empty;
    public double InitialTemp { get; set; }
    public Stopwatch timerSA { get; set; } = new Stopwatch();
    public double PenatlyRange { get; set; } = 0.03;//0.06
    public double Timeperit { get; set; } = 0;
    public List<string> Output { get; set; } = new List<string> { };
    public List<List<int>> Scalelog { get; set; } = new List<List<int>>();

    public Dictionary<int[], int> Neighborhood_sofar = new Dictionary<int[], int>();

    public Dictionary<int[], Tuple<int, List<Package2D>>> Neighborhood_withLoad = new Dictionary<int[], Tuple<int, List<Package2D>>>();
    internal Extreme_Algorithms Solver { get; set; } = new Extreme_Algorithms();
    Random rnd = new Random();
    public ParameterSA(int[] parameters, Extreme_Algorithms algos)
    {
        Parameter a1 = new Parameter(parameters[0], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.3, 2);
        Parameter a2 = new Parameter(parameters[1], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2);
        Parameter a3 = new Parameter(parameters[2], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5 }, 3, 0.1, 1);
        Parameter a4 = new Parameter(parameters[3], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2);
        Parameter rewarding = new Parameter(parameters[4], algos.StripHeight, new int[] { 0, 1 }, 1, 0.1, 1); // 0,1
        Parameter opt = new Parameter(parameters[5], algos.StripHeight, new int[] { (int)(parameters[5] * (1 - 2 * PenatlyRange)), (int)(parameters[5] * (1 - PenatlyRange)), parameters[5], (int)(parameters[5] * (1 + PenatlyRange)), (int)(parameters[5] * (1 + 2 * PenatlyRange)) }, 3, 0.1, 1);
        initialparameters = new Parameter[] { a1, a2, a3, a4, rewarding, opt };
        Bestval = algos.StripHeight;

        Solver = algos;
        Add_globalhistory(initialparameters, Bestval);

    }
    public int[] Par_asKey(Parameter[] parameters)
    {
        int[] parkey = new int[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {

            parkey[i] = (int)parameters[i].CurrentParval;
        }


        return parkey;
    }

    public Parameter[] Key_asPar(int[] input, int objval)
    {

        //int a1val = Convert.ToInt32(inputasstring.First().ToString());

        Parameter a1 = new Parameter(input[0], objval, initialparameters[0].Range, 5, 0.3, 2);
        Parameter a2 = new Parameter(input[1], objval, initialparameters[1].Range, 5, 0.2, 2);
        Parameter a3 = new Parameter(input[2], objval, initialparameters[2].Range, 3, 0.1, 1);
        Parameter a4 = new Parameter(input[3], objval, initialparameters[3].Range, 5, 0.2, 2);
        Parameter rewarding = new Parameter(input[4], objval, initialparameters[4].Range, 1, 0.1, 1);

        Parameter opt = new Parameter(input[5], objval, initialparameters[5].Range, 3, 0.1, 1);
        Parameter[] output = new Parameter[] { a1, a2, a3, a4, rewarding, opt };

        if (output.Last().CurrentParval == 0)
        {
            string here = string.Empty;
        }
        return output;

    }
    public Parameter[] Random_Par()
    {


        Parameter a1 = new Parameter(rnd.Next(0, 10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.3, 2);
        Parameter a2 = new Parameter(rnd.Next(0, 10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2);
        Parameter a3 = new Parameter(rnd.Next(0, 6), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5 }, 3, 0.1, 1);
        Parameter a4 = new Parameter(rnd.Next(0, 10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2);
        Parameter rewarding = new Parameter(rnd.Next(1, 2), int.MaxValue, initialparameters[4].Range, 1, 0.1, 1); //0,1
        //int inputopt = initialparameters.Last().CurrentParval;
        //int[] optval = new int[] { inputopt - 20, inputopt - 10, inputopt, inputopt + 10, inputopt + 20 };
        Parameter opt = new Parameter(initialparameters[5].Range[rnd.Next(initialparameters[5].Range.Length)], int.MaxValue, initialparameters[5].Range, 3, 0.1, 1);
        Parameter[] output = new Parameter[] { a1, a2, a3, a4, rewarding, opt };

        return output;

    }
    public void Add_globalhistory(Parameter[] parameters, int objval)
    {


        int[] parkey = Par_asKey(parameters.ToArray());

        Neighborhood_sofar.Add(parkey, objval);
    }
    public void Add_globalhistory_withLoad(Parameter[] parameters, int objval, List<Package2D> load)
    {


        int[] parkey = Par_asKey(parameters.ToArray());
        Tuple<int, List<Package2D>> outputtuple = new Tuple<int, List<Package2D>>(objval, load);
        Neighborhood_withLoad.Add(parkey, outputtuple);
    }


    public void SA(int TimeLimit)
    {
        if (TimeLimit == 0) { TimeLimit = int.MaxValue; }
        timerSA.Start();
        Bestval = int.MaxValue;
        int iteration = 0;
        int limit = 3;
        double Temperature = 5;
        InitialTemp = Temperature;
        double alpha = 0.95;
        Parameter[] par_internal = initialparameters.ToArray();
        //int startobjval = Bestval;
        int incumbentobjval = Bestval;
        while (iteration < limit && timerSA.ElapsedMilliseconds < TimeLimit)
        {
            int incumbentobjvalcopy = (int)incumbentobjval;
            Parameter[] par_internalcopy = (Parameter[])par_internal.Clone(); // par_internal.ToArray();
            LS(ref par_internalcopy, ref incumbentobjvalcopy, Temperature, TimeLimit); //do local search
            int[] Key_value = Par_asKey(par_internal);
            if (!Neighborhood_sofar.ContainsKey(Key_value)) { Add_globalhistory(par_internalcopy, incumbentobjvalcopy); } //add to history 
            if ((incumbentobjvalcopy <= Bestval)&& (incumbentobjvalcopy!=0))
            {
                Bestval = incumbentobjvalcopy;
                incumbentobjval = incumbentobjvalcopy;
                par_internal = par_internalcopy;  //override the parameters in the SA depending on the solution quality
                string parstring1 = String.Join(",", Par_asKey(par_internalcopy).Select(p => p.ToString()).ToArray());
                bestparameters = parstring1;
            }
            else if ((incumbentobjvalcopy <= incumbentobjval)&& (incumbentobjvalcopy != 0))
            {
                incumbentobjval = incumbentobjvalcopy;
                par_internal = par_internalcopy;

            }
            else
            {
                double decision = rnd.NextDouble();
                double diff = Convert.ToDouble(incumbentobjvalcopy - incumbentobjval);
                double currentstate = Math.Pow(Math.E, (-diff / Temperature));
                if (decision <= currentstate)
                {
                    incumbentobjval = incumbentobjvalcopy;
                    par_internal = par_internalcopy;
                }
                else
                {
                    if (Neighborhood_sofar.Keys.Count > 20)//choose random neighbor
                    {
                        int index = rnd.Next(0, Neighborhood_sofar.Keys.Count);
                        int[] key = Neighborhood_sofar.ElementAt(index).Key;
                        int val = Neighborhood_sofar.ElementAt(index).Value;
                        Parameter[] randomneighbor = Key_asPar(key, val);
                        incumbentobjval = val;
                        par_internal = randomneighbor;

                    }
                    else
                    {
                        incumbentobjval = int.MaxValue;
                        par_internal = Random_Par();

                    }
                }
            }
            iteration++;
            Temperature *= alpha;
            string parstring = String.Join(",", Par_asKey(par_internal).Select(p => p.ToString()).ToArray());
          
            Console.WriteLine("Parameters: " + parstring.PadRight(16) + "Obj val: ".PadLeft(14) + incumbentobjval.ToString());
        }
      
        Timeperit = (timerSA.ElapsedMilliseconds / (double)Neighborhood_sofar.Keys.Count);
        timerSA.Stop();
        return;
    }
    public void LS(ref Parameter[] parameters, ref int currentval, double Temperature, int TimeLimit)
    {
        try
        {
            Parameter[] par_copy = (Parameter[])parameters.Clone();
            int internal_Bestval = currentval;
            Dictionary<int, List<int[]>> Neighborhood_LS = new Dictionary<int, List<int[]>>(); // obj and parkey
            int[] parkeyinit = Par_asKey(parameters.ToArray());
            //Neighborhood_LS.Add(currentval, new List<int[]> { parkeyinit });
            List<Parameter[]> Samevalpars = new List<Parameter[]>();
            int samevalval = 0;
            Parameter[] Bestarray = par_copy.ToArray();
            bool unbrokenloop = true;
            while (unbrokenloop && timerSA.ElapsedMilliseconds < TimeLimit)
            {
                double choice = rnd.NextDouble();
                int index = Choose(par_copy.ToArray(), choice);
                if (index == int.MaxValue) { unbrokenloop = false; break; }
                int iteration = 0;
                int maxiteration = par_copy[index].Iterations_allowed;
                int direction = rnd.Next(0, 2); //eiher 0 minus or 1 plus
                int directionchange = 0; //have we changed direction so far for the parameter in this round of local search
                int stepsize = 1; //how many steps of move within the range
                while (iteration < maxiteration && timerSA.ElapsedMilliseconds < TimeLimit)
                {

                    int oldobj = currentval;//par_copy[index].CurrentObjval;

                    int[] par_copy_copy = Par_asKey(par_copy.ToArray());

                    par_copy[index].UpdateVal(direction, stepsize);
                    int[] parkey = Par_asKey(par_copy.ToArray());
                    int newobj = int.MaxValue;
                    if (!Neighborhood_sofar.ContainsKey(parkey))
                    {
                        Solver.Timelimit = TimeLimit - (int)timerSA.ElapsedMilliseconds;
                        newobj = Solver.RunNewPars(par_copy.ToArray()); //toarray?
                        List<int> tmp = Solver.Scalelog.ToList();
                        tmp.Insert(0, Solver.Epoint_total);
                        Scalelog.Add(tmp);
                        Neighborhood_sofar.Add(parkey, newobj);
                    }
                    else
                    {
                        iteration++;
                        continue;
                    }

                    par_copy[index].CurrentObjval = newobj;



                    
                    if (!(Neighborhood_LS.ContainsKey(newobj)))
                    {
                        Neighborhood_LS.Add(newobj, new List<int[]> { parkey });
                    }
                    else
                    {
                        Neighborhood_LS[newobj].Add(parkey);
                    }
                    if (newobj < oldobj)//if better
                    {
                        Samevalpars.Clear();
                        samevalval = 0;

                        if (newobj < internal_Bestval)
                        {
                            internal_Bestval = newobj;
                            Bestarray = par_copy.ToArray();
                            if (newobj <= Bestval)
                            {
                                Bestval = newobj;
                                Add_globalhistory(par_copy.ToArray(), Bestval);
                                Add_globalhistory_withLoad(par_copy.ToArray(), Bestval, Solver.Load_order.ToList());
                                iteration = maxiteration;
                                unbrokenloop = false;
                                foreach (Parameter p in par_copy) { p.LSround_completed = true; }
                                break;
                            }

                        }
                        else
                        {

                        }
                    }
                    else if (newobj == oldobj)//if same
                    {
                        samevalval = oldobj;
                        Samevalpars.Add(par_copy.ToArray());
                        if (Samevalpars.Count > 5) { unbrokenloop = false; iteration = maxiteration; break; }

                        double whattodo = (Temperature / InitialTemp);
                        if (whattodo < 0.60) //we are entering if temp is under 50 percent of initial
                        {

                            int refusalmove = rnd.Next(0, 2); //randomly choose whether to change direction 
                            if (refusalmove == 0)
                            {
                                par_copy[index].UndoLast();
                                if (directionchange == 0) //if direction has never been changed on this parameter in this round so far
                                {
                                    direction = direction == 0 ? 1 : 0;
                                    directionchange++;
                                }//change the direction
                                else //avoid going back and forth if the solution isnt improving
                                {
                                    iteration = maxiteration;
                                   
                                }
                            }
                        }
                        else { if (stepsize > 1) { stepsize--; } }

                    }
                    else if (oldobj < newobj)//if worse
                    {    //choose what to do depending on temperature
                        double diff = (double)(newobj - oldobj);
                        double whattodo = ((double)(oldobj - diff) / (double)oldobj) * (Temperature / InitialTemp);
                        //here
                        if (whattodo < 0.75) //we are refusing worse value (example 5 percent worse solution and already 0.9 og temp)
                        {
                            par_copy[index].UndoLast();
                            if (stepsize < par_copy[index].Maxstepsize)
                            {
                                int refusalmove = rnd.Next(0, 2); //randomly choose either to increase stepsize or to change direction 

                                if ((refusalmove == 0) | ((refusalmove == 1) && (directionchange != 0)))///if direction has been changed on this parameter in this round so far we force step increase
                                {
                                    stepsize++;
                                }
                               
                                else if ((refusalmove == 1) && (directionchange == 0))//if direction has never been changed on this parameter in this round so far
                                {

                                    direction = direction == 0 ? 1 : 0;
                                    directionchange++;

                                }
                                else //avoid going back and forth if the solution isnt improving
                                {
                                    iteration = maxiteration;
                                }
                                //either increase stepsize or reverse direction 
                            }
                            else //if step increase is not an option
                            {
                                if (stepsize > 1) { stepsize--; }
                                if (directionchange == 0) //if direction has never been changed on this parameter in this round so far
                                {
                                    direction = direction == 0 ? 1 : 0;
                                    directionchange++;
                                }//change the direction
                                else //avoid going back and forth if the solution isnt improving
                                {
                                    iteration = maxiteration;
                                }
                            }

                        }
                        else { stepsize = 1; }

                    }
                    iteration++;


                }
                par_copy[index].LSround_completed = true;

            }
            //if (Neighborhood_LS.Keys.Count > 1)
            //{   
            //    Neighborhood_LS.Remove(Neighborhood_LS.First().Key);
            //    int index = rnd.Next(0, Neighborhood_LS.First().Value.Count);
            //    List<int[]> chosenlist = Neighborhood_LS.First().Value;
            //    int[] parkey = chosenlist[index];
            //    Bestarray = Key_asPar(parkey, Neighborhood_LS.First().Key);
            //    currentval = Neighborhood_LS.First().Key;
            //}
            //else
            //{
            //  Neighborhood_LS.Add(newobj, new List<int[]> { parkey });

            if (Neighborhood_LS.First().Key != 0)
            {
                int index2 = rnd.Next(0, Neighborhood_LS.First().Value.Count);
                List<int[]> chosenlist = Neighborhood_LS.First().Value;
                int[] parkey2 = chosenlist[index2];
                Bestarray = Key_asPar(parkey2, Neighborhood_LS.First().Key);
                currentval = Neighborhood_LS.First().Key;
            }
            else if ((Neighborhood_LS.First().Key == 0) && (Neighborhood_LS.Keys.Count > 1))
            {
                Neighborhood_LS.Remove(Neighborhood_LS.First().Key);
                int index2 = rnd.Next(0, Neighborhood_LS.First().Value.Count);
                List<int[]> chosenlist = Neighborhood_LS.First().Value;
                int[] parkey2 = chosenlist[index2];
                Bestarray = Key_asPar(parkey2, Neighborhood_LS.First().Key);
                currentval = Neighborhood_LS.First().Key;
            }
            else
            {

                currentval = int.MaxValue;

            }


            //}
            //foreach (Parameter p in Bestarray)
            //{
            //    p.LSround_completed = false;
            //}
            //if (Bestarray.Last().CurrentParval == 0 || Bestarray.Last().CurrentParval < 80)
            //{
            //    string here = string.Empty;
            //}
            parameters = Bestarray.ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return;

    }



    public int Choose(Parameter[] parameters, double randomchoice)
    {
        SortedList<double, Parameter> SL = new SortedList<double, Parameter>();
        Parameter[] internalpar = (Parameter[])parameters.Where(x => x.LSround_completed != true).ToArray().Clone();
        double sf = 0;
        int index = 0;
        int outindex = int.MaxValue;
        if (internalpar.Length > 0)
        {

            foreach (Parameter p in internalpar)
            {
                sf = sf + p.Selection_Weight;
                if (p.Quality != 0)
                {
                    sf += p.Quality;
                    SL.Add(sf, p);
                }

                else { SL.Add(sf, p); }

            }

            randomchoice *= SL.Keys.Last();
            for (int i = 0; i < SL.Keys.Count - 1; i++)
            {
                if (SL.Keys[i + 1] >= randomchoice)
                {
                    index = i;
                    break;
                }
            }
            Parameter output = SL.Values[index];
            outindex = Array.IndexOf(parameters, output);
        }


        return outindex;
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
public class Parameter : ICloneable
{
    public int CurrentParval { get; set; } = -1;
    public double Selection_Weight { get; set; } = 0;
    public int CurrentObjval { get; set; } = int.MaxValue; //after moving and running, this needs update
    public int HistoryLength { get; set; } = 10;
    public bool Fixed { get; set; } = false;
    public int Iterations_allowed { get; set; } = 2;
    public bool LSround_completed { get; set; } = false;
    public int Average { get; set; } = 0;
    public int Maxstepsize { get; set; } = 1;
    public double No_used { get; set; } = 0;
    public double No_Improved { get; set; } = 0;
    public double Quality { get; set; } = 0;
    public Dictionary<int, int> History { get; set; } = new Dictionary<int, int>(); // The parameter value, objective value 
    public int[] Range { get; set; } = new int[10];

    public Parameter() { }
    public Parameter(int input, int currentobjval, int[] range, int iterations_allowed, double selectionweight, int maxstepsize)
    {
        HistoryLength = range.Length * 30;
        Range = range;
        CurrentParval = input;
        CurrentObjval = currentobjval;
        Iterations_allowed = iterations_allowed;
        Selection_Weight = selectionweight;
        Maxstepsize = maxstepsize;
    }
    public void UpdateVal(int direction, int stepsize)
    {


        switch (direction)
        {
            case 0: //minus
                { //add to history, change current val, update average and quality, 

                    if ((History.Count != 0) && (CurrentObjval < History.Last().Value)) { No_Improved++; }
                    Archive(CurrentParval, CurrentObjval);
                    if (Array.IndexOf(Range, CurrentParval) >= stepsize)
                    {
                        CurrentParval = Range[Array.IndexOf(Range, CurrentParval) - stepsize];
                    }
                    else if (Array.IndexOf(Range, CurrentParval) < stepsize)
                    {
                        CurrentParval = Range[Range.Length - (stepsize - Array.IndexOf(Range, CurrentParval))];

                    }

                    Average = (int)History.Keys.Average();
                    CurrentObjval = int.MaxValue;

                    No_used++;
                    if (Selection_Weight != 0) { Quality = No_Improved / No_used; }
                    break;
                }
            case 1: //plus
                {
                    if ((History.Count != 0) && (CurrentObjval < History.Last().Value)) { No_Improved++; }
                    Archive(CurrentParval, CurrentObjval);
                    if (Array.IndexOf(Range, CurrentParval) <= Range.Length - 1 - stepsize)
                    {
                        CurrentParval = Range[Array.IndexOf(Range, CurrentParval) + stepsize];
                    }
                    else if (Array.IndexOf(Range, CurrentParval) > Range.Length - 1 - stepsize)
                    {
                        int plusindex = stepsize - (Range.Length - 1 - Array.IndexOf(Range, CurrentParval)) - 1; // (stepsize - difference to top) -1   (as it starts at 0)
                        CurrentParval = Range[plusindex];
                    }
                    Average = (int)History.Keys.Average();
                    CurrentObjval = int.MaxValue;
                    No_used++;
                    if (Selection_Weight != 0) { Quality = No_Improved / No_used; }
                    break;
                }
        }
        //if (!Range.Contains(CurrentParval))
        //{
        //    string here = string.Empty;
        //}


        return;
    }
    public override string ToString()
    {
        return this.CurrentParval.ToString();
    }
    public void UndoLast()
    {
        CurrentObjval = History.Last().Value;
        CurrentParval = History.Last().Key;
        History.Remove(History.Last().Key);


    }
    public void Archive(int input, int objval)
    {
        if (!History.ContainsKey(input))
        {
            if (History.Keys.Count >= HistoryLength + 1)
            {
                History.Remove(History.First().Key);
                History.Add(input, objval);
            }
            else
            {
                History.Add(input, objval);
            }
        }
        else
        {
            if (History[input] > objval) { History[input] = objval; }
        }
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

