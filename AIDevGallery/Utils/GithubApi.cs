﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading.Tasks;

namespace AIDevGallery.Utils;

internal class GithubApi
{
    private static readonly string RawGhUrl = "https://raw.githubusercontent.com";

    /// <summary>
    /// Gets contens from a file in a Hugging Face repo
    /// </summary>
    /// <param name="fileUrl">url of file</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<string> GetContentsOfTextFile(string fileUrl)
    {
        var url = new GitHubUrl(fileUrl);

        var requestUrl = $"{RawGhUrl}/{url.Organization}/{url.Repo}/{url.Ref}/{url.Path}";

        using var client = new HttpClient();
        var response = await client.GetAsync(requestUrl);
        return await response.Content.ReadAsStringAsync();
    }
}