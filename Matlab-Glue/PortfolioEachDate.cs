using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace test_api2;


// snapshot of portfolio perfomance at specific date
public class PortfolioEachDate
{
    public DateTime Date;

    public Dictionary<string, decimal> Portfolio2Return;

    public PortfolioEachDate(DateTime Date, Dictionary<string, decimal> Portfolio2Return)
    {
        this.Date = Date;
        this.Portfolio2Return = Portfolio2Return;
    }

    public Dictionary<string, string> GetSummary()
    {
        var summary = new Dictionary<string, string>();
        summary["date"] = Date.ToString();
        foreach (KeyValuePair<string, decimal> e in Portfolio2Return)
        {
            summary[e.Key] = e.Value.ToString();
        }

        return summary;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(GetSummary());
    }
}