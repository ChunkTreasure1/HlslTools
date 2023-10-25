using System.Collections.Immutable;
using ShaderTools.CodeAnalysis.Hlsl.Symbols;

namespace ShaderTools.CodeAnalysis.Hlsl.Binding.BoundNodes
{
    internal sealed class BoundFunctionDeclaration : BoundFunction
    {
        public BoundType ReturnType { get; }

        public BoundFunctionDeclaration(FunctionSymbol functionSymbol, BoundType returnType, ImmutableArray<BoundVariableDeclaration> parameters, ImmutableArray<BoundVariableDeclaration> templateArguments, ImmutableArray<BoundTemplateType> templateTypeArguments)
            : base(BoundNodeKind.FunctionDeclaration, functionSymbol, parameters, templateArguments, templateTypeArguments)
        {
            ReturnType = returnType;
        }
    }
}