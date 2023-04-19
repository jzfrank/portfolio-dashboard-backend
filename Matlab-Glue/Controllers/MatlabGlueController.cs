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
    private readonly ILogger<MatlabGlueController> _logger;
    
    public MatlabGlueController(ILogger<MatlabGlueController> logger)
    {
        _logger = logger;
    }

    private void CreateMatlabScriptTmp()
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

    private void CreateMatlabScriptSetup()
    {
        const string path = "/Users/jin/MATLAB/Projects/PSARM_Matlab-master";
        string script = $"";
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

    // generate MatlabScript Setup for FD with selected parameters
    // returns the name of the generated file 
    private string CreateMatlabScriptSetupFD(
        double fdParam,
        double qFrac,
        int wsl,
        int wS,
        int wJ,
        List<double> tcl,
        List<double> levelGrid,
        List<double> qEwMinVec,
        double fShortallowed
        )
    {
        string runMode = "Test";
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
        sb.AppendLine($"FDparam = {fdParam};");
        sb.AppendLine($"qfrac = {qFrac};");
        sb.AppendLine($"wsl = {wsl};");
        sb.AppendLine(
            $"wS = {wS}; % estimation window (in-sample data); default = 1260 (5 years; 252 trading day per year)");
        sb.AppendLine(
            $"wJ = {wJ}; % forecasting window (rebalancing frequency; OOS data); COMFORT only does 1-step ahead forecast of H and Gt; thus, wJ only matters on the mean that is imposed AR/asym structure.");
        sb.AppendLine(
            $"tcl = [{String.Join(',', tcl)}]; % level of transaction costs");
        sb.AppendLine(
            $"level_grid = {levelGrid[0]}; % ES level for ES portfolio optimization; (real-valued vector of) ES level(s), typical values 0.01 0.05 0.1");
        sb.AppendLine(
            $"q_EwMinVec = [{String.Join(',', qEwMinVec)}]; % quantile levels for the mean on the frontier; DEAR (or tau) = quantile(EY(EY>EYwminVar),q_EwMinVec) for mean-ES/Var (uncon) portfolio optimization; EY is the mean vector;");
        sb.AppendLine(
            $"f_shortallowed = {fShortallowed}; % amount of short positions allowed for max-Sharpe portfolio optimization; if f_shortallowed=X, then 100(1+X)/100*X portfolio allowed, e.g. 0.5 corresponds to 150/50 portfolio");
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
            $"run_mode = '{runMode}'; % 'Full' for the whole sample period, and 'Test' for testing the first few rolling windows to see if the set-up is correct and feasible.");
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
        string file_name = $"setup_fd{fdParam}_q{qFrac}_wsl{wsl}_wS{wS}_wJ{wJ}_tcl{tcl[0]}_l{String.Join('_', levelGrid)}_q{String.Join('_', qEwMinVec)}_f_{fShortallowed}";
        file_name = file_name.Replace(".", "") + ".m";
        System.IO.File.WriteAllText(Path.Join(path, file_name), sb.ToString());
        return file_name;
    }
    

    [HttpGet("/api/GenerateMatlabFile")]
    // tcl, level_grd, q_EwMinVec could be a vector separated by comma ','
    public string GenerateMatlabFile(
        double fdParam,
        double qFrac,
        int wsl,
        int wS,
        int wJ,
        string tcl,
        string levelGrid,
        string qEwMinVec,
        double fShortallowed
        )
    {
        var tcl_list = new List<double>();
        foreach (var s in tcl.Split(','))
        {
            tcl_list.Add(Double.Parse(s));
        }
        
        var level_grid_list = new List<double>();
        foreach (var s in levelGrid.Split(','))
        {
            level_grid_list.Add(Double.Parse(s));
        }

        var q_EwMinVec_list = new List<double>();
        foreach (var s in qEwMinVec.Split(','))
        {
            q_EwMinVec_list.Add(Double.Parse(s));
        }
        
        string file_name = CreateMatlabScriptSetupFD(fdParam, qFrac, wsl, wS,wJ, tcl_list,level_grid_list,q_EwMinVec_list,fShortallowed);
        return file_name;
    }
    
    [HttpGet("/api/GetMatlabGlue")]
    public MatlabResult GetMatlabResult()
    {
        string path = "/Users/jin/MATLAB/Projects/simpleAddition";
        // string matlabCommand = generateMatlabCommand();
        CreateMatlabScriptTmp();
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
        CreateMatlabScriptSetup();
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
    
    [HttpGet("/api/GetMatlabPSARMResultFD")]
    // tcl, level_grd, q_EwMinVec could be a vector separated by comma ','
    public MatlabResult GetMatlabPSARMResultFD(
        double fdParam,
        double qFrac,
        int wsl,
        int wS,
        int wJ,
        string tcl,
        string levelGrid,
        string qEwMinVec,
        double fShortallowed
        )
    {
        List<double> parseString(String s)
        {
            var res = new List<double>();
            foreach (var num in s.Split(','))
            {
                res.Add(Double.Parse(num));
            }
            return res;
        }

        var tclList = parseString(tcl);
        var levelGridList = parseString(levelGrid);
        var qEwMinVecList = parseString(qEwMinVec);
        
        string fileName = CreateMatlabScriptSetupFD(fdParam, qFrac, wsl, wS,wJ, tclList,levelGridList,qEwMinVecList,fShortallowed);
        
        // PerfSummary_CRSP-US_FDparam=06_qfrac=02_wsl=250_window_Size_250_rebalancing_Frequency_1.mat
        
        const string path = "/Users/jin/MATLAB/Projects/PSARM_Matlab-master";

        string command = $"-nodisplay -nosplash -nodesktop -r \"run {path}/{ fileName }; exit\"";
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

    [HttpGet("/api/GetPortfolioSummary")]
    public string GetPortfolioSummary()
    {
        string summary;
        using (var reader =
               new StreamReader("/Users/jin/MATLAB/Projects/PSARM_Matlab-master/summary-mock-json.json"))
        {
            var line = reader.ReadLine();
            Console.WriteLine(line);
            summary = line;
        }

        return summary;
    }
    
    [HttpGet("/api/GetPortfolioTimeSeries")]
    public string GetPortfolioTimeSeries(
        double FDParam,
        double qFrac
        )
    {
        var data = new List<List<string>>();
        var header = new List<string>();
        bool isHeader = true;
        using(var reader = new StreamReader("/Users/jin/MATLAB/Projects/PSARM_Matlab-master/mock-data-PoVusedWithDateVec.csv"))
        {
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

        Console.WriteLine(FDParam + " " + qFrac);
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