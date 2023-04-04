using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration.CommandLine;

namespace test_api2.Controllers;

[ApiController]
[Route("[controller]")]
public class MatlabGlueController : Controller
{
    private readonly ILogger<WeatherForecastController> _logger;

    private void createMatlabScriptTmp()
    {
        string path = "/Users/jin/MATLAB/Projects/simpleAddition";
        StringBuilder sb = new StringBuilder();
        // sb.AppendLine($"cd {path};");
        sb.AppendLine("% This file is generated");
        sb.AppendLine("rng shuffle;");
        sb.AppendLine("result = simpleAdd(rand(1),rand(1));");
        sb.AppendLine("writematrix(result, \"./result.csv\");");
        System.IO.File.WriteAllText(path + "/tmp.m", sb.ToString());
    }

    private void createMatlabScriptSetup()
    {
        const string path = "/Users/jin/MATLAB/Projects/PSARM_Matlab-master"; 
        var sb = new StringBuilder();
        
        // Preparation 
        sb.AppendLine("tic;");
        sb.AppendLine("clear");
        sb.AppendLine("% Set the working directory"); 
        sb.AppendLine("folder=fileparts(which('main'));");
        sb.AppendLine("addpath(genpath(folder));");
        sb.AppendLine("strg = 'FD'; % FD, COMFORT");

        sb.AppendLine();
        sb.AppendLine();
        
        // Setting parameters
        // TODO: add conditional for COMFORT or FD
        sb.AppendLine("FDparam = 0.6;");
        sb.AppendLine("qfrac = 0.2;");
        sb.AppendLine("wsl = 250;");
        sb.AppendLine(
            "wS = 250; % estimation window (in-sample data); default = 1260 (5 years; 252 trading day per year)");
        sb.AppendLine(
            "wJ = 1; % forecasting window (rebalancing frequency; OOS data); COMFORT only does 1-step ahead forecast of H and Gt; thus, wJ only matters on the mean that is imposed AR/asym structure.");
        sb.AppendLine("tcl = [0,0.001]; % level of transaction costs");
        sb.AppendLine(
            "level_grid = 0.15; % ES level for ES portfolio optimization; (real-valued vector of) ES level(s), typical values 0.01 0.05 0.1");
        sb.AppendLine(
            "q_EwMinVec = [0.05, 0.1, 0.2]; % quantile levels for the mean on the frontier; DEAR (or tau) = quantile(EY(EY>EYwminVar),q_EwMinVec) for mean-ES/Var (uncon) portfolio optimization; EY is the mean vector;");
        sb.AppendLine(
            "f_shortallowed = 0.5; % amount of short positions allowed for max-Sharpe portfolio optimization; if f_shortallowed=X, then 100(1+X)/100*X portfolio allowed, e.g. 0.5 corresponds to 150/50 portfolio");
        sb.AppendLine();
        sb.AppendLine();
        
        // Set Data 
        sb.AppendLine("% Data");
        sb.AppendLine(
            "N_choose = 150; % set to NaN if you take only complete histories; to use it, need to rank stocks in UNIVERSE according to some criterion e.g., mcap.");
        sb.AppendLine("rankCrit = 'mcap';");
        sb.AppendLine(
            "datestart = 19950103; % if the period is not from 19950103 to 20201231, then need to generate a new universe.");
        sb.AppendLine("dateend = 20001229; ");
        sb.AppendLine(
            "numstockmin = 1000; % to remove dates that have less than numstockmin stocks with valid (not n/a) data.");
        sb.AppendLine(
            "numnanallowed = 31; % max number of NaN allowed for a stock in each RW; default = 31 (around 2.5% of wS 1260); they are replaced with zeros.");
        sb.AppendLine("useData_vec = {'CRSP-US'}; % for saved file's name(s) and latex table.");
        sb.AppendLine(
            "Retmat = load('US_top100_rev2.mat').datmatall_cell_computed{1}; % Returns (TxN, adjusted, daily, discrete, fraction, can have NaN).");
        sb.AppendLine("datestamp = load('US_top100_rev2.mat').datevec_final; % datetime format (e.g. 03-Jan-1972).");
        sb.AppendLine("stockstamp = load('US_top100_rev2.mat').stockstamp;");
        sb.AppendLine(
            "mcap = load('mcapmatall_dataonly.mat').mcapmatall_dataonly; % market cap (TxN); used for the sorting in the universe function that will then be used by N_choose; no need if 1. universe is already generated or 2. if N_choose is NaN, then this feature is deactivated and in this case, if want to generate universe, just use any dummy matrix.");
        sb.AppendLine(
            "universe = load('UNIVERSE_CRSP-US_wS=250_wJ=1_19950103to20001229_sortedbyMcap_numnanallowed=31_numstockmin=1000.mat').UNIVERSE; % universe resulting from processing the RetMat; namely, 1. NaN screening, and 2. sort by (e.g.) mcap. If it has not yet been generated or is unavailable, assign empty char array. If it exists, make sure it matches all the parameters set above.");
        sb.AppendLine();
        sb.AppendLine();
        
        // Execution 

        sb.AppendLine("% Execution");
        sb.AppendLine(
            "run_mode = 'Test'; % 'Full' for the whole sample period, and 'Test' for testing the first few rolling windows to see if the set-up is correct and feasible.");
        sb.AppendLine(
            "numRW_test = 4; % the first few rolling windows to be run to test the set-up; irrelevant if using 'Full' run_mode.");
        sb.AppendLine("parallelComp = 0; % 1 = do parallel computing over models, 0 = not do parallel computing");
        sb.AppendLine();
        sb.AppendLine();
        
        sb.AppendLine(
            "run_ALL5(N_choose,useData_vec,FDparam,qfrac,wsl,tcl,datestart,dateend,universe,Retmat,datestamp,mcap,numstockmin,numnanallowed,level_grid,q_EwMinVec,run_mode,numRW_test,parallelComp,stockstamp,rankCrit,f_shortallowed,wS,wJ)");
        
        // Save the time used
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("time_used=toc;");
        sb.AppendLine("writematrix(time_used, \"./time_used.csv\");");
        System.IO.File.WriteAllText(Path.Join(path, "setup_generated.m"), sb.ToString());
    }

    public MatlabGlueController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("/api/GetPassedParameters")]
    public MatlabResult GetPassedParameters(string param1)
    {
        return new MatlabResult
        {
            result = param1
        };
    }
    
    [HttpGet("/api/GetMatlabGlue")]
    public MatlabResult GetMatlabResult()
    {
        string path = "/Users/jin/MATLAB/Projects/simpleAddition";
        // string matlabCommand = generateMatlabCommand();
        createMatlabScriptTmp();
        string command = $"-nodisplay -nosplash -nodesktop -r \"run {path}/tmp.m; exit\"";
        Console.WriteLine("command " + command);
        var psi = new ProcessStartInfo();
        psi.FileName = "/Applications/MATLAB_R2022b.app/bin/matlab";
        psi.Arguments = command;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;
        var process = Process.Start(psi);
        
        process.WaitForExit();
        
        string output = "";
        using (var reader = new StreamReader($"{path}/result.csv"))
        {
            while (!reader.EndOfStream)
            {
                output = reader.ReadLine();
            }
        }
        
        return new MatlabResult
        {
            result = "some matlab result " + output
        };
    }

    [HttpGet("/api/GetMatlabPSARMResult")]
    public MatlabResult GetMatlabPSARMResult()
    {
        const string path = "/Users/jin/MATLAB/Projects/PSARM_Matlab-master";
        createMatlabScriptSetup();
        string command = $"-nodisplay -nosplash -nodesktop -r \"run {path}/setup_generated.m; exit\"";
        Console.WriteLine("command: " + command);
        var psi = new ProcessStartInfo();
        psi.FileName = "/Applications/MATLAB_R2022b.app/bin/matlab";
        psi.Arguments = command;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;
        var process = Process.Start(psi);
        
        process.WaitForExit();
        string output = "nothing";
        using (var reader = new StreamReader($"{path}/time_used.csv"))
        {
            while (!reader.EndOfStream)
            {
                output = reader.ReadLine();
            }
        }
        
        return new MatlabResult()
        {
            result = output
        };
    }

    [HttpGet("/api/GetPortfolioTimeSeries")]
    public string GetPortfolioTimeSeries()
    {

        var data = new List<List<string>>();
        var header = new List<string>();
        bool isHeader = true;
        using(var reader = new StreamReader("/Users/jin/MATLAB/Projects/PSARM_Matlab-master/mock-data-PoVusedWithDateVec.csv"))
        {
            // List<string> listA = new List<string>();
            // List<string> listB = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                if (isHeader)
                {
                    isHeader = false;
                    foreach (string v in values)
                    {
                        header.Add(v);
                        data.Add(new List<string>());
                    }
                }
                else
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        data[i].Add(values[i]);
                    }
                }
            }
        }

        var dataFrame = new Dictionary<string, List<string>>();
        for (int i = 0; i < header.Count(); i++)
        {
            dataFrame[header[i]] = data[i];
        }
        
        Console.WriteLine(JsonSerializer.Serialize(dataFrame));
        return JsonSerializer.Serialize(dataFrame);


        //     var portfolioTimeSeries = new PortfolioTimeSeries();
        //     for (var i = 0; i < 10; i++)
        //     {
        //         var dt = DateTime.Today.AddDays(i);
        //         var p2R = new Dictionary<string, decimal>() { { "a", i }, { "b", i + 2 } };
        //         var portfolioEachDate = new PortfolioEachDate(dt, p2R);
        //         portfolioTimeSeries.Series.Add(portfolioEachDate);
        //     }
        //
        //     for (var i = 0; i < 10; i++)
        //     {
        //         Console.WriteLine(portfolioTimeSeries.Series[i]);
        //     }
        //
        //     // portfolioTimeSeries.Series.Select(e => e.GetSummary());
        //
        //     var options = new JsonSerializerOptions { WriteIndented = true };
        //     var tmp = new Dictionary<string, List<string>>();
        //     tmp["k1"] = new List<string>() { "1", "2", "3" };
        //     tmp["k2"] = new List<string>() { "1.1", "2.1", "3.1" };
        //     tmp["k3"] = new List<string>() { "1.2", "2.2", "3.2" };
        //     Console.WriteLine(JsonSerializer.Serialize(
        //         tmp));
        //
        // // Console.WriteLine(portfolioTimeSeries.ToString());
        // return portfolioTimeSeries.ToString();
    }
    
    
    

}