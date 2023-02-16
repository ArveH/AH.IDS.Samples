﻿using System.Text.Json;

namespace Shared.Net6;

public static class TokenResponseExtensions
{
    public static string PrettyPrintJson(this string raw)
    {
        var doc = JsonDocument.Parse(raw).RootElement;
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}