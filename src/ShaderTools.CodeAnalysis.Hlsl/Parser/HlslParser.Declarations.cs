using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using ShaderTools.CodeAnalysis.Hlsl.Syntax;
using ShaderTools.CodeAnalysis.Syntax;

namespace ShaderTools.CodeAnalysis.Hlsl.Parser
{
    internal partial class HlslParser
    {
        private BaseListSyntax ParseBaseList()
        {
            var colon = Match(SyntaxKind.ColonToken);
            var baseType = ParseIdentifier();
            return new BaseListSyntax(colon, baseType);
        }

        private SyntaxNode ParseClassMember()
        {
            if (IsPossibleFunctionDeclaration())
                return ParseFunctionDefinitionOrDeclaration(false);
            return ParseDeclarationStatement();
        }

        private StructTypeSyntax ParseStructType(SyntaxKind syntaxKind)
        {
            var @struct = Match(syntaxKind);

            // Name is optional -  but if omitted, this *must* be part of a variable declaration.
            var name = NextTokenIf(SyntaxKind.IdentifierToken);

            BaseListSyntax baseList = null;
            if (Current.Kind == SyntaxKind.ColonToken)
                baseList = ParseBaseList();

            var openBrace = Match(SyntaxKind.OpenBraceToken);

            var members = new List<SyntaxNode>();
            while (Current.Kind != SyntaxKind.CloseBraceToken)
            {
                if (IsPossibleClassMember())
                {
                    members.Add(ParseClassMember());
                }
                else
                {
                    var action = SkipBadTokens(
                        p => !p.IsPossibleClassMember(),
                        p => p.IsTerminator(),
                        SyntaxKind.CloseBraceToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeBrace = Match(SyntaxKind.CloseBraceToken);

            return new StructTypeSyntax(@struct, name, baseList, openBrace, members, closeBrace);
        }

        private InterfaceTypeSyntax ParseInterfaceType()
        {
            var @interface = Match(SyntaxKind.InterfaceKeyword);
            var name = Match(SyntaxKind.IdentifierToken);

            var openBrace = Match(SyntaxKind.OpenBraceToken);

            var methods = new List<FunctionDeclarationSyntax>();
            while (Current.Kind != SyntaxKind.CloseBraceToken)
            {
                if (IsPossibleFunctionDeclaration())
                {
                    methods.Add(ParseFunctionDeclaration());
                }
                else
                {
                    var action = SkipBadTokens(
                        p => !p.IsPossibleFunctionDeclaration(),
                        p => p.IsTerminator(),
                        SyntaxKind.CloseBraceToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeBrace = Match(SyntaxKind.CloseBraceToken);

            return new InterfaceTypeSyntax(@interface, name, openBrace, methods, closeBrace);
        }

        private FunctionDeclarationSyntax ParseFunctionDeclaration()
        {
            return (FunctionDeclarationSyntax)ParseFunctionDefinitionOrDeclaration(true);
        }

        private DeclarationNameSyntax ParseDeclarationName(bool declarationOnly)
        {
            var name = Match(SyntaxKind.IdentifierToken);

            var result = new IdentifierDeclarationNameSyntax(name) as DeclarationNameSyntax;

            if (!declarationOnly)
            {
                while (Current.Kind == SyntaxKind.ColonColonToken)
                {
                    var colonColon = Match(SyntaxKind.ColonColonToken);
                    var right = new IdentifierDeclarationNameSyntax(Match(SyntaxKind.IdentifierToken));

                    result = new QualifiedDeclarationNameSyntax(result, colonColon, right);
                }
            }

            return result;
        }

        private SyntaxNode ParseFunctionDefinitionOrDeclaration(bool declarationOnly, TypeTemplateArgumentListSyntax templateArguments = null)
        {
            var attributes = ParseAttributes();

            var modifiers = new List<SyntaxToken>();
            ParseDeclarationModifiers(modifiers);

            var returnType = ParseReturnType();

            var name = ParseDeclarationName(declarationOnly);

            var openParen = Match(SyntaxKind.OpenParenToken);

            var parameters = new List<SyntaxNodeBase>();
            while (Current.Kind != SyntaxKind.CloseParenToken)
            {
                if (IsPossibleParameter())
                {
                    parameters.Add(ParseParameter());
                    if (Current.Kind != SyntaxKind.CloseParenToken)
                    {
                        var action = SkipBadTokens(
                            p => p.Current.Kind != SyntaxKind.CommaToken,
                            p => p.IsTerminator() || p.Current.Kind == SyntaxKind.OpenParenToken,
                            SyntaxKind.CommaToken);
                        if (action == PostSkipAction.Abort)
                            break;

                        if (Current.Kind == SyntaxKind.CommaToken)
                        {
                            parameters.Add(Match(SyntaxKind.CommaToken));
                        }
                    }
                }
                else
                {
                    var action = SkipBadTokens(
                        p => !p.IsPossibleParameter(),
                        p => p.IsTerminator() || p.Current.Kind == SyntaxKind.OpenParenToken,
                        SyntaxKind.CloseParenToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeParen = Match(SyntaxKind.CloseParenToken);

            SemanticSyntax semantic = null;
            if (Current.Kind == SyntaxKind.ColonToken)
                semantic = ParseSemantic();

            if (!declarationOnly && (name.Kind == SyntaxKind.QualifiedDeclarationName || Current.Kind == SyntaxKind.OpenBraceToken))
            {
                var body = ParseBlock(new List<AttributeDeclarationSyntaxBase>());
                var semicolon = NextTokenIf(SyntaxKind.SemiToken);

                return new FunctionDefinitionSyntax(templateArguments, attributes, modifiers, returnType,
                    name, new ParameterListSyntax(openParen, new SeparatedSyntaxList<ParameterSyntax>(parameters), closeParen),
                    semantic, body, semicolon);
            }
            else
            {
                var semicolon = Match(SyntaxKind.SemiToken);

                Debug.Assert(name.Kind == SyntaxKind.IdentifierDeclarationName);

                return new FunctionDeclarationSyntax(templateArguments, attributes, modifiers, returnType,
                    name, new ParameterListSyntax(openParen, new SeparatedSyntaxList<ParameterSyntax>(parameters), closeParen),
                    semantic, semicolon);
            }
        }

        private bool IsPossibleTemplateArgument()
        {
            var resetPoint = GetResetPoint();
            try
            {
                var st = ScanType();

                if (st != ScanTypeFlags.MustBeType)
                {
                    return false;
                }

                if (Lookahead.Kind == SyntaxKind.LessThanToken)
                {
                    return false;
                }

                return true;
            }
            finally
            {
                Reset(ref resetPoint);
            }
        }

        private bool IsPossibleTemplateArgumentList()
        {
            var resetPoint = GetResetPoint();
            
            try
            {
                while(Current.Kind != SyntaxKind.GreaterThanToken)
                {
                    NextToken();
                }

                if (Lookahead.Kind == SyntaxKind.OpenParenToken)
                {
                    return true;
                }

                return false;    
            }
            finally
            {
                Reset(ref resetPoint);
            }
        }

        private bool IsPossibleTemplateTypeArgument()
        {
            var resetPoint = GetResetPoint();
            try
            {
                switch (Current.Kind)
                {
                    case SyntaxKind.ClassKeyword:
                    case SyntaxKind.StructKeyword:
                    case SyntaxKind.TypenameKeyword:
                        return true;
                }

                if (Lookahead.Kind == SyntaxKind.LessThanToken)
                {
                    return false;
                }

                return false;
            }
            finally
            {
                Reset(ref resetPoint);
            }
        }

        private bool IsPossibleParameter()
        {
            return IsPossibleAttributeSpecifierList() || SyntaxFacts.IsParameterModifier(Current) || IsPossibleParameterDeclaration();
        }

        private bool IsPossibleParameterDeclaration()
        {
            var tk = Current.Kind;

            // Although "<identifier> <literal>" is invalid, it's common enough that we try to parse it anyway, and report on the error.
            if (tk == SyntaxKind.IdentifierToken && (Lookahead.Kind == SyntaxKind.IdentifierToken || Lookahead.Kind.IsLiteral()))
                return true;

            switch (Current.Kind)
            {
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.StructKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.TypedefKeyword:
                    return true;
            }

            var resetPoint = GetResetPoint();
            try
            {
                var modifiers = new List<SyntaxToken>();
                ParseDeclarationModifiers(modifiers);

                var st = ScanType();

                if (st == ScanTypeFlags.NotType)
                    return false;

                if (Lookahead.Kind == SyntaxKind.OpenParenToken)
                    return false;

                return true;
            }
            finally
            {
                Reset(ref resetPoint);
            }
        }

        TemplateArgumentSyntax ParseTemplateArgument()
        {
            var type = ParseType(true);
            var declarator = ParseVariableDeclarator(type);
            return new TemplateArgumentSyntax(type, declarator);
        }

        TemplateTypeArgumentSyntax ParseTemplateTypeArgument()
        {
            var typeDeclarator = Current;
            NextToken();

            var typeName = ParseName();

            return new TemplateTypeArgumentSyntax(typeDeclarator, typeName);
        }

        private ParameterSyntax ParseParameter()
        {
            var attributes = ParseAttributes();

            var modifiers = new List<SyntaxToken>();
            ParseParameterModifiers(modifiers);

            var type = ParseType(true);

            var declarator = ParseVariableDeclarator(type);

            return new ParameterSyntax(attributes, modifiers, type, declarator);
        }

        private void ParseParameterModifiers(List<SyntaxToken> list)
        {
            while (SyntaxFacts.IsParameterModifier(Current))
                list.Add(NextToken());
        }

        private SemanticSyntax ParseSemantic()
        {
            var colon = Match(SyntaxKind.ColonToken);
            var semantic = Match(SyntaxKind.IdentifierToken);

            return new SemanticSyntax(colon, semantic);
        }

        private TypeTemplateArgumentListSyntax ParseTypeTemplateArgumentList()
        {
            var @template = Match(SyntaxKind.TemplateKeyword);
            var openSign = Match(SyntaxKind.LessThanToken);

            var arguments = new List<SyntaxNodeBase>();
            var typeArguments = new List<SyntaxNodeBase>();

            while (Current.Kind != SyntaxKind.GreaterThanToken)
            {
                if (IsPossibleTemplateArgument())
                {
                    arguments.Add(ParseTemplateArgument());
                    if (Current.Kind != SyntaxKind.GreaterThanToken)
                    {
                        var action = SkipBadTokens(
                            p => p.Current.Kind != SyntaxKind.CommaToken,
                            p => p.IsTerminator() || p.Current.Kind == SyntaxKind.LessThanToken,
                            SyntaxKind.CommaToken);
                        if (action == PostSkipAction.Abort)
                            break;

                        if (Current.Kind == SyntaxKind.CommaToken)
                        {
                            arguments.Add(Match(SyntaxKind.CommaToken));
                        }
                    }
                }
                else if (IsPossibleTemplateTypeArgument())
                {
                    typeArguments.Add(ParseTemplateTypeArgument());
                    if (Current.Kind != SyntaxKind.GreaterThanToken)
                    {
                        var action = SkipBadTokens(
                            p => p.Current.Kind != SyntaxKind.CommaToken,
                            p => p.IsTerminator() || p.Current.Kind == SyntaxKind.LessThanToken,
                            SyntaxKind.CommaToken);
                        if (action == PostSkipAction.Abort)
                            break;

                        if (Current.Kind == SyntaxKind.CommaToken)
                        {
                            typeArguments.Add(Match(SyntaxKind.CommaToken));
                        }
                    }
                }
                else
                {
                    var action = SkipBadTokens(
                        p => !p.IsPossibleTemplateArgument() && !p.IsPossibleTemplateTypeArgument(),
                        p => p.IsTerminator() || p.Current.Kind == SyntaxKind.LessThanToken,
                        SyntaxKind.GreaterThanToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeSign = Match(SyntaxKind.GreaterThanToken);

            return new TypeTemplateArgumentListSyntax(@template, openSign, new SeparatedSyntaxList<TemplateArgumentSyntax>(arguments), new SeparatedSyntaxList<TemplateTypeArgumentSyntax>(typeArguments), closeSign);
        }

        private SyntaxNode ParseTemplateFunctionDefinitionOrDeclaration(bool declarationOnly)
        {
            var templateArguments = ParseTypeTemplateArgumentList();
            return ParseFunctionDefinitionOrDeclaration(declarationOnly, templateArguments);
        }

        private ConstantBufferSyntax ParseConstantBuffer()
        {
            var attributes = ParseAttributes();

            var cbuffer = NextToken();
            var name = Match(SyntaxKind.IdentifierToken);

            RegisterLocation register = null;
            if (Current.Kind == SyntaxKind.ColonToken && Lookahead.Kind == SyntaxKind.RegisterKeyword)
                register = ParseRegisterLocation();

            var openBrace = Match(SyntaxKind.OpenBraceToken);

            var fields = new List<VariableDeclarationStatementSyntax>();
            while (Current.Kind != SyntaxKind.CloseBraceToken)
            {
                if (IsPossibleVariableDeclarationStatement())
                {
                    fields.Add(ParseVariableDeclarationStatement());
                }
                else
                {
                    var action = SkipBadTokens(
                        p => !p.IsPossibleVariableDeclarationStatement(),
                        p => p.IsTerminator(),
                        SyntaxKind.CloseBraceToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeBrace = Match(SyntaxKind.CloseBraceToken);
            var semicolon = NextTokenIf(SyntaxKind.SemiToken);

            return new ConstantBufferSyntax(attributes, cbuffer, name, register, openBrace, fields, closeBrace, semicolon);
        }

        private TechniqueSyntax ParseTechnique()
        {
            var technique = NextToken();

            SyntaxToken name = null;
            if (Current.Kind == SyntaxKind.IdentifierToken)
                name = Match(SyntaxKind.IdentifierToken);

            AnnotationsSyntax annotations = null;
            if (Current.Kind == SyntaxKind.LessThanToken)
                annotations = ParseAnnotations();

            var openBrace = Match(SyntaxKind.OpenBraceToken);

            var passes = new List<PassSyntax>();
            while (Current.Kind != SyntaxKind.CloseBraceToken)
            {
                if (Current.Kind == SyntaxKind.PassKeyword)
                {
                    passes.Add(ParsePass());
                }
                else
                {
                    var action = SkipBadTokens(
                        p => p.Current.Kind != SyntaxKind.PassKeyword,
                        p => p.IsTerminator(),
                        SyntaxKind.CloseBraceToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeBrace = Match(SyntaxKind.CloseBraceToken);
            var semicolon = NextTokenIf(SyntaxKind.SemiToken);

            return new TechniqueSyntax(technique, name, annotations, openBrace, passes, closeBrace, semicolon);
        }

        private PassSyntax ParsePass()
        {
            var pass = Match(SyntaxKind.PassKeyword);

            SyntaxToken name = null;
            if (Current.Kind == SyntaxKind.IdentifierToken)
                name = Match(SyntaxKind.IdentifierToken);

            AnnotationsSyntax annotations = null;
            if (Current.Kind == SyntaxKind.LessThanToken)
                annotations = ParseAnnotations();

            var openBrace = Match(SyntaxKind.OpenBraceToken);

            var statements = new List<StatementSyntax>();
            while (Current.Kind != SyntaxKind.CloseBraceToken)
            {
                if (IsPossiblePassStatement())
                {
                    statements.Add(ParsePassStatement());
                }
                else
                {
                    var action = SkipBadTokens(
                        p => !p.IsPossiblePassStatement(),
                        p => p.IsTerminator(),
                        SyntaxKind.CloseBraceToken);
                    if (action == PostSkipAction.Abort)
                        break;
                }
            }

            var closeBrace = Match(SyntaxKind.CloseBraceToken);

            return new PassSyntax(pass, name, annotations, openBrace, statements, closeBrace);
        }

        private ExpressionStatementSyntax ParsePassStatement()
        {
            ExpressionSyntax expression;
            _allowGreaterThanTokenAroundRhsExpression = true;
            try
            {
                expression = ParseExpression();
            }
            finally
            {
                _allowGreaterThanTokenAroundRhsExpression = false;
            }

            var semicolon = Match(SyntaxKind.SemiToken);
            return new ExpressionStatementSyntax(new List<AttributeDeclarationSyntaxBase>(), expression, semicolon);
        }

        private bool IsPossiblePassStatement()
        {
            return Current.Kind == SyntaxKind.IdentifierToken;
        }

        private RegisterLocation ParseRegisterLocation()
        {
            var colon = Match(SyntaxKind.ColonToken);
            var register = Match(SyntaxKind.RegisterKeyword);
            var openParen = Match(SyntaxKind.OpenParenToken);
            var address = Match(SyntaxKind.IdentifierToken);

            LogicalRegisterSpace logicalRegisterSpace = null;
            if (Current.Kind == SyntaxKind.CommaToken)
            {
                var comma = Match(SyntaxKind.CommaToken);
                var spaceToken = Match(SyntaxKind.IdentifierToken);
                logicalRegisterSpace = new LogicalRegisterSpace(comma, spaceToken);
            }

            var closeParen = Match(SyntaxKind.CloseParenToken);

            return new RegisterLocation(colon, register, openParen, address, logicalRegisterSpace, closeParen);
        }

        private PackOffsetLocation ParsePackOffsetLocation()
        {
            var colon = Match(SyntaxKind.ColonToken);
            var packOffset = Match(SyntaxKind.PackoffsetKeyword);
            var openParen = Match(SyntaxKind.OpenParenToken);
            var register = Match(SyntaxKind.IdentifierToken);

            var dot = NextTokenIf(SyntaxKind.DotToken);
            var component = NextTokenIf(SyntaxKind.IdentifierToken);
            var componentPart = (dot != null)
                ? new PackOffsetComponentPart(dot, component)
                : null;

            var closeParen = Match(SyntaxKind.CloseParenToken);

            return new PackOffsetLocation(colon, packOffset, openParen, register, componentPart, closeParen);
        }
    }
}