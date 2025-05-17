namespace TradingAgent.Core.Storage;

public class PerformanceMetricsCalculator
{
    /// <summary>
    /// 수익률을 저장하는 리스트 (예: 0.01은 1% 수익)
    /// </summary>
    private readonly List<double> _returns;

    public PerformanceMetricsCalculator(List<double> returns)
    {
        _returns = returns ?? new List<double>();
    }

    /// <summary>
    /// 누적 수익률 (Cumulative Return)
    /// </summary>
    /// <returns></returns>
    public double CalculateCumulativeReturn()
    {
        if (_returns.Count == 0) return 0;
        return _returns.Aggregate(1.0, (acc, r) => acc * (1 + r)) - 1;
    }

    /// <summary>
    /// 연간 수익률 (Annualized Return) = (1 + 누적수익률)^(연간거래일수 / 총 기간수) - 1
    /// </summary>
    /// <param name="tradingDaysPerYear"></param>
    /// <returns></returns>
    public double CalculateAnnualizedReturn(int tradingDaysPerYear = 252)
    {
        if (_returns.Count == 0) return 0;
        var cumulativeReturn = CalculateCumulativeReturn();
        var numberOfPeriods = _returns.Count;
        
        return Math.Pow(1 + cumulativeReturn, (double)tradingDaysPerYear / numberOfPeriods) - 1;
    }

    // 샤프 지수 계산 (Sharpe Ratio)
    // riskFreeRate: 무위험 수익률 (연간 기준, 예: 0.02는 2%)
    // tradingDaysPerYear: 연간 거래일 수 (예: 252)
    public double CalculateSharpeRatio(double riskFreeRate = 0.0, int tradingDaysPerYear = 365)
    {
        // 표준편차 계산하려면 2개는 있어야 함
        if (_returns.Count is 0 or < 2) return 0;

        var averageReturn = _returns.Average();
        var stdDev = CalculateStandardDeviation(_returns);

        if (stdDev == 0) return 0;

        // 일별 또는 기간별 무위험 수익률로 변환
        var periodicRiskFreeRate = Math.Pow(1 + riskFreeRate, 1.0 / tradingDaysPerYear) - 1;
        
        // (평균 기간 수익률 - 기간 무위험 수익률) / 기간 수익률 표준편차
        var sharpeRatio = (averageReturn - periodicRiskFreeRate) / stdDev;
        
        return sharpeRatio * Math.Sqrt(tradingDaysPerYear);
    }

    // (Maximum Drawdown - MDD)
    public double CalculateMaxDrawdown()
    {
        if (_returns.Count == 0) return 0;

        var peak = 1.0;
        var maxDrawdown = 0.0;
        var cumulativeReturns = new List<double> { 1.0 };

        foreach (var r in _returns)
        {
            cumulativeReturns.Add(cumulativeReturns.Last() * (1 + r));
        }

        for (var i = 0; i < cumulativeReturns.Count; i++)
        {
            if (cumulativeReturns[i] > peak)
            {
                peak = cumulativeReturns[i];
            }
            
            var drawdown = (peak - cumulativeReturns[i]) / peak;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }
        return -maxDrawdown;
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        if (values == null || !values.Any() || values.Count() < 2) return 0;
        
        var avg = values.Average();
        var sumOfSquares = values.Sum(val => (val - avg) * (val - avg));
        
        return Math.Sqrt(sumOfSquares / (values.Count() -1));
    }
}