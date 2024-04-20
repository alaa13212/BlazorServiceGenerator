using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Remal.BlazorServiceGenerator.Helpers;
using Remal.BlazorServiceGenerator.Models;

namespace Remal.BlazorServiceGenerator;

[Generator(LanguageNames.CSharp)]
public class BlazorServiceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			SourceGenerationHelper.AttributeFileName, 
			SourceText.From(SourceGenerationHelper.AttributeCode, Encoding.UTF8)));
		
		var interfaceDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(SourceGenerationHelper.AttributeFullName, 
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: static (ctx, _) => (InterfaceDeclarationSyntax) ctx.TargetNode);

		var compilationAndInterfaces = context.CompilationProvider.Combine(interfaceDeclarations.Collect());

		context.RegisterSourceOutput(compilationAndInterfaces,
			static (spc, source) => Execute(source.Item1, source.Item2, spc));
	}
	
	private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
		=> node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 };

	private static void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
	{
		if (interfaces.IsDefaultOrEmpty)
		{
			// nothing to do yet
			return;
		}

		// I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
		IEnumerable<InterfaceDeclarationSyntax> distinctInterfaces = interfaces.Distinct();

		// Convert each InterfaceDeclarationSyntax to an BlazorServiceModel
		List<BlazorServiceModel> servicesToGenerate = GetTypesToGenerate(compilation, distinctInterfaces, context);


		foreach (BlazorServiceModel serviceModel in servicesToGenerate)
		{
			string result = SourceGenerationHelper.GenerateServiceClass(serviceModel);
			context.AddSource($"{serviceModel.ClassName}.g.cs", SourceText.From(result, Encoding.UTF8));
		}
		
		if (servicesToGenerate.Count > 0)
		{
			// generate the source code and add it to the output
			string result = SourceGenerationHelper.GenerateEndpointsMapper(servicesToGenerate);
			context.AddSource("BlazorServicesExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
		}
	}

	private static List<BlazorServiceModel> GetTypesToGenerate(Compilation compilation, IEnumerable<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
	{
		// Get the semantic representation of our marker attribute 
		INamedTypeSymbol? attribute = compilation.GetTypeByMetadataName(SourceGenerationHelper.Namespace + "." + SourceGenerationHelper.AttributeName);
		if (attribute == null)
		{
			// If this is null, the compilation couldn't find the marker attribute type
			// which suggests there's something very wrong! Bail out..
			return [];
		}

		List<BlazorServiceModel> serviceModels = [];
		foreach (InterfaceDeclarationSyntax? interfaceSyntax in interfaces)
		{
			// stop if we're asked to
			context.CancellationToken.ThrowIfCancellationRequested();
			
			SemanticModel semanticModel = compilation.GetSemanticModel(interfaceSyntax.SyntaxTree);
			if (semanticModel.GetDeclaredSymbol(interfaceSyntax) is not INamedTypeSymbol interfaceSymbol)
			{
				// something went wrong, bail out
				continue;
			}

			BlazorServiceModel blazorServiceModel = new BlazorServiceModel(interfaceSymbol, semanticModel, interfaceSyntax.SpanStart);
			
			if(!blazorServiceModel.IsValid())
				continue;
			
			// Create an BlazorServiceModel for use in the generation phase
			serviceModels.Add(blazorServiceModel);
		}

		return serviceModels;
	}
}