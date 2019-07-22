// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace CaptainPlanet.Models
{
    public class Category
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("score")]
        public double Confidence;
    }
}