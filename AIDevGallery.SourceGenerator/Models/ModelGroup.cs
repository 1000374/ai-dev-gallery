﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace AIDevGallery.SourceGenerator.Models;

internal class ModelGroup : IModelGroup
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public int? Order { get; init; }
    public required Dictionary<string, ModelFamily> Models { get; init; }
}