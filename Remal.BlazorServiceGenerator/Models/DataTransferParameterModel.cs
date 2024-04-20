using Remal.BlazorServiceGenerator.Extensions;
using Remal.BlazorServiceGenerator.Helpers;

namespace Remal.BlazorServiceGenerator.Models;

public class DataTransferParameterModel
{
    public string Name { get; }
    public List<ParameterModel> Parameters { get; }

    public DataTransferParameterModel(string className, string methodName, IEnumerable<ParameterModel> parameters)
    {
        Name = className + methodName + "Dto";
        Parameters = parameters.Where(model => !model.Type.IsPrimitive).ToList();
    }

    public void AppendModelCode(CodeWriter writer)
    {
        writer.WriteLine($"file class {Name}");
        writer.OpenBlock(() =>
        {
            AddProperties(writer);
            
            writer.Space();
        
            AddConstructor(writer);
        });
    }

    private void AddProperties(CodeWriter writer)
    {
        foreach (ParameterModel parameter in Parameters)
        {
            writer.Indent();
            writer.Write("public ");
            parameter.AppendModelCode(writer, true);
            writer.Write(" { get; }\n");
        }
    }

    private void AddConstructor(CodeWriter writer)
    {
        writer.WriteLine($"public {Name}({GetSignature()})");
        writer.OpenBlock(() =>
        {
            foreach (ParameterModel parameter in Parameters)
            {
                writer.WriteLine($"{parameter.Name.UpperFirstLetter()} = {parameter.Name};");
            }
        });
    }


    public string GetSignature()
    {
        return string.Join(", ", Parameters.Select(parameter => $"{parameter.Type.ModelType} {parameter.Name}"));
    }
	
    public string GetArguments()
    {
        return string.Join(", ", Parameters.Select(parameter => $"{parameter.Name}"));
    }
}