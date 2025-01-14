﻿//-----------------------------------------------------------------------
// <copyright file="SwaggerJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;

namespace NSwag.Generation
{
    /// <summary>A <see cref="JsonSchemaGenerator"/> which only generate the schema for the root type. 
    /// Referenced types are added to the service's Definitions collection. </summary>
    public class OpenApiSchemaGenerator : JsonSchemaGenerator
    {
        private bool _isRootType = true;

        /// <summary>Initializes a new instance of the <see cref="OpenApiSchemaGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public OpenApiSchemaGenerator(OpenApiDocumentGeneratorSettings settings) : base(settings)
        {
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <param name="typeDescription">The type desription.</param>
        /// <param name="schema">The properties</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns></returns>
        protected override void GenerateObject(JsonSchema schema, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
        {
            if (_isRootType)
            {
                _isRootType = false;
                base.GenerateObject(schema, typeDescription, schemaResolver);
                _isRootType = true;
            }
            else
            {
                if (!schemaResolver.HasSchema(typeDescription.ContextualType.OriginalType, false))
                {
                    _isRootType = true;
                    Console.WriteLine($"Generating for {typeDescription.ContextualType.TypeName}...");
                    //try
                    //{
                        Generate(typeDescription.ContextualType.OriginalType, schemaResolver);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine($"Exception: {ex}");
                    //}
                    _isRootType = false;
                }

                schema.Reference = schemaResolver.GetSchema(typeDescription.ContextualType.OriginalType, false);
            }
        }

        /// <summary>Generetes a schema directly or referenced for the requested schema type; also adds nullability if required.</summary>
        /// <typeparam name="TSchemaType">The resulted schema type which may reference the actual schema.</typeparam>
        /// <param name="contextualType">The type of the schema to generate.</param>
        /// <param name="isNullable">Specifies whether the property, parameter or requested schema type is nullable.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <param name="transformation">An action to transform the resulting schema (e.g. property or parameter) before the type of reference is determined (with $ref or allOf/oneOf).</param>
        /// <returns>The requested schema object.</returns>
        public override TSchemaType GenerateWithReferenceAndNullability<TSchemaType>(
            ContextualType contextualType, bool isNullable,
            JsonSchemaResolver schemaResolver, Action<TSchemaType, JsonSchema> transformation = null)
        {
            if (contextualType.TypeName == "Task`1")
            {
                contextualType = contextualType.OriginalGenericArguments[0];
            }
            else if (contextualType.TypeName == "JsonResult`1")
            {
                contextualType = contextualType.OriginalGenericArguments[0];
            }
            else if (contextualType.TypeName == "ActionResult`1")
            {
                contextualType = contextualType.OriginalGenericArguments[0];
            }

            if (IsFileResponse(contextualType))
            {
                if (Settings.SchemaType == SchemaType.Swagger2)
                {
                    return new TSchemaType { Type = JsonObjectType.File };
                }
                else
                {
                    return new TSchemaType { Type = JsonObjectType.String, Format = JsonFormatStrings.Binary };
                }
            }

            Console.WriteLine($"Generating contextual type {contextualType.TypeName}...");

            return base.GenerateWithReferenceAndNullability(contextualType, isNullable, schemaResolver, transformation);
        }

        private bool IsFileResponse(Type returnType)
        {
            return returnType.IsAssignableToTypeName("FileResult", TypeNameStyle.Name) ||
                   returnType.Name == "IActionResult" ||
                   returnType.Name == "IHttpActionResult" ||
                   returnType.Name == "HttpResponseMessage" ||
                   returnType.IsAssignableToTypeName("ActionResult", TypeNameStyle.Name) ||
                   returnType.InheritsFromTypeName("HttpResponseMessage", TypeNameStyle.Name);
        }
    }
}
