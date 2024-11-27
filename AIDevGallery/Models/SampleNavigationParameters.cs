﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AIDevGallery.Samples.SharedCode;
using System.Threading;
using System.Threading.Tasks;

namespace AIDevGallery.Models
{
    internal class SampleNavigationParameters(
            string modelPath,
            HardwareAccelerator hardwareAccelerator,
            LlmPromptTemplate? promptTemplate,
            TaskCompletionSource sampleLoadedCompletionSource,
            CancellationToken loadingCanceledToken)
        : BaseSampleNavigationParameters(sampleLoadedCompletionSource, loadingCanceledToken)
    {
        public string ModelPath { get; } = modelPath;
        public HardwareAccelerator HardwareAccelerator { get; } = hardwareAccelerator;

        protected override string ChatClientModelPath => ModelPath;
        protected override LlmPromptTemplate? ChatClientPromptTemplate => promptTemplate;
    }
}