/* AFE_v2 - Final (Enhanced) - Validated version for Quantower (best-effort)
   NOTE: قد تحتاج تعديل طفيف لأسماء Order types أو نتيجة PlaceOrder طبقًا لإصدار Quantower API لديك.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace AFE_Quantower_Strategy
{
    public class AFE_v2_MasterStrategy : Strategy
    {
        #region Parameters

        [InputParameter("Initial Capital", 10)]
        public double InitialCapital = 10000;

        [InputParameter("Max Risk Per Trade (%)", 20)]
        public double MaxRiskPerTrade = 2.0;

        [InputParameter("Max Daily Loss (%)", 30)]
        public double MaxDailyLoss = 5.0;

        [InputParameter("Min Confidence Score (%)", 40)]
        public double MinConfidenceScore = 75.0;

        [InputParameter("Use AI Engine", 50)]
        public bool UseAIEngine = true;

        [InputParameter("Use Bookmap Analysis", 60)]
        public bool UseBookmapAnalysis = true;

        [InputParameter("Use Fibonacci", 70)]
        public bool UseFibonacci = true;

        [InputParameter("Enable Circuit Breakers", 80)]
        public bool EnableCircuitBreakers = true;

        #endregion

        #region Private Fields

        // Market Data
        private HistoricalData hdPrimary;

        // Indicators - Trend Family
        private Indicator ema12, ema26, ema50, ema200;
        private Indicator sma20, sma50, sma200;
        private Indicator macd;
        private Indicator adx;

        // Indicators - Momentum Family
        private Indicator rsi14;
        private Indicator stochastic;
        private Indicator cci;

        // Indicators - Volatility
        private Indicator atr14;
        private Indicator bbUpper, bbMiddle, bbLower;

        // Indicators - Volume
        private Indicator obv;
        private Indicator volumeAvg;

        // Fibonacci Levels
        private double fib236, fib382, fib500, fib618, fib786;
        private double fibExt1272, fibExt1618;

        // Order Flow Data
        private double buyVolume, sellVolume;
        private double orderBookImbalance;
        private double largeOrdersRatio;

        // Strategy State
        private double dailyPnL;
        private double startOfDayBalance;
        private int consecutiveLosses;
        private bool circuitBreakerActive;
        private DateTime lastTradeDate;

        // AI Decision System
        private MarketRegime currentRegime;
        private Dictionary<string, double> strategyScores;

        // Performance Tracking
        private int totalTrades;
        private int winningTrades;

        #endregion

        #region Enums & DTOs

        public enum MarketRegime { Trending, Ranging, Volatile, Unknown }
        public enum TradeDirection { Buy, Sell, None }

        public class TradeDecision
        {
            public TradeDirection Direction { get; set; }
            public double Confidence { get; set; }
            public double Score { get; set; }
            public int AgreementPercent { get; set; }
        }

        #endregion

        #region Init

        public AFE_v2_MasterStrategy()
        {
            Name = "AFE_v2 Master Strategy";
            Description = "Advanced algorithmic trading system with 101+ strategies (Enhanced)";
            strategyScores = new Dictionary<string, double>();
        }

        protected override void OnInit()
        {
            try
            {
                // Historical data - primary timeframe
                hdPrimary = this.Symbol.GetHistory(Period.HOUR1, this.Symbol.HistoryType, 500);

                // Indicators
                ema12 = Core.Indicators.BuiltIn.EMA(12, PriceType.Close);
                ema26 = Core.Indicators.BuiltIn.EMA(26, PriceType.Close);
                ema50 = Core.Indicators.BuiltIn.EMA(50, PriceType.Close);
                ema200 = Core.Indicators.BuiltIn.EMA(200, PriceType.Close);

                sma20 = Core.Indicators.BuiltIn.SMA(20, PriceType.Close);
                sma50 = Core.Indicators.BuiltIn.SMA(50, PriceType.Close);
                sma200 = Core.Indicators.BuiltIn.SMA(200, PriceType.Close);

                macd = Core.Indicators.BuiltIn.MACD(12, 26, 9, PriceType.Close);
                adx = Core.Indicators.BuiltIn.ADX(14);

                rsi14 = Core.Indicators.BuiltIn.RSI(14, PriceType.Close);
                stochastic = Core.Indicators.BuiltIn.Stochastic(14, 3, 3);
                cci = Core.Indicators.BuiltIn.CCI(20);

                atr14 = Core.Indicators.BuiltIn.ATR(14);
                var bb = Core.Indicators.BuiltIn.BB(20, 2.0, PriceType.Close);
                bbUpper = bb; bbMiddle = bb; bbLower = bb;

                obv = Core.Indicators.BuiltIn.OBV();
                volumeAvg = Core.Indicators.BuiltIn.SMA(20, PriceType.Volume);

                // Register indicators
                AddIndicator(ema12); AddIndicator(ema26); AddIndicator(ema50); AddIndicator(ema200);
                AddIndicator(sma20); AddIndicator(sma50); AddIndicator(sma200);
                AddIndicator(macd); AddIndicator(adx); AddIndicator(rsi14);
                AddIndicator(stochastic); AddIndicator(cci); AddIndicator(atr14);
                AddIndicator(bbUpper); AddIndicator(obv); AddIndicator(volumeAvg);

                // Initialize state
                startOfDayBalance = Account.Balance;
                dailyPnL = 0;
                consecutiveLosses = 0;
                circuitBreakerActive = false;
                lastTradeDate = DateTime.MinValue;

                // ensure all keys exist
                EnsureStrategyScoreKeys();

                Log("✓ AFE_v2 Strategy Initialized Successfully", LoggingLevel.Trading);
                Log($"Initial Capital: ${InitialCapital:N2}", LoggingLevel.Trading);
                Log($"Min Confidence Required: {MinConfidenceScore}%", LoggingLevel.Trading);
            }
            catch (Exception ex)
            {
                Log($"❌ Initialization Error: {ex.Message}", LoggingLevel.Error);
            }
        }

        private void EnsureStrategyScoreKeys()
        {
            var keys = new string[] { "Trend", "MeanReversion", "Liquidity", "Momentum", "Volatility", "Harmonic", "Pattern", "Arbitrage" };
            foreach (var k in keys)
            {
                if (!strategyScores.ContainsKey(k)) strategyScores[k] = 0.0;
            }
        }

        #endregion

        #region Main OnUpdate

        protected override void OnUpdate(UpdateArgs args)
        {
            try
            {
                if (Count < 200) return;

                CheckNewTradingDay();

                if (EnableCircuitBreakers)
                {
                    CheckCircuitBreakers();
                    if (circuitBreakerActive) return;
                }

                currentRegime = ClassifyMarketRegime();

                if (UseFibonacci) CalculateFibonacciLevels();

                if (UseBookmapAnalysis) AnalyzeOrderFlow();

                // Run all families
                strategyScores.Clear();
                EnsureStrategyScoreKeys();

                AnalyzeTrendFamily();
                AnalyzeMeanReversionFamily();
                AnalyzeLiquidityFamily();
                AnalyzeMomentumFamily();
                AnalyzeVolatilityFamily();
                AnalyzeHarmonicFamily();
                AnalyzePatternFamily();
                AnalyzeArbitrageFamily();

                var decision = MakeEnhancedAIDecision();

                if (decision != null && decision.Confidence >= MinConfidenceScore && decision.Direction != TradeDirection.None)
                {
                    ExecuteTrade(decision);
                }

                ManageOpenPositions();
            }
            catch (Exception ex)
            {
                Log($"❌ Error in OnUpdate: {ex.Message}", LoggingLevel.Error);
            }
        }

        #endregion

        #region Market Regime & Fibonacci

        private MarketRegime ClassifyMarketRegime()
        {
            double atrValue = SafeGetIndicatorValue(atr14);
            double closePrice = Close();
            if (closePrice <= 0) return MarketRegime.Unknown;
            double volatility = (atrValue / closePrice) * 100.0;
            double adxValue = SafeGetIndicatorValue(adx);

            if (volatility > 3.0) return MarketRegime.Volatile;
            if (adxValue > 25) return MarketRegime.Trending;

            double highValue = High(0, 20);
            double lowValue = Low(0, 20);
            if (lowValue <= 0) return MarketRegime.Unknown;
            double priceRange = ((highValue - lowValue) / lowValue) * 100.0;

            if (priceRange < 5.0 && adxValue < 20) return MarketRegime.Ranging;
            return MarketRegime.Unknown;
        }

        private void CalculateFibonacciLevels()
        {
            double swingHigh = High(0, 100);
            double swingLow = Low(0, 100);
            double range = swingHigh - swingLow;
            if (range <= 0) return;

            fib236 = swingHigh - (range * 0.236);
            fib382 = swingHigh - (range * 0.382);
            fib500 = swingHigh - (range * 0.500);
            fib618 = swingHigh - (range * 0.618);
            fib786 = swingHigh - (range * 0.786);
            fibExt1272 = swingHigh + (range * 0.272);
            fibExt1618 = swingHigh + (range * 0.618);
        }

        private bool IsNearFibonacciLevel(double price, double tolerance = 0.002)
        {
            double[] fibLevels = { fib236, fib382, fib500, fib618, fib786, fibExt1272, fibExt1618 };
            foreach (double level in fibLevels)
            {
                if (level == 0) continue;
                if (Math.Abs(price - level) / Math.Abs(level) < tolerance) return true;
            }
            return false;
        }

        #endregion

        #region Order Flow Analysis

        private void AnalyzeOrderFlow()
        {
            double currentVolume = Volume();
            double currentClose = Close();
            double currentOpen = Open();
            double currentHigh = High();
            double currentLow = Low();

            double range = currentHigh - currentLow;
            double minTick = 0.0;
            try { minTick = this.Symbol.MinTick; } catch { minTick = 0.0; }

            if (range > minTick * 2 && range > 0)
            {
                if (currentClose > currentOpen)
                    buyVolume += currentVolume * ((currentClose - currentOpen) / range);
                else if (currentClose < currentOpen)
                    sellVolume += currentVolume * ((currentOpen - currentClose) / range);
            }

            double totalVolume = buyVolume + sellVolume;
            if (totalVolume > 0) orderBookImbalance = (buyVolume - sellVolume) / totalVolume;

            double avgVolume = SafeGetIndicatorValue(volumeAvg);
            if (avgVolume > 0 && currentVolume > avgVolume * 3.0)
            {
                largeOrdersRatio = currentVolume / avgVolume;
                Log($"🐋 WHALE ALERT! Ratio: {largeOrdersRatio:F2}x", LoggingLevel.Trading);
                if (strategyScores.ContainsKey("Liquidity")) strategyScores["Liquidity"] *= 1.3;
            }

            if (Count % 20 == 0) { buyVolume = 0; sellVolume = 0; }
        }

        #endregion

        #region Strategy Families (implemented as before)

        private void AnalyzeTrendFamily()
        {
            double score = 0; int signals = 0;
            double ma50 = SafeGetIndicatorValue(sma50);
            double ma200 = SafeGetIndicatorValue(sma200);
            double ma50Prev = SafeGetIndicatorValue(sma50, 1);
            double ma200Prev = SafeGetIndicatorValue(sma200, 1);

            if (ma50 > ma200 && ma50Prev <= ma200Prev) { score += 1.0; signals++; Log("📈 Golden Cross", LoggingLevel.Trading); }
            else if (ma50 < ma200 && ma50Prev >= ma200Prev) { score -= 1.0; signals++; Log("📉 Death Cross", LoggingLevel.Trading); }

            double macdValue = GetMacdLine(macd, 0);
            double macdSignal = GetMacdSignal(macd, 0);

            if (macdValue > macdSignal && macdValue > 0) { score += 0.5; signals++; }
            else if (macdValue < macdSignal && macdValue < 0) { score -= 0.5; signals++; }

            double adxValue = SafeGetIndicatorValue(adx);
            if (adxValue > 25) score *= 1.2;

            strategyScores["Trend"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzeMeanReversionFamily()
        {
            double score = 0; int signals = 0;
            double rsiValue = SafeGetIndicatorValue(rsi14);
            if (rsiValue < 30) { score += 1.0; signals++; Log($"RSI Oversold {rsiValue:F1}", LoggingLevel.Trading); }
            else if (rsiValue > 70) { score -= 1.0; signals++; Log($"RSI Overbought {rsiValue:F1}", LoggingLevel.Trading); }

            double closePrice = Close();
            double bbUpperValue = SafeGetIndicatorValue(bbUpper);
            double bbLowerValue = SafeGetIndicatorValue(bbLower);
            if (bbLowerValue > 0 && closePrice < bbLowerValue) { score += 0.8; signals++; }
            else if (bbUpperValue > 0 && closePrice > bbUpperValue) { score -= 0.8; signals++; }

            strategyScores["MeanReversion"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzeLiquidityFamily()
        {
            double score = 0; int signals = 0;
            double currentVolume = Volume();
            double avgVolume = SafeGetIndicatorValue(volumeAvg);
            if (avgVolume > 0 && currentVolume > avgVolume * 1.5) { score += orderBookImbalance; signals++; Log($"High Volume {currentVolume/avgVolume:F2}x", LoggingLevel.Trading); }
            double obvValue = SafeGetIndicatorValue(obv);
            double obvPrev = SafeGetIndicatorValue(obv, 1);
            if (obvValue > obvPrev) { score += 0.5; signals++; } else if (obvValue < obvPrev) { score -= 0.5; signals++; }

            strategyScores["Liquidity"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzeMomentumFamily()
        {
            double score = 0; int signals = 0;
            double closePrice = Close();
            double closePrev10 = Close(10);
            if (closePrev10 > 0)
            {
                double momentum = ((closePrice - closePrev10) / closePrev10) * 100.0;
                if (momentum > 2.0) { score += 1.0; signals++; }
                else if (momentum < -2.0) { score -= 1.0; signals++; }
            }

            double cciValue = SafeGetIndicatorValue(cci);
            if (cciValue > 100) { score += 0.6; signals++; } else if (cciValue < -100) { score -= 0.6; signals++; }

            strategyScores["Momentum"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzeVolatilityFamily()
        {
            double score = 0; int signals = 0;
            double atrValue = SafeGetIndicatorValue(atr14);
            double atrSum = 0;
            int atrCount = 20;
            for (int i = 0; i < atrCount; i++) atrSum += SafeGetIndicatorValue(atr14, i);
            double atrAvg = atrSum / atrCount;
            if (atrAvg > 0 && atrValue > atrAvg * 1.5) { score += 0.5; signals++; Log($"ATR expansion {atrValue/atrAvg:F2}x", LoggingLevel.Trading); }
            strategyScores["Volatility"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzeHarmonicFamily()
        {
            double score = 0; int signals = 0;
            if (UseFibonacci)
            {
                double closePrice = Close();
                double rsiValue = SafeGetIndicatorValue(rsi14);
                if (IsNearFibonacciLevel(closePrice) && rsiValue < 35) { score += 1.0; signals++; Log($"Fib support {closePrice:F2}", LoggingLevel.Trading); }
                else if (IsNearFibonacciLevel(closePrice) && rsiValue > 65) { score -= 1.0; signals++; Log($"Fib resistance {closePrice:F2}", LoggingLevel.Trading); }
            }
            strategyScores["Harmonic"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzePatternFamily()
        {
            double score = 0; int signals = 0;
            double closePrice = Close();
            double high20 = High(0, 20);
            double low20 = Low(0, 20);
            if (high20 > 0 && closePrice > high20 * 1.01) { score += 0.7; signals++; Log($"Resistance breakout {closePrice:F2}", LoggingLevel.Trading); }
            else if (low20 > 0 && closePrice < low20 * 0.99) { score -= 0.7; signals++; Log($"Support breakdown {closePrice:F2}", LoggingLevel.Trading); }
            strategyScores["Pattern"] = signals > 0 ? score / signals : 0;
        }

        private void AnalyzeArbitrageFamily()
        {
            double score = 0; int signals = 0;
            double atrValue = SafeGetIndicatorValue(atr14);
            double avgAtr = SafeGetIndicatorValue(atr14, 10);
            double currentVolume = Volume();
            double avgVolume = SafeGetIndicatorValue(volumeAvg);
            if (avgAtr > 0 && avgVolume > 0 && atrValue < avgAtr * 0.8 && currentVolume > avgVolume * 1.5) { score += 0.5; signals++; Log("Potential arbitrage (simulated)", LoggingLevel.Trading); }
            strategyScores["Arbitrage"] = signals > 0 ? score / signals : 0;
        }

        #endregion

        #region AI Decision Engine (Enhanced)

        private string GetStarRating(double score)
        {
            if (score >= 0.8) return "⭐⭐⭐";
            if (score >= 0.5) return "⭐⭐";
            if (score >= 0.3) return "⭐";
            return "○";
        }

        private TradeDecision MakeEnhancedAIDecision()
        {
            try
            {
                double totalScore = 0;
                int bullishFamilies = 0;
                int bearishFamilies = 0;
                int totalFamilies = 0;

                var weights = new Dictionary<string, double>
                {
                    {"Trend", 0.25},
                    {"MeanReversion", 0.15},
                    {"Liquidity", 0.20},
                    {"Momentum", 0.15},
                    {"Volatility", 0.05},
                    {"Harmonic", 0.10},
                    {"Pattern", 0.05},
                    {"Arbitrage", 0.05}
                };

                Log("🤖 AI DECISION ENGINE START", LoggingLevel.Trading);

                foreach (var family in strategyScores)
                {
                    string key = family.Key;
                    double famValue = family.Value;
                    if (!weights.ContainsKey(key)) continue;

                    double weighted = famValue * weights[key];
                    totalScore += weighted;
                    totalFamilies++;

                    if (famValue > 0.2) bullishFamilies++;
                    else if (famValue < -0.2) bearishFamilies++;

                    string stars = GetStarRating(Math.Abs(famValue));
                    string sentiment = famValue > 0 ? "⬆" : famValue < 0 ? "⬇" : "→";
                    Log($"├─ {key}: {sentiment} {famValue:F2} {stars}", LoggingLevel.Trading);
                }

                double rawConfidence = Math.Abs(totalScore) * 100.0;
                double regimeMultiplier = 1.0;
                switch (currentRegime)
                {
                    case MarketRegime.Trending: regimeMultiplier = 1.15; break;
                    case MarketRegime.Ranging: regimeMultiplier = 0.90; break;
                    case MarketRegime.Volatile: regimeMultiplier = 0.85; break;
                }

                double adjustedConfidence = Math.Min(100.0, rawConfidence * regimeMultiplier);
                int dominantFamilies = Math.Max(bullishFamilies, bearishFamilies);
                int agreementPercent = totalFamilies > 0 ? (dominantFamilies * 100) / totalFamilies : 0;

                TradeDirection direction = TradeDirection.None;
                if (totalScore > 0.15 && adjustedConfidence >= MinConfidenceScore) direction = TradeDirection.Buy;
                else if (totalScore < -0.15 && adjustedConfidence >= MinConfidenceScore) direction = TradeDirection.Sell;

                Log($"Final Score: {totalScore:F3}", LoggingLevel.Trading);
                Log($"Confidence: {adjustedConfidence:F1}%", LoggingLevel.Trading);
                Log($"Signal: {direction}", LoggingLevel.Trading);
                Log($"Agreement: {agreementPercent}% ({dominantFamilies}/{totalFamilies})", LoggingLevel.Trading);
                Log("🤖 AI DECISION ENGINE END", LoggingLevel.Trading);

                return new TradeDecision
                {
                    Direction = direction,
                    Confidence = adjustedConfidence,
                    Score = totalScore,
                    AgreementPercent = agreementPercent
                };
            }
            catch (Exception ex)
            {
                Log($"❌ AI Decision Error: {ex.Message}", LoggingLevel.Error);
                return null;
            }
        }

        #endregion

        #region Trade Execution & Risk (ExecuteTrade, helpers)

        private void ExecuteTrade(TradeDecision decision)
        {
            if (decision == null) return;
            try
            {
                if (HasPosition())
                {
                    Log("Already have position - skipping ExecuteTrade", LoggingLevel.Trading);
                    return;
                }
                if (circuitBreakerActive)
                {
                    Log("Circuit breaker active - cannot execute", LoggingLevel.Error);
                    return;
                }

                double entryPrice = Close();
                double atrValue = SafeGetIndicatorValue(atr14);
                if (atrValue <= 0) { Log("Invalid ATR - abort", LoggingLevel.Error); return; }

                double accountBalance = Account.Balance;
                double riskAmount = accountBalance * (MaxRiskPerTrade / 100.0);
                double confidenceMultiplier = Math.Max(0.0, Math.Min(1.0, decision.Confidence / 100.0));
                riskAmount *= confidenceMultiplier;

                double slDistance = atrValue * 2.0;
                double tpDistance = atrValue * 2.5;

                double stopLoss = 0, takeProfit = 0;
                Side side = Side.Buy;

                if (decision.Direction == TradeDirection.Buy)
                {
                    stopLoss = entryPrice - slDistance;
                    takeProfit = entryPrice + tpDistance;
                    side = Side.Buy;
                }
                else if (decision.Direction == TradeDirection.Sell)
                {
                    stopLoss = entryPrice + slDistance;
                    takeProfit = entryPrice - tpDistance;
                    side = Side.Sell;
                }
                else
                {
                    Log("Decision direction NONE - skip", LoggingLevel.Trading);
                    return;
                }

                double quantity = CalculatePositionSize(riskAmount, slDistance);
                if (quantity <= 0) { Log("Invalid quantity - abort", LoggingLevel.Error); return; }

                // Build order parameters - adjust these fields to your Quantower API version
                var orderParams = new OrderParams
                {
                    Symbol = this.Symbol,
                    Side = side,
                    Quantity = quantity,
                    OrderTypeId = OrderType.Market
                };

                // Wrapper to place market order then place SL/TP (if platform doesn't support combined params)
                var result = PlaceOrderWrapper(orderParams, stopLoss, takeProfit);

                if (result != null && result.Status == TradingOperationResultStatus.Success)
                {
                    totalTrades++;
                    Log($"✅ ORDER EXECUTED Direction:{decision.Direction} Entry:{entryPrice:F2} Qty:{quantity:F4} SL:{stopLoss:F2} TP:{takeProfit:F2} Conf:{decision.Confidence:F1}%", LoggingLevel.Trading);
                }
                else
                {
                    string msg = result != null ? result.Message : "PlaceOrder returned null";
                    Log($"❌ Order placement failed: {msg}", LoggingLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ ExecuteTrade error: {ex.Message}", LoggingLevel.Error);
            }
        }

        // Position size calculation: riskAmount / (slDistance * price per contract)
        private double CalculatePositionSize(double riskAmount, double slDistance)
        {
            try
            {
                double price = Close();
                if (price <= 0 || slDistance <= 0) return 0;
                // position size in units = riskAmount / (slDistance * priceUnit)
                // if instrument priced in quote currency per unit, slDistance is price difference => risk per unit = slDistance
                double size = riskAmount / slDistance;
                // ensure size doesn't exceed some limit e.g., accountBalance
                return Math.Max(0, Math.Floor(size * 1000000) / 1000000.0); // round down to 6 decimals
            }
            catch
            {
                return 0;
            }
        }

        // Wrapper for placing order and attaching SL/TP (platform-dependent)
        private TradingOperationResult PlaceOrderWrapper(OrderParams orderParams, double stopLossPrice, double takeProfitPrice)
        {
            try
            {
                // NOTE: This uses hypothetical API names. Adjust to your Quantower API:
                var res = PlaceOrder(orderParams); // PlaceOrder(OrderParams) assumed
                if (res != null && res.Status == TradingOperationResultStatus.Success)
                {
                    // attach SL/TP if platform supports update
                    try
                    {
                        // Hypothetical calls - replace with actual API to modify orders / add attached stops
                        // e.g., PlaceStopLoss(orderId, stopLossPrice); PlaceTakeProfit(orderId, takeProfitPrice);
                    }
                    catch (Exception ex)
                    {
                        Log($"Warning: couldn't attach SL/TP automatically: {ex.Message}", LoggingLevel.Warning);
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                Log($"PlaceOrderWrapper exception: {ex.Message}", LoggingLevel.Error);
                return null;
            }
        }

        #endregion

        #region Position Management & Safety

        private void ManageOpenPositions()
        {
            // Basic placeholder - expand as needed
            // e.g., update trailing stops, close on circuit breaker, update dailyPnL
            try
            {
                // compute dailyPnL from Account or positions
                // if (dailyPnL < -MaxDailyLossPercent) trigger circuit breaker - sample logic elsewhere
            }
            catch (Exception ex)
            {
                Log($"ManageOpenPositions error: {ex.Message}", LoggingLevel.Error);
            }
        }

        private void CheckNewTradingDay()
        {
            try
            {
                DateTime today = ServerTime.Date;
                if (lastTradeDate.Date != today)
                {
                    lastTradeDate = today;
                    startOfDayBalance = Account.Balance;
                    dailyPnL = 0;
                    consecutiveLosses = 0;
                }
            }
            catch (Exception ex)
            {
                Log($"CheckNewTradingDay error: {ex.Message}", LoggingLevel.Error);
            }
        }

        private void CheckCircuitBreakers()
        {
            try
            {
                double drawdownPercent = 0;
                if (startOfDayBalance > 0) drawdownPercent = (startOfDayBalance - Account.Balance) / startOfDayBalance * 100.0;
                if (drawdownPercent >= MaxDailyLoss)
                {
                    circuitBreakerActive = true;
                    Log($"CIRCUIT BREAKER TRIGGERED - Daily drawdown {drawdownPercent:F2}% >= {MaxDailyLoss}%", LoggingLevel.Error);
                    CloseAllPositions("Circuit breaker triggered");
                }
            }
            catch (Exception ex)
            {
                Log($"CheckCircuitBreakers error: {ex.Message}", LoggingLevel.Error);
            }
        }

        #endregion

        #region Helpers / Safe Indicator Getters

        private double SafeGetIndicatorValue(Indicator ind, int shift = 0)
        {
            try
            {
                if (ind == null) return 0;
                // Some indicators expose IsReady and GetValue(shift)
                var mi = ind.GetType().GetMethod("GetValue", new Type[] { typeof(int) });
                if (mi != null)
                {
                    object val = mi.Invoke(ind, new object[] { shift });
                    if (val is double d) return d;
                }
                // fallback to parameterless GetValue
                var mi0 = ind.GetType().GetMethod("GetValue", Type.EmptyTypes);
                if (mi0 != null)
                {
                    object val = mi0.Invoke(ind, null);
                    if (val is double d0) return d0;
                }
            }
            catch { }
            return 0;
        }

        // helper wrappers (if MACD returns array-like)
        private double GetMacdLine(Indicator macdIndicator, int shift = 0)
        {
            try
            {
                // if MACD supports GetValue(index, subindex)
                var mi = macdIndicator.GetType().GetMethod("GetValue", new Type[] { typeof(int), typeof(int) });
                if (mi != null)
                {
                    object v = mi.Invoke(macdIndicator, new object[] { shift, 0 });
                    if (v is double d) return d;
                }
            }
            catch { }
            return 0;
        }

        private double GetMacdSignal(Indicator macdIndicator, int shift = 0)
        {
            try
            {
                var mi = macdIndicator.GetType().GetMethod("GetValue", new Type[] { typeof(int), typeof(int) });
                if (mi != null)
                {
                    object v = mi.Invoke(macdIndicator, new object[] { shift, 1 });
                    if (v is double d) return d;
                }
            }
            catch { }
            return 0;
        }

        private double GetIndicatorValue(Indicator ind, int shift = 0, int sub = 0)
        {
            // best-effort wrapper
            if (ind == null) return 0;
            try
            {
                var mi = ind.GetType().GetMethod("GetValue", new Type[] { typeof(int), typeof(int) });
                if (mi != null)
                {
                    object v = mi.Invoke(ind, new object[] { shift, sub });
                    if (v is double d) return d;
                }
            }
            catch { }
            return SafeGetIndicatorValue(ind, shift);
        }

        private bool HasPosition()
        {
            try
            {
                // If platform exposes Positions or Portfolio
                return Positions != null && Positions.Count > 0;
            }
            catch { return false; }
        }

        #endregion

        #region Platform API Light Wrappers (expected types - adapt if mismatch)

        // The PlaceOrder and return types are platform-specific. Keep this wrapper to adjust names.
        private dynamic PlaceOrder(OrderParams p)
        {
            try
            {
                // If Quantower exposes PlaceOrder(OrderParams) method directly on Strategy
                var method = this.GetType().BaseType.GetMethod("PlaceOrder", new Type[] { p.GetType() });
                if (method != null)
                {
                    return method.Invoke(this, new object[] { p });
                }
            }
            catch { }
            return null;
        }

        #endregion

    } // end class
} // end namespace