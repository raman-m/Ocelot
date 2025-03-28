﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Middleware;

public class OcelotPipelineConfiguration
{
    /// <summary>
    /// This is called after the global error handling middleware so any code before calling next.invoke
    /// is the next thing called in the Ocelot pipeline. Anything after next.invoke is the last thing called
    /// in the Ocelot pipeline before we go to the global error handler.
    /// </summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> PreErrorResponderMiddleware { get; set; }

    /// <summary>This allows the user to completely override Ocelot's <see cref="Responder.Middleware.ResponderMiddleware"/>.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> ResponderMiddleware { get; set; }

    /// <summary>This is to allow the user to run any extra authentication before the Ocelot authentication kicks in.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> PreAuthenticationMiddleware { get; set; }

    /// <summary>This allows the user to completely override Ocelot's <see cref="Authentication.Middleware.AuthenticationMiddleware"/>.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> AuthenticationMiddleware { get; set; }

    /// <summary>This is to allow the user to run any extra authorization before the Ocelot authorization kicks in.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> PreAuthorizationMiddleware { get; set; }

    /// <summary>This allows the user to completely override Ocelot's <see cref="Authorization.Middleware.AuthorizationMiddleware"/>.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> AuthorizationMiddleware { get; set; }

    /// <summary>This allows the user to completely override Ocelot's <see cref="Headers.Middleware.ClaimsToHeadersMiddleware"/>.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> ClaimsToHeadersMiddleware { get; set; }

    /// <summary>This allows the user to implement there own query string manipulation logic.</summary>
    /// <value>A <see cref="Func{T1, T2, T3}"/> delegate object.</value>
    public Func<HttpContext, Func<Task>, Task> PreQueryStringBuilderMiddleware { get; set; }

    /// <summary>This is an extension that will branch to different pipes.</summary>
    /// <value>A <see cref="Dictionary{TFunc, TAction}"/> collection.</value>
    public Dictionary<Func<HttpContext, bool>, Action<IApplicationBuilder>> MapWhenOcelotPipeline { get; } = new(); // TODO fix this data structure
}
