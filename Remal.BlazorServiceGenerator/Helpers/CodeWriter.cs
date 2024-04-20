﻿using System.Text;

namespace Remal.BlazorServiceGenerator.Helpers;

public class CodeWriter
{
    private readonly StringBuilder _sb = new();
    
    public int IndentLevel { get; set; }

    public CodeWriter Indent()
    {
        for (int i = 0; i < IndentLevel; i++)
            _sb.Append("\t");
        return this;
    }

    public CodeWriter Write(string line)
    {
        _sb.Append(line);
        return this;
    }
    
    public CodeWriter WriteLine(string line)
    {
        Indent();
        _sb.AppendLine(line);
        return this;
    }
    
    public CodeWriter AutoGeneratedFileComment()
    {
        WriteLine("// <auto-generated/>");
        Space();
        return this;
    }

    public CodeWriter AppendNamespace(string @namespace = SourceGenerationHelper.Namespace)
    {
        WriteLine($"namespace {@namespace};");
        Space();
        return this;
    }
    
    public CodeWriter GeneratedCodeAttribute(string name = SourceGenerationHelper.GeneratorName, string version = SourceGenerationHelper.GeneratorVersion)
    {
        WriteLine($"[System.CodeDom.Compiler.GeneratedCode(\"{name}\", \"{version}\")]");
        
        return this;
    }
    
    public CodeWriter EditorBrowsableNeverAttribute()
    {
        WriteLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
        
        return this;
    }

    public void OpenBlock(Action blockBuilder)
    {
        WriteLine("{");
        IndentLevel++;
        blockBuilder();
        IndentLevel--;
        WriteLine("}");
    }

    public CodeWriter Space()
    {
        Indent();
        _sb.AppendLine();
        return this;
    }
    
    public CodeWriter Space(int size)
    {
        for (int i = 0; i < size; i++)
            Space();
        
        return this;
    }

    #region Overrides of Object

    /// <inheritdoc />
    public override string ToString()
    {
        return _sb.ToString();
    }

    #endregion
}