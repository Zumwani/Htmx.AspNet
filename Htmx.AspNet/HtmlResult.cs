using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HTMX;

public class HtmlResult<THomeViewModel>(HtmxController<THomeViewModel> controller) : ContentResult, IHtmlResult where THomeViewModel : IHomeViewModel, new()
{

    public IHtmlResult ClearHtml()
    {
        partials.Clear();
        wrappers.Clear();
        return this;
    }

    public IActionResult GeneratedHtml() =>
        controller.GeneratedHtml();

    #region Title

    public IHtmlResult SetTitle(string title)
    {
        partials.Insert(0, ("Part/_Title", title));
        return this;
    }

    #endregion
    #region Background

    public IHtmlResult SetBackground(string background)
    {
        partials.Insert(0, ("Part/_Background", background));
        return this;
    }

    #endregion
    #region Partials

    readonly List<(string partialName, object? model)> partials = [];

    /// <summary>Renders the partial as html and adds it to the response.</summary>
    public IHtmlResult AddPartial(string name)
    {
        partials.Add((name, null));
        return this;
    }

    /// <summary>Renders the model with the specified partial.</summary>
    public IHtmlResult AddPartial<T>(string name, T model) =>
        AddPartials(name, model);

    /// <summary>Renders all models with the specified partial.</summary>
    public IHtmlResult AddPartials<T>(string name, params T[] model)
    {
        if (model.Length == 0)
            partials.Add((name, null));
        else
            foreach (var obj in model)
                partials.Add((name, obj));

        return this;
    }

    #endregion
    #region Wrappers

    readonly List<string> wrappers = [];

    /// <summary>Wraps the html response in <paramref name="start"/> and <paramref name="end"/>.</summary>
    public IHtmlResult WrapIn(string partial)
    {
        wrappers.Add(partial);
        return this;
    }

    #endregion
    #region Render

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        controller.OnHTMLResponse(this);

        //Render the partials added using AddPartial methods, and write it as response to client

        var response = context.HttpContext.Response;
        response.Headers["ContentType"] = "text/html";

        string html;
        html = await RenderPartials();
        html = await RenderWrappers(html);

        using var sw = new StreamWriter(response.Body, Encoding.UTF8);
        await sw.WriteAsync(html);
        await sw.DisposeAsync(); //Must dispose manually, asp.net throws otherwise
    }

    async Task<string> RenderPartials()
    {
        var sb = new StringBuilder();
        foreach (var (partialName, model) in partials)
            sb.AppendLine(await RenderViewToStringAsync(partialName, model));

        return sb.ToString();
    }

    async Task<string> RenderWrappers(string html)
    {
        foreach (var partial in wrappers)
            html = await RenderViewToStringAsync(partial, html);

        return html;
    }

    //Taken, with modifications, from https://stackoverflow.com/a/65462120/24282772
    async Task<string> RenderViewToStringAsync(string viewNamePath, object? model = null)
    {
        if (string.IsNullOrEmpty(viewNamePath))
            viewNamePath = controller.ControllerContext.ActionDescriptor.ActionName;

        controller.ViewData.Model = model;

        using var writer = new StringWriter();

        var view = FindView(viewNamePath);
        var viewContext = new ViewContext(controller.ControllerContext, view, controller.ViewData, controller.TempData, writer, new HtmlHelperOptions());

        await view.RenderAsync(viewContext);

        var s = writer.GetStringBuilder().ToString().Trim();
        return s;
    }

    IView FindView(string viewNamePath)
    {
        var viewEngine = (ICompositeViewEngine)controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine))!;

        var viewResult =
            viewNamePath.EndsWith(".cshtml")
            ? viewEngine.GetView(viewNamePath, viewNamePath, false)
            : viewEngine.FindView(controller.ControllerContext, viewNamePath, false);

        if (!viewResult.Success)
        {
            var endPointDisplay = controller.HttpContext.GetEndpoint()!.DisplayName;

            if (endPointDisplay!.Contains(".Areas."))
            {
                //search in Areas
                var areaName = endPointDisplay[(endPointDisplay.IndexOf(".Areas.") + ".Areas.".Length)..];
                areaName = areaName[..areaName.IndexOf(".Controllers.")];

                viewNamePath = $"~/Areas/{areaName}/views/{controller.HttpContext.Request.RouteValues["controller"]}/{controller.HttpContext.Request.RouteValues["action"]}.cshtml";

                viewResult = viewEngine.GetView(viewNamePath, viewNamePath, false);
            }

            if (!viewResult.Success)
                throw new Exception($"A view with the name '{viewNamePath}' could not be found");
        }

        return viewResult.View;
    }

    #endregion

}
