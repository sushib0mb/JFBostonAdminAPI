using System.Reflection.Metadata.Ecma335;
using JFBostonAdminAPI.Models;
using Microsoft.AspNetCore.Diagnostics;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Setup supabase as a service
var url = builder.Configuration["SUPABASE_URL"];
var key = builder.Configuration["SUPABASE_ANON_KEY"];
var options = new Supabase.SupabaseOptions { AutoConnectRealtime = true };

builder.Services.AddSingleton(provider => new Supabase.Client(url, key, options));

var app = builder.Build();

app.MapGet("/api/schedule", async (Supabase.Client client) =>
{
    var result = await client.From<Performances>().Get();

    // This "Select" maps your complex model to a simple list of values
    var cleanData = result.Models.Select(p => new
    {
        p.Id,
        p.Name,
        p.StartTime
    }).OrderBy(p => p.StartTime);

    return Results.Ok(cleanData);
});

app.MapPost("/api/schedule/delay/{id}", async (int id, Supabase.Client client) =>
{
    // Fetches the performance according to id
    var result = await client.From<Performances>().Where(x => x.Id == id).Single();

    // Delays the startTime variable by 15 minutes if the id exists
    if (result == null)
    {
        return Results.NotFound($"No performance found with ID {id}");
    }

    result.StartTime = result.StartTime.AddMinutes(15);
    await result.Update<Performances>();

    return Results.Ok($"Performance {id} updated to {result.StartTime}");
});

// Delays all performance by x minutes starting from the performance
app.MapPost("api/schedule/shuffle/{id}", async (int id, int minutes, Supabase.Client client) =>
{
    // Fetch the performance with the id that starts the delay
    var result = await client.From<Performances>().Where(x => x.Id == id).Single();
    if (result == null) return Results.NotFound($"No performance found with id {id}");

    // Convert to UTC, which is the timezone supabase uses for comparisons
    var safeStartTime = result.StartTime.ToUniversalTime();

    // Fetches all performances starting after or at the same time as the selected performance
    var futurePerformances = await client.From<Performances>().Where(x => x.StartTime >= safeStartTime).Get();

    var listToUpdate = futurePerformances.Models;

    foreach (var p in listToUpdate)
    {
        p.StartTime = p.StartTime.AddMinutes(minutes);
    }

    await client.From<Performances>().Upsert(listToUpdate);

    return Results.Ok($"Shifted {listToUpdate.Count} performances by {minutes} minutes");
});

app.Run();
