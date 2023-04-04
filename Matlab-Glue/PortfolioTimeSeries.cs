namespace test_api2;

public class PortfolioTimeSeries
{
    public List<PortfolioEachDate> Series;

    public PortfolioTimeSeries()
    {
        Series = new List<PortfolioEachDate>(); 
    }

    public override string ToString()
    {
        return string.Join(",", Series.Select(e => e.ToString()));
    }
}