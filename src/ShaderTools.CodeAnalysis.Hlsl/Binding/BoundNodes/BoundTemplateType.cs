using ShaderTools.CodeAnalysis.Hlsl.Symbols;
using ShaderTools.CodeAnalysis.Hlsl.Syntax;

namespace ShaderTools.CodeAnalysis.Hlsl.Binding.BoundNodes
{
    internal sealed class BoundTemplateType : BoundType
    {
        private TemplateTypeArgumentSyntax TemplateTypeArgumentSyntax { get; }

        public BoundTemplateType(TemplateTypeArgumentSyntax templateTypeArgumentSymbol)
            : base(BoundNodeKind.TemplateType, TypeFacts.Unknown)
        {
            TemplateTypeArgumentSyntax = templateTypeArgumentSymbol;
        }
    }
}
