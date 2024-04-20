using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Remal.BlazorServiceGenerator.Helpers;

namespace Remal.BlazorServiceGenerator.Models;

public class BlazorServiceModel
{
	public string ServiceName { get; }
	public string SimpleName { get; }
	public string ClassName { get; }
	public List<MethodModel> Methods { get; }

	public BlazorServiceModel(INamedTypeSymbol interfaceSymbol, SemanticModel semanticModel, int position)
	{
		ServiceName = interfaceSymbol.ToDisplayString();
		SimpleName = (ServiceName.Contains('.') ? ServiceName.Substring(ServiceName.LastIndexOf('.') + 1) : ServiceName).TrimStart('I');
		ClassName = GetClassName(SimpleName);
		
		// Get all the members in the interface
		ImmutableArray<ISymbol> interfaceMembers = interfaceSymbol.GetMembers();
		Methods = interfaceMembers.OfType<IMethodSymbol>().Where(methodSymbol => !methodSymbol.IsStatic)
			.Select(methodSymbol => new MethodModel(methodSymbol, semanticModel, position, ClassName))
			.ToList();

		HandleDuplicates(Methods);
	}

	public bool IsValid()
	{
		return Methods.All(model => model.IsValid());
	}

	public void AppendModelCode(CodeWriter writer)
	{
		writer.GeneratedCodeAttribute();
		writer.WriteLine($"public class {ClassName} : {ServiceName}");
		writer.OpenBlock(() =>
		{
			writer.WriteLine("private System.Net.Http.HttpClient HttpClient { get; }");
			writer.Space();
			
			writer.WriteLine($"public {ClassName}(System.Net.Http.HttpClient httpClient)");
			writer.OpenBlock(() => writer.WriteLine("HttpClient = httpClient;"));
		
			foreach (MethodModel methodModel in Methods)
			{
				methodModel.AppendModelCode(writer, SimpleName);
				writer.Space();
			}
		});

		writer.Space();

		writer.WriteLine("internal static partial class BlazorServicesRouting");
		writer.OpenBlock(() =>
		{
			writer.GeneratedCodeAttribute();
			writer.EditorBrowsableNeverAttribute();
			
			
			writer.WriteLine($"internal static Microsoft.AspNetCore.Routing.RouteGroupBuilder Map{ClassName}Endpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder blazorEndpoints)");
			writer.OpenBlock(() =>
			{
				writer.WriteLine($"""Microsoft.AspNetCore.Routing.RouteGroupBuilder serviceEndpoints = Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup(blazorEndpoints, "{SimpleName}");""");
				writer.Space();
				foreach (MethodModel methodModel in Methods)
				{
					methodModel.AppendEndpointMapping(writer, this);
				}

				writer.Space();
				writer.WriteLine("return serviceEndpoints;");
			});
		});
		
		
		foreach (var dtoModel in Methods.Where(model => model.DataTransferParameter != null))
		{
			dtoModel.DataTransferParameter!.AppendModelCode(writer);
			writer.Space();
		}
		
	}

	private static string GetClassName(string simpleName)
	{
		if (simpleName.EndsWith("Service"))
			return simpleName.Replace("Service", "BlazorService");
		
		if (simpleName.EndsWith("Manager"))
			return simpleName.Replace("Manager", "BlazorManager");
		
		return "Blazor" + simpleName;
	}
	
	
	private void HandleDuplicates(List<MethodModel> methods)
	{
		IEnumerable<IGrouping<string,MethodModel>> groupings = methods.GroupBy(method => (method.RequireSerialization? "1" : "0") + method.Name);
		foreach (IGrouping<string,MethodModel> duplicateMethods in groupings)
		{
			int i = 0;
			foreach (var method in duplicateMethods)
			{
				if(i++ == 0)
					continue;
				method.EndPointName += i.ToString();
				
				if(method.DataTransferParameter != null)
					method.DataTransferParameter = new DataTransferParameterModel(ClassName, method.EndPointName, method.Parameters);
			}
		}
	}
}