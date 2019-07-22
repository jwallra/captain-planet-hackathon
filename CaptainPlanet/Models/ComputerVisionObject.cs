// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace CaptainPlanet.Models
{
    public class ComputerVisionObject
    {
        [JsonProperty("rectangle")]
        public BoundingRectangle Rectangle;

        [JsonProperty("object")]
        public string ObjectDescription;

        [JsonProperty("confidence")]
        public double Confidence;

        [JsonProperty("parent")]
        public Parent Parent;
    }
}