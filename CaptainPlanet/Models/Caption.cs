// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace CaptainPlanet.Models
{
    public class Caption
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("confidence")]
        public double Confidence;
    }
}