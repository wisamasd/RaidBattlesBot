﻿using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace RaidBattlesBot
{
  public class JsonpMediaTypeFormatter : TextOutputFormatter
  {
    private static readonly MediaTypeHeaderValue _applicationJavaScript = new MediaTypeHeaderValue("application/javascript");
    private static readonly MediaTypeHeaderValue _applicationJsonp = new MediaTypeHeaderValue("application/json-p");
    private static readonly MediaTypeHeaderValue _textJavaScript = new MediaTypeHeaderValue("text/javascript");

    private readonly TextOutputFormatter _jsonOutputFormatter;
    private readonly string _callbackQueryParameter;

    public JsonpMediaTypeFormatter(TextOutputFormatter jsonOutputFormatter, string callbackQueryParameter = null)
    {
      _jsonOutputFormatter = jsonOutputFormatter ?? throw new ArgumentNullException(nameof(jsonOutputFormatter));
      _callbackQueryParameter = callbackQueryParameter ?? "callback";

      SupportedMediaTypes.Add(_textJavaScript);
      SupportedMediaTypes.Add(_applicationJavaScript);
      SupportedMediaTypes.Add(_applicationJsonp);
      
      foreach (var encoding in _jsonOutputFormatter.SupportedEncodings)
      {
        SupportedEncodings.Add(encoding);
      }
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
      if (context == null)
      {
        throw new ArgumentNullException(nameof(context));
      }

      return _jsonOutputFormatter.CanWriteResult(context);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
      if (context == null)
      {
        throw new ArgumentNullException(nameof(context));
      }

      if (IsJsonpRequest(context.HttpContext.Request, _callbackQueryParameter, out var callback))
      {
        if (!CallbackValidator.IsValid(callback))
        {
          throw new InvalidOperationException($"Callback '{callback}' is invalid!");
        }

        using (var writer = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
        {
          // the /**/ is a specific security mitigation for "Rosetta Flash JSONP abuse"
          // the typeof check is just to reduce client error noise
          await writer.WriteAsync($"/**/ typeof {callback} === 'function' && {callback}(");
          await writer.FlushAsync();
          await _jsonOutputFormatter.WriteResponseBodyAsync(context, selectedEncoding);
          await writer.FlushAsync();
          await writer.WriteAsync(");");
          await writer.FlushAsync();
        }
      }
      else
      {
        await _jsonOutputFormatter.WriteResponseBodyAsync(context, selectedEncoding);
      }
    }

    private static bool IsJsonpRequest(HttpRequest request, string callbackQueryParameter, out string callback)
    {
      callback = null;

      if (request == null || request.Method.ToUpperInvariant() != "GET")
      {
        return false;
      }

      callback = request.Query
          .Where(kvp => kvp.Key.Equals(callbackQueryParameter, StringComparison.OrdinalIgnoreCase))
          .Select(kvp => kvp.Value)
          .FirstOrDefault();

      return !string.IsNullOrEmpty(callback);
    }
  }

  public static class CallbackValidator
  {
    private static readonly Regex JsonpCallbackFormat = new Regex(@"[^0-9a-zA-Z\$_\.]|^(abstract|boolean|break|byte|case|catch|char|class|const|continue|debugger|default|delete|do|double|else|enum|export|extends|false|final|finally|float|for|function|goto|if|implements|import|in|instanceof|int|interface|long|native|new|null|package|private|protected|public|return|short|static|super|switch|synchronized|this|throw|throws|transient|true|try|typeof|var|volatile|void|while|with|NaN|Infinity|undefined)$");

    public static bool IsValid(string callback)
    {
      return !JsonpCallbackFormat.IsMatch(callback);
    }
  }
}