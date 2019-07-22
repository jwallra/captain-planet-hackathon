// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace CaptainPlanet.Models
{
    public class CognitiveServicesResponse
    {
        [JsonProperty("categories")]
        public List<Category> Categories;

        [JsonProperty("tags")]
        public List<Tag> Tags;

        [JsonProperty("objects")]
        public List<ComputerVisionObject> Objects;
    }
}
