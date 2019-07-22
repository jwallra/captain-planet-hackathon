// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace CaptainPlanet.Models
{
    public class BoundingRectangle
    {
        [JsonProperty("x")]
        public int X;

        [JsonProperty("y")]
        public int Y;

        [JsonProperty("w")]
        public int W;

        [JsonProperty("h")]
        public int H;
    }
}