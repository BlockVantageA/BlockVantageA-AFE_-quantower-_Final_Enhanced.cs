/* AFE_v2 - Final (Enhanced) - Validated version for Quantower
   NOTE: هذا الكود تم تنقيحه ليكون قابلاً للتجميع بالحد الأقصى.
   قد تحتاج تعديلاً طفيفاً لأسماء Order types/Enums طبقاً لإصدار Quantower API لديك.
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

                // MACD has 2 buffers: 0 for MACD Line, 1 for Signal Line
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
            // تشمل مستويات الامتداد في فحص القرب
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

            // تصحيح: التحقق من النطاق والقيمة
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

        #region Strategy Families (8 Families)

        private void AnalyzeTrendFamily()
        {
            double score = 0; int signals = 0;
            double ma50 = SafeGetIndicatorValue(sma50);
            double ma200 = SafeGetIndicatorValue(sma200);
            double ma50Prev = SafeGetIndicatorValue(sma50, 1);
            double ma200Prev = SafeGetIndicatorValue(sma200, 1);

            if (ma50 > ma200 && ma50Prev <= ma200Prev) { score += 1.0; signals++; Log("📈 Golden Cross", LoggingLevel.Trading); }
            else if (ma50 < ma200 && ma50Prev >= ma200Prev) { score -= 1.0; signals++; Log("📉 Death Cross", LoggingLevel.Trading); }

            // تصحيح: استخدام SafeGetIndicatorValue للمؤشرات متعددة المخازن
            double macdValue = SafeGetIndicatorValue(macd, 0, 0); // MACD Line
            double macdSignal = SafeGetIndicatorValue(macd, 0, 1); // Signal Line

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
            double score = 0; 
            int signals = 0;

            // Simple support/resistance breakout
            double closePrice = Close();
            double high20 = High(0, 20);
            double low20 = Low(0, 20);

            if (closePrice > high20 * 1.01)
            { 
                score += 0.7; 
                signals++; 
                Log($"💥 Resistance Breakout at {closePrice:F2}", LoggingLevel.Trading); 
            }
            else if (closePrice < low20 * 0.99) 
            { 
                score -= 0.7; 
                signals++; 
                Log($"💥 Support Breakdown at {closePrice:F2}", LoggingLevel.Trading); 
            }

            // إكمال الدالة
            strategyScores["Pattern"] = signals > 0 ? score / signals : 0;
        }
        
        private void AnalyzeArbitrageFamily()
        {
            double score = 0;
            int signals = 0;

            // Simulated Arbitrage logic: low volatility + high volume = potential mispricing
            double atrValue = SafeGetIndicatorValue(atr14);
            double avgAtr = SafeGetIndicatorValue(atr14, 10);
            double currentVolume = Volume();
            double avgVolume = SafeGetIndicatorValue(volumeAvg);
            
            if (avgAtr > 0 && avgVolume > 0 && atrValue < avgAtr * 0.8 && currentVolume > avgVolume * 1.5)
            {
                score += 0.5; // Neutral signal, but suggests opportunity
                signals++;
                Log("⚖️ Potential Arbitrage Opportunity Detected (Simulated)", LoggingLevel.Trading);
            }

            strategyScores["Arbitrage"] = signals > 0 ? score / signals : 0;
        }

        #endregion

        #region AI Decision Engine

        private string GetStarRating(double score)
        {
            if (score >= 0.8) return "⭐⭐⭐";
            if (score >= 0.5) return "⭐⭐";
            if (score >= 0.3) return "⭐";
            return "○";
        }

        private TradeDecision MakeEnhancedAIDecision()
        {
            double totalScore = 0;
            int bullishFamilies = 0;
            int bearishFamilies = 0;
            int totalFamilies = 0;

            // Family Weights (Must sum to 1.0)
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

            Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);
            Log("🤖 AI DECISION ENGINE", LoggingLevel.Trading);
            Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);

            // Calculate weighted score
            foreach (var family in strategyScores)
            {
                if (weights.ContainsKey(family.Key))
                {
                    double weightedScore = family.Value * weights[family.Key];
                    totalScore += weightedScore;
                    totalFamilies++;

                    if (family.Value > 0.2) bullishFamilies++;
                    else if (family.Value < -0.2) bearishFamilies++;

                    string stars = GetStarRating(Math.Abs(family.Value));
                    string sentiment = family.Value > 0 ? "⬆" : family.Value < 0 ? "⬇" : "→";
                    Log($"├─ {family.Key} Family: {sentiment} {family.Value:F2} {stars}", LoggingLevel.Trading);
                }
            }

            // 1. Calculate base confidence
            double rawConfidence = Math.Abs(totalScore) * 100;
            
            // 2. Adjust confidence based on market regime
            double regimeMultiplier = 1.0;
            switch (currentRegime)
            {
                case MarketRegime.Trending: regimeMultiplier = 1.15; break;
                case MarketRegime.Ranging: regimeMultiplier = 0.90; break;
                case MarketRegime.Volatile: regimeMultiplier = 0.85; break;
            }

            double adjustedConfidence = Math.Min(100, rawConfidence * regimeMultiplier);

            // 3. Apply further enhancements/reductions (Volatility Check)
            double enhancementFactor = 1.0;
            double atrValue = SafeGetIndicatorValue(atr14);
            double atrAvg = 0;
            for (int i = 0; i < 20; i++) atrAvg += SafeGetIndicatorValue(atr14, i);
            atrAvg /= 20.0;
            
            if (atrAvg > 0 && atrValue > atrAvg * 3.0)
            {
                // Extreme Volatility Spike -> Reduce Confidence
                enhancementFactor *= 0.90;
                Log("⚠️ EXTREME VOLATILITY DETECTED - Confidence Reduced", LoggingLevel.Trading);
            }

            // Apply final adjustments
            double finalConfidence = Math.Min(100, adjustedConfidence * enhancementFactor);

            // Calculate agreement percentage
            int dominantFamilies = Math.Max(bullishFamilies, bearishFamilies);
            int agreementPercent = totalFamilies > 0 ? (dominantFamilies * 100) / totalFamilies : 0;

            // Determine direction
            TradeDirection direction = TradeDirection.None;
            
            if (totalScore > 0.15 && finalConfidence >= MinConfidenceScore)
            {
                direction = TradeDirection.Buy;
            }
            else if (totalScore < -0.15 && finalConfidence >= MinConfidenceScore)
            {
                direction = TradeDirection.Sell;
            }

            Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);
            Log($"Final Score: {totalScore:F3}", LoggingLevel.Trading);
            Log($"Confidence: {finalConfidence:F1}%", LoggingLevel.Trading);
            Log($"Signal: {direction}", LoggingLevel.Trading);
            Log($"Agreement: {agreementPercent}% ({dominantFamilies}/{totalFamilies} families)", LoggingLevel.Trading);
            Log($"Market Regime: {currentRegime}", LoggingLevel.Trading);
            Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);

            return new TradeDecision
            {
                Direction = direction,
                Confidence = finalConfidence,
                Score = totalScore,
                AgreementPercent = agreementPercent
            };
        }

        #endregion

        #region Trade Execution

        private void ExecuteTrade(TradeDecision decision)
        {
            if (HasPosition()) return;
            if (circuitBreakerActive) return;

            double entryPrice = Close();
            double atrValue = SafeGetIndicatorValue(atr14);
            if (atrValue <= 0) return;

            double accountBalance = Account.Balance;
            double riskAmount = accountBalance * (MaxRiskPerTrade / 100.0);

            // Dynamic sizing based on confidence
            double confidenceMultiplier = decision.Confidence / 100.0;
            riskAmount *= confidenceMultiplier;

            double slDistance = atrValue * 2.0;
            double tpDistance = atrValue * 3.0; // R:R = 1:1.5

            double stopLoss, takeProfit;
            Operation operation;

            if (decision.Direction == TradeDirection.Buy)
            {
                operation = Operation.Buy;
                stopLoss = entryPrice - slDistance;
                takeProfit = entryPrice + tpDistance;
            }
            else // Sell
            {
                operation = Operation.Sell;
                stopLoss = entryPrice + slDistance;
                takeProfit = entryPrice - tpDistance;
            }

            double quantity = CalculatePositionSize(riskAmount, slDistance);

            if (quantity <= 0)
            {
                 Log($"⚠️ Failed to calculate valid quantity. Risk Amount: {riskAmount:F2}, SL Distance: {slDistance:F4}", LoggingLevel.Error);
                 return;
            }

            try
            {
                var orderParams = new OrderParams
                {
                    Symbol = this.Symbol,
                    Side = operation == Operation.Buy ? Side.Buy : Side.Sell,
                    Quantity = quantity,
                    OrderTypeId = OrderType.Market,
                    StopLoss = new StopLoss
                    {
                        Type = StopLossType.Price,
                        Value = stopLoss
                    },
                    TakeProfit = new TakeProfit
                    {
                        Type = TakeProfitType.Price,
                        Value = takeProfit
                    }
                };

                // NOTE: PlaceOrder must return a valid TradingOperationResult
                var result = PlaceOrder(orderParams);

                if (result.Status == TradingOperationResultStatus.Success)
                {
                    totalTrades++;
                    Log($"✅ ORDER EXECUTED | {decision.Direction} {quantity:F4} at {entryPrice:F2}", LoggingLevel.Trading);
                }
                else
                {
                    Log($"❌ Order Failed: {result.Message}", LoggingLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Execution Error: {ex.Message}", LoggingLevel.Error);
            }
        }

        private double CalculatePositionSize(double riskAmount, double slDistance)
        {
            if (slDistance <= 0) return 0;

            double positionSize = riskAmount / slDistance;
            
            double minSize = this.Symbol.MinLot;
            double maxSize = this.Symbol.MaxLot;
            
            positionSize = Math.Max(minSize, Math.Min(maxSize, positionSize));
            
            return Math.Round(positionSize, 4);
        }

        private bool HasPosition()
        {
            return this.GetPositions().Any();
        }

        #endregion

        #region Position Management

        private void ManageOpenPositions()
        {
            var positions = this.GetPositions();
            
            foreach (var position in positions)
            {
                double currentPrice = Close();
                double entryPrice = position.OpenPrice;
                double atrValue = SafeGetIndicatorValue(atr14);
                double positionStopLoss = position.StopLoss.HasValue ? position.StopLoss.Value : 0;
                
                if (atrValue <= 0) continue;

                // Trailing stop logic (Move stop to break-even + buffer)
                double slDistance = atrValue * 2.0;
                double profitToActivateTrailing = slDistance * 0.75; // 75% of initial risk

                if (position.Side == Side.Buy)
                {
                    double profitPoints = currentPrice - entryPrice;
                    if (profitPoints > profitToActivateTrailing)
                    {
                        double newStopLoss = entryPrice + (atrValue * 0.5); // Breakeven + buffer
                        if (positionStopLoss < newStopLoss)
                        {
                            ModifyPosition(position, newStopLoss, position.TakeProfit);
                            Log($"📊 Trailing Stop Updated (Buy): {newStopLoss:F2}", LoggingLevel.Trading);
                        }
                    }
                }
                else // Sell position
                {
                    double profitPoints = entryPrice - currentPrice;
                    if (profitPoints > profitToActivateTrailing)
                    {
                        double newStopLoss = entryPrice - (atrValue * 0.5);
                        if (positionStopLoss > newStopLoss || positionStopLoss == 0)
                        {
                            ModifyPosition(position, newStopLoss, position.TakeProfit);
                            Log($"📊 Trailing Stop Updated (Sell): {newStopLoss:F2}", LoggingLevel.Trading);
                        }
                    }
                }
            }
        }

        private void ModifyPosition(Position position, double newStopLoss, double? newTakeProfit)
        {
            try
            {
                var modifyParams = new ModifyPositionParams
                {
                    Position = position,
                    StopLoss = new StopLoss
                    {
                        Type = StopLossType.Price,
                        Value = newStopLoss
                    },
                    TakeProfit = newTakeProfit.HasValue ? new TakeProfit { Type = TakeProfitType.Price, Value = newTakeProfit.Value } : null
                };

                // NOTE: ModifyPosition must be called on the Strategy base class
                base.ModifyPosition(modifyParams);
            }
            catch (Exception ex)
            {
                Log($"❌ Error modifying position: {ex.Message}", LoggingLevel.Error);
            }
        }

        #endregion

        #region Risk Management

        private void CheckCircuitBreakers()
        {
            double currentBalance = Account.Balance;
            
            // Check for daily loss
            double dailyLossPercent = 0;
            if (startOfDayBalance > 0)
            {
                 dailyLossPercent = ((currentBalance - startOfDayBalance) / startOfDayBalance) * 100;
            }

            if (startOfDayBalance > 0 && Math.Abs(dailyLossPercent) >= MaxDailyLoss)
            {
                circuitBreakerActive = true;
                CloseAllPositions();
                Log("🚨 CIRCUIT BREAKER TRIGGERED: Max Daily Loss Reached", LoggingLevel.Error);
            }

            // Check for consecutive losses
            if (consecutiveLosses >= 3)
            {
                circuitBreakerActive = true;
                CloseAllPositions();
                Log("🚨 CIRCUIT BREAKER TRIGGERED: 3 Consecutive Losses", LoggingLevel.Error);
            }
        }

        private void CloseAllPositions()
        {
            foreach (var position in this.GetPositions())
            {
                try
                {
                    ClosePosition(position);
                }
                catch (Exception ex)
                {
                    Log($"❌ Error closing position: {ex.Message}", LoggingLevel.Error);
                }
            }
        }

        private void CheckNewTradingDay()
        {
            DateTime currentDate = Core.TimeUtils.DateTimeUtcNow.Date;
            
            if (lastTradeDate != currentDate)
            {
                startOfDayBalance = Account.Balance;
                dailyPnL = 0;
                consecutiveLosses = 0;
                circuitBreakerActive = false;
                lastTradeDate = currentDate;
                
                Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);
                Log($"📅 NEW TRADING DAY: {currentDate:yyyy-MM-dd}", LoggingLevel.Trading);
                Log($"💰 Starting Balance: ${startOfDayBalance:N2}", LoggingLevel.Trading);
                Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);
            }
        }

        #endregion

        #region Helper Methods (Quantower API Access & Safety)

        // Wrapper for indicator access with null/readiness checks
        private double SafeGetIndicatorValue(Indicator indicator, int shift = 0, int bufferIndex = 0)
        {
            try 
            {
                if (indicator == null || !indicator.IsReady)
                    return 0;
                
                return indicator.GetValue(shift, bufferIndex);
            }
            catch 
            { 
                return 0; 
            }
        }

        // Price access helpers
        private double GetPrice(PriceType priceType, int shift = 0)
        {
            try { return GetValue(priceType, shift); }
            catch { return 0; }
        }
        
        private double Close(int shift = 0) => GetPrice(PriceType.Close, shift);
        private double Open(int shift = 0) => GetPrice(PriceType.Open, shift);
        private double Volume(int shift = 0) => GetPrice(PriceType.Volume, shift);

        private double High(int shift = 0, int lookback = 1)
        {
            double maxVal = 0;
            for (int i = shift; i < shift + lookback; i++)
            {
                double high = GetPrice(PriceType.High, i);
                if (high > maxVal) maxVal = high;
            }
            return maxVal;
        }

        private double Low(int shift = 0, int lookback = 1)
        {
            double minVal = double.MaxValue;
            for (int i = shift; i < shift + lookback; i++)
            {
                double low = GetPrice(PriceType.Low, i);
                if (low < minVal) minVal = low;
            }
            return minVal == double.MaxValue ? 0 : minVal;
        }

        protected override void OnPositionClosed(Position position)
        {
            base.OnPositionClosed(position);
            
            bool isWin = position.GrossPnL > 0;
            
            if (isWin)
            {
                winningTrades++;
                consecutiveLosses = 0;
                Log($"✅ Position Closed: PROFIT ${position.GrossPnL:F2}", LoggingLevel.Trading);
            }
            else
            {
                // Only count realized losses as consecutive
                if (position.GrossPnL < 0) consecutiveLosses++;
                Log($"❌ Position Closed: LOSS ${position.GrossPnL:F2}", LoggingLevel.Trading);
            }
            
            totalTrades++;
            
            double winRate = totalTrades > 0 ? (double)winningTrades / totalTrades * 100 : 0;
            
            Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);
            Log($"📊 Performance Stats: Total Trades: {totalTrades}, Win Rate: {winRate:F1}%", LoggingLevel.Trading);
            Log("═══════════════════════════════════════════════════════", LoggingLevel.Trading);
        }

        protected override void OnStop()
        {
            // Final summary log
            base.OnStop();
        }

        #endregion
    }
}
