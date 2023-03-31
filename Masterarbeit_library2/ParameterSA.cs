using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Masterarbeit_library2;

public class ParameterSA
{
    public ParameterSA() { }
    public Parameter[] initialparameters { get; set; } = new Parameter[] { };
    public int Bestval { get; set; }
    public double InitialTemp { get; set; }

    public Dictionary<int, int> Neighborhood_sofar = new Dictionary<int, int>();
    public Extreme_Algorithms Solver { get; set; }
    Random rnd = new Random();
    public ParameterSA(int[] parameters, Extreme_Algorithms algos)
    {
        Parameter a1 = new Parameter(parameters[0], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5);
        Parameter a2 = new Parameter(parameters[1], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5);
        Parameter a3 = new Parameter(parameters[2], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5 }, 3);
        Parameter a4 = new Parameter(parameters[3], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5);
        Parameter rewarding = new Parameter(parameters[4], algos.StripHeight, new int[] { 0, 1 }, 1);
        Parameter opt = new Parameter(parameters[5], algos.StripHeight, new int[] { parameters[5] - 20, parameters[5] - 10, parameters[5], parameters[5] + 10, parameters[5] + 20 }, 3);

        Bestval = algos.StripHeight;

        Solver = algos;
        Add_globalhistory(initialparameters, Bestval);

    }
    public void Add_globalhistory(Parameter[] parameters, int objval)
    {

        string key = string.Empty;
        foreach (Parameter p in parameters)
        {
            key.Concat(Convert.ToString(p.CurrentParval));
        }
        int parkey = int.Parse(key);

        Neighborhood_sofar.Add(parkey, objval);
    }

    public void SA(Parameter[] parameters, Extreme_Algorithms Solver)
    {
        int iteration = 0;
        int limit = 1000;
        double Temperature = 10;
        InitialTemp = Temperature;
        double alpha = 0.99;
        while ((iteration < limit) | (parameters.Where(x => x.Fixed != true).ToArray().Length != 0))
        {
            //do Local search
            //add to history 
            //accept or deny and overrite
            //repeat
        }
    }
    public void LS(Parameter[] parameters, Extreme_Algorithms Solver, double Temperature)
    {
        Parameter[] par_copy = parameters.Where(x => x.Fixed != true).ToArray();
        while (par_copy.ToArray().Length > 0)
        {
            double choice = rnd.NextDouble();
            int index = Choose(par_copy.ToArray(), choice);
            int iteration = 0;
            int maxiteration = par_copy.ToArray()[index].Iterations_allowed;
            int direction = 2;
            while (iteration < maxiteration)
            {
                if ((par_copy.ToArray()[index].CurrentParval != par_copy.ToArray()[index].Average) &&
                        (par_copy.ToArray()[index].No_used > 1000))
                {
                    par_copy[index].CurrentParval = par_copy[index].Average;
                    par_copy[index].Fixed = true;
                    iteration = maxiteration;
                }
                else if ((par_copy.ToArray()[index].CurrentParval == par_copy.ToArray()[index].Average) &&
                        (par_copy.ToArray()[index].No_used < 1000))
                {
                    int oldobj = par_copy[index].CurrentObjval;
                    direction = rnd.Next(0, 2);
                    par_copy[index].UpdateVal(direction);
                    int newobj = Solver.RunNewPars(par_copy);
                    par_copy[index].CurrentObjval = newobj;

                    if (newobj < oldobj)//if better
                    {
                        if (newobj < Bestval)
                        {
                            Bestval = newobj;
                            Add_globalhistory(parameters, Bestval);
                            iteration = maxiteration;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (newobj == oldobj)//if same
                    {
                        //choose what to do depending on temparature
                    }
                    else if (oldobj < newobj)//if worse
                    {    //choose what to do depending on temperature
                        double whattodo = ((double)(newobj-oldobj) / (double)newobj) * ((InitialTemp-(InitialTemp-Temperature))/InitialTemp);
                        //here
                        par_copy[index].UndoLast();
                        direction = direction == 0 ? 1 : 0; //change the direction
                    }
                    iteration++;
                }
                else if ((par_copy.ToArray()[index].CurrentParval != par_copy.ToArray()[index].Average) &&
                        (par_copy.ToArray()[index].No_used < 1000))
                {

                }
            }
        }


    }
    public int Choose(Parameter[] parameters, double randomchoice)
    {
        SortedList<double, Parameter> SL = new SortedList<double, Parameter>();
        Parameter[] internalpar = parameters.Where(x => x.LSround_completed != true).ToArray();
        double sf = 0;
        int index = 0;
        foreach (Parameter p in internalpar)
        {
            if (p.Quality != 0) { SL.Add(sf + 0.2 + p.Quality, p); }
            else { SL.Add(sf + 0.2, p); }

        }

        foreach (double key in SL.Keys)
        {
            SL.Keys[SL.Keys.IndexOf(key)] = key / SL.Keys.Last();
        }
        for (int i = 0; i < SL.Keys.Count; i++)
        {
            if (SL.Keys[i] <= randomchoice)
            {
                index = i;
                break;
            }
        }
        return index;
    }
}
public class Parameter
{
    public int CurrentParval { get; set; } = int.MaxValue;
    public int CurrentObjval { get; set; } = int.MaxValue; //after moving and running, this needs update
    public int HistoryLength { get; set; } = 10;
    public bool Fixed { get; set; } = false;
    public int Iterations_allowed { get; set; } = 2;
    public bool LSround_completed { get; set; } = false;
    public int Average { get; set; } = 0;
    public double No_used { get; set; } = 0;
    public double No_Improved { get; set; } = 0;
    public double Quality { get; set; } = 0;
    public Dictionary<int, int> History { get; set; } = new Dictionary<int, int>(); // The parameter value, objective value 
    public int[] Range { get; set; } = new int[10];

    public Parameter() { }
    public Parameter(int input, int currentobjval, int[] range, int iterations_allowed)
    {
        HistoryLength = range.Length * 2;
        Range = range;
        CurrentParval = input;
        CurrentObjval = currentobjval;
        Iterations_allowed = iterations_allowed;
    }
    public void UpdateVal(int direction)
    {
        switch (direction)
        {
            case 0: //minus
                { //add to history, change current val, update average and quality, 

                    if (CurrentObjval < History.Last().Value) { No_Improved++; }
                    Archive(CurrentParval, CurrentObjval);
                    if (CurrentParval == Range[0])
                    {
                        CurrentParval = Range[Range.Length - 1];
                    }
                    else
                    {
                        CurrentParval = Range[Array.IndexOf(Range, CurrentParval) - 1];
                    }

                    Average = (int)History.Keys.Average();
                    CurrentObjval = int.MaxValue;

                    No_used++;
                    Quality = No_Improved / No_used;
                    break;
                }
            case 1: //plus
                {
                    if (CurrentObjval < History.Last().Value) { No_Improved++; }
                    Archive(CurrentParval, CurrentObjval);
                    if (CurrentParval == Range[Range.Length - 1])
                    {
                        CurrentParval = Range[0];
                    }
                    else
                    {
                        CurrentParval = Range[Array.IndexOf(Range, CurrentParval) + 1];
                    }
                    Average = (int)History.Keys.Average();
                    CurrentObjval = int.MaxValue;
                    No_used++;
                    Quality = No_Improved / No_used;
                    break;
                }
        }
        return;
    }
    public void UndoLast()
    {
        CurrentObjval = History.Last().Value;
        CurrentParval = History.Last().Key;
        History.Remove(History.Last().Key);

    }
    public void Archive(int input, int objval)
    {
        if (History.Keys.Count >= HistoryLength+1)
        {
            History.Remove(History.First().Key);
            History.Add(input, objval);
        }
        else
        {
            History.Add(input, objval);
        }
    }
}

