using Microsoft.CodeAnalysis;
using Remal.BlazorServiceGenerator.Extensions;
using Remal.BlazorServiceGenerator.Helpers;

namespace Remal.BlazorServiceGenerator.Models;

public class ParameterModel
{
    public TypeModel Type { get; set; }
    public string Name { get; set; }

    public ParameterModel(IParameterSymbol parameterSymbol, SemanticModel semanticModel, int position)
    {
        Type = new TypeModel(parameterSymbol.Type, semanticModel, position);
        Name = parameterSymbol.Name;
    }

    public ParameterModel(DataTransferParameterModel dataTransferParameter)
    {
        Type = new TypeModel(dataTransferParameter);
        Name = "dataTransferObject";
    }

    public void AppendModelCode(CodeWriter writer, bool capitalize = false)
    {
        string name = capitalize ? Name.UpperFirstLetter() : Name;
        writer.Write($"{Type.Name} {name}");
    }
}