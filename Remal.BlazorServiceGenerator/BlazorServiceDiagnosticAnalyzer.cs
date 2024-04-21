using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Remal.BlazorServiceGenerator.Extensions;
using Remal.BlazorServiceGenerator.Helpers;

namespace Remal.BlazorServiceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BlazorServiceDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MethodNotAwaitable = new("BS0001", "Methods not await-able", "The method '{0} {1}' must be await-able. All [BlazorService] methods must be await-able.", "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: "All BlazorService methods must be await-able.");
    private static readonly DiagnosticDescriptor MemberMustBeMethod = new("BS0002", "Member not allowed", "The member '{0}' is not allowed here. Only methods are allowed in a [BlazorService].", "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: "All BlazorService members must be methods.");
    private static readonly DiagnosticDescriptor ReturnTypeMustBeSerializable = new("BS0003", "Return type must be serializable", "The return type '{0}' of method '{0}' is not serializable. All [BlazorService] method must return a json serializable type.", "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: "All BlazorService method must return a json serializable type.");
    private static readonly DiagnosticDescriptor MethodParameterMustBeSerializable = new("BS0004", "Method parameter must be serializable", "The parameter '{0}' of method '{1}' is not serializable. All [BlazorService] method parameters must be of a json serializable type.", "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: "All BlazorService method parameters must be of a json serializable type.");
    private static readonly DiagnosticDescriptor MethodMustNotBeGeneric = new("BS0005", "Method may not be generic", "The method '{0} {1}' must NOT be generic. All [BlazorService] methods must NOT be generic.", "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: "All BlazorService methods must NOT be await-able.");
    
    #region Overrides of DiagnosticAnalyzer

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        MethodNotAwaitable,
        MemberMustBeMethod,
        ReturnTypeMustBeSerializable,
        MethodParameterMustBeSerializable,
        MethodMustNotBeGeneric
    ];
    
    
    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(Action, SyntaxKind.InterfaceDeclaration);
    }

    private void Action(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } interfaceDeclarationSyntax)
            return;
        
        SemanticModel semanticModel = context.SemanticModel;
        INamedTypeSymbol? interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);
        if (interfaceSymbol is null)
            return;

        bool isBlazorService = interfaceSymbol.GetAttributes()
            .Any(data => data.AttributeClass!.ToDisplayString() == SourceGenerationHelper.AttributeFullName);
        
        if(!isBlazorService)
            return;

        foreach (MemberDeclarationSyntax memberSyntax in interfaceDeclarationSyntax.Members)
        {
            if(memberSyntax is MethodDeclarationSyntax methodSyntax)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax)!;
                string returnType = methodSymbol.ReturnType.ToDisplayString();
                string methodName = methodSymbol.ToDisplayString();

                if (methodSymbol.IsGenericMethod)
                {
                    Diagnostic diagnostic = Diagnostic.Create(MethodMustNotBeGeneric, methodSyntax.TypeParameterList!.GetLocation(), returnType, methodName);
                    context.ReportDiagnostic(diagnostic);
                }
                
                if (!methodSymbol.IsAwaitable(semanticModel, interfaceDeclarationSyntax.SpanStart))
                {
                    Diagnostic diagnostic = Diagnostic.Create(MethodNotAwaitable, methodSyntax.ReturnType.GetLocation(), returnType, methodName);
                    context.ReportDiagnostic(diagnostic);
                }

                if (!SourceGenerationHelper.IsJsonSerializable(returnType))
                {
                    Diagnostic diagnostic = Diagnostic.Create(ReturnTypeMustBeSerializable, methodSyntax.GetLocation(), returnType, methodName);
                    context.ReportDiagnostic(diagnostic);
                }
                
                foreach (var parameterSymbol in methodSymbol.Parameters)
                {
                    if (!SourceGenerationHelper.IsJsonSerializable(parameterSymbol.Type.ToDisplayString()))
                    {
                        var parameterSyntax = methodSyntax.ParameterList.Parameters.First(syntax => syntax.Identifier.Text == parameterSymbol.Name);
                        Diagnostic diagnostic = Diagnostic.Create(MethodParameterMustBeSerializable, parameterSyntax.GetLocation(), parameterSymbol.Name, methodName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
            else
            {
                var memberSymbol = semanticModel.GetDeclaredSymbol(memberSyntax)!;
                Diagnostic diagnostic = Diagnostic.Create(MemberMustBeMethod, memberSyntax.GetLocation(), memberSymbol.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
        
    }


    #endregion
}