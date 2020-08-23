﻿// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Silk.NET.SilkTouch
{
    public partial class NativeApiGenerator
    {
        private static void SpanMarshaller(ref IMarshalContext ctx, Action next)
        {
            bool[] b = new bool[ctx.ParameterVariables.Length];
            
            for (var index = 0; index < ctx.ParameterVariables.Length; index++)
            {
                if (!(ctx.LoadTypes[index] is INamedTypeSymbol named))
                    continue;
                
                if (!named.IsGenericType) continue;

                if (!SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, ctx.Compilation.GetTypeByMetadataName("System.Span`1"))) continue;

                b[index] = true;
            }

            var oldParameterIds = ctx.ParameterVariables.ToArray();

            var vars = new (int, string)[ctx.ParameterVariables.Length];
            for (var index = 0; index < ctx.ParameterVariables.Length; index++)
            {
                // in this loop, update all types & expressions

                var shouldPin = b[index];
                if (!shouldPin) continue;
                
                ctx.DeclareExtraRef(oldParameterIds[index], 1); // ref
                
                var loadType = ctx.LoadTypes[index];
                loadType = ctx.Compilation.CreatePointerTypeSymbol((loadType as INamedTypeSymbol)!.TypeArguments[0]);
                ctx.LoadTypes[index] = loadType;

                var (id, name) = ctx.DeclareSpecialVariableNoInlining(loadType, false);
                ctx.SetParameterToVariable(index, id);
                ctx.BeginBlock();
                vars[index] = (id, name);
            }

            next();

            for (var index = 0; index < ctx.ParameterVariables.Length; index++)
            {
                // in this loop, actually emit the `fixed` statements, with the statements of `next()` as body

                var (id, name) = vars[index];
                var shouldPin = b[index];
                if (!shouldPin) continue;

                var loadType = ctx.LoadTypes[index].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var old = ctx.ResolveVariable(oldParameterIds[index]);
                ctx.EndBlock((x, ctx) => FixedStatement
                (
                    VariableDeclaration
                    (
                        IdentifierName(loadType),
                        SingletonSeparatedList
                            (VariableDeclarator(Identifier(name), null, EqualsValueClause(old.Value)))
                    ), x
                ));
            }
        }
    }
}