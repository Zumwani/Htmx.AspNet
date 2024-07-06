using Microsoft.AspNetCore.Mvc;

namespace HTMX;

public interface IHtmlResult
{
    IHtmlResult AddPartial(string name);
    IHtmlResult AddPartial<TModel>(string name, TModel model);
    IHtmlResult AddPartials<TModel>(string name, params TModel[] model);
    IHtmlResult SetBackground(string background);
    IHtmlResult SetTitle(string title);
    IHtmlResult WrapIn(string partial);
    IHtmlResult ClearHtml();
    IActionResult GeneratedHtml();
}
