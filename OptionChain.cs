using Ecng.Serialization;
using MoreLinq;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;
using Synapse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.OptionLoader
{
    public class OptionChain : BaseViewModel
    {

        private LoaderRoot _root;
        public OptionChain()
        {
            _root = LoaderRoot.GetInstance();
            Options = new List<Security>();
        }

        private List<eMarketData> _underlyingData;
        public List<eMarketData> UnderlyingData
        {
            get { return _underlyingData; }
            set
            {
                if (_underlyingData == value)
                    return;

                _underlyingData = value;
                NotifyPropertyChanged();
            }
        }



        private List<eMarketData> _optionData;
        public List<eMarketData> OptionData
        {
            get { return _optionData; }
            set
            {
                if (_optionData == value)
                    return;

                _optionData = value;
                NotifyPropertyChanged();
            }
        }

        private Security _underlyingAsset;
        public Security UnderlyingAsset
        {
            get { return _underlyingAsset; }
            set
            {
                if (_underlyingAsset == value)
                    return;

                _underlyingAsset = value;
                NotifyPropertyChanged();
            }
        }

        private List<Security> _options;
        public List<Security> Options
        {
            get { return _options; }
            set
            {
                if (_options == value)
                    return;

                _options = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> UnderlyingTrades { set; get; }
        public List<string> OptionTrades { set; get; }
        public List<string> OptionValues { set; get; }
        public List<string> OptionQuotes { set; get; }


        // id;price;volume;side 
        public string GetUnderlyingTrade(Trade trade)
        {
            return string.Format("{0};{1};{2};{3}", _root.GetLoaderId(), trade.Price, trade.Volume, ToChar(trade.OrderDirection));
        }

        // id;code;price;volume;side 
        public string GetOptionTrade(Trade trade)
        {
            return string.Format("{0};{1};{2};{3};{4}", _root.GetLoaderId(), trade.Security.Code, trade.Price, trade.Volume, ToChar(trade.OrderDirection));
        }

        // id;code;askPrice;askVolume;bidPrice;bidVolume
        public string GetBestPair(Security security)
        {
            return string.Format("{0};{1};{2};{3};{4}", _root.GetLoaderId(), security.Code, ToQuoteValue(security, Sides.Sell), ToQuoteValue(security, Sides.Sell, true), ToQuoteValue(security, Sides.Buy), ToQuoteValue(security, Sides.Buy, true));
        }

        public string ToQuoteValue(Security security, Sides side, bool isVolume = false)
        {
            if (side == Sides.Sell)
            {
                if (security.BestAsk != null)
                {
                    if (isVolume)
                    {
                        return security.BestAsk.Volume.ToString();
                    }
                    else
                    {
                        return security.BestAsk.Price.ToString();
                    }
                }
            }
            else if (side == Sides.Buy)
            {
                if (security.BestBid != null)
                {
                    if (isVolume)
                    {
                        return security.BestBid.Volume.ToString();
                    }
                    else
                    {
                        return security.BestBid.Price.ToString();
                    }
                }
            }

            return "";
        }

        public string ToLevel1Value(Security security, Sides side, bool isVolume = false)
        {
            //TODO
            return "";
        }

        public string ToOptionCode(Security security)
        {
            return security.Code;
        }

        public string ToChar(Sides? side)
        {
            return side == null ? "" : side == Sides.Buy ? "B" : "S";
        }


        public override void Load(SettingsStorage storage)
        {
            try
            {
                if (_root.Connector == null)
                    throw new ArgumentNullException(nameof(_root.Connector));

                if (!storage.Contains("UnderlyingAsset"))
                    throw new ArgumentException("UnderlyingAsset not found.");

                var id = storage.GetValue<string>("UnderlyingAsset");

                UnderlyingAsset = _root.Connector.Securities.FirstOrDefault(s => s.Id == id);

                if (UnderlyingAsset == null)
                    throw new ArgumentNullException(nameof(UnderlyingAsset));

                var optionIds = storage.GetValue<string[]>("Options");

                if (optionIds.Any())
                    throw new ArgumentException(string.Format("Options for {0} not found", id));

                optionIds.ForEach(o =>
                {
                    var option = _root.Connector.Securities.FirstOrDefault(s => s.Id == o);

                    if (option == null)
                    {
                        this.AddWarningLog(string.Format("Option {0} not found", o));
                        return;
                    }

                    Options.Add(option);

                });

            }
            catch (Exception ex)
            {
                this.AddError(ex);
            }

        }

        public override void Save(SettingsStorage storage)
        {
            storage.SetValue("UnderlyingAsset", UnderlyingAsset.Id);
            storage.SetValue("Options", Options.Select(s => s.Id).ToArray());
        }

    }





    [Flags]
    public enum eMarketData
    {
        /// <summary>
        /// Лучшие покупка/продажа.
        /// </summary>
        BestPair = 1,
        /// <summary>
        /// Бета.
        /// </summary>
        Beta = 2,
        /// <summary>
        /// Дельта.
        /// </summary>
        Delta = 4,
        /// <summary>
        /// Гамма.
        /// </summary>
        Gamma = 8,
        /// <summary>
        /// Волатильность (историческая).
        /// </summary>
        HistoricalVolatility = 16,
        /// <summary>
        /// Волатильность (подразумеваемая).
        /// </summary>
        ImpliedVolatility = 32,
        /// <summary>
        /// Послед. сделка.
        /// </summary>
        LastTrade = 64,
        /// <summary>
        /// ГО (покупка).
        /// </summary>
        MarginBuy = 128,
        /// <summary>
        /// ГО (продажа).
        /// </summary>
        MarginSell = 256,
        /// <summary>
        /// Открытый интерес.
        /// </summary>
        OpenInterest = 512,
        /// <summary>
        /// Ро.
        /// </summary>
        Rho = 1024,
        /// <summary>
        /// Теоретическая цена.
        /// </summary>
        TheorPrice = 2048,
        /// <summary>
        /// Тета.
        /// </summary>
        Theta = 4096,
        /// <summary>
        /// Вега.
        /// </summary>
        Vega = 8192
    }


}
