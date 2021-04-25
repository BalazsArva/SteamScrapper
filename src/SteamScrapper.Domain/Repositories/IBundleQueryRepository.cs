﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamScrapper.Domain.Repositories
{
    public interface IBundleQueryRepository
    {
        Task<int> CountUnscannedBundlesFromAsync(DateTime from);

        Task<IEnumerable<long>> GetBundleIdsNotScannedFromAsync(DateTime from, int page, int pageSize, SortDirection sortDirection);
    }
}