# Blazor Service Source Generator
Source generator that reduce boilerplate code needed to make WebAssembly compatible blazor services.

# Installation
You can install using NuGet
```shell
dotnet add package Remal.BlazorServiceGenerator --version 0.1.3
```

**NOTE: The generator requires extra setup before it is functioning properly. See [setup](#setup) section.**

# The Problem
Let's start with a basic Blazor solution. There are probably 3 projects `Shared`, `Server` and `Client`. Somewhere in the `Server` project there is a piece of code like this:

*Server / Greeter.razor*
```razor
@page "/Greeter"
@using BlazorApp.Shared.Services
@rendermode InteractiveServer

@inject IGreeterService GreeterService

Enter your name:
<input type="text" @bind="Name"/>

<button @onclick="Great">Great</button>

@if (Greeting != null)
{
    <p>Server Greeting: @Greeting</p> 
}

@code {
    private string Name { get; set; } = string.Empty;
    private string? Greeting { get; set; }
    
    private async Task Great()
    {
        Greeting = await GreeterService.Greet(Name);
    }
}
```

With service like this:

*Server / GreeterService.cs* 
```csharp
public class GreeterService : IGreeterService
{
    public async Task<string> Greet(string name)
    {
        // TODO use an AI driven blockchain stack to retrieve a personalized greeting message  
        await Task.Delay(200);
        
        return $"Hello, {name}!";
    }
}
```

If you want to make this component compatible with WebAssembly you need to implement `IGreeterService` again but this time with http client which is mostly boilerplate code.

*Client / GreeterBlazorService.cs*
```csharp
public class GreeterBlazorService : IGreeterService
{
    private HttpClient HttpClient { get; }
	
    public GreeterBlazorService(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }
    
    public async Task<string> Greet (string name)
    {
        string path = $"/GreeterService/Greet?name={name}";
        return await HttpClient.GetStringAsync(path);
    }
	
}
```

Make sure to also add the new service to blazor dependencies in client project

*Client / Program.cs*
```csharp
builder.Services.AddScoped<IGreeterService, GreeterBlazorService>();
```

And then add appropriate endpoint mapping for `/GreeterService/Greet` either through MVC or minimal APIs:

*Server / Program.cs*
```csharp 
app.MapGet("/GreeterService/Greet", ([FromQuery] string name, [FromServices] IGreeterService service) => service.Greet(name));
```


This code gets redundant very quickly, epically if you are moving many components to WebAssembly

# The Solution
After installing and [setting up](#setup) BlazorServiceGenerator you can mark an interface with `[BlazorService]` attribute. This will auto generate all previous boilerplate including:
* `HttpClient` based service implementation
* Adding service as dependency to `Client` project
* Endpoint mappings for `Server` project

*Shared / IGreeterService.cs*
```csharp
[BlazorService]
public interface IGreeterService
{
    // ...
}
```

# Setup
After adding the generator to the `Shared` project. There will be a one time setup process to make sure the generator works properly.

First: make sure the `Shared` project has *framework reference* for `Microsoft.AspNetCore.App`.

*Shared.csproj*
```xml
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" PrivateAssets="all" />
    ...
</ItemGroup>
```

Second: make sure the `Client` project has the following in its `PropertyGroup`:

*Client.csproj*
```xml
<PropertyGroup>
    ...
    <GenerateMvcApplicationPartsAssemblyAttributes>false</GenerateMvcApplicationPartsAssemblyAttributes>
</PropertyGroup>
```

Third: Add blazor service dependencies to `Client` project.

*Client / Program.cs*
```csharp
// This method is auto generated and may not be avialable before using [BlazorService]
builder.Services.AddBlazorServices();
```

Finally: Add endpoint mapping to `Server` project.

*Server / Program.cs*
```csharp
// This method is auto generated and may not be avialable before using [BlazorService]
app.MapBlazorServices();
```

# Limitations
There are some limitations on interfaces that can be marked as `[BlazorService]`. Some are due to network requirement others are just to reduce complexity.
* All BlazorService methods must be await-able - By Design - code should be asynchronous due to network usage
* Member must be a method - By Design - extenstion of previous rule
* Return type must be serializable - By Design - due to network usage
* Method parameters must be serializable - By Design - due to network usage
* Method may not be generic - Technical - to reduce generator complexity

# License
This project is licensed under the terms of the MIT license.

Copyright (c) 2024 Ali Albarrak