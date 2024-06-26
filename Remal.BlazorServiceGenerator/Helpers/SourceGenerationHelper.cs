﻿using Remal.BlazorServiceGenerator.Models;

namespace Remal.BlazorServiceGenerator.Helpers;

public class SourceGenerationHelper
{
	public const string GeneratorName = "BlazorServiceGenerator";
	public const string GeneratorVersion = "1.0.0";
	public const string Namespace = "Remal.BlazorServiceGenerator";
	public const string AttributeName = "BlazorServiceAttribute";
	public const string AttributeFullName = $"{Namespace}.{AttributeName}";
	public const string AttributeFileName = "BlazorServiceAttribute.g.cs";
	
	public const string AttributeCode = $$"""
		// <auto-generated/>
		
		namespace {{Namespace}};

		[System.AttributeUsage(System.AttributeTargets.Interface)]
		public class {{AttributeName}} : System.Attribute { }
		""";



	public static string GenerateServiceClass(BlazorServiceModel serviceModel)
	{
		CodeWriter writer = new CodeWriter();
		
		writer.AutoGeneratedFileComment();
		writer.AppendNamespace();

		serviceModel.AppendModelCode(writer);

		return writer.ToString();
	}

	public static string GenerateEndpointsMapper(List<BlazorServiceModel> services)
	{
		CodeWriter writer = new CodeWriter();
		writer.AutoGeneratedFileComment();
		writer.AppendNamespace();


		writer.GeneratedCodeAttribute();
		writer.WriteLine("public static class BlazorServicesDependenciesExtensions");
		writer.OpenBlock(() =>
		{
			writer.WriteLine("public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBlazorServices(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
			writer.OpenBlock(() =>
			{
				foreach (BlazorServiceModel blazorServiceModel in services)
				{
					writer.WriteLine($"Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<{blazorServiceModel.ServiceName}, {Namespace}.{blazorServiceModel.ClassName}>(services);");
				}

				writer.Space();
				writer.WriteLine("return services;");
			});
		});


		writer.Space();
		writer.GeneratedCodeAttribute();
		writer.WriteLine("public static class BlazorServicesEndpointRouteBuilderExtensions");
		writer.OpenBlock(() =>
		{
			writer.WriteLine("public static Microsoft.AspNetCore.Routing.RouteGroupBuilder MapBlazorServices(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)");
			writer.OpenBlock(() =>
			{
				writer.WriteLine("Microsoft.AspNetCore.Routing.RouteGroupBuilder blazorEndpoints = Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup(app, \"/BlazorServices\");");
				writer.Space();
				
				
				foreach (BlazorServiceModel blazorServiceModel in services)
				{
					writer.WriteLine($"{Namespace}.BlazorServicesRouting.Map{blazorServiceModel.ClassName}Endpoints(blazorEndpoints);");
				}

				writer.Space();
				writer.WriteLine("return blazorEndpoints;");
			});
		});
		
		return writer.ToString();
	}


	public static bool IsJsonSerializable(string typeName)
	{
		return !(
			typeName.Contains("System.Reflection") ||
			typeName.Contains("System.Linq.Expression") ||
			typeName.Contains("System.Type") ||
			typeName.Contains("System.RuntimeType"));
	}
}