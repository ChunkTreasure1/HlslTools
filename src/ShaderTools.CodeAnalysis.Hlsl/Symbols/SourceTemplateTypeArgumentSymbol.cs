using ShaderTools.CodeAnalysis.Hlsl.Syntax;
using ShaderTools.CodeAnalysis.Syntax;
using ShaderTools.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ShaderTools.CodeAnalysis.Hlsl.Symbols
{
    public sealed class SourceTemplateTypeArgumentSymbol : ParameterSymbol
    {
        internal SourceTemplateTypeArgumentSymbol(TemplateTypeArgumentSyntax syntax, Symbol parent, TypeSymbol valueType, ParameterDirection direction = ParameterDirection.In)
            : base(syntax.TypeName.ToString(), string.Empty, parent, valueType, direction)
        {
            Syntax = syntax;

            SourceTree = syntax.SyntaxTree;
            Locations = ImmutableArray.Create(Syntax.TypeName.SourceRange);
            DeclaringSyntaxNodes = ImmutableArray.Create((SyntaxNodeBase)syntax);
        }

        public TemplateTypeArgumentSyntax Syntax { get; }

        public override bool HasDefaultValue => false;

        public override string DefaultValueText => Syntax.TypeName?.ToString();

        public override SyntaxTreeBase SourceTree { get; }
        public override ImmutableArray<SourceRange> Locations { get; }
        public override ImmutableArray<SyntaxNodeBase> DeclaringSyntaxNodes { get; }
    }
}
