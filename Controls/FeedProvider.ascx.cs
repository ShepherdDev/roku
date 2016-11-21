using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Roku
{
    [DisplayName( "Feed Provider" )]
    [Category( "com_shepherdchurch > Roku" )]
    [Description( "Supplies a Roku formatted XML feed based on a Content Channel." )]

    [ContentChannelField( "Content Channel", "The source of the roku item feed.", true, order: 0 )]
    [DefinedTypeField( "Series Type", "The Defined Type that contains the values of the different Series. The Content Channel must contain an attribute of this type as well.", true, order: 1 )]
    [CodeEditorField( "Category Template", "Lava templare for category request", Rock.Web.UI.Controls.CodeEditorMode.Lava, height: 400, order: 2, defaultValue: @"{% assign currentSeries = TeachingSerieses.first -%}
<categories>
    <category title=""Archive"" description=""Past Messages from Shepherd Church"" sd_img=""http://www.theshepherd.org/Roku/Archive.jpg"" hd_img=""http://www.theshepherd.org/Roku/Archive.jpg"">
    {%- for series in TeachingSerieses -%}
        <categoryLeaf title=""{{ series.Value | Escape }}"" description=""{{ series.Description | Escape }}"" feed=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}page/{{ PageParameter.PageId }}?id={{ series.Id }}"" />
    {%- endfor -%}
    </category>
    <category title=""Current Series"" description=""{{ currentSeries.Value }}"" sd_img=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}GetImage.ashx?guid={{ currentSeries | Attribute:'SDThumbnail','RawValue' }}"" hd_img=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}GetImage.ashx?guid={{ currentSeries | Attribute:'HDThumbnail','RawValue' }}"">
        <categoryLeaf title=""{{ currentSeries.Value | Escape }}"" description=""{{ currentSeries.Description | Escape }}"" feed=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}page/{{ PageParameter.PageId }}?id={{ currentSeries.Id }}"" />
    </category>
    <category title=""About Us"" description=""About Shepherd Church in Porter Ranch, California"" sd_img=""http://www.theshepherd.org/Roku/AboutUs.jpg"" hd_img=""http://www.theshepherd.org/Roku/AboutUs.jpg"">
        <categoryLeaf title=""About Shepherd Church in Porter Ranch, California"" description=""About Shepherd Church in Porter Ranch, California"" feed=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}page/{{ PageParameter.PageId }}?about=true"" />
    </category>
</categories>" )]
    [CodeEditorField( "Feed Template", "Lava templare for category request", Rock.Web.UI.Controls.CodeEditorMode.Lava, height: 400, order: 3, defaultValue: @"{%- if PageParameter.about == ""true"" -%}
<feed>
    <resultLength>4</resultLength>
    <endIndex>4</endIndex>
     <item sdImg=""http://www.theshepherd.org/Roku/AboutUs.jpg"" hdImg=""http://www.theshepherd.org/Roku/AboutUs.jpg"">
        <title>About Shepherd Church</title>
        <releaseDate>2014</releaseDate>
        <contentId>A001</contentId>
        <media>
            <streamQuality>SD</streamQuality>
            <streamBitrate>1200</streamBitrate>
            <streamUrl>http://player.vimeo.com/external/102782556.sd.mp4?s=473825f7bfe98b6f57a81d42bd603736</streamUrl>
        </media>
        <media>
            <streamQuality>HD</streamQuality>
            <streamBitrate>2000</streamBitrate>
            <streamUrl>http://player.vimeo.com/external/102782556.hd.mp4?s=674e2b0813c0d032d159ec41bc7ef7dc</streamUrl>
        </media>
        <synopsis>Welcome to Shepherd!</synopsis>
        <genres>Teaching</genres>
        <runtime>143</runtime>
    </item>
</feed>
{%- else -%}
<feed>
    <resultLength>4</resultLength>
    <endIndex>4</endIndex>
    {%- for item in Items -%}
    {%- capture runtime -%}{{ item.ChannelItem | Attribute:""Time"" | Split:"":"" | First }}{% endcapture -%}
    {%- if runtime == """" %}{% assign runtime = 0 %}{% endif -%}
    {%- capture sdVideo %}{{ item.ChannelItem | Attribute:""Video-SD"",""RawValue"" }}{% endcapture -%}
    {%- capture hdVideo %}{{ item.ChannelItem | Attribute:""Video-HD"",""RawValue"" }}{% endcapture -%}
    <item sdImg=""{{ item.SDImage }}"" hdImg=""{{ item.HDImage }}"">
        <title>{{ item.ChannelItem.Title | Escape }}</title>
        <releaseDate>{{ item.ChannelItem.StartDateTime | Date: ""yyyy"" }}</releaseDate>
        <contentId>{{ item.ChannelItem.Guid }}</contentId>
        {%- if sdVideo != """" -%}
        <media>
            <streamQuality>SD</streamQuality>
            <streamBitrate>1200</streamBitrate>
            <streamUrl>{{ sdVideo }}</streamUrl>
        </media>
        {%- endif -%}
        {%- if hdVideo != """" -%}
        <media>
            <streamQuality>HD</streamQuality>
            <streamBitrate>2000</streamBitrate>
            <streamUrl>{{ hdVideo }}</streamUrl>
        </media>
        {%- endif -%}
        <HDFlag>{% if hdVideo != """" %}true{% else %}false{% endif %}</HDFlag>
        <synopsis>{{ item.ChannelItem.Content | Escape }}</synopsis>
        <genres>Teaching</genres>
        <runtime>{{ runtime | Times:60 }}</runtime>
    </item>
    {%- endfor -%}
</feed>
{%- endif -%}" )]

    public partial class FeedProvider : RockBlock
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, null );
            string template = string.Empty;

            if ( !string.IsNullOrWhiteSpace( Request.QueryString["id"] ) )
            {
                template = GetAttributeValue( "FeedTemplate" );
                GetSeries( mergeFields );
            }
            else
            {
                template = GetAttributeValue( "CategoryTemplate" );
                GetSeriesList( mergeFields );
            }

            if ( UserCanAdministrate )
            {
                ltContent.Text = template.ResolveMergeFields( mergeFields ).EncodeHtml();
                ltDebug.Text = mergeFields.lavaDebugInfo();
            }
            else
            {
            }
        }

        /// <summary>
        /// Get a feed from the content channel limited to the series the user supplied in the ID parameter.
        /// </summary>
        /// <param name="mergeFields">Lava Merge Fields to be populated.</param>
        void GetSeries( Dictionary<string, object> mergeFields )
        {
            var seriesTypes = DefinedTypeCache.Read( GetAttributeValue( "SeriesType" ).AsGuid() );
            var series = DefinedValueCache.Read( Request.QueryString["id"].AsInteger() );

            if ( seriesTypes != null && series != null && !string.IsNullOrWhiteSpace( GetAttributeValue( "ContentChannel" ) ) )
            {
                ContentChannel channel = new ContentChannelService( new RockContext() ).Get( GetAttributeValue( "ContentChannel" ).AsGuid() );
                string seriesTypesIdString = seriesTypes.Id.ToString();

                if ( channel.Items.Count > 0 )
                {
                    foreach ( var i in channel.Items )
                    {
                        i.LoadAttributes();
                    }

                    //
                    // Get the key to use on the items for finding the series DefinedValue.
                    //
                    var seriesKey = channel.Items.First().Attributes
                        .Where( a => a.Value.QualifierValues.ContainsKey( "definedtype" ) && a.Value.QualifierValues["definedtype"].Value == seriesTypesIdString )
                        .Select( a => a.Value ).FirstOrDefault().Key;

                    //
                    // Get a list of items that should be displayed.
                    //
                    var items = channel.Items
                        .Where( i => i.GetAttributeValue( seriesKey ) != null && i.GetAttributeValue( seriesKey ).ToUpper() == series.Guid.ToString().ToUpper() );

                    List<FeedItem> feedItems = new List<FeedItem>();
                    foreach ( var item in items)
                    {
                        feedItems.Add( GenerateItem( item ) );
                    }
                    mergeFields.Add( "Items", feedItems );
                }
            }
        }

        /// <summary>
        /// Build the content channel series information.
        /// </summary>
        /// <param name="mergeFields">Lava Merge Fields to be populated.</param>
        void GetSeriesList( Dictionary<string, object> mergeFields )
        {
            BinaryFileService bfs = new BinaryFileService( new RockContext() );
            var seriesTypes = DefinedTypeCache.Read( GetAttributeValue( "SeriesType" ).AsGuid() );

            if ( seriesTypes != null )
            {
                ContentChannel channel = new ContentChannelService( new RockContext() ).Get( GetAttributeValue( "ContentChannel" ).AsGuid() );
                string seriesTypesIdString = seriesTypes.Id.ToString();

                //
                // Get the last 12 months worth of items in the content channel.
                //
                if ( channel.Items.Count > 0 )
                {
                    foreach ( var i in channel.Items )
                    {
                        i.LoadAttributes();
                    }

                    //
                    // Get the key to use on the items for finding the series DefinedValue.
                    //
                    var seriesKey = channel.Items.First().Attributes
                        .Where( a => a.Value.QualifierValues.ContainsKey( "definedtype" ) && a.Value.QualifierValues["definedtype"].Value == seriesTypesIdString )
                        .Select( a => a.Value ).FirstOrDefault().Key;

                    //
                    // Get a list of the DefinedValues, in order, for the series' that should
                    // be displayed.
                    //
                    DateTime limitDate = DateTime.Now.AddMonths( -12 );
                    var seriesList = channel.Items.Where( i => i.StartDateTime > limitDate )
                        .OrderByDescending( i => i.StartDateTime )
                        .Select( i => i.GetAttributeValue( seriesKey ) )
                        .Where( k => k != null )
                        .Distinct().Select( k => DefinedValueCache.Read( k ) );

                    mergeFields.Add( "TeachingSerieses", seriesList );
                }
            }
        }

        /// <summary>
        /// Generate a Feed Item based on the values from the ContentChannelItem.
        /// </summary>
        /// <param name="xml">XmlWriter to use when building the new Item node.</param>
        /// <param name="item">The ContentChannelItem that contains the information about the item to build.</param>
        FeedItem GenerateItem( ContentChannelItem item )
        {
            string sdThumbnail = null, hdThumbnail = null;
            XmlDocument vimdoc = new XmlDocument();

            item.LoadAttributes();
            try
            {
                vimdoc.Load( "http://vimeo.com/api/v2/video/" + item.GetAttributeValue( "VimeoID" ) + ".xml" );
                if ( vimdoc.GetElementsByTagName( "thumbnail_large" ).Count > 0 )
                {
                    sdThumbnail = hdThumbnail = vimdoc.GetElementsByTagName( "thumbnail_large" )[0].InnerXml;
                }
            }
            catch
            {
                sdThumbnail = hdThumbnail = null;
            }

            return new FeedItem { SDImage = sdThumbnail, HDImage = hdThumbnail, ChannelItem = item };
        }
    }

    /// <summary>
    /// Helper class to provide the Vimeo thumbnails along with the content channel item.
    /// </summary>
    public class FeedItem : DotLiquid.ILiquidizable
    {
        public string SDImage { get; set; }
        public string HDImage { get; set; }
        public ContentChannelItem ChannelItem { get; set; }

        public object ToLiquid()
        {
            var dictionary = new Dictionary<string, object>();

            dictionary.Add( "SDImage", SDImage );
            dictionary.Add( "HDImage", HDImage );
            dictionary.Add( "ChannelItem", ChannelItem );

            return dictionary;
        }
    }
}
