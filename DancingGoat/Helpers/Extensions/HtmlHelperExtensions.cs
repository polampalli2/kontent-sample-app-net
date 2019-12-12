﻿using DancingGoat.Models;
using KenticoCloud.ContentManagement.Helpers.Models;
using KenticoCloud.Delivery.ImageTransformation;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Kentico.Kontent.Delivery;
using Kentico.Kontent.Delivery.ImageTransformation;

namespace DancingGoat.Helpers.Extensions
{
    public static class HtmlHelperExtensions
    {

        /// <summary>
        /// Generates an IMG tag for an image file.
        /// </summary>
        /// <remarks>Should replace it with the asp.net core's img TagHelper in each view</remarks>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="asset">Asset</param>
        /// <param name="title">Title</param>
        /// <param name="cssClass">CSS class</param>
        /// <param name="width">Optional width size</param>
        /// <param name="height">Optional height size</param>
        /// <param name="sizes">Media conditions mapping screen width to image size</param>
        public static IHtmlContent AssetImage(this IHtmlHelper htmlHelper, IConfiguration configuration, KenticoCloud.Delivery.Asset asset, string title = null, string cssClass = "", int? width = null, int? height = null, ResponsiveImageSizes sizes = null)
        {
            if (asset == null)
            {
                return new StringHtmlContent(string.Empty);
            }
            
            var imageUrlBuilder = new ImageUrlBuilder(asset.Url);
            var image = new TagBuilder("img");

            if (width.HasValue)
            {
                image.MergeAttribute("width", width.ToString());
                imageUrlBuilder = imageUrlBuilder.WithWidth(Convert.ToDouble(width));
            }

            if (height.HasValue)
            {
                image.MergeAttribute("height", height.ToString());
                imageUrlBuilder = imageUrlBuilder.WithHeight(Convert.ToDouble(height));
            }
            bool.TryParse(configuration.GetSection("AppConfiguration")["ResponsiveImagesEnabled "], out bool ResponsiveImagesEnabled);
            if (ResponsiveImagesEnabled && !width.HasValue && !height.HasValue)
            {
                image.MergeAttribute("srcset", GenerateSrcsetValue(asset.Url, configuration));

                if (sizes != null)
                {
                    image.MergeAttribute("sizes", sizes.GenerateSizesValue());
                }
            }

            image.MergeAttribute("src", $"{imageUrlBuilder.Url}");
            image.AddCssClass(cssClass);
            string titleToUse = title ?? asset.Description ?? string.Empty;
            image.MergeAttribute("alt", titleToUse);
            image.MergeAttribute("title", titleToUse);
            image.TagRenderMode = TagRenderMode.SelfClosing;

            string result;
            using (var writer = new StringWriter())
            {
                image.WriteTo(writer, HtmlEncoder.Default);
                result = writer.ToString();
            }

            return new HtmlString(result);
        }

        /// <summary>
        /// Generates an IMG tag for an inline image.
        /// </summary>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="image">Inline image.</param>
        /// <param name="sizes">Media conditions mapping screen width to image size</param>
        public static IHtmlContent InlineImage(this IHtmlHelper htmlHelper, IConfiguration configuration, KenticoCloud.Delivery.IInlineImage image, ResponsiveImageSizes sizes = null)
        {
            if (image == null)
            {
                return new StringHtmlContent(string.Empty);
            }

            var imageTag = new TagBuilder("img");
            bool.TryParse(configuration.GetSection("AppConfiguration")["ResponsiveImagesEnabled"], out bool ResponsiveImagesEnabled);
            if (ResponsiveImagesEnabled)
            {
                imageTag.MergeAttribute("srcset", GenerateSrcsetValue(image.Src, configuration));

                if (sizes != null)
                {
                    imageTag.MergeAttribute("sizes", sizes.GenerateSizesValue());
                }
            }

            imageTag.MergeAttribute("src", image.Src);
            imageTag.MergeAttribute("alt", image.AltText);
            imageTag.TagRenderMode = TagRenderMode.SelfClosing;

            string result;

            using (var writer = new StringWriter())
            {
                imageTag.WriteTo(writer, HtmlEncoder.Default);
                result = writer.ToString();
            }
            
            return new HtmlString(result);
        }

        /// <summary>
        /// Displays a <see cref="DateTime"/> in a formatted manner.
        /// </summary>
        /// <param name="htmlHelper">HTML helper</param>
        /// <param name="expression">The expression of the model property</param>
        /// <param name="format">The formatting character</param>
        /// <remarks>The TValue generic parameter is chosen instead of DateTime just to save views from falling to exceptions. With TValue, the views will get rendered, only this helper method will return an empty <see cref="MvcHtmlString"/>.</remarks>
        public static IHtmlContent DateTimeFormattedFor<TModel>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, DateTime?>> expression, string format)
        {
            return htmlHelper.DisplayFor(expression, "DateTime", new DateTimeFormatterParameters { FormatCharacter = format });
        }

        /// <summary>
        /// Returns a navigation button linked to Kentico Kontent's item suitable for block elements.
        /// </summary>
        /// <param name="htmlHelper">HTML helper</param>
        /// <param name="language">Codename of language variant.</param>
        /// <param name="elementIdentifiers">Identifiers of hierarchy of content item.</param>
        public static IHtmlContent BlockElementEditLink(
            this IHtmlHelper htmlHelper,
            IConfiguration configuration,
            string language,
            params ElementIdentifier[] elementIdentifiers
            )
        {
            var itemUrl = GetItemElementUrl(configuration, language, elementIdentifiers);

            var generatedHtml = $@"
<a target=""_blank"" class=""edit-link__overlay--block"" href=""{itemUrl}"" >
  <span>
      <i aria-hidden=""true"" class=""edit-link__button-icon edit-link__button-icon--block""></i>
  </span>
</a>";

            return new HtmlString(generatedHtml);
        }

        /// <summary>
        /// Returns a navigation button linked to Kentico Kontent's item suitable for inline elements.
        /// </summary>
        /// <param name="htmlHelper">HTML helper</param>
        /// <param name="language">Codename of language variant.</param>
        /// <param name="elementIdentifiers">Identifiers of hierarchy of content item.</param>
        public static IHtmlContent InlineElementEditLink(
            this IHtmlHelper htmlHelper,
            IConfiguration configuration,
            string language,
            params ElementIdentifier[] elementIdentifiers
            )
        {
            var itemUrl = GetItemElementUrl(configuration, language, elementIdentifiers);

            var generatedHtml = $@"
<a target=""_blank"" class=""edit-link__overlay--inline"" href=""{itemUrl}"">
    <i aria-hidden=""true"" class=""edit-link__button-icon edit-link__button-icon--inline""></i>
</a>";

            return new StringHtmlContent(generatedHtml);
        }

        /// <summary>
        /// Displays Edit Mode Panel while using preview api.
        /// </summary>
        /// <param name="htmlHelper">HTML helper</param>
        /// <param name="itemId">Id (guid) of content item identifier</param>
        /// <param name="language">Codename of language variant</param>
        public static async Task EditPanelAsync(this IHtmlHelper htmlHelper, IConfiguration configuration, string itemId, string language)
        {
            bool.TryParse(configuration.GetSection("AppConfiguration")["UsePreviewApi"], out var isPreview);

            if (isPreview)
            {
                var itemUrl = GetItemUrl(language, itemId, configuration);
                var editPanelViewModel = new EditPanelViewModel() { ItemUrl = itemUrl };
                await htmlHelper.RenderPartialAsync("EditModePanel", editPanelViewModel);
            }
        }
        /// <summary>
        /// Returns an HTML input element with a label and validation fields for each property in the object that is represented by the <see cref="Expression"/> expression.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="html">The HTML helper instance that this method extends.</param>
        /// <param name="expression">An expression that identifies the object that contains the displayed properties.</param>
        /// <param name="explanationText">An explanation text describing usage of the rendered field.</param>
        public static IHtmlContent ValidatedEditorFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string explanationText = "", string id = "")
        {
            string label;
            string editor;

            if (!string.IsNullOrEmpty(id))
            {
                label = html.LabelFor(expression, new { @for = id }).ToString();
                editor = html.EditorFor(expression, new { id }).ToString();
            }
            else
            {
                editor = html.EditorFor(expression).ToString();
                label = html.LabelFor(expression).ToString();
            }

            var message = html.ValidationMessageFor(expression).ToString();
            var explanationTextHtml = "";

            if (!string.IsNullOrEmpty(explanationText))
            {
                explanationTextHtml = "<div class=\"explanation-text\">" + explanationText + "</div>";
            }

            var generatedHtml = $@"
<div class=""form-group"">
    <div class=""form-group-label"">{label}</div>
    <div class=""form-group-input"">{editor}
       {explanationTextHtml}
    </div>
    <div class=""message-validation"">{message}</div>
</div>";

            return new StringHtmlContent(generatedHtml);
        }


        private static string GetItemUrl(string language, string itemId, IConfiguration configuration)
        {
            return EditLinkHelper.GetInstance(configuration).Builder.BuildEditItemUrl(language, itemId);
        }

        private static string GetItemElementUrl(IConfiguration configuration, string language, params ElementIdentifier[] elementIdentifiers)
        {
            return EditLinkHelper.GetInstance(configuration).Builder.BuildEditItemUrl(language, elementIdentifiers);
        }

        private static string GenerateSrcsetValue(string imageUrl, IConfiguration configuration)
        {
            var imageUrlBuilder = new ImageUrlBuilder(imageUrl);

            return string.Join(",", configuration.GetSection("AppConfiguration")["ResponsiveWidths"].Select(w
                => $"{imageUrlBuilder.WithWidth(Convert.ToDouble(w)).Url} {w}w"));
        }
    }
}
