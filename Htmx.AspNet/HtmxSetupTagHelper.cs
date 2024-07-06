using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HTMX;

[HtmlTargetElement("htmx")]
public class HtmxTagHelper : TagHelper
{
    public string Version { get; set; } = "latest";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "script";

        // Construct the URL based on the version
        var src = Version == "latest"
            ? "https://unpkg.com/htmx.org"
            : $"https://unpkg.com/htmx.org@{Version}";

        output.Attributes.SetAttribute("src", src);
        output.TagMode = TagMode.StartTagAndEndTag;
    }
}
