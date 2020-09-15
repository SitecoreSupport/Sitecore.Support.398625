using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Globalization;

namespace Sitecore.Support.ContentSearch
{
    public class SitecoreItemCrawler : Sitecore.ContentSearch.SitecoreItemCrawler
    {
        public SitecoreItemCrawler()
        {
        }
        public override void Update(IProviderUpdateContext context, IIndexableUniqueId indexableUniqueId, IndexEntryOperationContext operationContext, IndexingOptions indexingOptions = 0)
        {
            base.Update(context, indexableUniqueId, operationContext, indexingOptions);
            ItemUri itemUri = indexableUniqueId as SitecoreItemUniqueId;
            ItemUri itemUriFirstVersion = new ItemUri(itemUri.ItemID, itemUri.Language, Sitecore.Data.Version.First, itemUri.DatabaseName);
            SitecoreItemUniqueId indexableUniqueIdFirstVersion = new SitecoreItemUniqueId(itemUriFirstVersion);
            if (context.Index.EnableItemLanguageFallback)
            {
                RemoveFallbackFromIndexIfLanguageHasTheFirstVersion(context, indexableUniqueId);
                base.Update(context, indexableUniqueIdFirstVersion, operationContext, indexingOptions);
            }
        }

        private void RemoveFallbackFromIndexIfLanguageHasTheFirstVersion(IProviderUpdateContext context, IIndexableUniqueId indexableId)
        {
            var item = GetIndexable(indexableId as SitecoreItemUniqueId)?.Item;

            if (item == null || !context.Index.EnableItemLanguageFallback || item.IsFallback)
            {
                return;
            }

            if (!LanguageHasFallback(item.Language, item.Database))
            {
                return;
            }

            bool isFirstVersionInTheLanguage = GetItemVersions(item).Length == 1;
            if (!isFirstVersionInTheLanguage)
            {
                return;
            }

            if (item.Version != Sitecore.Data.Version.First)
            {
                UpdateFirstVersion(context, item,
                    new IndexEntryOperationContext()
                    {
                        NeedUpdateAllLanguages = false,
                        NeedUpdateChildren = false,
                        NeedUpdateAllVersions = false,
                        NeedUpdatePreviousVersion = false
                    });
            }
        }

        private static Sitecore.Data.Version[] GetItemVersions(Item item)
        {
            using (new WriteCachesDisabler())
            {
                return item.Versions.GetVersionNumbers() ?? Array.Empty<Sitecore.Data.Version>();
            }
        }

        private static bool LanguageHasFallback(Language lang, Database database)
        {
            return LanguageFallbackManager.GetFallbackLanguage(lang, database) != null;
        }

        private void UpdateFirstVersion(IProviderUpdateContext context, Item item, IndexEntryOperationContext indexEntryOperationContext)
        {
            var firstVersionUri = new ItemUri(item.ID, item.Language, Sitecore.Data.Version.First, item.Database);
            Update(context, new SitecoreItemUniqueId(firstVersionUri), indexEntryOperationContext);
        }
    }
}