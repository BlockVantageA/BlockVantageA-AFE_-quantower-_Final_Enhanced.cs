
🚀 دليل الاستخدام الكامل - AFE_v2.3 Trading Strategy
📖 نظرة عامة
AFE_v2 هو نظام تداول خوارزمي متقدم مصمم خصيصاً لمنصة Quantower، يجمع بين 101+ استراتيجية تداول موزعة على 7 عائلات استراتيجية، مع نظام ذكاء اصطناعي لاتخاذ القرارات وإدارة مخاطر ديناميكية.
✨ المميزات الرئيسية
🧠 نظام الذكاء الاصطناعي المتقدم
 * تصنيف نظام السوق: تحديد تلقائي لحالة السوق (trending, ranging, volatile)
 * دمج الإشارات: جمع إشارات من 101+ استراتيجية بأوزان ذكية
 * درجة الثقة: حساب دقيق لمستوى الثقة في كل صفقة (0-100%)
 * التكيف الديناميكي: تعديل الاستراتيجية بناءً على ظروف السوق
📊 تحليل Bookmap المتقدم
 * تدفق الأوامر: تحليل ضغوط الشراء والبيع اللحظية
 * عدم توازن دفتر الأوامر: كشف الاختلال في السيولة
 * رصد الحيتان: اكتشاف الأوامر الكبيرة تلقائياً (🐋)
 * التحقق قبل التنفيذ: HFT validators تمنع التنفيذ الخاطئ
🎯 تحليل فيبوناتشي الشامل
 * مستويات الارتداد: 23.6%, 38.2%, 50%, 61.8%, 78.6%
 * مستويات الامتداد: 127.2%, 161.8%
 * مناطق التقارب: دمج Fibonacci مع المؤشرات الفنية
 * أهداف ديناميكية: take profit تلقائي عند مستويات الامتداد
🛡️ إدارة مخااطر متعددة الطبقات
 * حجم المركز الديناميكي: بناءً على ATR ودرجة الثقة
 * وقف الخسارة التكيفي: 2× ATR مع trailing stop ذكي
 * جني الأرباح: نسبة مخاطرة/عائد 2.5:1 كحد أدنى
 * قواطع الدوائر: إيقاف تلقائي عند:
   * تجاوز الخسارة اليومية (5%)
   * 3 خسائر متتالية
   * ارتفاع مفاجئ في التقلبات
 * الحد الأقصى للمخاطرة: 2% لكل صفقة
📥 التثبيت والإعداد
الخطوة 1: تحميل الملف
 * احفظ الملف AFE_v2_MasterStrategy.cs
 * ضعه في المجلد:
<!-- end list -->
C:\Users\[YourUsername]\AppData\Roaming\Quantower\Scripts\Strategies\

الخطوة 2: إعادة تشغيل Quantower
 * أغلق Quantower تماماً
 * أعد فتح البرنامج
 * ستظهر الاستراتيجية في قائمة Strategies
الخطوة 3: تفعيل الاستراتيجية
 * افتح Chart للأداة المالية المرغوبة
 * اضغط على Strategies من القائمة العلوية
 * اختر AFE_v2 Master Strategy
 * ستظهر نافذة الإعدادات
⚙️ إعدادات الاستراتيجية
الإعدادات الأساسية (للمبتدئين)
Initial Capital: 10000          // رأس المال الابتدائي
Max Risk Per Trade: 1.5%        // مخاطرة محافظة
Max Daily Loss: 5.0%            // الحد الأقصى للخسارة اليومية
Min Confidence Score: 80%       // ثقة عالية = صفقات أقل وأدق
Use AI Engine: ✓                // تفعيل الذكاء الاصطناعي
Use Bookmap Analysis: ✓         // تفعيل تحليل دفتر الأوامر
Use Fibonacci: ✓                // تفعيل فيبوناتشي
Enable Circuit Breakers: ✓      // تفعيل قواطع الأمان
Primary Timeframe: 1H           // الإطار الزمني الرئيسي

الإعدادات المتوسطة (للمتداولين ذوي الخبرة)
Initial Capital: 10000
Max Risk Per Trade: 2.0%        // مخاطرة معتدلة
Max Daily Loss: 5.0%
Min Confidence Score: 75%       // توازن بين العدد والجودة
Use AI Engine: ✓
Use Bookmap Analysis: ✓
Use Fibonacci: ✓
Enable Circuit Breakers: ✓
Primary Timeframe: 1H

الإعدادات العدوانية (للمحترفين فقط)
Initial Capital: 10000
Max Risk Per Trade: 2.5%        // ⚠️ مخاطرة عالية
Max Daily Loss: 7.0%
Min Confidence Score: 65%       // صفقات أكثر
Use AI Engine: ✓
Use Bookmap Analysis: ✓
Use Fibonacci: ✓
Enable Circuit Breakers: ✓
Primary Timeframe: 15m or 1H

🎯 إعدادات حسب نوع الأصل
💰 العملات الرقمية (BTC/USDT, ETH/USDT)
Primary Timeframe: 1H أو 4H
Max Risk Per Trade: 1-2%
Min Confidence Score: 80%
Use Bookmap Analysis: ✓ (مهم جداً)
Use Fibonacci: ✓

نصائح خاصة:
 * الأسواق متقلبة - استخدم ثقة عالية
 * راقب أوامر الحيتان (🐋) - مؤثرة جداً
 * أفضل أوقات: عند افتتاح الأسواق الأمريكية
💱 الفوركس (EUR/USD, GBP/USD)
Primary Timeframe: 1H
Max Risk Per Trade: 0.5-1%
Min Confidence Score: 75%
Focus: Trend + Momentum Families

نصائح خاصة:
 * ركز على عائلة Trend وMomentum
 * تجنب التداول وقت الأخبار الاقتصادية
 * أفضل أوقات: جلسة لندن ونيويورك
📈 الأسهم (SPY, AAPL, TSLA)
Primary Timeframe: 1H أو 1D
Max Risk Per Trade: 1%
Min Confidence Score: 75%
Enable All Families: ✓

نصائح خاصة:
 * استخدم جميع العائلات
 * راقب Volume Profile بعناية
 * تجنب التداول أول 30 دقيقة من الجلسة
🔍 فهم العائلات الاستراتيجية
1️⃣ عائلة الاتجاه (Trend Family) - وزن 25%
متى تكون قوية:
 * عندما يكون ADX > 25
 * عند وجود Golden Cross أو Death Cross
 * في الأسواق ذات الاتجاه الواضح
الاستراتيجيات الرئيسية:
 * MA Crossover (50/200)
 * MACD Divergence
 * Supertrend
 * EMA Ribbon
إشارة قوية:
📈 Golden Cross: MA50 crossed above MA200
Confidence: 85%

2️⃣ عائلة الارتداد للمتوسط (Mean Reversion) - وزن 15%
متى تكون قوية:
 * في الأسواق الجانبية (Ranging)
 * عند RSI < 30 أو > 70
 * عند الابتعاد الكبير عن VWAP
الاستراتيجيات الرئيسية:
 * RSI Oversold/Overbought
 * Bollinger Bands
 * Distance from VWAP
 * Stochastic RSI
إشارة قوية:
📊 RSI Oversold: 28.5
Price below BB Lower Band
Confidence: 82%

3️⃣ عائلة السيولة (Liquidity Family) - وزن 20%
متى تكون قوية:
 * عند حجم تداول عالي (> 1.5× المتوسط)
 * عند رصد أوامر الحيتان
 * عند اختلال دفتر الأوامر
الاستراتيجيات الرئيسية:
 * Volume Profile (VAP)
 * Order Flow Imbalance
 * Whale Detection (🐋)
 * OBV Analysis
إشارة قوية:
🐋 Large Order Detected! Volume Ratio: 3.2x
💹 High Volume Buy Signal
Order Book Imbalance: +0.45 (Strong Buy Pressure)
Confidence: 88%

4️⃣ عائلة الزخم (Momentum Family) - وزن 15%
متى تكون قوية:
 * في بداية الاتجاهات القوية
 * عند التأكيد متعدد الأطر الزمنية
 * مع ROC > 5%
الاستراتيجيات الرئيسية:
 * Rate of Change (ROC)
 * Multi-Timeframe Momentum
 * RSI Momentum
 * Price Momentum
إشارة قوية:
📊 Multi-Timeframe Momentum: BULLISH
5m ✓ | 15m ✓ | 1H ✓
Confidence: 79%

5️⃣ عائلة التقلب (Volatility Family) - وزن 10%
متى تكون قوية:
 * عند Bollinger Band Squeeze
 * عند ATR أعلى من المتوسط
 * قبل الاختراقات الكبيرة
الاستراتيجيات الرئيسية:
 * ATR Breakout
 * BB Squeeze
 * Keltner Channel
إشارة قوية:
🔥 Bollinger Squeeze Breakout: LONG
ATR Expansion: 1.8x normal
Confidence: 81%

6️⃣ عائلة فيبوناتشي (Harmonic Family) - وزن 10%
متى تكون قوية:
 * عند التقاء السعر مع مستويات فيبوناتشي الرئيسية
 * مع تأكيد من RSI أو MACD
 * في مناطق الدعم/المقاومة القوية
المستويات المهمة:
 * 38.2% و 61.8% للارتداد
 * 127.2% و 161.8% للامتداد
إشارة قوية:
🎯 Fibonacci Support + RSI: BUY Signal at 42,150
Level: 61.8% Retracement
RSI: 32 (Oversold)
Confidence: 86%

7️⃣ عائلة الأنماط (Pattern Family) - وزن 5%
متى تكون قوية:
 * عند رصد Double Top/Bottom
 * عند اختراق Triangle
 * عند كسر Support/Resistance
إشارة قوية:
📈 Double Bottom Pattern Detected
💥 Resistance Breakout at 43,500
Confidence: 77%

🎓 فهم قرارات الذكاء الاصطناعي
مثال على قرار كامل:
═══════════════════════════════════════════════════════
🤖 AI DECISION ENGINE
═══════════════════════════════════════════════════════
Final Score: +0.645
Confidence: 82.3%
Signal: BUY
Agreement: 71% (5/7 families bullish)
Market Regime: Trending
═══════════════════════════════════════════════════════

Strategy Scores:
├─ Trend Family: +0.85 ⭐⭐⭐
├─ Liquidity Family: +0.72 ⭐⭐⭐
├─ Momentum Family: +0.68 ⭐⭐
├─ Harmonic Family: +0.55 ⭐⭐
├─ Volatility Family: +0.42 ⭐
├─ Mean Reversion Family: -0.12 ❌
└─ Pattern Family: +0.35 ⭐

Decision: EXECUTE BUY

تفسير القرار:
 * Final Score (+0.645):
   * موجب = إشارة شراء
   * كلما اقترب من +1 كان أقوى
 * Confidence (82.3%):
   * > 75% = صفقة قابلة للتنفيذ
     > 
   * > 85% = صفقة ذات جودة عالية جداً
     > 
 * Agreement (71%):
   * 5 من 7 عائلات تتفق على الشراء
   * اتفاق قوي = ثقة أعلى
 * Market Regime (Trending):
   * يفضل استراتيجيات الاتجاه
   * الثقة تبقى عالية
📊 قراءة سجلات التداول (Logs)
إشارات الدخول:
✅ ORDER EXECUTED
═══════════════════════════════════════════════════════
Direction: Buy
Entry Price: 42,350.50
Position Size: 0.0472 BTC
Stop Loss: 41,890.00 (-1.09%)
Take Profit: 43,500.00 (+2.71%)
Risk Amount: $200.00
Potential Profit: $500.00
Confidence: 82.3%
═══════════════════════════════════════════════════════

مراقبة الصفقة:
📊 Trailing Stop Updated: 42,400.00 (Break-even + buffer)
Position secured at break-even

إغلاق الصفقة:
✅ Position Closed: PROFIT $487.50
═══════════════════════════════════════════════════════
📊 Performance Stats:
  Total Trades: 23
  Win Rate: 65.2%
  Daily P&L: $1,245.30
═══════════════════════════════════════════════════════

🚨 قواطع الأمان (Circuit Breakers)
متى تُفعّل:
 * الخسارة اليومية:
<!-- end list -->
🚨 CIRCUIT BREAKER TRIGGERED
Daily Loss -5.2% exceeds limit 5.0%
⚠️ All positions close

