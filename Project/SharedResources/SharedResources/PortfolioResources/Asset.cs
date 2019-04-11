﻿using Shared.MarketStructures;

namespace Shared.PortfolioResources
{
    public class Asset
    {
        public Asset()
        {
            RelatedStock = new Stock();
        }

        public Asset(Stock relatedStock, float quantity)
        {
            RelatedStock = relatedStock;
            Quantity = quantity;
        }

        public Asset(Asset asset)
        {
            RelatedStock = asset.RelatedStock;
            Quantity = asset.Quantity;
        }

        public Stock RelatedStock
        {
            get; set;
        }

        public float Quantity
        {
            get; set;
        }
    }
}
