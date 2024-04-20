using Microsoft.CodeAnalysis;

namespace Remal.BlazorServiceGenerator.Extensions;

public static class TypedSymbolExtensions
{
	public static bool IsPrimitive(this ITypeSymbol typeSymbol)
	{
		switch (typeSymbol.SpecialType)
		{
			case SpecialType.System_Boolean:
			case SpecialType.System_SByte:
			case SpecialType.System_Int16:
			case SpecialType.System_Int32:
			case SpecialType.System_Int64:
			case SpecialType.System_Byte:
			case SpecialType.System_UInt16:
			case SpecialType.System_UInt32:
			case SpecialType.System_UInt64:
			case SpecialType.System_Single:
			case SpecialType.System_Double:
			case SpecialType.System_Char:
			case SpecialType.System_String:
				return true;
			
			case SpecialType.System_Nullable_T:
				return ((INamedTypeSymbol)typeSymbol).TypeArguments[0].IsPrimitive();
			
			default:
				return false;
		}
	}
	
    public static bool IsAwaitable(this ISymbol? symbol, SemanticModel semanticModel, int position)
    {
        var methodSymbol = symbol as IMethodSymbol;
        ITypeSymbol? typeSymbol = null;

        if (methodSymbol == null)
        {
            typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol == null)
            {
                return false;
            }
        }

        // otherwise: needs valid GetAwaiter
        var potentialGetAwaiters =
	        semanticModel.LookupSymbols(position,
		        container: typeSymbol ?? methodSymbol!.ReturnType.OriginalDefinition,
		        name: WellKnownMemberNames.GetAwaiter,
		        includeReducedExtensionMethods: true);
        var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
        return getAwaiters.Any(VerifyGetAwaiter);
    }

    private static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
    {
        var returnType = getAwaiter.ReturnType;

        // bool IsCompleted { get }
        if (!returnType.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null))
        {
            return false;
        }

        var methods = returnType.GetMembers().OfType<IMethodSymbol>();

        // NOTE: (vladres) The current version of C# Spec, §7.7.7.3 'Runtime evaluation of await expressions', requires that
        // NOTE: the interface method INotifyCompletion.OnCompleted or ICriticalNotifyCompletion.UnsafeOnCompleted is invoked
        // NOTE: (rather than any OnCompleted method conforming to a certain pattern).
        // NOTE: Should this code be updated to match the spec?

        // void OnCompleted(Action) 
        // Actions are delegates, so we'll just check for delegates.
        if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters.Length == 1 && x.Parameters[0].Type.TypeKind == TypeKind.Delegate))
            return false;

        // void GetResult() || T GetResult()
        return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
    }
}