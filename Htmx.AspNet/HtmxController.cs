using Microsoft.AspNetCore.Mvc;

namespace HTMX;

public class HtmxController<THomeViewModel>(string defaultInitialHtmxRequest) : Controller, IHtmlResult where THomeViewModel : IHomeViewModel, new()
{

    /// <summary>Returns <paramref name="html"/> as response.</summary>
    public IActionResult Html(string html) =>
        new ContentResult() { Content = html };

    /// <summary>Called right before response is sent. Use this to set title or wrap partials, or similar.</summary>
    public virtual void OnHTMLResponse(HtmlResult<THomeViewModel> html)
    { }

    HtmlResult<THomeViewModel>? m_result;

    public HtmlResult<THomeViewModel> HtmlResult => m_result ??= new(this);

    public IActionResult GeneratedHtml() =>
        IsHtmxRequest()
        ? HtmlResult
        : RedirectToHome();

    public bool IsHtmxRequest() =>
        Request is not null && Request.Headers["hx-request"] == "true";

    public ViewResult RedirectToHome(string? redirectUrl = null)
    {
        redirectUrl ??= ($"{Request?.Path}{Request?.QueryString}") ?? defaultInitialHtmxRequest;
        //User navigated to endpoint directly, lets redirect to page proper, then make request once more
        return View("~/Views/Home/Index.cshtml", new THomeViewModel() { InitialHtmxRequest = redirectUrl });
    }

    #region IHtmlResult

    public IHtmlResult AddPartial(string name) => HtmlResult.AddPartial(name);
    public IHtmlResult AddPartial<TModel>(string name, TModel model) => HtmlResult.AddPartial(name, model);
    public IHtmlResult AddPartials<TModel>(string name, params TModel[] model) => HtmlResult.AddPartials(name, model);
    public IHtmlResult SetBackground(string background) => HtmlResult.SetBackground(background);
    public IHtmlResult SetTitle(string title) => HtmlResult.SetTitle(title);
    public IHtmlResult WrapIn(string partial) => HtmlResult.WrapIn(partial);
    public IHtmlResult ClearHtml() => HtmlResult.ClearHtml();

    #endregion

}
