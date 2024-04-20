using Microsoft.CodeAnalysis;
using Remal.BlazorServiceGenerator.Extensions;
using Remal.BlazorServiceGenerator.Helpers;

namespace Remal.BlazorServiceGenerator.Models;

public class TypeModel
{
    public bool IsPrimitive { get; }
    public bool IsAwaitable { get; }
    public bool IsEnum { get; }
    public bool IsVoid { get; }
    public string Name { get; }
    public string ModelType { get; }
    public bool IsSerializable { get; }

    public TypeModel(ITypeSymbol typeSymbol, SemanticModel semanticModel, int position)
    {
        IsEnum = typeSymbol.TypeKind == TypeKind.Enum;
        IsPrimitive = typeSymbol.IsPrimitive() || IsEnum;
        IsAwaitable = typeSymbol.IsAwaitable(semanticModel, position);
        Name = typeSymbol.ToDisplayString();

        if (IsAwaitable)
        {
            var namedTypeSymbol = (INamedTypeSymbol) typeSymbol;
            if(namedTypeSymbol.TypeArguments.Length == 1)
            {
                ITypeSymbol modelType = namedTypeSymbol.TypeArguments[0];
                ModelType = modelType.ToDisplayString();
				
                IsEnum = modelType.TypeKind == TypeKind.Enum;
                IsPrimitive = modelType.IsPrimitive() || IsEnum;
            }
            else
            {
                ModelType = "void";
            }
        }
        else
        {
            ModelType = Name;
        }

        IsVoid = ModelType == "void";
        IsSerializable = SourceGenerationHelper.IsJsonSerializable(ModelType);
    }

    public TypeModel(DataTransferParameterModel dataTransferParameter)
    {
        Name = dataTransferParameter.Name;
        ModelType = dataTransferParameter.Name;
        IsSerializable = true;
    }
}