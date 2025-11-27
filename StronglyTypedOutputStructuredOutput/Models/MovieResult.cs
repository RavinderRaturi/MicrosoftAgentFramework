#pragma warning disable CA1819
// Copyright (c) Microsoft. All rights reserved.

namespace StronglyTypedOutputStructuredOutput.Models;

public class MovieResult
{
    public required string MessageBack { get; set; }
    public required Movie[] Top10Movies { get; set; }
}