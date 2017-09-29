using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SimpleFeedReader;

namespace YAPA.WPF
{
    /// <summary>
    /// https://github.com/RobThree/SimpleFeedReader/blob/master/SimpleFeedReader/DefaultFeedItemNormalizer.cs
    /// </summary>
    public class RssFeedNormalizer : IFeedItemNormalizer
    {
        private static Regex _controlCodesRegex = new Regex(@"[\x00-\x1F\x7f]", RegexOptions.Compiled);
        private static Regex _whiteSpaceRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        /// <summary>
        /// Normalizes a SyndicationItem into a FeedItem.
        /// </summary>
        /// <param name="feed">The <see cref="SyndicationFeed"/> on which the item was retrieved.</param>
        /// <param name="item">A <see cref="SyndicationItem"/> to normalize into a <see cref="FeedItem"/>.</param>
        /// <returns>Returns a normalized <see cref="FeedItem"/>.</returns>
        public virtual FeedItem Normalize(SyndicationFeed feed, SyndicationItem item)
        {
            var alternatelink = item.Links.FirstOrDefault(l => l.RelationshipType == null || l.RelationshipType.Equals("alternate", StringComparison.OrdinalIgnoreCase));

            Uri itemuri = null;
            Uri parsed;
            if (alternatelink == null && !Uri.TryCreate(item.Id, UriKind.Absolute, out parsed))
            {
                itemuri = parsed;
            }
            else
            {
                itemuri = alternatelink.GetAbsoluteUri();
            }

            return new FeedItem
            {
                Title = item.Title == null ? null : Normalize(item.Title.Text),
                Content = item.Content == null ? null : Normalize(((TextSyndicationContent)item.Content).Text),
                Summary = item.Summary == null ? null : Normalize(item.Summary.Text),
                Uri = itemuri,
            };
        }

        private static IEnumerable<Uri> GetFeedItemImages(SyndicationItem item)
        {
            return item.ElementExtensions
                .Where(p => p.OuterName.Equals("image"))
                .Select(p => new Uri(p.GetObject<XElement>().Value));
        }

        private static string Normalize(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = HtmlDecode(value);
                if (string.IsNullOrEmpty(value))
                    return value;

                value = StripDoubleOrMoreWhiteSpace(RemoveControlChars(value));
                value = value.Normalize().Trim();
            }
            return value;
        }

        private static string RemoveControlChars(string value)
        {
            return _controlCodesRegex.Replace(value, " ");
        }

        private static string StripDoubleOrMoreWhiteSpace(string value)
        {
            return _whiteSpaceRegex.Replace(value, " ");
        }


        private static string HtmlDecode(string value, int threshold = 5)
        {
            int c = 0;
            string newvalue = WebUtility.HtmlDecode(value);
            while (!newvalue.Equals(value) && c < threshold)    //Keep decoding (if a string is double/triple/... encoded; we want the original)
            {
                c++;
                value = newvalue;
                newvalue = WebUtility.HtmlDecode(value);
            }
            if (c >= threshold) //Decoding threshold exceeded?
                return null;

            return newvalue;
        }
    }
}
