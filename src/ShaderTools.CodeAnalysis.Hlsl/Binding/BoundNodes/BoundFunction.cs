using System.Collections.Immutable;
using ShaderTools.CodeAnalysis.Hlsl.Symbols;

namespace ShaderTools.CodeAnalysis.Hlsl.Binding.BoundNodes
{
    internal abstract class BoundFunction : BoundNode
    {
        public FunctionSymbol FunctionSymbol { get; }
        public ImmutableArray<BoundVariableDeclaration> Parameters { get; }
        public ImmutableArray<BoundVariableDeclaration> TemplateArguments { get; }
        public ImmutableArray<BoundTemplateType> TemplateTypeArguments { get; }

        protected BoundFunction(BoundNodeKind kind, FunctionSymbol functionSymbol, ImmutableArray<BoundVariableDeclaration> parameters, ImmutableArray<BoundVariableDeclaration> templateArguments, ImmutableArray<BoundTemplateType> templateTypeArguments)
            : base(kind)
        {
            FunctionSymbol = functionSymbol;
            Parameters = parameters;
            TemplateArguments = templateArguments;
            TemplateTypeArguments = templateTypeArguments;
        }
    }
}