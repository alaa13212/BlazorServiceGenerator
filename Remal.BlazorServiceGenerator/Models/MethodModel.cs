using Microsoft.CodeAnalysis;
using Remal.BlazorServiceGenerator.Extensions;
using Remal.BlazorServiceGenerator.Helpers;

namespace Remal.BlazorServiceGenerator.Models;

public class MethodModel
{
    public TypeModel ReturnType { get; set; }
	
    public string Name { get; set; }
	
    public string EndPointName { get; set; }
    public ICollection<ParameterModel> Parameters { get; set; }

    public bool RequireSerialization { get; set; }
    public DataTransferParameterModel? DataTransferParameter { get; set; }
	

    public MethodModel(IMethodSymbol methodSymbol, SemanticModel semanticModel, int position, string className)
    {
        Name = methodSymbol.Name;
        EndPointName = Name;
        ReturnType = new TypeModel(methodSymbol.ReturnType, semanticModel, position);
        Parameters = methodSymbol.Parameters.Select(parameter => new ParameterModel(parameter, semanticModel, position)).ToList();
		
        int requireSerializationCount = Parameters.Count(model => !model.Type.IsPrimitive);
        RequireSerialization = requireSerializationCount > 0;
        if (requireSerializationCount > 1)
        {
            DataTransferParameter = new DataTransferParameterModel(className, EndPointName, Parameters);
        }
    }

    public void AppendModelCode(CodeWriter writer, string serviceName)
    {
        writer.Indent();
        writer.Write($"public async {ReturnType.Name} {Name} (");
        
        
        bool isFirst = true;
        foreach (ParameterModel parameter in Parameters)
        {
            if(!isFirst)
                writer.Write(", ");
			
            parameter.AppendModelCode(writer);
            isFirst = false;
        }

        writer.Write(")\n");
        writer.OpenBlock(() =>
        {
            PrepareEndpointCode(writer, serviceName);
            
            bool requiresSerialization = Parameters.Any(parameter => !parameter.Type.IsPrimitive);
            if (requiresSerialization)
                SendPostRequestCode(writer);
            else
                SendGetRequestCode(writer);
            
        });
    }

    private void SendGetRequestCode(CodeWriter writer)
    {
        if (ReturnType.IsVoid)
        {
            writer.WriteLine("await HttpClient.GetAsync(path);");
        }
        else if (ReturnType.ModelType == "string")
        {
            writer.WriteLine("return await HttpClient.GetStringAsync(path);");
        }
        else
        {
            writer.WriteLine($"return await System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<{ReturnType.ModelType}>(HttpClient, path);");
        }
    }

    private void SendPostRequestCode(CodeWriter writer)
    {
        var parameter = PreparePostModel(writer);
        if (ReturnType.IsVoid)
        {
            writer.WriteLine($"await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(HttpClient, path, {parameter.Name});");
        }
        else
        {
            writer.WriteLine($"var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(HttpClient, path, {parameter.Name});");
            writer.WriteLine($"response.EnsureSuccessStatusCode();");

            if (ReturnType.ModelType == "string")
            {
                writer.WriteLine("return await response.Content.ReadAsStringAsync();");
            }
            else
            {
                writer.WriteLine("var jsonStream = await response.Content.ReadAsStreamAsync();");
                writer.WriteLine($"return System.Text.Json.JsonSerializer.Deserialize<{ReturnType.ModelType}>(jsonStream);");
            }
        }
    }

    private void PrepareEndpointCode(CodeWriter writer, string serviceName)
    {
        writer.WriteLine($"""string path = "/BlazorServices/{serviceName}/{EndPointName}";""");

        if(Parameters.Any(parameter => parameter.Type.IsPrimitive))
        {
            writer.Space();
            writer.WriteLine("var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);");
		
            foreach (ParameterModel parameter in Parameters.Where(parameter => parameter.Type.IsPrimitive))
            {
                writer.WriteLine($"""queryParams["{parameter.Name}"] = {(parameter.Type.ModelType == "string"? parameter.Name : parameter.Name + ".ToString()")};""");
            }
			
            writer.Space();
            writer.WriteLine("""path += "?" + queryParams;""");
        }
    }

    private ParameterModel PreparePostModel(CodeWriter writer)
    {
        if (DataTransferParameter == null)
            return Parameters.First(parameter => !parameter.Type.IsPrimitive);
        
        writer.WriteLine($"{DataTransferParameter.Name} dataTransferObject = new ({DataTransferParameter.GetArguments()});");
        return new ParameterModel(DataTransferParameter);
    }

    public string GetSignature(bool primitiveOnly = false)
    {
        var parameters = primitiveOnly ? Parameters.Where(parameter => parameter.Type.IsPrimitive) : Parameters;
        string result = string.Join(", ", parameters.Select(parameter => $"{(parameter.Type.IsPrimitive? "[Microsoft.AspNetCore.Mvc.FromQuery] " : "")}{parameter.Type.Name} {parameter.Name}"));
        if(string.IsNullOrWhiteSpace(result))
            return "";
        return result + ", ";

    }
	
    public string GetArguments(bool useDto = false)
    {
        return string.Join(", ", Parameters.Select(model => useDto && !model.Type.IsPrimitive? "dto." + model.Name.UpperFirstLetter() : model.Name));
    }

    public bool IsValid()
    {
        return ReturnType is { IsAwaitable: true, IsSerializable: true } && Parameters.All(model => model.Type.IsSerializable);
    }

    public void AppendEndpointMapping(CodeWriter writer, BlazorServiceModel blazorServiceModel)
    {
        if (DataTransferParameter != null)
        {
            writer.WriteLine($"Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapPost(serviceEndpoints, \"{EndPointName}\", ({GetSignature(true)}{DataTransferParameter.Name} dto, [Microsoft.AspNetCore.Mvc.FromServices] {blazorServiceModel.ServiceName} service) => service.{Name}({GetArguments(true)}));");
        }
        else if (RequireSerialization)
        {
            writer.WriteLine($"Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapPost(serviceEndpoints, \"{EndPointName}\", ({GetSignature()}[Microsoft.AspNetCore.Mvc.FromServices] {blazorServiceModel.ServiceName} service) => service.{Name}({GetArguments()}));");
        }
        else
        {
            writer.WriteLine($"Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGet(serviceEndpoints, \"{EndPointName}\", ({GetSignature()}[Microsoft.AspNetCore.Mvc.FromServices] {blazorServiceModel.ServiceName} service) => service.{Name}({GetArguments()}));");
        }
    }
}